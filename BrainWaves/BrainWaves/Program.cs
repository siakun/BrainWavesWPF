using System;
using Velopack;

namespace BrainWaves
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            // 설치, 제거, 업데이트 직후 Velopack이 앱을 훅 인자로 실행하면 Run()이 그 처리를 마치고 프로세스를 끝낸다.
            // WPF가 창을 띄우기 전에 가려내야 하므로 가장 먼저 호출한다.
            // 받아 둔 업데이트는 App.OnExit에서 적용을 예약한다. 시작할 때 자동 적용을 켜 두면
            // 사용자가 앱을 여는 순간 설치가 시작되어 앱이 한 번 더 재시작된다.
            VelopackApp.Build()
                .SetAutoApplyOnStartup(false)
                .Run();

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
