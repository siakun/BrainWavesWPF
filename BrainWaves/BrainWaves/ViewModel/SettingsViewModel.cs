using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainWaves.ViewModel
{
    public partial class SettingsViewModel : ObservableObject
    {
        public UpdatesViewModel Updates { get; } = UpdatesViewModel.Instance;

        public string RepositoryUrl => AppInfo.RepositoryUrl;

        public string IssuesUrl => AppInfo.IssuesUrl;

        // INTENT: 배포물에 함께 들어가는 오픈소스 패키지를 적는다. 버전은 프로젝트 파일이 원본이므로
        // 여기 옮겨 적지 않고, 패키지를 추가하거나 뺄 때 이 목록도 함께 고친다.
        public IReadOnlyList<OpenSourceLibrary> Libraries { get; } = new OpenSourceLibrary[]
        {
            new("CommunityToolkit.Mvvm", "MIT", "https://github.com/CommunityToolkit/dotnet"),
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
                MessageBox.Show($"Couldn't open {url}. {ex.Message}", "BrainWaves", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public sealed record OpenSourceLibrary(string Name, string License, string Url);
}
