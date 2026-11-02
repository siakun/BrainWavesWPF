using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using BrainWaves.Localization;
using BrainWaves.Model;
using BrainWaves.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainWaves.ViewModel
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsStore _settings = SettingsStore.Instance;

        public UpdatesViewModel Updates { get; } = UpdatesViewModel.Instance;

        public IReadOnlyList<LanguageOption> Languages => Localizer.Languages;

        /// <summary>
        /// 고른 화면 언어. 값의 원본은 설정 저장소이고, Localizer가 저장소의 변경을 받아 화면 언어를 바꾼다.
        /// </summary>
        public LanguageOption SelectedLanguage
        {
            get => Languages.FirstOrDefault(option => option.Code == _settings.Current.Language) ?? Languages[0];
            set
            {
                if (value is not null) SaveSetting(settings => settings with { Language = value.Code });
            }
        }

        /// <summary>
        /// 창을 닫으면 트레이로 숨길지 여부. 값의 원본은 설정 저장소이고 이 화면은 읽고 쓰기만 한다.
        /// </summary>
        public bool CloseToTray
        {
            get => _settings.Current.CloseToTray;
            set => SaveSetting(settings => settings with { CloseToTray = value });
        }

        private void SaveSetting(Func<AppSettings, AppSettings> change, [CallerMemberName] string? property = null)
        {
            try
            {
                _settings.Update(change);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // 저장하지 못했으면 화면의 값을 저장된 값으로 되돌린다.
                Debug.WriteLine($"[SettingsViewModel] Failed to save {property}: {ex.Message}");
                OnPropertyChanged(property);
            }
        }

        public string RepositoryUrl => AppInfo.RepositoryUrl;

        public string IssuesUrl => AppInfo.IssuesUrl;

        // INTENT: 배포물에 함께 들어가는 오픈소스 패키지를 적는다. 버전은 프로젝트 파일이 원본이므로
        // 여기 옮겨 적지 않고, 패키지를 추가하거나 뺄 때 이 목록도 함께 고친다.
        public IReadOnlyList<OpenSourceLibrary> Libraries { get; } = new OpenSourceLibrary[]
        {
            new("CommunityToolkit.Mvvm", "MIT", "https://github.com/CommunityToolkit/dotnet"),
            new("H.NotifyIcon", "MIT", "https://github.com/HavenDV/H.NotifyIcon"),
            new("MaterialDesignInXamlToolkit", "MIT", "https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit"),
            new("NAudio", "MIT", "https://github.com/naudio/NAudio"),
            new("Velopack", "MIT", "https://github.com/velopack/velopack"),
            new("Siakun.AutoUpdate", "Apache-2.0", "https://github.com/siakun/Siakun.AutoUpdate"),
        };

        public SettingsViewModel()
        {
            // 디자이너가 화면을 그릴 때마다 GitHub에 요청하지 않게 한다.
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) return;

            _ = Updates.EnsureVersionsLoadedAsync();
        }

        [RelayCommand]
        private void OpenLink(string? url)
        {
            if (string.IsNullOrEmpty(url)) return;

            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(Localizer.Instance.Format("Settings.OpenLinkFailed", url, ex.Message),
                    "BrainWaves", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public sealed record OpenSourceLibrary(string Name, string License, string Url);
}
