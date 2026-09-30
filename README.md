# 🧠 BrainWaves - 모던 바이노럴 비트 생성기

<p align="center">
  <img src="Images/program_waves.png" width="300" alt="BrainWaves 메인 화면"/>
</p>

<p align="center">
  <b>과학적으로 설계된 바이노럴 비트로 당신의 정신 상태를 변화시키세요</b>
</p>

<p align="center">
  <a href="#주요-기능">주요 기능</a> •
  <a href="#스크린샷">스크린샷</a> •
  <a href="#설치-방법">설치 방법</a> •
  <a href="#사용법">사용법</a> •
  <a href="#프리셋">프리셋</a> •
  <a href="#기술-스택">기술 스택</a>
</p>

---

## 🌟 개요

BrainWaves는 다양한 정신 상태를 달성하는 데 도움을 주는 바이노럴 비트를 생성하는 모던한 Windows 데스크톱 애플리케이션입니다. 각 귀에 약간 다른 주파수를 재생하여 뇌파 패턴에 영향을 줄 수 있는 청각적 착각을 만들어냅니다.

## ✨ 주요 기능

### 🎵 **정밀한 주파수 제어**
- 좌우 채널 독립적인 주파수 조절 (0.01Hz 정밀도, 버튼을 누르고 있으면 계속 조절)
- 실시간 스테레오 사인파 생성
- 각 채널별 개별 볼륨 조절과 전체 음량 조절

### 🧘 **13가지 과학적으로 설계된 프리셋**
- **수면 & 휴식**: 깊은 휴식을 위한 델타파와 세타파
- **집중 & 생산성**: 향상된 집중력을 위한 베타파
- **창의성 & 학습**: 창의적 사고를 위한 알파파
- **최고 성능**: 인지 능력 향상을 위한 감마파

### 🎨 **밤에 켜 두기 편한 화면**
- 어둡고 차분한 테마, 제목 표시줄까지 같은 색
- 왼쪽 채널은 청록, 오른쪽 채널은 호박색으로 화면 전체에서 같은 색으로 표시
- 뇌파 대역을 그리스 문자(δ, θ, α, β, γ)로 표시하고 프리셋을 대역별로 묶음
- 재생 중에는 두 채널의 파형이 서로 다른 속도로 흐름 (Windows에서 애니메이션을 끄면 멈춤)

### 🔄 **자동 업데이트**
- 새 버전을 백그라운드에서 받아 두고 앱을 닫을 때 설치
- 원하는 버전을 골라 설치하거나 이전 버전으로 되돌리기
- 베타 버전 수신 선택

## 📸 스크린샷

<table>
  <tr>
    <td align="center">
      <img src="Images/program_waves.png" width="250"/><br>
      <b>웨이브 컨트롤</b><br>
      <sub>수동으로 주파수 미세 조정</sub>
    </td>
    <td align="center">
      <img src="Images/program_presets.png" width="250"/><br>
      <b>프리셋 라이브러리</b><br>
      <sub>최적화된 설정에 빠르게 접근</sub>
    </td>
    <td align="center">
      <img src="Images/program_settings.png" width="250"/><br>
      <b>설정</b><br>
      <sub>업데이트와 앱 정보</sub>
    </td>
  </tr>
</table>

## 🧠 프리셋

| 프리셋 | 파동 유형 | 주파수 (Hz) | 목적 |
|--------|-----------|----------------|---------|
| 🌙 **깊은 수면** | 델타 | 1.63 | 가장 깊은 수면 단계 |
| 😴 **수면** | 델타 | 2.00 | 일반적인 수면 유도 |
| 🧘 **명상** | 세타 | 4.50 | 깊은 명상 |
| 🌊 **휴식** | 세타 | 4.56 | 스트레스 해소 |
| 👁️ **시각화** | 알파 | 8.67 | 정신적 이미지화 |
| 🎯 **집중** | 베타 | 29.00 | 강렬한 집중력 |
| 🎨 **창의성** | 베타 | 28.13 | 창의적 사고 |
| 💡 **인지** | 감마 | 40.00 | 최고의 정신 성능 |

## 💻 시스템 요구사항

- **OS**: Windows 10 이상 (64비트)
- **런타임**: 설치 파일에 포함되어 있어 따로 설치하지 않아도 됩니다
- **오디오**: 스테레오 헤드폰 또는 이어폰 (바이노럴 효과를 위해 필수)
- **RAM**: 최소 4GB
- **저장 공간**: 300MB 여유 공간

## 🚀 설치 방법

### 옵션 1: 릴리스 다운로드
1. [Releases](https://github.com/siakun/BrainWavesWPF/releases) 페이지로 이동
2. 최신 릴리스의 `BrainWaves-Setup-<버전>-x64.exe`를 받아 실행
3. 설치가 끝나면 BrainWaves가 시작 메뉴에 등록되고, 이후 버전은 앱이 스스로 업데이트합니다

설치하지 않고 쓰려면 `BrainWaves-Portable-<버전>-x64.zip`을 받아 압축을 풀고 `BrainWaves.exe`를 실행합니다. 포터블 판도 자동 업데이트를 지원합니다.

0.1.x 버전은 업데이트 기능이 없는 단일 실행 파일이라 새 버전으로 넘어가지 않습니다. 한 번만 위 방법으로 새로 받으면 그 뒤로는 자동으로 업데이트됩니다.

### 옵션 2: 소스에서 빌드
```bash
# 저장소 클론
git clone https://github.com/siakun/BrainWavesWPF.git
cd BrainWavesWPF

# 프로젝트 빌드
dotnet build

# 애플리케이션 실행
dotnet run --project BrainWaves/BrainWaves/BrainWaves.csproj
```

## 📖 사용법

### 시작하기
1. **스테레오 헤드폰 착용** - 바이노럴 비트 효과에 필수
2. **방법 선택**:
   - **빠른 시작**: Presets 탭에서 프리셋 선택
   - **사용자 정의**: Waves 탭에서 수동으로 주파수 조정
3. Waves 탭 위쪽의 **재생 버튼**을 눌러 세션 시작
4. 필요에 따라 게인 슬라이더로 **볼륨 조정**

### 업데이트
- 자동 업데이트를 켜 두면 새 버전을 받아 두었다가 앱을 닫을 때 설치합니다. 창 아래에 알림이 뜨면 **Restart now**로 바로 설치할 수도 있습니다
- Settings 탭의 **Install another version**에서 특정 버전을 골라 설치하거나 이전 버전으로 되돌릴 수 있습니다. 최신이 아닌 버전을 고르면 자동 업데이트가 꺼집니다
- 베타 버전을 받으려면 Settings 탭에서 **Beta versions**를 켭니다

### 프로 팁
- 🎧 최상의 결과를 위해 좋은 품질의 헤드폰 사용
- ⏱️ 일반적으로 15-30분 세션이 가장 효과적
- 🌙 수면을 위해서는 잠들기 30분 전부터 재생
- 🎯 집중을 위해서는 작업이나 공부 중에 사용

## ⚠️ 안전 주의사항

- **운전 중 사용 금지** 또는 기계 조작 시
- **간질이나 발작 장애가 있는 경우 사용 금지**
- 불편함을 느끼면 **사용 중단**
- **의료 기기가 아님** - 의학적 상태에 대해서는 의료 전문가와 상담

## 🛠️ 기술 스택

- **프레임워크**: WPF with .NET 8.0
- **아키텍처**: MVVM 패턴
- **UI 라이브러리**: MaterialDesignThemes (5.2.2)
- **MVVM 프레임워크**: CommunityToolkit.Mvvm (8.4.0)
- **오디오**: NAudio로 좌우 채널 사인파를 실시간 생성
- **배포와 업데이트**: Velopack 설치 패키지, [Siakun.AutoUpdate](https://github.com/siakun/Siakun.AutoUpdate)

## 🤝 기여하기

기여를 환영합니다! Pull Request를 자유롭게 제출해 주세요.

1. 저장소 포크
2. 기능 브랜치 생성 (`git checkout -b feature/AmazingFeature`)
3. 변경사항 커밋 (`git commit -m 'Add some AmazingFeature'`)
4. 브랜치에 푸시 (`git push origin feature/AmazingFeature`)
5. Pull Request 열기

## 📜 라이선스

이 프로젝트는 GNU Affero General Public License v3.0을 따릅니다 - 자세한 내용은 [LICENSE](LICENSE) 파일을 참조하세요.

## 🙏 감사의 말

- Google의 Material Design Icons
- 다양한 과학 출판물의 바이노럴 비트 주파수 연구
- 놀라운 라이브러리를 제공한 오픈소스 커뮤니티

---

<p align="center">
  <a href="https://github.com/Sia819">Sia819</a>가 ❤️를 담아 만들었습니다
</p>

<p align="center">
  <a href="https://github.com/siakun/BrainWavesWPF/issues">버그 신고</a> •
  <a href="https://github.com/siakun/BrainWavesWPF/issues">기능 요청</a>
</p>
```
