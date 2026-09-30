using System.Diagnostics;
using Siakun.AutoUpdate;

namespace BrainWaves.Services
{
    /// <summary>
    /// 앱 전체가 공유하는 Siakun.AutoUpdate의 UpdateService.
    /// 확인, 다운로드, 버전 전환, 종료 시 적용은 라이브러리가 맡고, 여기서는 이 앱의 저장소와 설정 저장만 연결한다.
    /// </summary>
    internal static class AppUpdates
    {
        public static UpdateService Service { get; } = Create();

        private static UpdateService Create()
        {
            var catalog = new GitHubReleaseCatalog(AppInfo.RepositoryUrl, log: Log);
            return new UpdateService(catalog, new StoredUpdateSettings(SettingsStore.Instance), Log);
        }

        private static void Log(string message) => Debug.WriteLine(message);

        /// <summary>
        /// 라이브러리는 구버전을 설치할 때 백그라운드 스레드에서 AutoUpdateEnabled를 끄고,
        /// setter가 저장까지 마치고 반환하기를 요구한다. SettingsStore.Update가 저장을 마친 뒤 반환하므로 그대로 넘긴다.
        /// </summary>
        private sealed class StoredUpdateSettings(SettingsStore store) : IUpdateSettings
        {
            public bool AutoUpdateEnabled
            {
                get => store.Current.AutoUpdateEnabled;
                set => store.Update(settings => settings with { AutoUpdateEnabled = value });
            }

            public bool PrereleaseEnabled => store.Current.PrereleaseEnabled;
        }
    }
}
