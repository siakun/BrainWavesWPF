using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BrainWaves.Behaviors
{
    /// <summary>
    /// 목록의 항목을 손잡이로 끌어 같은 목록 안에서 순서를 바꾼다.
    /// ItemsControl에 MoveCommand를 붙이고, 항목 템플릿 안의 손잡이 요소에 IsHandle을 붙인다.
    /// 끄는 동안 항목은 포인터를 따라 위아래로만 움직이고 지나친 항목은 비켜서며, 놓으면 MoveCommand에 ReorderMove를 넘긴다.
    /// 컬렉션의 순서를 실제로 바꾸는 일은 명령을 받은 쪽이 한다.
    /// </summary>
    // INTENT: OS 드래그앤드롭(DragDrop.DoDragDrop)을 쓰지 않고 손잡이가 마우스를 직접 붙잡는다. DoDragDrop은 놓을 수 있는지만
    // 커서 모양으로 알려 주므로 항목이 들어갈 자리는 따로 그려야 하고, 다른 창으로 끌어 나가는 경로까지 열어 둔다.
    // 같은 목록 안에서 줄을 옮기기만 하는 화면에서는 줄이 포인터를 따라오고 다른 줄이 비켜서는 쪽이 놓을 자리를 그대로 보여 준다.
    // 끄는 항목은 자기 목록의 위아래 끝을 넘지 못한다. 다른 목록으로는 옮길 수 없다는 제약이 화면에서도 보이게 한다.
    // 애니메이션은 Windows 애니메이션 효과 설정과 무관하게 재생한다. 비켜서는 움직임이 없으면 놓을 자리를 알 수 없다.
    public static class DragReorder
    {
        private static readonly Duration SlideDuration = TimeSpan.FromMilliseconds(150);

        // 끄는 동안 포인터가 스크롤 영역의 위아래 끝에서 이 거리 안에 들어오면 화면 밖에 있는 항목 쪽으로 목록을 굴린다.
        private const double AutoScrollZone = 40;
        // 자동 스크롤 한 번에 굴리는 최대 거리. 포인터가 끝에 가까울수록 이 값에 가까워진다.
        private const double AutoScrollMaxStep = 14;

        // 마우스는 하나라 한 번에 하나만 끈다.
        private static Session? _session;

        // 놓은 항목이 새 자리로 미끄러져 들어가는 중인 목록의 패널에서 켜진다. 그동안 같은 목록에서 새로 끌면
        // 진행 중인 움직임이 끊겨 튀므로 받지 않는다. 정적 컬렉션에 두지 않아, 화면이 버려지면 패널과 함께 사라진다.
        private static readonly DependencyProperty IsSettlingProperty =
            DependencyProperty.RegisterAttached(
                "IsSettling",
                typeof(bool),
                typeof(DragReorder),
                new PropertyMetadata(false));

        public static readonly DependencyProperty MoveCommandProperty =
            DependencyProperty.RegisterAttached(
                "MoveCommand",
                typeof(ICommand),
                typeof(DragReorder),
                new PropertyMetadata(null));

        public static ICommand? GetMoveCommand(DependencyObject target) =>
            (ICommand?)target.GetValue(MoveCommandProperty);

        public static void SetMoveCommand(DependencyObject target, ICommand? value) =>
            target.SetValue(MoveCommandProperty, value);

        public static readonly DependencyProperty IsHandleProperty =
            DependencyProperty.RegisterAttached(
                "IsHandle",
                typeof(bool),
                typeof(DragReorder),
                new PropertyMetadata(false, OnIsHandleChanged));

        public static bool GetIsHandle(DependencyObject target) =>
            (bool)target.GetValue(IsHandleProperty);

        public static void SetIsHandle(DependencyObject target, bool value) =>
            target.SetValue(IsHandleProperty, value);

        private static readonly DependencyPropertyKey IsDraggingPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "IsDragging",
                typeof(bool),
                typeof(DragReorder),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// 끄는 항목의 컨테이너에서 켜진다. 놓은 뒤에는 새 자리에 들어갈 때까지 켜져 있다.
        /// 상속되므로 항목 템플릿 안의 요소가 트리거로 끄는 동안의 모양을 그린다.
        /// </summary>
        public static readonly DependencyProperty IsDraggingProperty = IsDraggingPropertyKey.DependencyProperty;

        public static bool GetIsDragging(DependencyObject target) =>
            (bool)target.GetValue(IsDraggingProperty);

        private static void OnIsHandleChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is not UIElement handle) return;

            handle.MouseLeftButtonDown -= OnHandleMouseDown;
            handle.MouseMove -= OnHandleMouseMove;
            handle.MouseLeftButtonUp -= OnHandleMouseUp;
            handle.LostMouseCapture -= OnHandleLostMouseCapture;
            if (!(bool)e.NewValue) return;

            handle.MouseLeftButtonDown += OnHandleMouseDown;
            handle.MouseMove += OnHandleMouseMove;
            handle.MouseLeftButtonUp += OnHandleMouseUp;
            handle.LostMouseCapture += OnHandleLostMouseCapture;
        }

        private static void OnHandleMouseDown(object sender, MouseButtonEventArgs e)
        {
            // 손잡이는 누르는 영역이 겹치는 줄 버튼 안에 놓이므로, 끌지 않고 떼더라도 줄이 눌리지 않게 여기서 이벤트를 끝낸다.
            e.Handled = true;
            if (_session is not null) return;

            var handle = (UIElement)sender;
            if (FindItemContainer(handle) is not (FrameworkElement container, Panel host, ItemsControl list)) return;
            if (GetMoveCommand(list) is null || (bool)host.GetValue(IsSettlingProperty)) return;

            var containers = host.Children.OfType<FrameworkElement>().ToArray();
            var index = Array.IndexOf(containers, container);
            if (index < 0 || !handle.CaptureMouse()) return;

            // 끌기 시작 여부는 마우스가 일정 거리 이상 움직인 뒤에 정한다. 손잡이를 눌렀다 떼기만 하면 아무 일도 일어나지 않는다.
            _session = new Session(handle, list, host, containers, index, e.GetPosition(host).Y);
        }

        private static void OnHandleMouseMove(object sender, MouseEventArgs e)
        {
            if (_session is not { } session || session.Handle != sender) return;

            var pointerY = e.GetPosition(session.Host).Y;
            if (!session.IsDragging)
            {
                if (Math.Abs(pointerY - session.PressY) < SystemParameters.MinimumVerticalDragDistance) return;
                Begin(session);
            }
            Follow(session, pointerY);
        }

        private static void OnHandleMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_session is not { } session || session.Handle != sender) return;
            e.Handled = true;
            End(commit: true);
        }

        // 다른 창으로 전환하는 것처럼 마우스를 놓치면 옮기지 않고 제자리로 돌려놓는다.
        private static void OnHandleLostMouseCapture(object sender, MouseEventArgs e)
        {
            if (_session is { } session && session.Handle == sender) End(commit: false);
        }

        private static void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape || _session is not { IsDragging: true }) return;
            e.Handled = true;
            End(commit: false);
        }

        private static void Begin(Session session)
        {
            session.IsDragging = true;

            foreach (var container in session.Containers)
                SetOffset(container, 0);

            // 끄는 줄이 비켜서는 줄 위에 그려지게 한다.
            Panel.SetZIndex(session.Dragged, 1);
            session.Dragged.SetValue(IsDraggingPropertyKey, true);

            // 휠을 굴리거나 자동 스크롤로 목록이 움직여도 항목이 포인터 아래에 머물도록, 스크롤될 때마다 자리를 다시 계산한다.
            session.Scroller = FindAncestor<ScrollViewer>(session.List);
            if (session.Scroller is not null)
            {
                session.Scroller.ScrollChanged += OnScrollChanged;
                session.ScrollTimer = new DispatcherTimer(
                    TimeSpan.FromMilliseconds(16), DispatcherPriority.Normal, OnAutoScrollTick, session.Host.Dispatcher);
            }

            // 손잡이는 포커스를 받지 않으므로 Esc는 창에서 받는다.
            session.Window = Window.GetWindow(session.Host);
            if (session.Window is not null)
                session.Window.PreviewKeyDown += OnWindowPreviewKeyDown;
        }

        private static void Follow(Session session, double pointerY)
        {
            session.Offset = Math.Clamp(pointerY - session.PressY, session.MinOffset, session.MaxOffset);
            SetOffset(session.Dragged, session.Offset);

            // 내려갈 때는 끄는 항목의 아래 가장자리가, 올라갈 때는 위 가장자리가 다른 항목의 가운데를 지나면 그 자리로 들어간다.
            // INTENT: 가운데끼리 비교하면 목록 끝에 닿았을 때 두 가운데가 같아져 끝 자리로 들어가지 못하고,
            // 끄는 항목이 끝 항목보다 키가 크면 아예 닿지 못한다. 가장자리로 비교하면 끝에 닿는 순간 끝 자리가 된다.
            var old = session.OldIndex;
            var top = session.Tops[old] + session.Offset;
            var bottom = top + session.Heights[old];
            var target = old;
            for (var i = 0; i < old; i++)
            {
                if (top < session.Tops[i] + session.Heights[i] / 2) { target = i; break; }
            }
            for (var i = session.Containers.Length - 1; i > old; i--)
            {
                if (bottom > session.Tops[i] + session.Heights[i] / 2) { target = i; break; }
            }

            if (target == session.TargetIndex) return;
            session.TargetIndex = target;

            // 끄는 항목의 원래 자리와 들어갈 자리 사이의 항목만 그 높이만큼 비켜선다.
            var height = session.Heights[old];
            for (var i = 0; i < session.Containers.Length; i++)
            {
                if (i == old) continue;
                var shift = i >= target && i < old ? height
                          : i <= target && i > old ? -height
                          : 0;
                if (shift == session.Shifts[i]) continue;

                session.Shifts[i] = shift;
                TranslateOf(session.Containers[i]).BeginAnimation(TranslateTransform.YProperty, Slide(shift));
            }
        }

        private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_session is { IsDragging: true } session && e.VerticalChange != 0)
                Follow(session, Mouse.GetPosition(session.Host).Y);
        }

        private static void OnAutoScrollTick(object? sender, EventArgs e)
        {
            if (_session is not { IsDragging: true, Scroller: { } scroller } session) return;

            // 항목이 이미 목록 끝에 닿아 더 갈 수 없는 쪽으로는 굴리지 않는다. 굴리면 목록만 화면 밖으로 밀려난다.
            var pointerY = Mouse.GetPosition(scroller).Y;
            double step = 0;
            if (pointerY < AutoScrollZone && session.Offset > session.MinOffset)
                step = -AutoScrollMaxStep * Math.Min(1, (AutoScrollZone - pointerY) / AutoScrollZone);
            else if (pointerY > scroller.ActualHeight - AutoScrollZone && session.Offset < session.MaxOffset)
                step = AutoScrollMaxStep * Math.Min(1, (pointerY - (scroller.ActualHeight - AutoScrollZone)) / AutoScrollZone);

            if (step != 0)
                scroller.ScrollToVerticalOffset(scroller.VerticalOffset + step);
        }

        private static void End(bool commit)
        {
            if (_session is not { } session) return;

            // 마우스를 놓을 때 LostMouseCapture가 다시 들어오므로 세션을 먼저 비운다.
            _session = null;
            session.Handle.ReleaseMouseCapture();
            if (!session.IsDragging) return;

            session.ScrollTimer?.Stop();
            if (session.Scroller is not null) session.Scroller.ScrollChanged -= OnScrollChanged;
            if (session.Window is not null) session.Window.PreviewKeyDown -= OnWindowPreviewKeyDown;

            // 지금 그려진 자리를 항목별로 기억해 두고, 순서를 바꾼 뒤 각 항목을 이 자리에서 새 자리로 미끄러뜨린다.
            // 명령을 받은 쪽이 저장에 실패해 순서를 그대로 두거나 컨테이너를 새로 만들어도, 화면은 실제 배치된 자리로 돌아간다.
            var generator = session.List.ItemContainerGenerator;
            var drawnTops = new Dictionary<object, double>();
            for (var i = 0; i < session.Containers.Length; i++)
            {
                var container = session.Containers[i];
                var drawnItem = generator.ItemFromContainer(container);
                if (drawnItem is not null && drawnItem != DependencyProperty.UnsetValue)
                    drawnTops[drawnItem] = session.Tops[i] + TranslateOf(container).Y;
            }

            var item = generator.ItemFromContainer(session.Dragged);
            if (commit && session.TargetIndex != session.OldIndex && item != DependencyProperty.UnsetValue)
            {
                var move = new ReorderMove(item, session.OldIndex, session.TargetIndex);
                var command = GetMoveCommand(session.List);
                if (command?.CanExecute(move) == true) command.Execute(move);
            }

            foreach (var container in session.Containers)
                SetOffset(container, 0);
            session.Dragged.ClearValue(Panel.ZIndexProperty);
            session.Dragged.ClearValue(IsDraggingPropertyKey);
            session.Host.UpdateLayout();

            var settling = generator.ContainerFromItem(item) as FrameworkElement;
            AnimationClock? settlingClock = null;
            foreach (var container in session.Host.Children.OfType<FrameworkElement>())
            {
                var containerItem = generator.ItemFromContainer(container);
                if (containerItem is null || !drawnTops.TryGetValue(containerItem, out var drawnTop)) continue;

                var from = drawnTop - VisualTreeHelper.GetOffset(container).Y;
                if (Math.Abs(from) < 0.5) continue;

                var clock = Slide(0, from).CreateClock();
                TranslateOf(container).ApplyAnimationClock(TranslateTransform.YProperty, clock);
                if (container == settling) settlingClock = clock;
            }

            if (settling is null || settlingClock is null) return;

            // 놓은 항목은 새 자리에 들어갈 때까지 끄는 모양을 유지하고 다른 항목 위에 그린다.
            var host = session.Host;
            Panel.SetZIndex(settling, 1);
            settling.SetValue(IsDraggingPropertyKey, true);
            host.SetValue(IsSettlingProperty, true);
            settlingClock.Completed += (_, _) =>
            {
                settling.ClearValue(Panel.ZIndexProperty);
                settling.ClearValue(IsDraggingPropertyKey);
                host.ClearValue(IsSettlingProperty);
            };
        }

        // 항목 템플릿 안의 요소에서 위로 올라가 목록의 항목 컨테이너와 항목을 늘어놓는 패널, 그 목록을 찾는다.
        // 목록 안에 목록이 있으면 손잡이에서 가장 가까운 목록이 대상이다.
        private static (FrameworkElement Container, Panel Host, ItemsControl List)? FindItemContainer(DependencyObject element)
        {
            var child = element;
            var parent = VisualTreeHelper.GetParent(child);
            while (parent is not null)
            {
                if (parent is Panel { IsItemsHost: true } host
                    && child is FrameworkElement container
                    && ItemsControl.GetItemsOwner(host) is { } list)
                {
                    return (container, host, list);
                }
                child = parent;
                parent = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private static T? FindAncestor<T>(DependencyObject element) where T : DependencyObject
        {
            for (var current = VisualTreeHelper.GetParent(element); current is not null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is T match) return match;
            }
            return null;
        }

        private static TranslateTransform TranslateOf(UIElement element)
        {
            if (element.RenderTransform is TranslateTransform transform) return transform;

            transform = new TranslateTransform();
            element.RenderTransform = transform;
            return transform;
        }

        private static void SetOffset(UIElement element, double y)
        {
            var transform = TranslateOf(element);
            transform.BeginAnimation(TranslateTransform.YProperty, null);
            transform.Y = y;
        }

        // 시작값을 주지 않으면 지금 그려진 자리에서 출발한다. 끄는 도중 비켜서던 항목이 방향을 바꿔도 튀지 않는다.
        private static DoubleAnimation Slide(double to, double? from = null) =>
            new()
            {
                From = from,
                To = to,
                Duration = SlideDuration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            };

        private sealed class Session
        {
            public Session(UIElement handle, ItemsControl list, Panel host, FrameworkElement[] containers, int index, double pressY)
            {
                Handle = handle;
                List = list;
                Host = host;
                Containers = containers;
                OldIndex = index;
                TargetIndex = index;
                PressY = pressY;

                // 위치는 배치된 자리(RenderTransform을 뺀 값)로 잰다. 끄는 동안 움직여 그린 자리는 계산의 기준이 아니다.
                Tops = containers.Select(container => VisualTreeHelper.GetOffset(container).Y).ToArray();
                Heights = containers.Select(container => container.ActualHeight).ToArray();
                Shifts = new double[containers.Length];
                MinOffset = -Tops[index];
                MaxOffset = Tops[^1] + Heights[^1] - (Tops[index] + Heights[index]);
            }

            public UIElement Handle { get; }
            public ItemsControl List { get; }
            public Panel Host { get; }
            public FrameworkElement[] Containers { get; }
            public FrameworkElement Dragged => Containers[OldIndex];
            public int OldIndex { get; }
            public double PressY { get; }
            public double[] Tops { get; }
            public double[] Heights { get; }
            public double MinOffset { get; }
            public double MaxOffset { get; }

            public bool IsDragging { get; set; }
            public int TargetIndex { get; set; }
            public double Offset { get; set; }
            // 항목마다 지금 비켜선 거리. 같은 거리로 애니메이션을 다시 시작하지 않으려고 기억한다.
            public double[] Shifts { get; }
            public ScrollViewer? Scroller { get; set; }
            public DispatcherTimer? ScrollTimer { get; set; }
            public Window? Window { get; set; }
        }
    }

    /// <summary>
    /// 끌어서 옮긴 항목과 그 자리. 두 순번은 ObservableCollection.Move의 인자와 뜻이 같다.
    /// NewIndex는 옮긴 뒤 목록에서 그 항목의 순번이다.
    /// </summary>
    public sealed record ReorderMove(object Item, int OldIndex, int NewIndex);
}
