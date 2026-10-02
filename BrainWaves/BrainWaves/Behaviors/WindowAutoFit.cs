using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;

namespace BrainWaves.Behaviors
{
    /// <summary>
    /// 창의 최소 크기를 콘텐츠가 실제로 요구하는 크기에서 도출한다.
    /// 창 크기를 사람이 고른 값으로 두면 폰트, DPI, 요소 추가로 요구 높이가 바뀔 때
    /// 조용히 어긋나 스크롤바가 생기므로, 페이지를 띄울 때마다 다시 측정해 맞춘다.
    /// </summary>
    public static class WindowAutoFit
    {
        /// <summary>
        /// 창에 붙인다. 창 안의 Frame이 페이지를 띄울 때마다 창 크기를 다시 맞춘다.
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(WindowAutoFit),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject target) =>
            (bool)target.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject target, bool value) =>
            target.SetValue(IsEnabledProperty, value);

        /// <summary>
        /// 페이지에 붙인다. 목록처럼 항목 수만큼 길어지는 페이지는 False로 선언해
        /// 창이 그 길이를 따라가지 않게 한다. 선언하지 않은 페이지는 콘텐츠가 모두 보이도록 맞춘다.
        /// </summary>
        public static readonly DependencyProperty FitsContentProperty =
            DependencyProperty.RegisterAttached(
                "FitsContent",
                typeof(bool),
                typeof(WindowAutoFit),
                new PropertyMetadata(true));

        public static bool GetFitsContent(DependencyObject target) =>
            (bool)target.GetValue(FitsContentProperty);

        public static void SetFitsContent(DependencyObject target, bool value) =>
            target.SetValue(FitsContentProperty, value);

        private static void OnIsEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is not Window window) return;

            if ((bool)e.NewValue)
            {
                window.Loaded += OnWindowLoaded;
                window.DpiChanged += OnDpiChanged;
                window.StateChanged += OnStateChanged;
            }
            else
            {
                window.Loaded -= OnWindowLoaded;
                window.DpiChanged -= OnDpiChanged;
                window.StateChanged -= OnStateChanged;
            }
        }

        private static void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            var window = (Window)sender;
            var frame = FindDescendant<Frame>(window);
            if (frame is null) return;

            // 페이지는 창이 열린 뒤에 비동기로 들어오므로, 들어올 때마다 다시 맞춘다.
            frame.LoadCompleted += OnPageLoaded;

            // 이미 페이지가 들어와 있으면 첫 화면도 맞춘다.
            if (frame.Content is not null) Fit(window, frame);
        }

        private static void OnPageLoaded(object sender, NavigationEventArgs e)
        {
            var frame = (Frame)sender;
            if (Window.GetWindow(frame) is Window window) Fit(window, frame);
        }

        private static void OnDpiChanged(object sender, DpiChangedEventArgs e)
        {
            var window = (Window)sender;

            // 작업 영역이 더 좁은 화면으로 옮겼다면 앞서 잡은 최소 높이로는 창을 화면 안에 둘 수 없다.
            // 목록 페이지처럼 아래 Fit이 건너뛰는 경우까지 덮으려면 여기서 먼저 낮춰야 한다.
            double limit = GetWorkAreaHeight(window);
            if (window.MinHeight > limit) window.MinHeight = limit;

            var frame = FindDescendant<Frame>(window);
            if (frame is not null) Fit(window, frame);
        }

        private static void OnStateChanged(object? sender, EventArgs e)
        {
            var window = (Window)sender!;
            if (window.WindowState != WindowState.Normal) return;

            var frame = FindDescendant<Frame>(window);
            if (frame is not null) Fit(window, frame);
        }

        private static void Fit(Window window, Frame frame)
        {
            if (frame.Content is DependencyObject page && !GetFitsContent(page)) return;

            // 최대화나 최소화 상태의 높이는 사용자가 돌아갈 크기가 아니고, 이때 Height를 바꾸면 복원할 크기가 바뀐다.
            // 보통 상태로 돌아올 때 OnStateChanged가 다시 맞춘다.
            if (window.WindowState != WindowState.Normal) return;

            // 처음 연 창은 XAML의 SizeToContent="Height"로 첫 페이지에 맞춰진다. 그 뒤로는 창 높이를 이 코드와 사용자만 바꾼다.
            window.SizeToContent = SizeToContent.Manual;

            // 화면에 들어가지 않는 크기는 요구할 수 없다. 이때는 안쪽 ScrollViewer가 콘텐츠를 지킨다.
            double limit = GetWorkAreaHeight(window);
            double allowed = Math.Min(MeasureRequiredHeight(window), limit);

            // 최소 높이는 지금까지 띄운 페이지 가운데 가장 큰 요구를 따르되, 화면을 넘지는 않는다.
            window.MinHeight = Math.Min(Math.Max(window.MinHeight, allowed), limit);

            // 사용자가 키워 둔 창은 그대로 두고, 모자랄 때만 넓힌다.
            if (window.ActualHeight < allowed) window.Height = allowed;
        }

        /// <summary>
        /// 창 크기는 건드리지 않고, 콘텐츠가 모두 보이는 데 필요한 창 높이를 잰다.
        /// </summary>
        // INTENT: SizeToContent를 Height로 바꿔 UpdateLayout을 부르면 WPF가 창을 실제로 그 높이로 바꾼다.
        // 창이 콘텐츠보다 크면(사용자가 키웠거나 최대화) 줄었다가 원래 높이로 돌아오는 사이에 한 프레임이 그려져 창이 깜빡이고,
        // 최대화 상태에서는 화면을 채우지 못한 채 남는다. 그래서 클라이언트 영역만 무한 높이로 재고, 제목 표시줄과 테두리 높이를 더한다.
        // 그 높이는 지금 창 높이에서 클라이언트 영역 높이를 빼서 얻는다. 값을 코드에 적어 두면 테마나 배율이 바뀔 때 틀린 값이 된다.
        private static double MeasureRequiredHeight(Window window)
        {
            window.UpdateLayout();
            if (VisualTreeHelper.GetChildrenCount(window) == 0 ||
                VisualTreeHelper.GetChild(window, 0) is not UIElement client)
            {
                return window.ActualHeight;
            }

            double nonClientHeight = window.ActualHeight - client.RenderSize.Height;
            client.Measure(new Size(client.RenderSize.Width, double.PositiveInfinity));
            double required = client.DesiredSize.Height + nonClientHeight;

            // 무한 높이로 잰 결과가 남지 않도록 다음 배치에서 창의 실제 크기로 다시 재게 한다.
            window.InvalidateMeasure();
            return required;
        }

        /// <summary>
        /// 창이 올라가 있는 모니터의 작업 영역 높이를 창과 같은 단위로 돌려준다.
        /// SystemParameters.WorkArea는 주 모니터만 가리키므로 배율이나 크기가 다른 보조 모니터로
        /// 창을 옮기면 어긋난다. 창이 실제로 놓인 모니터를 찾아 그 창의 배율로 환산한다.
        /// </summary>
        private static double GetWorkAreaHeight(Window window)
        {
            if (PresentationSource.FromVisual(window) is not HwndSource source ||
                source.CompositionTarget is null)
            {
                return double.PositiveInfinity;
            }

            IntPtr monitor = MonitorFromWindow(source.Handle, MonitorDefaultToNearest);
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info)) return double.PositiveInfinity;

            double pixels = info.Work.Bottom - info.Work.Top;
            return pixels * source.CompositionTarget.TransformFromDevice.M22;
        }

        private const uint MonitorDefaultToNearest = 2;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public int Size;
            public NativeRect Monitor;
            public NativeRect Work;
            public uint Flags;
        }

        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) return match;

                var found = FindDescendant<T>(child);
                if (found is not null) return found;
            }
            return null;
        }
    }
}
