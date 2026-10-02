# 실제 백엔드 없이 WPF 화면 상태를 그려 확인하기

서버 응답이나 설치 상태가 갖춰져야만 나타나는 화면을, 그 조건을 실제로 만들지 않고 확인할 때 보는 문서입니다. 앱 어셈블리를 참조하는 작은 WPF 실행 파일(이하 검증 프로그램)에서 앱의 창과 페이지를 만들고, ViewModel 상태를 직접 바꾼 뒤 그 모습을 이미지로 남깁니다. 다른 실행 파일에서 앱의 페이지를 띄우면 `pack://application:,,,/` 주소가 앱이 아니라 검증 프로그램을 가리키므로 그 문제를 먼저 풀어야 합니다. 바인딩 오류 추적을 함께 켜면 화면에 드러나지 않는 결함도 잡힙니다.

## 언제 쓰나

- 상태가 외부 조건에 달린 화면을 볼 때. 이 저장소에서는 GitHub에 릴리스가 있어야 채워지는 버전 목록, 설치본에서 업데이트를 받아 둔 뒤에만 뜨는 알림 막대, 다운로드 진행률이 그런 화면이었습니다.
- 단위 테스트로는 바인딩 경로, 레이아웃, 스타일 적용을 확인할 수 없을 때.
- 실제 실행 파일을 조작하는 방법(아래 마지막 절)으로는 그 상태에 도달할 수 없을 때.
- 마지막에 쓴 입력 장치나 Windows 설정에 따라 달라지는 동작을, 실제 키보드와 마우스나 설정을 건드리지 않고 볼 때.

이 방법은 화면이 상태를 어떻게 그리는지를 확인합니다. 실제 서비스 호출, 스레드 전환 타이밍, 설치본에서만 참이 되는 조건(예: Velopack의 설치 판정)은 확인하지 못하므로, 그 부분은 패키징한 실행 파일로 따로 확인합니다.

## 구성

검증 프로그램은 앱 프로젝트를 참조하는 WPF 실행 파일입니다. 자체 `App.xaml`이 없으므로 기본 ApplicationDefinition을 끕니다.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms> <!-- 화면 복사에 System.Drawing을 쓴다 -->
    <EnableDefaultApplicationDefinition>false</EnableDefaultApplicationDefinition>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\<앱 경로>\<앱>.csproj" />
  </ItemGroup>
</Project>
```

`Main`은 앱의 `Application`을 만들어 리소스만 불러오고 `Run()`은 부르지 않습니다. `Run()`을 부르면 앱의 `OnStartup`이 자기 창을 따로 띄우고 백그라운드 작업을 시작하기 때문입니다. 대신 디스패처를 직접 돌려 창이 그려질 시간을 줍니다.

```csharp
[STAThread]
static void Main()
{
    UseAppAssemblyForPackUris(typeof(MyApp.App).Assembly); // 아래 "함정 1"

    var app = new MyApp.App();
    app.InitializeComponent();          // App.xaml의 리소스만 불러온다

    var window = new MyApp.View.MainWindow { Topmost = true };
    window.Show();
    Pump(1500);

    MyViewModel.Instance.ReadyVersion = "0.2.1";  // 보고 싶은 상태를 직접 넣는다
    Pump(600);
    Capture(window, "state-ready");
}

static void Pump(int milliseconds)
{
    var frame = new DispatcherFrame();
    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
    timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
    timer.Start();
    Dispatcher.PushFrame(frame);
}
```

상태는 명령을 실행해 만들지 말고 속성에 직접 넣습니다. 이 저장소의 프리셋 재생 표시는 재생 명령 대신 `IsPlaying` 속성만 켜서 그렸습니다. 명령을 실행하면 소리가 나거나 네트워크를 호출합니다. 페이지 생성자가 비동기 조회를 시작한다면(설정 화면의 버전 목록처럼) 그 조회가 끝난 뒤에 상태를 덮어씁니다. 조회가 끝나면서 목록을 새로 채우므로 먼저 넣은 값이 지워집니다.

다만 누르는 순간의 동작(애니메이션, 포커스)을 볼 때는 속성만 바꾸면 명령과 그 뒤에 이어지는 일이 빠져 결과가 달라집니다. 이 저장소의 탭 막대는 `IsChecked`만 바꿨을 때는 고르게 미끄러졌지만, 실제로 누르면 이어지는 페이지 전환에 움직임 앞부분이 가려졌습니다([원인과 해결](wpf-animation-skipped-while-ui-thread-busy.md)). 이런 동작은 마우스를 뗄 때 `ButtonBase`가 부르는 `OnClick`을 리플렉션으로 불러, 체크 변경과 `Click` 이벤트, 명령 실행이 실제와 같은 순서로 일어나게 해서 확인합니다. 소리를 내거나 외부를 호출하는 명령이 묶인 버튼에는 쓰지 않습니다.

## 함정 1: pack URI가 검증 프로그램을 가리킨다

**증상.** 창을 띄우자마자 `XamlParseException`이 나고 내부 예외가 `Item has already been added. Key in dictionary: '<키>'`입니다. 겹친 키는 첫 화면 페이지가 자기 `Page.Resources`에 정의한 첫 리소스의 키였습니다. 스택에는 `NavigationService.DoNavigate(Uri ...)`와 BAML 로드가 보입니다. 앱의 `Frame`이 `pack://application:,,,/View/Waves.xaml` 같은 주소로 첫 페이지를 불러오는 중이었습니다.

**관찰한 것.**

| 시도 | 결과 |
|---|---|
| `Application.ResourceAssembly`에 앱 어셈블리를 넣음 | `InvalidOperationException`. API 문서는 진입 어셈블리가 있는 앱에서 이 setter가 예외를 던진다고 적고 있습니다 |
| 리플렉션으로 `Application`의 `_resourceAssembly`만 바꿈 | `App.xaml`의 리소스는 읽었지만 같은 오류 |
| 창을 만든 뒤 `Frame`의 `Navigating`에서 URI 탐색 취소, 바인딩 해제와 `StopLoading` | 같은 오류. 스택에 `NavigateQueueItem`이 보여, 창 생성자에서 바인딩이 `Source`를 정할 때 이미 대기열에 들어간 탐색으로 보입니다 |
| `System.Windows.Navigation.BaseUriHelper`(PresentationCore)의 `_resourceAssembly`까지 바꿈 | 모든 페이지가 정상으로 열림 |

```csharp
static void UseAppAssemblyForPackUris(Assembly appAssembly)
{
    const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
    typeof(Application).GetField("_resourceAssembly", flags)!.SetValue(null, appAssembly);
    typeof(Visual).Assembly.GetType("System.Windows.Navigation.BaseUriHelper")!
        .GetField("_resourceAssembly", flags)!.SetValue(null, appAssembly);
}
```

**해석.** WPF는 `pack://application:,,,/View/Waves.xaml`처럼 어셈블리 이름이 없는 리소스 주소를 실행 중에 진입 어셈블리 기준으로 해석합니다([Application.ResourceAssembly](https://learn.microsoft.com/dotnet/api/system.windows.application.resourceassembly)). 검증 프로그램에서는 진입 어셈블리가 검증 프로그램 자신이라 이 기준이 어긋나, 탐색이 불러온 BAML과 페이지 생성자의 `InitializeComponent`가 불러오는 BAML을 WPF가 같은 것으로 보지 못하고 한 인스턴스에 두 번 채운 것으로 보입니다. 페이지 자신의 리소스 키가 겹쳤다는 점이 이 해석과 맞지만, WPF가 둘을 비교하는 내부 로직은 소스로 확인하지 않았습니다.

**한계와 대안.** 비공개 필드 이름에 기대므로 WPF 버전이 바뀌면 깨질 수 있고, 검증 프로그램에서만 씁니다. 앱의 탐색 주소에 어셈블리 이름을 넣으면(`pack://application:,,,/<어셈블리>;component/View/Waves.xaml`) 진입 어셈블리와 무관하게 해석되어 이 조치가 필요 없을 것으로 예상하지만, 이번에는 앱 코드를 바꾸지 않아 확인하지 않았습니다.

## 함정 2: 무엇이 이미지에 담기나

| 방법 | 담기는 것 | 담기지 않는 것 |
|---|---|---|
| `RenderTargetBitmap`으로 `window.Content`를 그림 | 창 안쪽. 다른 창이 가려도 그대로 그려집니다 | 제목 표시줄, `Popup`(콤보 상자의 목록, 도구 설명), 포커스 테두리 같은 장식(adorner) |
| 화면에서 창 영역을 복사(`Graphics.CopyFromScreen`) | 제목 표시줄과 `Popup`까지 화면에 보이는 그대로 | 창을 가린 다른 창이 대신 찍힙니다 |

장식은 창 템플릿의 `AdornerDecorator`가 콘텐츠 위에 둔 장식 층에 그려지므로 `window.Content`에는 없습니다. 창의 첫 시각 요소(`VisualTreeHelper.GetChild(window, 0)`)부터 `RenderTargetBitmap`으로 그리면 함께 담깁니다. `Popup`은 창과 별개의 최상위 창에 그려지므로 창의 시각 트리를 그리는 `RenderTargetBitmap`에는 나오지 않습니다. 드롭다운을 연 상태는 화면 복사로 찍었습니다. 화면 복사를 쓸 때는 창을 `Topmost`로 올립니다. 올리지 않았을 때는 앞에 떠 있던 다른 프로그램의 창이 대신 찍혔습니다.

창 영역은 `GetWindowRect` 대신 `DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS = 9, ...)`로 얻습니다. Windows 10 이후의 창 바깥에는 크기 조절용 투명 테두리가 있고, WPF의 `Width`와 `GetWindowRect`는 그 테두리까지 포함합니다. 100% 배율에서 `Width`가 450인 창을 이 값으로 복사하면 436픽셀 폭으로, 좌우 투명 테두리를 뺀 보이는 창만 담겼습니다.

제목 표시줄의 아이콘은 실행 중인 실행 파일의 아이콘이라, 검증 프로그램에서는 앱 아이콘 대신 기본 아이콘이 나옵니다. README용 이미지처럼 제목 표시줄까지 보일 때는 `window.Icon`에 앱 아이콘 파일을 지정합니다.

## 바인딩 오류 추적

화면이 그럴듯해 보여도 바인딩 경로가 틀린 곳은 값이 비어 있을 뿐 예외가 나지 않습니다. 검증 프로그램에서 WPF 추적 출력을 파일로 모으면 모든 상태를 거치는 동안 생긴 오류를 한 번에 봅니다.

```csharp
var trace = new TextWriterTraceListener(Path.Combine(outDir, "binding-trace.log"));
PresentationTraceSources.Refresh();
PresentationTraceSources.DataBindingSource.Listeners.Add(trace);
PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
PresentationTraceSources.ResourceDictionarySource.Listeners.Add(trace);
PresentationTraceSources.ResourceDictionarySource.Switch.Level = SourceLevels.Warning;
Trace.AutoFlush = true;

// 추적이 실제로 켜졌는지 확인하는 표본. 로그에 이 오류가 있어야 "오류 없음"을 믿을 수 있다.
var probe = new TextBlock { DataContext = new object() };
probe.SetBinding(TextBlock.TextProperty, new Binding("DeliberatelyMissingProperty"));
```

로그가 비어 있는 것만으로는 오류가 없다고 판단할 수 없습니다. 추적이 켜지지 않아도 로그는 비기 때문입니다. 일부러 틀린 바인딩 하나를 넣어 그 한 줄(`BindingExpression path error: 'DeliberatelyMissingProperty' ...`)만 남는지 확인합니다. 이 저장소에서는 표본 한 줄 외에 오류가 없음을 이 방법으로 확인했습니다.

## 입력 장치에 따라 달라지는 동작

포커스 테두리처럼 WPF가 마지막에 쓴 입력 장치(`InputManager.MostRecentInputDevice`)로 판단하는 동작은 속성을 바꾸거나 `RaiseEvent`로 이벤트를 흘려서는 재현되지 않습니다. 이 값은 키보드와 마우스 장치가 OS에서 들어온 원시 입력 보고를 처리할 때만 바뀌기 때문입니다. 검증 프로그램에서는 그 원시 보고를 직접 만들어 `InputManager.ProcessInput`에 넣습니다. 실제 키보드와 마우스를 쓰지 않으므로 다른 창에 입력이 들어가지 않습니다.

보고 형식은 PresentationCore의 비공개 형식이라 리플렉션으로 만듭니다.

```csharp
const BindingFlags instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
var core = typeof(InputManager).Assembly;
var reportArgsType = core.GetType("System.Windows.Input.InputReportEventArgs")!;
var previewReport = (RoutedEvent)typeof(InputManager)
    .GetField("PreviewInputReportEvent", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
var source = PresentationSource.FromVisual(window)!;

void Report(InputDevice device, object report)
{
    var args = (InputEventArgs)Activator.CreateInstance(reportArgsType, instance, null, new[] { device, report }, null)!;
    args.RoutedEvent = previewReport;
    InputManager.Current.ProcessInput(args);
}

// 키 하나를 누른다. 떼는 보고는 "KeyUp"으로 한 번 더 보낸다. Alt 조합이면 isSystemKey 자리에 true를 준다.
var keyboardReport = core.GetType("System.Windows.Input.RawKeyboardInputReport")!;
var keyboardActions = core.GetType("System.Windows.Input.RawKeyboardActions")!;
Report(Keyboard.PrimaryDevice, Activator.CreateInstance(keyboardReport, instance, null, new object[]
{
    source, InputMode.Foreground, Environment.TickCount, Enum.Parse(keyboardActions, "KeyDown"),
    0 /* scanCode */, false /* isExtendedKey */, false /* isSystemKey */, KeyInterop.VirtualKeyFromKey(Key.Tab), IntPtr.Zero
}, null)!);

// 요소를 마우스로 누른다. x, y는 창 클라이언트 영역의 물리 픽셀이다(DIP 좌표에 DPI 배율을 곱한 값).
var mouseReport = core.GetType("System.Windows.Input.RawMouseInputReport")!;
var mouseActions = core.GetType("System.Windows.Input.RawMouseActions")!;
foreach (var actions in new[] { "Activate, AbsoluteMove", "Button1Press", "Button1Release" })
    Report(Mouse.PrimaryDevice, Activator.CreateInstance(mouseReport, instance, null, new object[]
    {
        InputMode.Foreground, Environment.TickCount, source, Enum.Parse(mouseActions, actions), x, y, 0, IntPtr.Zero
    }, null)!);
```

보고마다 짧게 디스패처를 돌려(`Pump`) 앞 입력이 처리된 뒤 다음 입력을 넣습니다. 확인한 주의점은 다음과 같습니다.

- 마우스 장치는 버튼이 눌렸는지를 OS에서 다시 읽으므로, 실제 버튼이 눌려 있지 않으면 이벤트의 `ButtonState`가 `Released`로 나옵니다. 그래서 `ButtonBase`는 포커스만 가져가고 누름과 클릭은 일어나지 않습니다. 버튼에 묶인 명령은 실행되지 않으므로, 클릭 결과까지 보려면 `IInvokeProvider`나 위 구성 절의 `OnClick` 호출로 따로 누릅니다.
- 키 이벤트는 키보드 포커스를 가진 요소로 갑니다. 포커스가 없으면 받는 요소가 없으므로 먼저 요소에 포커스를 줍니다. 검증 프로그램 창이 OS의 활성 창이 아닐 때도 이 경로로 포커스 이동과 키 처리가 동작했습니다.
- 창이 포커스를 잃었다가 되찾는 상황은 키보드 보고의 `Deactivate`와 `Activate`로 키보드 포커스를 비우고 되살린 뒤, 비우기 전의 요소에 `Keyboard.Focus`를 불러 흉내 냅니다. WPF가 창 포커스를 되찾을 때 하는 순서와 같습니다.

생성자 인자 순서와 형식 이름은 비공개 구현이라 WPF 버전이 바뀌면 깨질 수 있습니다. 실패하면 dotnet/wpf 저장소의 `RawKeyboardInputReport.cs`, `RawMouseInputReport.cs`에서 현재 생성자를 확인합니다.

## Windows 설정에 따라 달라지는 동작

`SystemParameters`의 값은 처음 읽을 때 OS에서 가져와 비공개 정적 필드에 담아 두고 이후에는 그 필드를 돌려줍니다. 검증 프로그램에서는 한 번 읽어 필드를 채운 뒤 리플렉션으로 값을 바꿔 두 설정을 모두 확인하고, 끝나면 원래 값으로 돌려놓습니다. Windows 설정 자체는 바꾸지 않습니다.

```csharp
var original = SystemParameters.ClientAreaAnimation;   // 여기서 필드가 채워진다
var field = typeof(SystemParameters).GetField("_clientAreaAnimation", BindingFlags.NonPublic | BindingFlags.Static)!;
field.SetValue(null, true);                             // 애니메이션 효과를 켠 PC처럼 동작
// ... 확인 ...
field.SetValue(null, original);
```

코드에서 그때그때 `SystemParameters`를 읽는 동작에는 바로 반영됩니다. 이 저장소에서는 필드를 바꾸자 같은 실행 안에서 그 값을 읽는 코드의 분기가 바뀌는 것으로 확인했습니다. XAML에서 `{x:Static SystemParameters...}`로 읽는 값은 그 XAML을 불러올 때 정해지므로, 필드를 바꾼 뒤에 화면을 만듭니다. 필드 이름도 비공개 구현이므로, 찾지 못하면 `SystemParameters`의 비공개 정적 필드 가운데 설정 이름이 들어간 것을 나열해 찾습니다.

## 실제 실행 파일을 조작해 찍을 때

앱이 실제로 도달하는 상태(첫 화면, 재생 중 화면, 탭 이동)는 빌드한 실행 파일을 UI Automation으로 조작해 찍는 편이 앱의 시작 경로까지 함께 확인됩니다. 이때 확인한 주의점은 다음과 같습니다.

- 요소는 이름과 컨트롤 종류를 함께 조건으로 찾습니다. 이름만으로 찾자 탭 버튼 대신 같은 글자를 가진 페이지 제목 `TextBlock`이 먼저 잡혔습니다.
- 앱 창을 `SetWindowPos(hwnd, HWND_TOPMOST, ...)`로 맨 위에 고정합니다. 다른 창이 가리면 캡처와 마우스 클릭이 그 창으로 갑니다.
- 소리를 내는 동작은 먼저 음량을 0으로 내린 뒤 실행합니다. 이 저장소에서는 전체 음량 슬라이더에 `RangeValuePattern.SetValue(0)`을 준 뒤 재생 버튼을 `InvokePattern`으로 눌렀습니다.
- 커서 이동, 버튼 누름, 뗌을 지연 없이 연달아 보냈을 때는 첫 탭 클릭은 들어가고 두 번째 탭 클릭이 먹히지 않았습니다. 창을 맨 위에 고정한 뒤에도 같았고, 세 동작 사이에 100밀리초 안팎의 지연을 두자 이후 모든 클릭이 들어갔습니다. 비교한 횟수가 적어 원인으로 단정하지는 않습니다. 클릭 뒤에는 탭의 `SelectionItemPattern.IsSelected`로 실제로 선택됐는지 확인합니다.

## 근거

- 화면 상태를 들고 있는 ViewModel: [UpdatesViewModel.cs](../BrainWaves/BrainWaves/ViewModel/UpdatesViewModel.cs)
- 첫 페이지를 주소로 불러오는 창: [MainWindow.xaml](../BrainWaves/BrainWaves/View/MainWindow.xaml), [MainViewModel.cs](../BrainWaves/BrainWaves/ViewModel/MainViewModel.cs)
- [Application.ResourceAssembly](https://learn.microsoft.com/dotnet/api/system.windows.application.resourceassembly), [Pack URIs in WPF](https://learn.microsoft.com/dotnet/desktop/wpf/app-development/pack-uris-in-wpf), [RenderTargetBitmap](https://learn.microsoft.com/dotnet/api/system.windows.media.imaging.rendertargetbitmap)
