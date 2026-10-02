# 누른 직후 UI 스레드가 바빠 애니메이션 앞부분이 사라지는 문제

탭이나 버튼을 누를 때 시작한 짧은 애니메이션이 실제 조작에서는 거의 보이지 않고 끝 위치로 건너뛴 것처럼 보일 때 보는 문서입니다. WPF 애니메이션은 실제로 흐른 시간으로 진행률을 정합니다. 그래서 시작 직후 같은 클릭이 일으킨 작업(페이지 전환, 큰 목록 생성)이 UI 스레드를 차지하면, 그동안 화면은 그려지지 않고 진행률만 앞으로 갑니다. 처음에 빠르고 끝에서 느려지는 감속 곡선은 움직임 대부분이 앞부분에 몰려 있어 거의 다 사라집니다. 해결은 대기 중인 작업이 모두 끝난 뒤에 애니메이션을 시작하는 것입니다. 클릭 하나로 애니메이션과 무거운 화면 갱신이 함께 일어나는 WPF 화면에 해당합니다.

## 증상

하단 탭의 막대가 고른 탭으로 250밀리초 동안 감속하며 미끄러지게 만들었습니다(`CubicEase`, `EaseOut`). 탭의 `IsChecked`만 바꿔 확인할 때는 중간 프레임이 고르게 나왔는데, 실제로 탭을 누르면 막대가 거의 끝 위치에 나타난 뒤 조금만 움직였습니다.

겉보기가 같지만 원인이 다른 경우는 다음과 같습니다. 아래 진단에서 기록한 값으로 구별합니다.

- 애니메이션을 끄는 조건이 걸린 경우. 예를 들어 코드가 Windows 애니메이션 효과 설정(`SystemParameters.ClientAreaAnimation`)을 따르는데 그 설정이 꺼져 있으면, 중간 값 없이 바로 끝 값이 됩니다.
- 다른 코드가 애니메이션을 지운 경우. 크기 변경 같은 처리에서 같은 속성에 `BeginAnimation(속성, null)`을 부르거나 값을 바로 넣으면, 진행 중이던 애니메이션이 끝 값으로 바뀝니다.

## 원인

- WPF의 애니메이션 시계는 실제 경과 시간을 기준으로 진행합니다. UI 스레드가 막혀 있으면 프레임을 그리지 못할 뿐 시계는 흐르고, 다시 그릴 때 그때까지 흐른 시간에 해당하는 값이 바로 적용됩니다.
- 탭(`RadioButton`)을 누르면 `ButtonBase.OnClick` 안에서 체크가 바뀌어 `Checked` 이벤트가 먼저 일어나고, 이어서 명령이 실행됩니다. 이 앱의 명령은 `Frame`이 보여 줄 페이지 주소를 바꾸고, `NavigationService`는 탐색을 디스패처의 `Normal` 우선순위로 예약합니다. `Normal`은 화면을 그리는 `Render`보다 높아서, 클릭 처리가 끝나면 다음 프레임보다 먼저 페이지를 불러오고 배치합니다.
- 그래서 `Checked`에서 시작한 애니메이션은 페이지 전환이 끝날 때까지 한 프레임도 그려지지 않습니다. 이 앱에서는 페이지 전환이 0.05~0.1초 안팎 UI 스레드를 차지했고, 250밀리초 감속 곡선은 처음 그려질 때 이미 거리의 40~90%를 지나 있었습니다. 처음 불러오는 페이지일수록 오래 걸려 많이 사라졌습니다.

## 진단 절차

1. 실제 조작과 같은 경로로 재현합니다. 상태 속성(`IsChecked`)만 바꾸면 명령과 그 뒤의 작업이 일어나지 않아 문제가 숨습니다. 마우스를 뗄 때 `ButtonBase`가 부르는 `OnClick`을 리플렉션으로 부르면 체크 변경, `Click` 이벤트, 명령 실행이 같은 순서로 일어납니다.
   ```csharp
   typeof(ButtonBase).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(tab, null);
   ```
2. 누른 뒤 애니메이션 대상 값(여기서는 `TranslateTransform.X`)을 25밀리초 간격으로 기록합니다. 기록 사이에는 디스패처를 돌려 줍니다.
   - 첫 기록이 예정보다 늦게 나오고(디스패처가 막혀 있었음) 그때 값이 이미 끝 근처면 이 문서의 원인입니다.
   - 중간 값 없이 바로 끝 값이면 위 증상 절의 다른 원인을 봅니다.
   - 처음부터 중간 값이 고르게 이어지면 애니메이션 자체는 정상입니다.

## 해결 방법과 선택 기준

| 방법 | 결과 | 판단 |
|---|---|---|
| 대기 중인 작업이 끝난 뒤 시작 (`Dispatcher.InvokeAsync(..., DispatcherPriority.ContextIdle)`) | 움직임 전체가 보이고, 출발이 그 작업 시간만큼 늦어짐 | 이 저장소에서 쓴 방법입니다. 애니메이션 코드만 바꾸면 되고, 어떤 작업이 막는지 몰라도 동작합니다 |
| 막는 작업을 가볍게 만듦 (페이지 인스턴스 재사용 등) | 전환 자체가 빨라짐 | 근본적이지만 페이지가 상태를 계속 들고 있게 되므로 화면과 뷰모델의 수명을 함께 다시 설계해야 합니다 |
| 막는 작업을 애니메이션이 끝난 뒤로 미룸 | 움직임은 보이지만 페이지가 늦게 바뀜 | 누른 결과가 늦게 보여 반응이 느리게 느껴집니다 |
| 곡선을 선형으로 바꾸거나 시간을 늘림 | 사라지는 비율만 줄어듦 | 막히는 시간이 길어지면 같은 문제가 다시 생깁니다 |

`ContextIdle`은 `Background`보다 낮은 우선순위입니다. 탐색처럼 `Normal`로 예약된 작업뿐 아니라 배치(`Render`), `Loaded` 이벤트, `Background` 작업이 모두 끝난 뒤에 실행되므로, 무엇이 UI 스레드를 차지하는지 알지 못해도 그 뒤에 출발합니다.

```csharp
host.AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler((_, _) =>
    host.Dispatcher.InvokeAsync(() => MoveTo(host, indicator, animate: true), DispatcherPriority.ContextIdle)));
```

출발 시점에 고른 탭을 다시 읽으므로, 출발을 기다리는 사이 다른 탭을 눌러도 마지막에 고른 탭으로 갑니다. 애니메이션에 시작 값을 주지 않아 지금 그려진 위치에서 출발하므로, 움직이는 도중에 다른 탭을 눌러도 튀지 않습니다.

한계는 다음과 같습니다.

- 막대는 누른 순간이 아니라 새 페이지가 뜬 뒤에 출발합니다. 이 앱에서는 0.1초 안팎 늦었고, 페이지와 탭 글자가 먼저 바뀐 뒤 막대가 따라가는 모양이 됩니다.
- 화면이 계속 `Background` 이상의 작업을 예약하면 `ContextIdle`이 그만큼 늦게 옵니다. 그런 화면에서는 막는 작업을 가볍게 만드는 쪽을 함께 봅니다.

## 검증과 회귀 확인

같은 검증 프로그램에서 `OnClick`으로 탭을 차례로 누르고 막대 위치를 기록했습니다. Windows 애니메이션 효과 설정은 바꾸지 않았습니다.

| 누른 탭 | 즉시 출발했을 때 처음 그려진 위치 | 작업이 끝난 뒤 출발했을 때 이어진 위치 |
|---|---|---|
| Presets (0에서 145로) | 131 | 45, 75, 102, 123, 135, 142, 145 |
| Settings (145에서 290으로) | 244 | 166, 216, 244, 272, 283, 288, 290 |
| Waves (290에서 0으로) | 169 | 224, 158, 100, 53, 27, 9, 2, 0 |

탭을 누를 때 하는 일이 늘어나면 이 표를 다시 측정합니다. 상태 속성만 바꾸는 검증으로는 이 문제가 드러나지 않으므로, 누르는 순간의 동작을 볼 때는 위 진단 1의 방법으로 확인합니다.

## 근거

- 적용한 코드: [SlidingTabIndicator.cs](../BrainWaves/BrainWaves/Behaviors/SlidingTabIndicator.cs)
- 탭이 페이지를 바꾸는 경로: [MainWindow.xaml](../BrainWaves/BrainWaves/View/MainWindow.xaml)의 `NavigateFrameCommand`, [MainViewModel.cs](../BrainWaves/BrainWaves/ViewModel/MainViewModel.cs)
- WPF 소스(dotnet/wpf) [NavigationService.cs](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Navigation/NavigationService.cs): `NavigateQueueItem.PostNavigation`이 탐색을 `DispatcherPriority.Normal`로 예약
- [DispatcherPriority](https://learn.microsoft.com/dotnet/api/system.windows.threading.dispatcherpriority): 우선순위의 순서와 `ContextIdle`의 뜻
- [Timing behaviors overview](https://learn.microsoft.com/dotnet/desktop/wpf/graphics-multimedia/timing-behaviors-overview), [Animation overview](https://learn.microsoft.com/dotnet/desktop/wpf/graphics-multimedia/animation-overview)
