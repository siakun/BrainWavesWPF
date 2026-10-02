using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BrainWaves.Behaviors
{
    /// <summary>
    /// 키보드 포커스 테두리를 사용자의 마지막 조작에 맞춰 보이거나 숨긴다.
    /// 창에 IsEnabled를 붙이면 키보드로 조작한 뒤에는 테두리가 보이고, 마우스나 터치로 누른 뒤에는 숨는다.
    /// 포커스 테두리 템플릿은 창에서 물려받은 IsShown을 보고 테두리를 그릴지 정한다.
    /// </summary>
    // INTENT: WPF는 포커스가 옮겨 가는 순간 마지막 입력 장치가 키보드였으면 FocusVisualStyle을 붙이고, 다음 포커스 이동까지 그대로 둔다.
    // 마지막 입력 장치는 Alt나 Windows 키처럼 다른 창으로 넘어갈 때 누르는 보조 키로도 키보드가 되고,
    // 창이 다시 활성화되어 포커스를 되돌릴 때 그 값으로 판단한다. 그래서 마우스로만 조작해도 창을 오간 뒤에는 테두리가 생기고,
    // 그 요소를 다시 눌러도 포커스가 옮겨 가지 않으니 지워지지 않는다.
    // 이 판단을 바꾸는 공개 API가 없으므로 테두리를 붙이는 일은 WPF에 맡기고, 보일지만 여기서 정한다.
    // 보조 키만 누른 것은 조작으로 치지 않아야 창을 오가는 단축키가 테두리를 되살리지 않는다.
    public static class FocusCue
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(FocusCue),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject target) =>
            (bool)target.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject target, bool value) =>
            target.SetValue(IsEnabledProperty, value);

        /// <summary>
        /// 포커스 테두리를 보일지. 창에 두면 창 안의 요소와 팝업, 포커스 테두리까지 물려받는다.
        /// 기본값이 True라서 IsEnabled를 붙이지 않은 창은 WPF가 정한 대로 테두리를 그린다.
        /// </summary>
        public static readonly DependencyProperty IsShownProperty =
            DependencyProperty.RegisterAttached(
                "IsShown",
                typeof(bool),
                typeof(FocusCue),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetIsShown(DependencyObject target) =>
            (bool)target.GetValue(IsShownProperty);

        public static void SetIsShown(DependencyObject target, bool value) =>
            target.SetValue(IsShownProperty, value);

        private static bool _isTracking;

        // 아직 아무 조작도 없을 때는 숨긴다. 처음 누른 Tab이 키보드 조작으로 잡혀 그때부터 보인다.
        private static bool _isShown;

        private static void OnIsEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is not Window window || !(bool)e.NewValue) return;

            // 창이 만들어질 때 바로 지켜보기 시작해야 첫 조작을 놓치지 않는다.
            if (!_isTracking)
            {
                InputManager.Current.PreNotifyInput += OnPreNotifyInput;
                _isTracking = true;
            }
            window.SetValue(IsShownProperty, _isShown);
        }

        // 입력이 요소에 전달되기 전에 판단한다. 그 입력이 옮긴 포커스에 붙는 테두리가 바뀐 값을 물려받는다.
        private static void OnPreNotifyInput(object sender, NotifyInputEventArgs e)
        {
            var input = e.StagingItem.Input;
            bool isShown;
            if (input.RoutedEvent == Keyboard.PreviewKeyDownEvent && input is KeyEventArgs key)
            {
                if (IsModifier(key)) return;
                isShown = true;
            }
            // 터치와 펜도 마우스 누름으로 이어서 올라오므로 마우스 누름 하나로 받는다.
            else if (input.RoutedEvent == Mouse.PreviewMouseDownEvent)
            {
                isShown = false;
            }
            else
            {
                return;
            }

            if (isShown == _isShown) return;
            _isShown = isShown;

            // XAML 디자이너처럼 Application이 없는 곳에서도 창을 띄우므로 없으면 넘어간다.
            var windows = Application.Current?.Windows;
            if (windows is null) return;
            foreach (var window in windows.OfType<Window>().Where(GetIsEnabled))
                window.SetValue(IsShownProperty, isShown);
        }

        private static bool IsModifier(KeyEventArgs e) =>
            (e.Key == Key.System ? e.SystemKey : e.Key) is
                Key.LeftShift or Key.RightShift or
                Key.LeftCtrl or Key.RightCtrl or
                Key.LeftAlt or Key.RightAlt or
                Key.LWin or Key.RWin;
    }
}
