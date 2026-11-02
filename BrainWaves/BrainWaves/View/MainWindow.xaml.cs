using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Interop;
using BrainWaves.Services;
using CommunityToolkit.Mvvm.Input;

namespace BrainWaves.View
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // INTENT: 화면은 하단 탭으로만 고른다. Frame은 방문 기록을 남겨 Alt+←, Backspace, 마우스의 뒤로 가기 버튼으로
            // 이전 화면을 다시 띄우는데, 그러면 고른 탭과 보이는 화면이 어긋난다. 기록을 남기지 않아 돌아갈 화면이 없게 한다.
            PageFrame.Navigated += (_, _) =>
            {
                while (PageFrame.CanGoBack) PageFrame.RemoveBackEntry();
            };

            TrayIcon.Icon = LoadTrayIcon();
            TrayIcon.LeftClickCommand = new RelayCommand(ShowFromTray);
        }

        /// <summary>
        /// 창을 닫으면 트레이로 숨기는 설정이 켜져 있으면 닫지 않고 숨긴다. 앱과 소리는 그대로 이어진다.
        /// </summary>
        // INTENT: 앱 전체를 끝낼 때(Application.Shutdown)는 WPF가 여기서 닫기를 취소해도 무시하고 창을 닫는다.
        // 그래서 트레이 메뉴의 종료, 업데이트를 적용하려는 재시작, Windows 로그오프는 따로 구분하지 않아도 그대로 앱을 끝낸다.
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (e.Cancel || !SettingsStore.Instance.Current.CloseToTray) return;

            e.Cancel = true;
            Hide();
        }

        // 트레이 아이콘은 프로세스가 끝나도 Windows가 바로 지우지 않아, 포인터를 올리기 전까지 빈 아이콘이 남는다. 창을 닫을 때 직접 지운다.
        // 지운 아이콘은 표시 상태를 바꾸면 예외를 던지므로, 설정이 나중에 바뀌어도 닿지 않게 바인딩을 먼저 끊는다.
        protected override void OnClosed(EventArgs e)
        {
            BindingOperations.ClearAllBindings(TrayIcon);
            TrayIcon.Dispose();
            base.OnClosed(e);
        }

        private void ShowFromTray()
        {
            Show();
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            Activate();
        }

        /// <summary>
        /// 트레이 아이콘의 오른쪽 버튼 메뉴를 라이브러리 대신 연다.
        /// </summary>
        // INTENT: 라이브러리는 메뉴를 연 다음 메뉴 창을 전경으로 올린다. 이 순서에서는 메뉴가 열리자마자 닫히거나,
        // 항목을 눌러도 실행되지 않고 메뉴만 닫혔다. 앱 창을 먼저 전경으로 올린 뒤 메뉴를 연다. 창이 숨어 있어도 전경이 될 수 있다.
        // 위치는 WPF가 포인터 자리에 맞추게 한다. 라이브러리는 실제 픽셀 좌표를 그대로 넘겨 배율이 높은 화면에서 메뉴가 포인터에서 벗어난다.
        private void OnTrayMenuOpening(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            SetForegroundWindow(new WindowInteropHelper(this).EnsureHandle());

            var menu = TrayIcon.ContextMenu;
            menu.Placement = PlacementMode.MousePoint;
            menu.IsOpen = true;
        }

        private void OnTrayOpenClick(object sender, RoutedEventArgs e) => ShowFromTray();

        private void OnTrayQuitClick(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        /// <summary>
        /// 알림 영역의 작은 아이콘 크기(실제 픽셀)에 맞는 이미지를 앱 아이콘 파일에서 고른다.
        /// </summary>
        // INTENT: 아이콘을 IconSource로 주면 라이브러리가 16px 이미지 하나로 만들어, 125%나 150% 배율에서는 알림 영역이
        // 그 이미지를 늘려 흐리게 그린다. 아이콘 파일에 여러 크기가 들어 있으므로 지금 배율에 맞는 이미지를 직접 고른다.
        private static System.Drawing.Icon LoadTrayIcon()
        {
            var size = GetSystemMetricsForDpi(SmallIconWidthIndex, GetDpiForSystem());
            using var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/BrainWaves.ico")).Stream;
            return new System.Drawing.Icon(stream, size, size);
        }

        // SM_CXSMICON: 작은 아이콘의 폭
        private const int SmallIconWidthIndex = 49;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForSystem();

        [DllImport("user32.dll")]
        private static extern int GetSystemMetricsForDpi(int index, uint dpi);
    }
}
