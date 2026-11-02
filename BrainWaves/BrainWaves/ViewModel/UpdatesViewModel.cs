using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BrainWaves.Localization;
using BrainWaves.Model;
using BrainWaves.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Siakun.AutoUpdate;

namespace BrainWaves.ViewModel
{
    /// <summary>
    /// 업데이트 상태와 조작. 메인 창의 알림 막대와 설정 화면이 같은 인스턴스를 본다.
    /// </summary>
    // INTENT: 앱 수명 동안 하나만 둔다. 페이지는 탐색할 때마다 새로 만들어지므로 설치 진행률이나
    // 받아 둔 업데이트 같은 상태를 페이지의 ViewModel에 두면 다른 화면으로 옮기는 순간 사라진다.
    // 메시지로 알리지 않고 상태를 들고 있는 것도 같은 이유다. 나중에 열린 화면도 현재 상태를 바로 읽는다.
    public sealed partial class UpdatesViewModel : ObservableObject
    {
        private static UpdatesViewModel? _instance;

        private readonly UpdateService _updates = AppUpdates.Service;
        private readonly SettingsStore _settings = SettingsStore.Instance;
        private readonly Dispatcher _dispatcher;
        private CancellationTokenSource? _installCancellation;
        private Task? _versionsLoad;
        private int _versionsGeneration;
        private bool _versionsFailed;

        public static UpdatesViewModel Instance => _instance ??= new UpdatesViewModel();

        private UpdatesViewModel()
        {
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            // 라이브러리는 다운로드를 마친 스레드에서, 설정 저장소는 저장한 스레드에서 알린다.
            _updates.UpdateReady += (_, e) => _dispatcher.BeginInvoke(() => ReadyVersion = e.Version.ToNormalizedString());
            _settings.Changed += (_, _) => _dispatcher.BeginInvoke(() =>
            {
                OnPropertyChanged(nameof(AutoUpdateEnabled));
                OnPropertyChanged(nameof(PrereleaseEnabled));
            });

            IsManagedInstall = _updates.IsManagedInstall;
            CurrentVersion = _updates.CurrentVersion?.ToNormalizedString() ?? AppInfo.BuildVersion;
            if (!IsManagedInstall) StatusText = new LocalizedText("Updates.DevBuild");
        }

        /// <summary>
        /// Setup이나 포터블 압축 파일로 설치한 실행 파일인지 여부. 개발 빌드는 목록만 볼 수 있고 설치는 할 수 없다.
        /// </summary>
        public bool IsManagedInstall { get; }

        public string CurrentVersion { get; }

        public bool AutoUpdateEnabled
        {
            get => _settings.Current.AutoUpdateEnabled;
            set => ChangeSetting(settings => settings with { AutoUpdateEnabled = value });
        }

        public bool PrereleaseEnabled
        {
            get => _settings.Current.PrereleaseEnabled;
            set
            {
                // 라이브러리가 목록에서 베타를 거를지 이 설정으로 정하므로 목록을 다시 받는다.
                if (ChangeSetting(settings => settings with { PrereleaseEnabled = value }))
                    _versionsLoad = LoadVersionsAsync();
            }
        }

        /// <summary>
        /// 내려받아 두고 종료할 때 적용할 버전. 없으면 null.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsUpdateReady))]
        [NotifyCanExecuteChangedFor(nameof(RestartNowCommand))]
        private string? readyVersion;

        public bool IsUpdateReady => ReadyVersion is not null;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanPickVersion))]
        [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
        [NotifyCanExecuteChangedFor(nameof(InstallSelectedVersionCommand))]
        [NotifyCanExecuteChangedFor(nameof(CancelInstallCommand))]
        [NotifyCanExecuteChangedFor(nameof(RestartNowCommand))]
        private bool isInstalling;

        [ObservableProperty]
        private string? installingVersion;

        [ObservableProperty]
        private int installProgress;

        [ObservableProperty]
        private string? downloadSize;

        /// <summary>
        /// 마지막으로 한 확인이나 설치의 결과를 사용자에게 설명하는 문장. 알릴 것이 없으면 null.
        /// 언어를 바꾸면 화면이 다시 만들 수 있게 완성된 문자열이 아니라 키와 인자로 둔다.
        /// </summary>
        [ObservableProperty]
        private LocalizedText? statusText;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanPickVersion))]
        private IReadOnlyList<VersionOption> versions = Array.Empty<VersionOption>();

        /// <summary>
        /// 버전 목록을 고를 수 있는지. 받는 중에 대상을 바꾸면 화면과 실제로 받는 버전이 어긋난다.
        /// </summary>
        public bool CanPickVersion => !IsInstalling && Versions.Count > 0;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(InstallSelectedVersionCommand))]
        private VersionOption? selectedVersion;

        /// <summary>
        /// 버전 목록이 비어 있거나 불러오는 중일 때 그 사정을 알리는 문장.
        /// </summary>
        [ObservableProperty]
        private LocalizedText? versionsHint;

        /// <summary>
        /// 앱 화면을 띄운 뒤 한 번 호출한다. 새 버전을 받으면 ReadyVersion이 채워진다.
        /// </summary>
        public void StartBackgroundCheck() => _ = _updates.CheckAndDownloadAsync();

        /// <summary>
        /// 설정 화면을 열 때 호출한다. 한 번 받은 목록은 다시 받지 않고, 실패했으면 다시 시도한다.
        /// </summary>
        public Task EnsureVersionsLoadedAsync()
        {
            if (_versionsLoad is null || _versionsFailed) _versionsLoad = LoadVersionsAsync();
            return _versionsLoad;
        }

        private bool CanCheckForUpdates() => !IsInstalling;

        [RelayCommand(CanExecute = nameof(CanCheckForUpdates))]
        private async Task CheckForUpdatesAsync()
        {
            StatusText = new LocalizedText("Updates.Checking");
            await _updates.CheckAndDownloadAsync();
            await (_versionsLoad = LoadVersionsAsync());
            StatusText = DescribeCheckResult();
        }

        private bool CanInstallSelectedVersion() =>
            IsManagedInstall && !IsInstalling && SelectedVersion is { IsCurrent: false };

        [RelayCommand(CanExecute = nameof(CanInstallSelectedVersion))]
        private async Task InstallSelectedVersionAsync()
        {
            if (SelectedVersion is not { } target) return;

            using var cancellation = new CancellationTokenSource();
            _installCancellation = cancellation;
            // 라이브러리는 수동 설치를 시작하면 앞서 받아 둔 자동 업데이트를 적용 대상에서 내린다.
            // 이 설치가 실패하거나 취소돼도 그 업데이트를 대신 적용하지 않으므로 알림도 함께 내린다.
            ReadyVersion = null;
            InstallingVersion = target.Version;
            InstallProgress = 0;
            DownloadSize = null;
            IsInstalling = true;
            StatusText = new LocalizedText("Updates.DownloadingStatus", target.Version);

            // UI 스레드에서 만든 Progress는 다운로드 스레드의 보고를 UI 스레드로 옮겨 준다.
            var progress = new Progress<int>(value => InstallProgress = value);
            var size = new Progress<long>(bytes => DownloadSize = $"{bytes / 1048576.0:F1} MB");

            try
            {
                // 성공하면 라이브러리가 재시작을 요청하고, App이 정상 종료하면서 받은 버전을 적용한다.
                await _updates.InstallVersionAsync(target.Release,
                    progress: ((IProgress<int>)progress).Report,
                    onDownloadSizeResolved: ((IProgress<long>)size).Report,
                    cancelToken: cancellation.Token);
                StatusText = new LocalizedText("Updates.Restarting", target.Version);
            }
            catch (OperationCanceledException)
            {
                StatusText = new LocalizedText("Updates.Canceled");
            }
            catch (Exception ex)
            {
                StatusText = new LocalizedText("Updates.InstallFailed", target.Version, ex.Message);
            }
            finally
            {
                _installCancellation = null;
                IsInstalling = false;
            }
        }

        private bool CanCancelInstall() => IsInstalling;

        [RelayCommand(CanExecute = nameof(CanCancelInstall))]
        private void CancelInstall() => _installCancellation?.Cancel();

        private bool CanRestartNow() => IsUpdateReady && !IsInstalling;

        [RelayCommand(CanExecute = nameof(CanRestartNow))]
        private void RestartNow() => _updates.ApplyAndRestartNow();

        private async Task LoadVersionsAsync()
        {
            // 베타 설정을 연달아 바꾸면 요청이 겹친다. 늦게 끝난 옛 요청이 새 목록을 덮지 않게 한다.
            var generation = ++_versionsGeneration;
            VersionsHint = new LocalizedText("Updates.LoadingVersions");

            try
            {
                var releases = await _updates.GetAvailableVersionsAsync();
                if (generation != _versionsGeneration) return;

                var current = _updates.CurrentVersion;
                Versions = releases.Select(release => new VersionOption(release, release.Version == current)).ToList();
                SelectedVersion = Versions.FirstOrDefault(option => option.IsCurrent) ?? Versions.FirstOrDefault();
                VersionsHint = Versions.Count == 0 ? new LocalizedText("Updates.NoVersions") : null;
                _versionsFailed = false;
            }
            catch (Exception ex)
            {
                if (generation != _versionsGeneration) return;

                System.Diagnostics.Debug.WriteLine($"[UpdatesViewModel] Version list failed: {ex.Message}");
                Versions = Array.Empty<VersionOption>();
                SelectedVersion = null;
                VersionsHint = new LocalizedText("Updates.VersionsFailed");
                _versionsFailed = true;
            }
        }

        private LocalizedText DescribeCheckResult()
        {
            if (ReadyVersion is not null)
                return new LocalizedText("Updates.ReadyOnClose", ReadyVersion);
            if (_versionsFailed)
                return new LocalizedText("Updates.Unreachable");

            // 목록이 비었다는 사정은 목록 아래 안내(VersionsHint)가 알리므로 여기서는 결과만 말한다.
            var latest = Versions.FirstOrDefault()?.Release;
            if (latest is null)
                return new LocalizedText("Updates.NoneAvailable");

            var latestVersion = latest.Version.ToNormalizedString();
            var current = _updates.CurrentVersion;
            if (current is null)
                return new LocalizedText("Updates.LatestIsDev", latestVersion);
            if (latest.Version <= current)
                return new LocalizedText("Updates.UpToDate");
            if (!AutoUpdateEnabled)
                return new LocalizedText("Updates.AvailablePick", latestVersion);
            return new LocalizedText("Updates.AvailableNotDownloaded", latestVersion);
        }

        private bool ChangeSetting(Func<AppSettings, AppSettings> change)
        {
            try
            {
                _settings.Update(change);
                return true;
            }
            catch (Exception ex)
            {
                StatusText = new LocalizedText("Updates.SaveFailed", ex.Message);
                // 저장하지 못한 값으로 스위치가 남지 않게 원래 값을 다시 읽힌다.
                OnPropertyChanged(nameof(AutoUpdateEnabled));
                OnPropertyChanged(nameof(PrereleaseEnabled));
                return false;
            }
        }
    }

    /// <summary>
    /// 버전 선택 목록의 항목.
    /// </summary>
    public sealed record VersionOption(ReleaseVersion Release, bool IsCurrent)
    {
        public string Version => Release.Version.ToNormalizedString();

        public LocalizedText? Tag =>
            IsCurrent ? new LocalizedText("Version.Installed")
            : Release.IsPrerelease ? new LocalizedText("Version.Beta")
            : null;
    }
}
