- 이 파일이 담당하는 것: BrainWavesWPF의 구조, 화면과 창 크기 정책, 디자인 시스템과 자동 업데이트가 연결된 자리, 이 저장소의 브랜치 전략입니다.
- 위치만 참조하는 것: 빌드와 릴리스 명령은 `.claude/build_and_release.md`에, 릴리스 내역 작성 기준은 `.agents/skills/release-notes/SKILL.md`에, 색과 글꼴 값은 `BrainWaves/BrainWaves/Resources/Theme/Palette.xaml`에, 재사용할 기술 레퍼런스는 `docs/`에 있습니다.
- 담지 않는 것: 커밋 규칙과 푸시 정책처럼 모든 저장소에 공통인 규칙, Siakun.AutoUpdate와 Velopack의 내부 동작입니다.

# BrainWavesWPF
BrainWaves는 바이노럴 비트를 생성하는 WPF 데스크톱 애플리케이션입니다.

STACK: .NET 8, WPF, MVVM

View/          : XAML UI (프레임 네비게이션)
ViewModel/     : 화면 로직과 상태
Model/         : 프리셋, 뇌파 대역, 사용자 설정
Services/      : 오디오 재생, 설정 저장, 업데이트 연결
Converters/    : 값 변환기
Behaviors/     : 첨부 속성으로 붙이는 UI 동작
Resources/     : 색, 글꼴, 컨트롤 스타일, 앱 아이콘



양쪽 귀에 약간 다른 주파수의 오디오를 재생하여 뇌파 패턴에 영향을 줄 수 있는 비트를 생성합니다.
.NET 8.0으로 빌드되었으며 MVVM 아키텍처를 따라야합니다.

## 네비게이션 아키텍처
앱은 MainViewModel이 제어하는 프레임 기반 네비게이션을 사용합니다. 네비게이션 명령은 메인 윈도우의 네비게이션 바를 유지하면서 세 페이지 간을 전환합니다. 각 페이지는 중앙 프레임 요소에 로드됩니다.

### Project Features
- 좌우 채널을 색으로 구분하는 어두운 테마 UI
- 13가지 사전 설정된 뇌파 상태 (집중, 수면, 명상 등)
- 실시간 주파수 조절 및 볼륨 컨트롤
- 뇌파 대역을 그리스 문자(δ, θ, α, β, γ)로 표시하고 프리셋을 대역별로 묶음
- 별표한 프리셋을 목록 맨 위 Favorites에 모아 표시
- GitHub 릴리스를 통한 자동 업데이트와 버전 선택

## Project Architecture
애플리케이션은 세 가지 주요 레이어로 구성된 MVVM 패턴을 따릅니다:

## Project Library Dependencies
버전은 `BrainWaves/BrainWaves/BrainWaves.csproj`가 원본입니다.
- **CommunityToolkit.Mvvm** - 소스 생성기 기반 MVVM 프레임워크, ObservableObject, RelayCommand, Messaging 제공
- **MaterialDesignThemes**, **MaterialDesignColors** - 아이콘(PackIcon)과 기본 컨트롤 스타일
- **NAudio** - 좌우 채널 사인파의 실시간 재생
- **Siakun.AutoUpdate** - GitHub 릴리스 조회, 업데이트 다운로드, 버전 전환. Velopack은 이 패키지의 의존성으로 들어오므로 앱에서 따로 참조하지 않습니다


### 진입점
- `Program.cs` - Velopack 초기화를 WPF보다 먼저 실행한 뒤 App을 띄웁니다. 프로젝트 파일의 `StartupObject`가 이 클래스를 진입점으로 지정하고, `App.xaml`은 XAML 디자이너가 앱 리소스를 읽도록 ApplicationDefinition으로 둡니다.
- `AppInfo.cs` - 저장소 주소, 개발 빌드 버전, 설정 폴더처럼 여러 곳이 함께 쓰는 고정 정보

### Services (`/Services/`)
- `AudioService.cs` - 재생 상태를 가진 싱글톤 오디오 재생 관리자. 동시 재생 방지와 디바운싱 포함
- `SettingsStore.cs` - 사용자 설정을 `%AppData%\BrainWaves\settings.json`에 읽고 저장합니다. 설정 값의 원본은 여기 하나입니다.
- `AppUpdates.cs` - Siakun.AutoUpdate의 `UpdateService`에 이 앱의 저장소 주소와 설정 저장을 연결합니다.

### Model (`/Model/`)
- `PresetData.cs` - 좌우 주파수와 그 차이(비트)로 정해지는 프리셋. 즐겨찾기를 저장할 때는 표시 이름이 아니라 바꾸지 않는 `Id`를 씁니다.
- `BrainwaveBand.cs` - 비트가 속하는 뇌파 대역의 경계, 기호, 설명. 대역 판정은 이 표 한 곳에서만 합니다.
- `AppSettings.cs` - 자동 업데이트, 베타 수신, 즐겨찾기 같은 사용자 설정. 이 버전이 모르는 항목도 저장할 때 지우지 않습니다.

### View (`/View/`)
- `MainWindow.xaml` - 프레임, 업데이트 알림 막대, 하단 탭
- `Waves.xaml` - 지금 들리는 비트와 재생 버튼, 전체 음량, 좌우 채널 조절
- `ChannelCard.xaml` - 한 채널의 주파수와 음량 카드. Waves가 좌우에 하나씩 놓습니다.
- `Presets.xaml` - 사전 구성된 주파수 조합을 뇌파 대역별로 묶은 목록. 별표한 프리셋은 맨 위 Favorites에 한 번 더 보입니다.
- `Settings.xaml` - 업데이트 설정과 버전 선택, 앱 소개, 오픈소스 라이브러리, GitHub 링크

### ViewModel (`/ViewModel/`)
- `MainViewModel.cs` - 네비게이션 처리 및 프리셋 컬렉션 관리 (집중, 수면, 명상 등 13개의 사전 구성 상태)
- `WavesViewModel.cs` - 주파수 조절, 재생/정지, 볼륨 컨트롤 관리. WeakReferenceMessenger를 통한 프리셋 선택 수신
- `PresetsViewModel.cs` - 프리셋 목록 관리 및 선택 시 재생 토글 기능. PresetDataViewModel로 UI 상태 확장. 즐겨찾기 상태는 `SettingsStore`에 저장된 값에서 매번 다시 계산합니다.
- `SettingsViewModel.cs` - 설정 화면의 링크와 오픈소스 목록
- `UpdatesViewModel.cs` - 업데이트 상태와 조작. 앱 전체에 하나만 두고 메인 창의 알림 막대와 설정 화면이 함께 봅니다.

### Converters (`/Converters/`)
- `BoolToPlayStopTextConverter.cs` - 재생 상태에 따른 텍스트 변환

### Behaviors (`/Behaviors/`)
- `WindowAutoFit.cs` - 창의 최소 크기와 시작 크기를 콘텐츠가 요구하는 크기에서 도출한다. 창에 `WindowAutoFit.IsEnabled`를 붙이면 페이지를 띄울 때마다 다시 측정한다. 목록처럼 항목 수만큼 길어지는 페이지는 `WindowAutoFit.FitsContent="False"`로 선언해 창이 그 길이를 따라가지 않게 한다. 측정은 창 크기를 바꾸지 않고 하며, 최대화나 최소화한 동안에는 맞추지 않고 보통 상태로 돌아올 때 맞춘다.
- `TitleBar.cs` - Windows가 그리는 제목 표시줄의 색을 앱 색에 맞춘다. 제목 표시줄을 직접 그리지 않으므로 창 이동과 스냅 동작은 Windows 기본 그대로다.
- `SlidingTabIndicator.cs` - 고른 탭을 가리키는 막대 하나를 그 탭의 자리와 폭으로 옮깁니다. 탭이 바뀌면 그 탭이 띄우는 페이지가 뜬 뒤 미끄러지듯 옮겨 가고, Windows 애니메이션 효과 설정과 무관하게 움직입니다.
- `FocusCue.cs` - 키보드 포커스 테두리를 마지막 조작에 맞춰 보이거나 숨깁니다. 창에 `FocusCue.IsEnabled`를 붙이면 키보드로 조작할 때만 테두리가 보이고 마우스로 누른 뒤에는 숨습니다.

## 창 크기 정책
창 크기를 사람이 고른 값으로 두면 폰트, DPI 배율, 요소 추가로 콘텐츠 요구 높이가 바뀔 때 조용히 어긋나 스크롤바가 생긴다. 그래서 `MainWindow`는 높이를 지정하지 않고 `WindowAutoFit`이 측정한 값을 쓴다. 각 페이지의 ScrollViewer는 지우지 않고 안전망으로 남긴다. 콘텐츠 요구가 화면 작업 영역을 넘어 창을 더 키울 수 없을 때 요소가 잘리지 않게 받아내는 역할이다.

새 페이지를 추가할 때는 선언 없이 두면 콘텐츠가 모두 보이도록 창이 맞춰진다. 항목 수에 따라 길어지는 페이지만 `FitsContent="False"`를 붙인다.

### 핵심 기능
- `PlaySound.cs` - NAudio로 좌우 채널에 서로 다른 주파수의 사인파를 실시간으로 생성해 재생

## 자동 업데이트
배포는 Velopack 설치 패키지로 하고, 업데이트 확인과 다운로드, 버전 전환, 종료 시 적용은 Siakun.AutoUpdate가 맡습니다. 앱은 다음만 연결합니다.

- `Program.Main`이 가장 먼저 `VelopackApp`을 실행합니다. 시작할 때 자동 적용은 끕니다.
- `App.OnStartup`이 창을 그린 뒤 백그라운드 확인을 시작하고, 라이브러리의 재시작 요청을 받으면 정상 종료합니다.
- `App.OnExit`이 소리를 멈춘 뒤 받아 둔 업데이트의 적용을 예약합니다.
- 설정 화면과 알림 막대는 `UpdatesViewModel`의 상태만 봅니다. 설정 값은 `SettingsStore`가 원본이므로 화면이 값을 따로 들고 있다가 덮어쓰지 않게 합니다.

릴리스에 올리는 파일 구성은 라이브러리가 기대하는 형태를 따라야 합니다. 절차와 이유는 `.claude/build_and_release.md`와 `.github/workflows/release.yml`의 주석에 있습니다.

## External Instructions
@.claude/build_and_release.md

## 릴리스 내역
릴리스마다 `docs/releases/<버전>.md`를 [release-notes 스킬](.agents/skills/release-notes/SKILL.md)의 작성 기준에 따라 작성합니다. 배포 워크플로는 이 파일이 없으면 게시하지 않고, GitHub 릴리스 본문과 설치 패키지에 같은 파일을 씁니다.

## 기술 레퍼런스
이 저장소에서 원인을 확인한 재사용할 WPF 지식은 `docs/`에 있습니다. 화면 스타일이나 화면 검증에서 비슷한 증상을 조사하기 전에 먼저 봅니다.

### 디자인 시스템
색, 글꼴, 컨트롤 스타일은 `Resources/`의 사전에만 정의하고 화면은 그 키를 참조합니다. 색 값을 XAML에 직접 적지 않습니다.
- 색은 역할로 나눕니다. 채도가 있는 색은 좌우 채널을 표시하는 두 가지(`ChannelLeft`, `ChannelRight`)뿐이고, 화면 어디에서든 같은 채널을 뜻합니다.
- 뇌파 대역은 색이 아니라 `BrainwaveBand`의 그리스 문자로 구분합니다.
- 주 동작(재생)은 채널 색이 아니라 가장 밝은 글자 색으로 칠합니다.
- 숫자와 제목은 `Font.Display`(Bahnschrift), 설명과 목록은 `Font.Body`(Segoe UI)를 씁니다.
- 아이콘은 Material Design 아이콘(PackIcon)을 사용합니다.
- 누르는 컨트롤의 `FocusVisualStyle`은 `ButtonStyles.xaml`의 `FocusVisual`이나 `FocusVisual.Pill`로 지정합니다. 이 두 스타일만 `FocusCue`를 따르므로, WPF 기본 점선 테두리를 쓰는 컨트롤은 마우스로 누른 뒤에도 테두리가 남을 수 있습니다.

## 메시징 패턴

앱은 CommunityToolkit.Mvvm의 WeakReferenceMessenger를 사용하여 컴포넌트 간 통신:

- `PresetSelectedMessage`: 프리셋 선택 시 주파수 데이터 전달
- `PlaybackStateChangedMessage`: 재생 상태 변경 알림
- `AudioParametersChangedMessage`: 주파수/볼륨 변경 알림

## GitHub 리포지토리

https://github.com/siakun/BrainWavesWPF

## 브랜치 전략: GitHub Flow

이 저장소는 GitHub Flow를 씁니다. main은 항상 배포 가능한 상태로 두고, 모든 작업은 main에서 딴 작업 브랜치에서 합니다.

전역 규칙의 develop 브랜치 운용은 이 저장소에 적용하지 않습니다. 이 절이 우선합니다. "기본 브랜치 직접 커밋 금지"는 그대로 지키되, 커밋할 곳은 develop이 아니라 그 작업의 브랜치입니다.

작업마다 main에서 브랜치를 따고, 끝나면 PR로 main에 합친 뒤 브랜치를 지웁니다. 오래 사는 브랜치를 두지 않습니다.

커밋 메시지 컨벤션, 작성자 표기, 푸시 정책은 전역 규칙을 그대로 따릅니다. push는 사용자가 직접 합니다.

## Project Tree
BrainWavesWPF
├─ .agents
│  └─ skills
│     └─ release-notes
│        └─ SKILL.md
├─ .claude
│  ├─ build_and_release.md
│  ├─ compact_summary.md
│  └─ settings.local.json
├─ .config
│  └─ dotnet-tools.json
├─ .github
│  └─ workflows
│     └─ release.yml
├─ BrainWaves
│  ├─ BrainWaves
│  │  ├─ App.xaml
│  │  ├─ App.xaml.cs
│  │  ├─ AppInfo.cs
│  │  ├─ AssemblyInfo.cs
│  │  ├─ Behaviors
│  │  │  ├─ FocusCue.cs
│  │  │  ├─ SlidingTabIndicator.cs
│  │  │  ├─ TitleBar.cs
│  │  │  └─ WindowAutoFit.cs
│  │  ├─ BrainWaves.csproj
│  │  ├─ Converters
│  │  │  └─ BoolToPlayStopTextConverter.cs
│  │  ├─ Model
│  │  │  ├─ AppSettings.cs
│  │  │  ├─ BrainwaveBand.cs
│  │  │  └─ PresetData.cs
│  │  ├─ PlaySound.cs
│  │  ├─ Program.cs
│  │  ├─ Resources
│  │  │  ├─ Animations
│  │  │  │  └─ Storyboards.xaml
│  │  │  ├─ BrainWaves.ico
│  │  │  ├─ Styles
│  │  │  │  ├─ ButtonStyles.xaml
│  │  │  │  ├─ CardStyles.xaml
│  │  │  │  ├─ ComboBoxStyles.xaml
│  │  │  │  ├─ SliderStyles.xaml
│  │  │  │  └─ TextStyles.xaml
│  │  │  └─ Theme
│  │  │     └─ Palette.xaml
│  │  ├─ Services
│  │  │  ├─ AppUpdates.cs
│  │  │  ├─ AudioService.cs
│  │  │  └─ SettingsStore.cs
│  │  ├─ View
│  │  │  ├─ ChannelCard.xaml
│  │  │  ├─ ChannelCard.xaml.cs
│  │  │  ├─ MainWindow.xaml
│  │  │  ├─ MainWindow.xaml.cs
│  │  │  ├─ Presets.xaml
│  │  │  ├─ Presets.xaml.cs
│  │  │  ├─ Settings.xaml
│  │  │  ├─ Settings.xaml.cs
│  │  │  ├─ Waves.xaml
│  │  │  └─ Waves.xaml.cs
│  │  └─ ViewModel
│  │     ├─ MainViewModel.cs
│  │     ├─ PresetsViewModel.cs
│  │     ├─ SettingsViewModel.cs
│  │     ├─ UpdatesViewModel.cs
│  │     └─ WavesViewModel.cs
│  └─ BrainWaves.sln
├─ build.bat
├─ CLAUDE.md
├─ docs
│  ├─ releases
│  │  └─ <버전>.md
│  ├─ wpf-implicit-style-inside-control-template.md
│  └─ wpf-ui-state-verification-without-backend.md
├─ Images
│  ├─ program_presets.png
│  ├─ program_settings.png
│  └─ program_waves.png
├─ LICENSE
└─ README.md
