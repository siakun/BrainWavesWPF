# Frame으로 페이지를 오갈 때 화면 값이 사라지거나 탭과 어긋나는 문제

`Frame`으로 페이지를 바꾸는 WPF 앱에서, 한 페이지에서 바꾼 값이 다른 페이지에 갔다 오면 기본값으로 돌아가거나, 고른 탭과 보이는 페이지가 어긋날 때 보는 문서입니다. 주소(URI)로 탐색하는 `Frame`은 갈 때마다 페이지를 새로 만들고, 페이지 XAML에 선언한 ViewModel도 함께 새로 만듭니다. 그래서 ViewModel에 둔 상태는 페이지를 떠나는 순간 사라집니다. 또 `Frame`은 방문 기록을 남기므로, 내비게이션 막대를 감춰도 뒤로 가기 입력이 이전 페이지를 다시 띄웁니다. 해결은 화면을 오가도 남아야 하는 상태를 앱 수명 동안 하나뿐인 객체에 두고 페이지가 그 객체에 바인딩하게 하는 것, 그리고 탭으로만 화면을 고르는 앱이면 방문 기록을 남기지 않는 것입니다.

## 증상

- Waves 화면에서 재생하지 않은 채 전체 음량을 20으로, 왼쪽 주파수를 90으로 바꾼 뒤 Presets 탭에 갔다 돌아오면 50과 75로 돌아갔습니다.
- 재생 중에 Presets 탭을 떠났다 돌아오면 재생 중인 프리셋의 표시가 사라졌고, 그 프리셋을 다시 눌러도 멈추지 않고 다시 재생했습니다.
- Presets 탭을 고른 상태에서 뒤로 가기 명령이 오면 화면은 Waves로 돌아가는데 탭은 Presets에 남았습니다.

재생 중에 바꾼 값이 소리에 반영되지 않거나 다른 화면의 조작이 값을 덮어쓰는 문제도 같은 구조에서 나왔습니다. 이 앱은 값의 사본을 페이지마다 두고 메시지로 맞췄는데, 메시지를 보내는 경로가 일부 조작뿐이라 사본끼리 어긋났습니다.

## 원인

- 주소로 탐색하는 `Frame`은 페이지에 갈 때마다 BAML을 다시 읽어 새 인스턴스를 만듭니다. 페이지 XAML의 `<Page.DataContext><vm:WavesViewModel/></Page.DataContext>`도 그때마다 새 ViewModel을 만듭니다. 검증 프로그램에서 탭을 오간 뒤의 ViewModel은 앞의 것과 다른 인스턴스였고, 필드 초기값으로 돌아가 있었습니다.
- 앞의 ViewModel이 들고 있던 값을 새 ViewModel이 받아 오는 경로는 없습니다. 이 앱은 재생 중일 때만 오디오 서비스에서 값을 읽어 와, 재생하지 않는 동안 바꾼 값이 사라졌습니다. 재생 중인 프리셋도 Presets 화면의 ViewModel이 필드로 기억하고 있어서, 새로 만든 화면은 그것을 몰랐습니다.
- `Frame`은 `NavigationUIVisibility="Hidden"`이어도 자기 방문 기록을 남기고 `NavigationCommands.BrowseBack`을 처리합니다. 이 명령의 기본 단축키는 Alt+←와 Backspace입니다. 마우스의 뒤로 가기 버튼이 보내는 브라우저 뒤로 가기 신호(`APPCOMMAND_BROWSER_BACKWARD`)도 WPF가 같은 명령으로 바꿉니다. 탭을 `RadioButton`으로 직접 그린 앱은 이 탐색을 모르므로 탭과 화면이 어긋납니다.

## 진단 절차

1. 페이지를 오간 뒤 그 페이지의 `DataContext`가 앞의 것과 같은 인스턴스인지 봅니다(`ReferenceEquals`). 다르면 ViewModel에 둔 상태는 탭을 옮길 때마다 사라지는 구조입니다.
2. 사라지는 값이 어느 객체에 있는지 적어 봅니다. 화면을 떠나도 남아야 하는 값이 페이지 ViewModel의 필드에 있으면 이 문서의 원인입니다. 같은 값을 여러 객체가 나눠 들고 메시지나 이벤트로 맞추고 있으면, 맞추는 경로가 모든 조작을 덮는지도 봅니다.
3. 탭과 화면이 어긋나면 `Frame`에 `NavigationCommands.BrowseBack`을 실행해 봅니다. 화면이 바뀌고 탭이 그대로면 방문 기록 때문입니다.

실제 키보드와 마우스 없이 탭을 누르고 명령을 실행하는 방법은 [실제 백엔드 없이 WPF 화면 상태를 그려 확인하기](wpf-ui-state-verification-without-backend.md)에 있습니다.

## 해결 방법과 선택 기준

| 방법 | 결과 | 판단 |
|---|---|---|
| 상태를 앱 수명 동안 하나뿐인 객체에 두고, 페이지 ViewModel은 그 객체를 노출 | 페이지를 몇 번 새로 만들어도 같은 값을 봄 | 이 저장소에서 쓴 방법입니다. 값의 원본이 하나라 어느 경로로 바꿔도 모든 화면이 같은 값을 봅니다 |
| 페이지를 한 번 만들어 계속 재사용 | 페이지와 ViewModel이 남아 값도 남음 | 상태가 여전히 페이지에 묶여 있어, 다른 페이지가 같은 값을 쓰면 사본을 맞추는 문제가 그대로 남습니다. 이번에는 시험하지 않았습니다 |
| 사본마다 메시지로 값을 맞춤 | 보내는 경로를 빠뜨리면 사본끼리 어긋남 | 고치기 전의 구조입니다. 새 조작을 추가할 때마다 메시지를 보내야 하고, 빠뜨려도 드러나지 않습니다 |

선택한 방법에서 정한 기준은 다음과 같습니다.

- 소리 값(주파수, 채널 음량, 전체 음량, 재생 여부)의 원본은 오디오 서비스 하나입니다. 값을 바꾸면 재생 중인 소리에 바로 반영하고, 멈춰 있으면 다음 재생에 씁니다. 화면은 `{Binding Audio.MasterVolume}`처럼 ViewModel이 노출한 이 객체에 바로 바인딩합니다. 같은 앱의 업데이트 상태도 같은 방식으로 둡니다.
- 다른 화면의 표시가 그 상태에서 나온다면, 따로 기억하지 않고 상태에서 매번 계산합니다. 프리셋의 재생 표시는 "재생 중이고 두 채널 주파수가 그 프리셋과 같은가"로 정합니다. 그래서 새로 만든 Presets 화면도 표시가 맞고, Waves에서 주파수를 바꾸면 표시가 꺼집니다.
- 페이지 ViewModel이 앱 수명 객체의 변경 알림을 받을 때는 `PropertyChangedEventManager.AddHandler`처럼 약한 구독을 씁니다. 일반 이벤트 구독은 오래 사는 객체가 버려진 ViewModel을 계속 붙잡습니다.
- 값의 허용 범위도 원본 객체에 둡니다. 슬라이더의 `Minimum`, `Maximum`은 그 상수를 `x:Static`으로 읽습니다. 범위를 화면과 ViewModel에 따로 적으면, 버튼으로 옮긴 값이 슬라이더 끝을 넘어 손잡이와 숫자가 어긋납니다.
- 사용자가 다시 켜도 남기를 기대하는 값(이 앱에서는 전체 음량)은 설정 저장소에 저장합니다. 슬라이더를 끄는 동안에는 값이 잇달아 바뀌므로, 움직임이 멈추고 0.5초 뒤에 한 번 저장하고 앱을 끝낼 때 남은 저장을 마칩니다.

방문 기록은 페이지에 도착할 때마다 지웁니다.

```csharp
PageFrame.Navigated += (_, _) =>
{
    while (PageFrame.CanGoBack) PageFrame.RemoveBackEntry();
};
```

돌아갈 기록이 없으면 `BrowseBack`을 실행할 수 없으므로, 어떤 입력이 와도 화면이 탭과 따로 바뀌지 않습니다. 앱이 정말 방문 기록을 써야 한다면 반대로 탭의 선택을 지금 페이지에 바인딩해 탭이 화면을 따라가게 해야 합니다.

## 검증과 회귀 확인

검증 프로그램에서 실제 앱처럼 `Frame`이 주소로 페이지를 불러오게 두고, 탭은 마우스를 뗄 때와 같은 경로(`ButtonBase.OnClick`)로 눌렀습니다. 값은 화면의 슬라이더와 버튼을 바인딩 경로 그대로 움직여 바꿨습니다. 설정 저장은 임시 파일로 돌려 사용자의 설정 파일은 바꾸지 않았습니다.

| 조작 | 고치기 전 | 고친 뒤 |
|---|---|---|
| 재생하지 않은 채 전체 음량 20, 왼쪽 주파수 90으로 바꾼 뒤 Presets에 갔다 옴 | 50, 75로 돌아감 | 20, 90 그대로 |
| 전체 음량을 바꾸고 0.5초 뒤 | 저장하지 않음 | 설정 파일에 저장되고, 새로 읽은 설정도 같은 값 |
| 재생 중 왼쪽 주파수를 120으로 바꿈 | 소리에 반영되지 않음(코드에서 확인) | 소리를 만드는 쪽의 주파수가 120으로 바뀜 |
| 프리셋을 고름 | 전체 음량이 50%로 바뀜(코드에서 확인) | 주파수만 바뀌고 음량은 그대로 |
| 프리셋 재생 중 Waves에 갔다가 Presets로 돌아옴 | 재생 표시가 사라짐(코드에서 확인) | 표시가 그대로이고, 다시 누르면 멈춤 |
| Presets 탭을 고른 뒤 `BrowseBack` 실행 | 화면만 Waves로 바뀌고 탭은 Presets | 화면과 탭 모두 Presets |
| 전체 음량을 바꾸자마자 앱 종료 | 저장하지 않음 | 바꾼 값이 저장됨 |

재생은 전체 음량을 0%와 1%로 낮춰 들리지 않게 확인했습니다. 화면을 오가도 남아야 하는 값을 새로 추가할 때는, 그 값을 바꾼 뒤 탭을 오가고 같은 값이 보이는지 위와 같은 방법으로 확인합니다.

## 근거

- 적용한 코드
  - [AudioService.cs](../BrainWaves/BrainWaves/Services/AudioService.cs): 소리 값의 원본과 저장
  - [WavesViewModel.cs](../BrainWaves/BrainWaves/ViewModel/WavesViewModel.cs), [Waves.xaml](../BrainWaves/BrainWaves/View/Waves.xaml): `Audio` 노출과 바인딩
  - [PresetsViewModel.cs](../BrainWaves/BrainWaves/ViewModel/PresetsViewModel.cs): 재생 표시 계산과 약한 구독
  - [MainWindow.xaml.cs](../BrainWaves/BrainWaves/View/MainWindow.xaml.cs): 방문 기록 지우기
- 같은 원칙으로 둔 앞선 예: [UpdatesViewModel.cs](../BrainWaves/BrainWaves/ViewModel/UpdatesViewModel.cs)
- WPF 소스(dotnet/wpf)
  - [NavigationCommands.cs](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Input/Command/NavigationCommands.cs): `BrowseBack`의 기본 단축키 `Alt+Left;Backspace`
  - [CommandDevice.cs](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Input/Command/CommandDevice.cs): `APPCOMMAND_BROWSER_BACKWARD`를 `BrowseBack`으로 바꿈
- [Navigation overview (WPF .NET Framework)](https://learn.microsoft.com/dotnet/desktop/wpf/app-development/navigation-overview), [Weak event patterns](https://learn.microsoft.com/dotnet/desktop/wpf/events/weak-event-patterns)
