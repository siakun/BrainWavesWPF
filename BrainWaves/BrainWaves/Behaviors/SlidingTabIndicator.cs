using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BrainWaves.Behaviors
{
    /// <summary>
    /// 고른 탭을 가리키는 막대 하나를 그 탭의 자리로 옮긴다.
    /// 막대에 IsEnabled를 붙이면, 막대와 같은 부모 패널 안의 RadioButton 가운데 체크된 탭의 위치와 폭에 막대를 맞추고,
    /// 고른 탭이 바뀔 때는 막대가 옆으로 미끄러지듯 옮겨 간다.
    /// </summary>
    // INTENT: 탭마다 막대를 따로 두면 고른 탭이 바뀔 때 한쪽 막대가 꺼지고 다른 쪽이 켜질 뿐이라 이동이 보이지 않는다.
    // 막대를 하나만 두고 옮겨야 어느 탭에서 어느 탭으로 갔는지 눈으로 따라갈 수 있다.
    // 탭 폭은 창 폭에 따라 바뀌므로 막대의 위치와 폭은 XAML에 적지 않고 실제로 배치된 탭에서 잰다.
    public static class SlidingTabIndicator
    {
        private static readonly Duration SlideDuration = TimeSpan.FromMilliseconds(250);

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(SlidingTabIndicator),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject target) =>
            (bool)target.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject target, bool value) =>
            target.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is FrameworkElement indicator && (bool)e.NewValue)
                indicator.Loaded += OnIndicatorLoaded;
        }

        private static void OnIndicatorLoaded(object sender, RoutedEventArgs e)
        {
            var indicator = (FrameworkElement)sender;
            indicator.Loaded -= OnIndicatorLoaded;
            if (indicator.Parent is not Panel host) return;

            if (indicator.RenderTransform is not TranslateTransform)
                indicator.RenderTransform = new TranslateTransform();

            // RadioButton의 Checked는 부모로 올라오는 이벤트라 탭마다 구독하지 않고 패널에서 한 번 받는다.
            // INTENT: 탭을 고르면 그 탭에 묶인 일(이 앱에서는 페이지 전환)이 곧바로 UI 스레드를 차지해 잠시 화면이 그려지지 않는다.
            // 애니메이션 시계는 그동안에도 흐르므로 고른 즉시 출발하면 다시 그려질 때 막대가 이미 대부분 옮겨 가 있어 이동이 보이지 않는다.
            // 그래서 대기 중인 일이 모두 끝난 뒤(ContextIdle)에 출발한다. 막대는 새 페이지가 뜬 다음 그 탭으로 따라 미끄러진다.
            host.AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler((_, _) =>
                host.Dispatcher.InvokeAsync(() => MoveTo(host, indicator, animate: true), DispatcherPriority.ContextIdle)));
            host.SizeChanged += (_, _) => MoveTo(host, indicator, animate: false);
            MoveTo(host, indicator, animate: false);
        }

        private static void MoveTo(Panel host, FrameworkElement indicator, bool animate)
        {
            var selected = host.Children.OfType<RadioButton>().FirstOrDefault(tab => tab.IsChecked == true);
            if (selected is null || selected.ActualWidth <= 0) return;

            var x = selected.TranslatePoint(new Point(0, 0), host).X;
            var width = selected.ActualWidth;
            var transform = (TranslateTransform)indicator.RenderTransform;

            // 처음 자리를 잡을 때와 창 크기가 바뀔 때는 바로 옮긴다.
            // INTENT: 고른 탭이 바뀔 때는 Windows의 애니메이션 효과 설정(SystemParameters.ClientAreaAnimation)과 무관하게 미끄러진다.
            // 그 설정은 성능 옵션으로 시각 효과를 줄일 때도 함께 꺼져, 따르면 그런 PC에서는 막대가 늘 바로 옮겨 가 이동이 보이지 않는다.
            // 막대는 탭을 누른 순간에만 짧게 움직이므로, 재생하는 동안 계속 움직이는 파형 흐름(Waves.xaml)만 그 설정을 따른다.
            if (!animate)
            {
                transform.BeginAnimation(TranslateTransform.XProperty, null);
                indicator.BeginAnimation(FrameworkElement.WidthProperty, null);
                transform.X = x;
                indicator.Width = width;
                return;
            }

            // 시작값을 주지 않아 지금 그려진 자리에서 출발한다. 움직이는 도중에 다른 탭을 눌러도 막대가 튀지 않는다.
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            transform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(x, SlideDuration) { EasingFunction = ease });
            indicator.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation(width, SlideDuration) { EasingFunction = ease });
        }
    }
}
