using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrainWaves.ViewModel
{
    public partial class SettingsViewModel : ObservableObject
    {
        private const string RepositoryAddress = "https://github.com/siakun/BrainWavesWPF";

        public string RepositoryUrl => RepositoryAddress;

        public string IssuesUrl => RepositoryAddress + "/issues";

        public string Version { get; } = FormatVersion(Assembly.GetExecutingAssembly().GetName().Version);

        // INTENT: 배포물에 함께 들어가는 오픈소스 패키지를 적는다. 버전은 프로젝트 파일이 원본이므로
        // 여기 옮겨 적지 않고, 패키지를 추가하거나 뺄 때 이 목록도 함께 고친다.
        public IReadOnlyList<OpenSourceLibrary> Libraries { get; } = new OpenSourceLibrary[]
        {
            new("CommunityToolkit.Mvvm", "MIT", "https://github.com/CommunityToolkit/dotnet"),
            new("MaterialDesignInXamlToolkit", "MIT", "https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit"),
            new("NAudio", "MIT", "https://github.com/naudio/NAudio"),
        };

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

        private static string FormatVersion(Version? version) =>
            version is null ? "unknown" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    public sealed record OpenSourceLibrary(string Name, string License, string Url);
}
