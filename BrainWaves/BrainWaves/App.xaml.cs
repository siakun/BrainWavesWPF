using System.Windows;
using BrainWaves.Services;
using BrainWaves.View;
using BrainWaves.ViewModel;

namespace BrainWaves
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 라이브러리는 설치할 버전을 받은 뒤 이 이벤트로 종료를 요청한다. 어느 스레드에서 오든
            // 정상 종료 경로(OnExit)를 타도록 UI 스레드에서 Shutdown을 부른다.
            AppUpdates.Service.RestartRequested += (_, _) => Dispatcher.BeginInvoke(() => Shutdown());

            var window = new MainWindow();

            // 업데이트 확인은 창을 그린 뒤에 시작해 첫 화면이 네트워크를 기다리지 않게 한다.
            window.ContentRendered += (_, _) => UpdatesViewModel.Instance.StartBackgroundCheck();
            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 소리를 먼저 멈춰 오디오 장치를 놓은 뒤, 받아 둔 업데이트를 이 프로세스가 끝난 다음 적용하도록 예약한다.
            AudioService.Instance.Stop();
            AppUpdates.Service.ApplyOnExit();

            base.OnExit(e);
        }
    }
}
