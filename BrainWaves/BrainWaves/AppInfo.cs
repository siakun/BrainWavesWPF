using System;
using System.IO;
using System.Reflection;

namespace BrainWaves
{
    /// <summary>
    /// 여러 화면과 서비스가 함께 쓰는 앱의 고정 정보.
    /// </summary>
    internal static class AppInfo
    {
        /// <summary>
        /// 소스 코드 링크와 업데이트 조회가 같은 저장소를 가리켜야 하므로 주소를 한 곳에 둔다.
        /// </summary>
        public const string RepositoryUrl = "https://github.com/siakun/BrainWavesWPF";

        public const string IssuesUrl = RepositoryUrl + "/issues";

        /// <summary>
        /// 이 빌드의 버전. 설치본은 Velopack이 알려 주는 버전을 우선하고, 이 값은 개발 빌드에서 쓴다.
        /// </summary>
        public static string BuildVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version is null ? "unknown" : $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }

        /// <summary>
        /// 설정 파일을 두는 폴더.
        /// Velopack은 %LocalAppData%\BrainWaves를 설치 폴더로 관리하며 업데이트와 제거 때 그 안을 교체하거나 지운다.
        /// 사용자 설정은 Velopack이 손대지 않는 %AppData%\BrainWaves에 둔다.
        /// </summary>
        public static string DataDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BrainWaves");
    }
}
