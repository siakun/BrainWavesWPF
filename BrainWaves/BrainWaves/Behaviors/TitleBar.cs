using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace BrainWaves.Behaviors
{
    /// <summary>
    /// Windows가 그리는 창 제목 표시줄을 앱 색에 맞춘다.
    /// 창에 TitleBar.Background와 TitleBar.Foreground를 붙이면 창 핸들이 만들어질 때 적용한다.
    /// </summary>
    // INTENT: 제목 표시줄을 WindowChrome으로 직접 그리지 않고 DWM에 색만 넘긴다. 직접 그리면 끌어서 옮기기,
    // 화면 가장자리 맞춤, 최대화 버튼의 스냅 레이아웃을 다시 만들어야 하고, WindowAutoFit이 재는 창 높이도
    // 바뀐다. 색 지정을 지원하지 않는 Windows 10에서는 어두운 모드 표시줄만 적용되고, 그보다 오래된
    // 빌드에서는 호출이 실패해도 기본 표시줄로 남는다.
    public static class TitleBar
    {
        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.RegisterAttached(
                "Background",
                typeof(Color?),
                typeof(TitleBar),
                new PropertyMetadata(null, OnColorChanged));

        public static Color? GetBackground(DependencyObject target) =>
            (Color?)target.GetValue(BackgroundProperty);

        public static void SetBackground(DependencyObject target, Color? value) =>
            target.SetValue(BackgroundProperty, value);

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.RegisterAttached(
                "Foreground",
                typeof(Color?),
                typeof(TitleBar),
                new PropertyMetadata(null, OnColorChanged));

        public static Color? GetForeground(DependencyObject target) =>
            (Color?)target.GetValue(ForegroundProperty);

        public static void SetForeground(DependencyObject target, Color? value) =>
            target.SetValue(ForegroundProperty, value);

        private static void OnColorChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is not Window window) return;

            // 핸들이 아직 없으면 만들어지는 순간에 적용한다. 이미 있으면 바로 적용한다.
            window.SourceInitialized -= OnSourceInitialized;
            window.SourceInitialized += OnSourceInitialized;
            Apply(window);
        }

        private static void OnSourceInitialized(object? sender, EventArgs e)
        {
            if (sender is Window window) Apply(window);
        }

        private static void Apply(Window window)
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero) return;

            int useDarkMode = 1;
            DwmSetWindowAttribute(handle, DwmUseImmersiveDarkMode, ref useDarkMode, sizeof(int));

            if (GetBackground(window) is Color background)
            {
                int value = ToColorRef(background);
                DwmSetWindowAttribute(handle, DwmCaptionColor, ref value, sizeof(int));
            }

            if (GetForeground(window) is Color foreground)
            {
                int value = ToColorRef(foreground);
                DwmSetWindowAttribute(handle, DwmTextColor, ref value, sizeof(int));
            }
        }

        // COLORREF는 0x00BBGGRR 순서다.
        private static int ToColorRef(Color color) => color.R | (color.G << 8) | (color.B << 16);

        private const int DwmUseImmersiveDarkMode = 20;
        private const int DwmCaptionColor = 35;
        private const int DwmTextColor = 36;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }
}
