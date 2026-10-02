# 필요한 창 높이를 재다가 창이 깜빡이는 문제

페이지를 바꿀 때 창이 한 프레임 동안 줄었다가 원래 크기로 돌아오거나, 최대화한 창이 화면을 채우지 못하고 줄어들 때 보는 문서입니다. 콘텐츠가 요구하는 창 높이를 얻으려고 `SizeToContent`를 `Height`로 바꾸고 `UpdateLayout`을 부르면, WPF는 높이를 계산만 하지 않고 창을 실제로 그 높이로 바꿉니다. 창이 콘텐츠보다 크면 줄었다가 되돌아가는 사이에 한 프레임이 그려집니다. 해결은 창 크기를 건드리지 않고 클라이언트 영역을 무한 높이로 잰 뒤, 지금의 제목 표시줄과 테두리 높이를 더하는 것입니다. 페이지마다 창 높이를 콘텐츠에 맞추는 WPF 앱에 해당합니다.

## 증상

- 창을 Waves 화면이 필요한 높이보다 키워 둔 채 Presets나 Settings에 갔다가 Waves로 돌아오면, 창이 한 프레임 동안 작아졌다가 다시 커집니다.
- 최대화한 상태에서 Waves로 돌아오면 창이 줄어든 채 남습니다. 창 상태는 여전히 최대화이고, 최대화를 풀면 창이 화면 높이만큼 커집니다.
- 창을 키우지 않았을 때는 생기지 않고, 바인딩 오류도 없었습니다.

겉보기가 비슷한 경우로, 창 크기는 그대로인데 페이지 콘텐츠만 잠깐 다른 배치로 그려지는 깜빡임이 있습니다. 아래 진단 1에서 창 크기 변경이 기록되지 않으면 이 문서의 원인이 아니므로 페이지 배치를 봅니다.

## 원인

이전 코드는 페이지를 띄울 때마다 다음처럼 필요한 높이를 쟀습니다.

```csharp
double current = window.ActualHeight;
window.SizeToContent = SizeToContent.Height;
window.UpdateLayout();
double required = window.ActualHeight;   // 콘텐츠에 맞춘 창 높이
window.SizeToContent = SizeToContent.Manual;
// ...
window.Height = Math.Max(current, required);
```

- `SizeToContent`가 `Height`인 창은 배치할 때마다 콘텐츠가 원하는 높이로 창을 맞춥니다. `UpdateLayout`이 그 배치를 바로 실행하므로, 이 코드는 높이를 재는 동안 창을 실제로 콘텐츠 높이로 바꿉니다.
- 창이 콘텐츠 높이와 같으면 크기가 바뀌지 않아 아무 일도 보이지 않습니다. 창을 키워 둔 경우에만 콘텐츠 높이로 줄었다가 마지막 줄에서 원래 높이로 돌아가고, 두 번의 크기 변경 사이에 화면이 그려지면 줄어든 창이 보입니다.
- 최대화 상태에서도 창을 콘텐츠 높이로 줄이고, 상태는 최대화로 남깁니다. 이어서 `Height`에 최대화한 높이를 넣으므로, 최대화를 풀 때 돌아갈 크기도 화면 높이로 바뀝니다.

## 진단 절차

1. 창 크기가 실제로 바뀌는 순간을 기록합니다. 창의 `HwndSource`에 메시지 후크를 걸어 `WM_WINDOWPOSCHANGED`의 높이를 남기면, 한 동작 안에서 일어난 크기 변경이 순서대로 남습니다. 속성 값을 시간 간격으로 기록하면 한 번의 처리 안에서 줄었다 커지는 변화는 잡히지 않습니다.
   ```csharp
   [StructLayout(LayoutKind.Sequential)]
   struct WindowPos { public IntPtr Hwnd, InsertAfter; public int X, Y, Cx, Cy; public uint Flags; }

   window.SourceInitialized += (_, _) =>
   {
       var source = (HwndSource)PresentationSource.FromVisual(window)!;
       source.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
       {
           const int WM_WINDOWPOSCHANGED = 0x0047, SWP_NOSIZE = 0x0001;
           if (msg == WM_WINDOWPOSCHANGED)
           {
               var pos = Marshal.PtrToStructure<WindowPos>(lParam);
               if ((pos.Flags & SWP_NOSIZE) == 0)
                   Debug.WriteLine($"height -> {pos.Cy * source.CompositionTarget!.TransformFromDevice.M22}");
           }
           return IntPtr.Zero;
       });
   };
   ```
2. 창 높이가 콘텐츠와 같을 때, 콘텐츠보다 클 때, 최대화했을 때 같은 동작을 반복해 비교합니다. 한 동작에 줄어드는 기록과 원래 높이로 돌아가는 기록이 이어서 남으면, 높이를 재는 코드가 창을 바꾸고 있는 것입니다.
3. 바인딩 추적이 깨끗한지도 봅니다. 이 증상은 바인딩 값과 무관하게 창 크기 변경으로 생기므로, 추적에 오류가 없으면 바인딩 쪽은 더 보지 않아도 됩니다.

## 해결 방법과 선택 기준

창 크기는 그대로 두고 클라이언트 영역(창 템플릿의 첫 시각 요소)만 지금 폭, 무한 높이로 잽니다. 거기에 지금 창 높이에서 클라이언트 영역 높이를 뺀 값, 곧 제목 표시줄과 테두리 높이를 더하면 `SizeToContent`가 계산하던 창 높이가 나옵니다.

```csharp
static double MeasureRequiredHeight(Window window)
{
    window.UpdateLayout();
    if (VisualTreeHelper.GetChild(window, 0) is not UIElement client) return window.ActualHeight;

    double nonClientHeight = window.ActualHeight - client.RenderSize.Height;
    client.Measure(new Size(client.RenderSize.Width, double.PositiveInfinity));
    double required = client.DesiredSize.Height + nonClientHeight;

    window.InvalidateMeasure();   // 다음 배치에서 실제 크기로 다시 재게 한다
    return required;
}
```

- 제목 표시줄과 테두리 높이를 지금의 배치에서 빼서 얻으므로, 테마나 배율, 창 스타일이 바뀌어도 값을 고칠 필요가 없습니다. 같은 높이를 상수나 시스템 값의 조합으로 계산하면 그런 변화마다 어긋날 수 있습니다.
- 무한 높이로 재면 `*` 행은 내용만큼, `ScrollViewer`는 내용 전체 높이만큼 원하므로 `SizeToContent`와 같은 값이 나옵니다. 이 저장소에서는 두 방법 모두 549.3을 냈습니다.
- 직접 부른 `Measure`의 결과가 다음 배치에 남지 않도록, 재고 난 뒤 창을 다시 재게 합니다.

창 높이를 바꾸는 쪽도 함께 고쳤습니다.

- 창이 필요한 높이보다 작을 때만 `Height`를 바꿉니다. 키워 둔 창에는 손대지 않습니다.
- 최대화나 최소화 상태에서는 재지도 바꾸지도 않습니다. 이 상태에서 `Height`를 바꾸면 복원할 때의 크기가 바뀝니다. 대신 `StateChanged`에서 보통 상태로 돌아올 때 다시 맞춥니다.
- 처음 연 창은 XAML의 `SizeToContent="Height"`로 첫 페이지에 맞추고, 첫 맞춤에서 `Manual`로 돌린 뒤로는 다시 켜지 않습니다.

한계는 다음과 같습니다.

- 무한 높이로 재면 목록은 항목을 모두 늘어놓은 높이를 원합니다. 항목 수만큼 길어지는 페이지는 이 저장소처럼 맞춤 대상에서 뺍니다(`WindowAutoFit.FitsContent="False"`).
- 지금 폭을 기준으로 잽니다. 폭이 바뀌어 줄바꿈이 달라지는 콘텐츠는 폭이 바뀐 뒤 다시 재야 하며, 이 저장소는 페이지를 띄울 때와 배율이 바뀔 때, 보통 상태로 돌아올 때만 잽니다.

## 검증과 회귀 확인

검증 프로그램에서 탭을 실제 클릭과 같은 경로(`ButtonBase.OnClick`)로 누르며 `WM_WINDOWPOSCHANGED`로 창 높이 변화를 기록했습니다.

| 상황 | 고치기 전 | 고친 뒤 |
|---|---|---|
| 앱 시작 | 550, 549.3 두 번 바뀜 | 549.3 한 번 |
| 기본 높이에서 Presets를 거쳐 Waves로 | 변화 없음 | 변화 없음 |
| 749.3으로 키운 창에서 Presets나 Settings를 거쳐 Waves로 | 550으로 줄었다가 749.3으로 돌아옴 | 변화 없음 |
| 최대화한 창에서 Presets를 거쳐 Waves로 | 550으로 줄어든 채 남음 | 변화 없음 |
| 최대화 해제 | 화면 높이(1422.7)로 복원 | 최대화 전 높이(749.3)로 복원 |
| 400으로 줄인 창에서 Presets를 거쳐 Waves로 | 확인하지 않음 | 549.3으로 한 번에 커짐 |

창 맞춤 코드를 고칠 때는 위 표의 상황을 같은 방법으로 다시 기록해, 한 동작에 크기 변경이 두 번 이상 남지 않는지 봅니다.

## 근거

- 적용한 코드: [WindowAutoFit.cs](../BrainWaves/BrainWaves/Behaviors/WindowAutoFit.cs), 첫 크기를 정하는 [MainWindow.xaml](../BrainWaves/BrainWaves/View/MainWindow.xaml)의 `SizeToContent="Height"`
- [Window.SizeToContent](https://learn.microsoft.com/dotnet/api/system.windows.window.sizetocontent), [Layout (WPF .NET)](https://learn.microsoft.com/dotnet/desktop/wpf/advanced/layout)
- [WM_WINDOWPOSCHANGED](https://learn.microsoft.com/windows/win32/winmsg/wm-windowposchanged), [WINDOWPOS](https://learn.microsoft.com/windows/win32/api/winuser/ns-winuser-windowpos)
