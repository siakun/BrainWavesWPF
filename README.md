<h1 align="center">BrainWaves</h1>

<p align="center">
  <b>왼쪽 귀에 75 Hz, 오른쪽 귀에 73 Hz를 들려주면 머릿속에서 1초에 두 번씩 커졌다 작아지는 소리가 들립니다.</b><br>
  이 소리를 바이노럴 비트라고 합니다. BrainWaves는 바이노럴 비트를 들려주는 Windows 앱입니다.<br>
  수면, 명상, 집중용 프리셋이 들어 있어 헤드폰을 쓰고 하나만 누르면 됩니다.
</p>

<p align="center">
  <img src="Images/program_waves.png" width="320" alt="Waves 화면. 왼쪽 75 Hz와 오른쪽 73 Hz로 만든 2.00 Hz 델타 대역 비트">
</p>

<p align="center">
  <a href="https://github.com/siakun/BrainWavesWPF/releases/latest"><img src="https://img.shields.io/github/v/release/siakun/BrainWavesWPF?style=for-the-badge&label=Windows%EC%9A%A9%20%EB%82%B4%EB%A0%A4%EB%B0%9B%EA%B8%B0&color=5ccfe6" alt="Windows용 내려받기"></a>
</p>

## 시작하기

1. [최신 릴리스](https://github.com/siakun/BrainWavesWPF/releases/latest)에서 `BrainWaves-Setup-<버전>-x64.exe`를 받아 실행합니다. 설치가 끝나면 시작 메뉴에 등록됩니다.
2. 헤드폰이나 이어폰을 씁니다. 두 귀에 서로 다른 소리가 따로 들어가야 바이노럴 비트가 생기므로 스피커로는 들을 수 없습니다.
3. Presets 탭에서 프리셋을 누릅니다. 다시 누르면 멈춥니다.

## 할 수 있는 것

- **대역별 프리셋**

  Deep Sleep부터 Meditation, Focus, Cognitive tasks까지 프리셋이 뇌파 대역(δ, θ, α, β, γ)별로 묶여 있습니다. 자주 듣는 프리셋은 별을 눌러 목록 맨 위 Favorites에 모아 둡니다.

- **직접 맞추기**

  Waves 탭에서 왼쪽과 오른쪽 주파수를 0.01 Hz 단위로, 채널 음량과 전체 음량을 따로 맞춥니다. 재생 중에 바꿔도 바로 들립니다. 맞춘 비트가 어느 대역인지는 화면 위쪽에 보입니다. 전체 음량은 다음에 켤 때도 그대로입니다.

- **알아서 하는 업데이트**

  새 버전이 나오면 사용하는 동안 받아 두었다가 앱을 닫을 때 설치합니다. 문제가 생긴 버전은 Settings 탭에서 이전 버전으로 되돌릴 수 있습니다.

- **밤에 켜 두기 편한 화면**

  밝은 면이 눈에 띄지 않도록 제목 표시줄까지 어두운 색으로 맞췄습니다. 왼쪽 채널은 어디서나 청록색, 오른쪽 채널은 주황색으로 표시합니다. 재생하는 동안에는 두 채널의 파형이 서로 다른 속도로 흐릅니다.

## 화면

<table>
  <tr>
    <td align="center">
      <img src="Images/program_presets.png" width="300" alt="Presets 화면"><br>
      대역별로 묶은 프리셋과 맨 위의 Favorites
    </td>
    <td align="center">
      <img src="Images/program_settings.png" width="300" alt="Settings 화면"><br>
      자동 업데이트와 버전 선택
    </td>
  </tr>
</table>

## 설치와 업데이트

- 설치하지 않고 사용하려면 `BrainWaves-Portable-<버전>-x64.zip`을 받아 압축을 풀고 `BrainWaves.exe`를 실행합니다. 포터블 판도 똑같이 스스로 업데이트합니다.
- Windows 10 이상 64비트에서 돌아갑니다. 실행에 필요한 .NET 런타임이 들어 있어 따로 설치하지 않아도 됩니다.
- Settings 탭에서 자동 업데이트를 끄거나 **Install another version**으로 원하는 버전을 골라 설치할 수 있습니다. 최신이 아닌 버전을 고르면 다시 최신으로 올라가지 않도록 자동 업데이트가 꺼집니다. 베타 버전까지 받으려면 **Beta versions**를 켭니다.
- 0.1.x에는 업데이트 기능이 없습니다. 그 버전을 사용하고 있다면 한 번만 위 설치 파일을 새로 받아야 합니다.

## 주의

- 의료 기기가 아닙니다. 바이노럴 비트를 듣고 느끼는 효과는 사람마다 다릅니다. 건강 문제는 의료 전문가와 상의해 주세요.
- 뇌전증(간질)이나 발작을 겪은 적이 있다면 사용하면 안 됩니다.
- 운전하거나 기계를 다루는 동안에는 사용하면 안 됩니다.
- 음량은 낮게 시작해 편안한 만큼만 올려 주세요. 불편하면 바로 멈춰 주세요.

## 직접 빌드하기

.NET 8 SDK가 필요합니다.

```bash
git clone https://github.com/siakun/BrainWavesWPF.git
cd BrainWavesWPF
dotnet run --project BrainWaves/BrainWaves/BrainWaves.csproj
```

## 라이선스와 문의

GNU Affero General Public License v3.0을 따릅니다. 전문은 [LICENSE](LICENSE)에 있습니다. 앱이 사용하는 오픈소스 라이브러리는 Settings 탭에서 볼 수 있습니다.

버그나 제안은 [Issues](https://github.com/siakun/BrainWavesWPF/issues)에 남겨 주세요. Pull Request도 환영합니다.

만든 사람: [siakun](https://github.com/siakun)
