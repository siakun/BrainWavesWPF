# 마우스로 누른 컨트롤에 키보드 포커스 테두리가 남는 문제

마우스로만 조작했는데 버튼이나 탭에 `FocusVisualStyle` 테두리가 생기고, 그 요소를 다시 눌러도 지워지지 않을 때 보는 문서입니다. WPF는 포커스가 옮겨 가는 순간과 Alt를 누르는 순간에 테두리를 붙일지 정합니다. 기준은 마지막으로 쓴 입력 장치이고, 붙인 테두리는 다음 포커스 이동까지 그대로 둡니다. 마지막 입력 장치는 창을 오갈 때 누른 Alt 같은 보조 키로도 키보드가 되므로, 마우스 사용자에게도 테두리가 생깁니다. 해결은 테두리를 붙이는 일은 WPF에 그대로 맡기고, 보일지만 사용자의 마지막 조작으로 따로 정해 `FocusVisualStyle` 템플릿이 그 값을 따르게 하는 것입니다. `FocusVisualStyle`로 키보드 포커스를 그리는 WPF 앱에 해당합니다.

## 증상

하단 탭을 마우스로 눌러 오가다 보면 어느 순간 고른 탭 둘레에 둥근 포커스 테두리가 생깁니다. 그 탭을 다시 눌러도 남아 있고, 다른 탭을 누르면 사라집니다. 대부분의 클릭에서는 테두리가 생기지 않으므로 스타일을 잘못 적은 문제가 아니라 조작 순서에 달린 문제입니다.

겉보기가 같지만 원인이 다른 경우가 있습니다. Windows의 키보드 단서 표시(`SystemParameters.KeyboardCues`, 설정의 "액세스 키에 밑줄 표시")가 켜진 PC에서는 조작 순서와 무관하게 클릭할 때마다 테두리가 생깁니다. 아래 진단 1단계로 구별하며, 해결 방법은 같습니다.

## 원인

WPF 소스에서 확인한 동작은 다음과 같습니다.

1. `KeyboardNavigation.ShowFocusVisual`은 기존 테두리를 지운 뒤, `AlwaysShowFocusVisual`이 참이거나 `InputManager.Current.MostRecentInputDevice`가 `KeyboardDevice`일 때만 포커스된 요소에 `FocusVisualStyle`을 장식(adorner)으로 붙입니다. `AlwaysShowFocusVisual`의 초기값은 `SystemParameters.KeyboardCues`입니다.
2. `MostRecentInputDevice`는 키를 누르거나 뗄 때 키보드로 바뀌고, 마우스 버튼을 누르거나 떼거나 세로 휠을 돌릴 때 마우스로 바뀝니다. 마우스 이동과 창 활성화는 이 값을 바꾸지 않습니다.
3. 왼쪽이나 오른쪽 Alt를 누르면 `KeyboardNavigation.ProcessInput`이 포커스 이동 없이 `ShowFocusVisual`을 부르고, 같은 분기에서 액세스 키 밑줄도 켭니다. Alt를 누른 순간 마지막 장치는 이미 키보드이므로 테두리가 붙습니다.
4. 창이 Win32 포커스를 잃으면 `HwndKeyboardInputProvider`가 포커스된 요소를 기억하고 키보드 포커스를 비웁니다. 다시 포커스를 받으면 기억한 요소에 `Keyboard.Focus`를 다시 불러, 1의 판단이 그때의 마지막 장치로 다시 일어납니다.
5. 버튼 계열(`ButtonBase`)은 마우스 왼쪽 버튼을 누를 때 `Focus()`를 부릅니다. 이미 포커스를 가진 요소면 포커스가 옮겨 가지 않아 1의 판단이 일어나지 않으므로, 붙어 있던 테두리가 그대로 남습니다.

1은 포커스를 받을 때마다 일어납니다. 이 호출 지점은 소스로 확인하지 않았고, 검증 프로그램에서 포커스가 옮겨 갈 때마다 테두리가 다시 판단되는 것으로 확인했습니다.

이 다섯 가지가 겹쳐 마우스 사용자에게 테두리가 생기는 경로는 다음과 같습니다.

- 탭을 마우스로 누른 뒤 Alt+Tab으로 다른 창에 갑니다. Alt를 누르는 순간에는 아직 이 창이 앞에 있어 그 입력을 받으므로, 3에 따라 누른 탭에 테두리가 붙습니다. 돌아와 같은 탭을 눌러도 5에 따라 남습니다.
- Alt 없이 창을 떠났더라도 그 전에 앱에서 키를 누른 적이 있으면, 돌아올 때 4의 포커스 복원이 테두리를 붙입니다.
- 방향키나 Tab으로 포커스를 옮긴 뒤 그 요소를 마우스로 누르면 5에 따라 테두리가 남습니다.

## 진단 절차

1. 키보드 단서 표시가 켜져 있는지 봅니다.
   ```powershell
   Add-Type -AssemblyName PresentationFramework; [System.Windows.SystemParameters]::KeyboardCues
   ```
   - True면 WPF가 포커스 이동마다 테두리를 붙이므로 클릭할 때마다 재현됩니다.
   - False면 다음 단계로 조작 순서에 따른 문제인지 확인합니다.
2. 요소를 마우스로 누른 뒤 Alt를 눌렀다 뗍니다. 이때 테두리가 생기면 위 3과 5의 경로입니다. 생기지 않으면 Alt+Tab으로 다른 창에 갔다가 돌아와 봅니다(4의 경로).
3. 어느 경로든 테두리를 누가 붙였는지 확인하려면 포커스를 받는 시점(`GotKeyboardFocus`)의 `InputManager.Current.MostRecentInputDevice`를 기록합니다. 키보드였으면 WPF의 판단으로 붙은 것입니다. 마우스였고 키보드 단서 표시도 꺼져 있는데 테두리가 있으면 이 문서의 원인이 아니므로, 템플릿이 `IsKeyboardFocused`나 `IsFocused`로 직접 그리는 테두리가 있는지 봅니다.

실제 키보드와 마우스 없이 이 순서를 재현하는 방법은 [실제 백엔드 없이 WPF 화면 상태를 그려 확인하기](wpf-ui-state-verification-without-backend.md)의 "입력 장치에 따라 달라지는 동작" 절에 있습니다.

## 해결 방법과 선택 기준

| 방법 | 결과 | 판단 |
|---|---|---|
| 컨트롤의 `FocusVisualStyle`을 `{x:Null}`로 | 테두리가 사라짐 | 키보드 사용자가 포커스 위치를 볼 수 없게 됩니다. 키보드로 갈 필요가 없는 요소라면 테두리를 지우기보다 `Focusable="False"`로 포커스 대상에서 빼는 편이 맞습니다 |
| 리플렉션으로 비공개 `KeyboardNavigation.AlwaysShowFocusVisual`을 끔 | 키보드 단서 표시가 켜진 경우만 막음 | Alt와 포커스 복원 경로는 그대로 남고, 비공개 이름에 기대므로 WPF 버전이 바뀌면 깨질 수 있습니다 |
| 템플릿에서 `IsKeyboardFocused` 트리거로 테두리를 직접 그림 | WPF의 판단을 거치지 않음 | `IsKeyboardFocused`는 마우스로 얻은 포커스에도 참이라 같은 문제를 다시 풀어야 하고, 컨트롤마다 템플릿을 고쳐야 합니다 |
| 테두리는 WPF가 붙이고, 보일지만 마지막 조작으로 정함 | 마우스로 누른 뒤에는 숨고 키보드로 옮기면 보임 | 이 저장소에서 쓴 방법입니다. 포커스 테두리 스타일만 고치면 되고 컨트롤 템플릿은 그대로 둡니다 |

선택한 방법은 세 부분으로 이루어집니다.

- `InputManager.Current.PreNotifyInput`에서 `PreviewKeyDown`이 오면 보이게, `PreviewMouseDown`이 오면 숨기게 상태를 바꿉니다. `PreNotifyInput`은 입력이 요소에 전달되기 전에 불리므로, 그 입력 때문에 옮겨 간 포커스에 붙는 테두리가 이미 바뀐 상태를 봅니다. 터치와 펜은 마우스 누름으로 이어서 올라오므로 마우스 누름 하나로 받습니다.
- 상태는 창에 둔 상속 첨부 속성으로 내려보냅니다. 포커스 테두리 장식은 창의 장식 층(`AdornerLayer`) 아래 시각 트리에 들어가므로 창의 값을 물려받고, 콤보 상자 목록 같은 팝업 안의 요소도 `Popup`을 거쳐 물려받습니다.
- `FocusVisualStyle`의 템플릿은 그 값이 거짓이면 테두리를 숨깁니다.

```csharp
InputManager.Current.PreNotifyInput += (_, e) =>
{
    var input = e.StagingItem.Input;
    if (input.RoutedEvent == Keyboard.PreviewKeyDownEvent && input is KeyEventArgs key && !IsModifier(key))
        SetShown(true);
    else if (input.RoutedEvent == Mouse.PreviewMouseDownEvent)
        SetShown(false);
};
```

```xml
<ControlTemplate>
    <Rectangle x:Name="Ring" Stroke="..." StrokeThickness="1.5" Margin="-3"/>
    <ControlTemplate.Triggers>
        <Trigger Property="local:FocusCue.IsShown" Value="False">
            <Setter TargetName="Ring" Property="Visibility" Value="Hidden"/>
        </Trigger>
    </ControlTemplate.Triggers>
</ControlTemplate>
```

정할 때 따른 기준은 다음과 같습니다.

- 보조 키(Shift, Ctrl, Alt, Windows 키)만 누른 것은 조작으로 치지 않습니다. Alt+Tab처럼 창을 떠나는 단축키는 앞의 보조 키를 누르는 순간 이 창이 그 입력을 받으므로, 이를 조작으로 치면 원인의 3과 4 경로가 그대로 남습니다. Alt를 누른 채 다른 키를 누르면 그 키가 조작으로 잡혀 테두리가 보입니다. Alt의 키 이벤트는 `Key`가 `Key.System`이고 실제 키는 `SystemKey`에 있으므로 둘을 함께 봅니다.
- 아직 아무 조작도 없을 때는 숨깁니다. 처음 누른 Tab이 조작으로 잡혀 그때부터 보입니다. 이 Tab을 놓치지 않도록 창이 만들어질 때 바로 입력 감시를 시작합니다. 테두리 스타일이 처음 쓰일 때 시작하면 첫 Tab이 지나간 뒤라 첫 테두리가 숨습니다.
- 상속 첨부 속성의 기본값은 참으로 둡니다. 감시를 켜지 않은 창은 WPF의 원래 동작으로 돌아가, 키보드 사용자가 테두리를 잃는 쪽으로 실패하지 않습니다.

한계는 다음과 같습니다.

- 테두리를 붙이는 시점은 여전히 WPF가 정합니다. 마우스로 누른 버튼에서 Space를 눌러도 그 버튼에 테두리가 새로 생기지 않고, 다음 포커스 이동부터 보입니다.
- 키보드 단서 표시를 켠 사용자도 마우스로 누른 뒤에는 테두리를 보지 못합니다. 그 설정을 마우스로 얻은 포커스까지 테두리로 보여 달라는 요청으로 해석하지 않고, 키보드로 옮길 때만 보이는 쪽을 택했습니다.
- 상태를 정적 값 하나로 두므로 UI 스레드가 하나인 앱을 전제로 합니다. UI 스레드가 여럿이면 스레드마다 `InputManager`를 따로 감시하고 상태도 나눠야 합니다.

## 검증과 회귀 확인

검증 프로그램에서 실제 입력과 같은 경로(원시 입력 보고)로 같은 순서를 흘려, WPF 기본 동작과 고친 뒤를 비교했습니다.

| 순서 | WPF 기본 | 고친 뒤 |
|---|---|---|
| 탭을 마우스로 누름 | 없음 | 없음 |
| Alt를 누름 | 보임 | 숨음 |
| 창이 비활성이 됐다가 포커스를 되돌림 | 보임 | 숨음 |
| 같은 탭을 다시 마우스로 누름 | 보임 | 숨음 |
| 오른쪽 방향키로 옆 탭에 포커스 | 보임 | 보임 |
| 그 탭을 마우스로 누름 | 보임 | 숨음 |
| Tab으로 다음 요소에 포커스 | 보임 | 보임 |
| 키보드 단서 표시를 켜고 마우스로 누름 | 보임 | 숨음 |

키와 마우스 버튼은 OS 입력이 WPF에 들어오는 원시 보고로 흘렸습니다. 창 비활성과 포커스 복원은 `HwndKeyboardInputProvider`가 하는 일(비활성 보고 뒤 기억한 요소에 `Keyboard.Focus`)을 그대로 따라 했고, 실제 창 전환으로는 확인하지 않았습니다. 키보드 단서 표시는 Windows 설정을 바꾸지 않고 `AlwaysShowFocusVisual`을 리플렉션으로 켜서 흉내 냈습니다.

회귀를 막으려면 다음을 확인합니다.

- 새 컨트롤 스타일의 `FocusVisualStyle`이 이 저장소의 포커스 테두리 스타일(`FocusVisual`, `FocusVisual.Pill`) 가운데 하나를 쓰는지 봅니다. WPF 기본 점선 테두리를 쓰는 컨트롤은 이 정책을 따르지 않습니다.
- 새 창을 만들면 그 창에도 `FocusCue.IsEnabled`를 붙입니다. 붙이지 않은 창은 WPF 기본 동작으로 그려집니다.

## 근거

- 적용한 코드: [FocusCue.cs](../BrainWaves/BrainWaves/Behaviors/FocusCue.cs), [ButtonStyles.xaml](../BrainWaves/BrainWaves/Resources/Styles/ButtonStyles.xaml)의 `FocusVisual`, `FocusVisual.Pill`, [MainWindow.xaml](../BrainWaves/BrainWaves/View/MainWindow.xaml)
- WPF 소스(dotnet/wpf)
  - [KeyboardNavigation.cs](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Input/KeyboardNavigation.cs): `ShowFocusVisual`의 판단 조건, `AlwaysShowFocusVisual`의 초기값, `ProcessInput`의 Alt 분기
  - [KeyboardDevice.cs](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Input/KeyboardDevice.cs), [MouseDevice.cs](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Input/MouseDevice.cs): `MostRecentInputDevice`를 바꾸는 입력
  - [HwndKeyboardInputProvider.cs](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndKeyboardInputProvider.cs): 창이 포커스를 잃고 되찾을 때의 포커스 기억과 복원
  - [ButtonBase.cs](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/Primitives/ButtonBase.cs): `OnMouseLeftButtonDown`의 `Focus()` 호출
- [Styling for Focus in Controls, and FocusVisualStyle](https://learn.microsoft.com/dotnet/desktop/wpf/advanced/styling-for-focus-in-controls-and-focusvisualstyle)
