## 개발 명령어

```bash
# 프로젝트 빌드
dotnet build BrainWaves/BrainWaves.sln

# 애플리케이션 실행
dotnet run --project BrainWaves/BrainWaves/BrainWaves.csproj

# 빌드 아티팩트 정리
dotnet clean BrainWaves/BrainWaves.sln

# NuGet 패키지 복원
dotnet restore BrainWaves/BrainWaves.sln
```

개발 빌드는 Velopack으로 설치한 실행 파일이 아니므로 업데이트를 확인하지 않고, 설정 화면의 버전 전환도 막혀 있습니다.

## 릴리스

배포는 GitHub Actions가 Velopack 설치 패키지로 하고, 앱은 Siakun.AutoUpdate로 GitHub 릴리스를 조회해 스스로 업데이트합니다.

1. `BrainWaves/BrainWaves/BrainWaves.csproj`의 `Version`을 올립니다. 버전은 이 값 한 곳에서만 정합니다.
2. 사용자용 릴리스 내역 `docs/releases/<버전>.md`를 작성합니다. 작성 기준은 `.agents/skills/release-notes/SKILL.md`에 있습니다.
3. 두 변경을 PR로 main에 합친 뒤, 합친 커밋에 태그 `v<버전>`을 붙여 push합니다. `v` 없는 태그(`0.2.0`)도 받습니다.
4. `.github/workflows/release.yml`이 태그와 `Version`이 같은지, 릴리스 내역 파일이 있고 제목과 변경 목록을 갖췄는지 확인합니다. 하나라도 어긋나면 게시하기 전에 멈춥니다.
5. 워크플로가 직전 릴리스를 기준으로 delta를 만들고, 릴리스 내역을 본문으로 한 `Release v<버전>`을 게시합니다. 올라가는 파일은 `BrainWaves-Setup-<버전>-x64.exe`, `BrainWaves-Portable-<버전>-x64.zip`, nupkg, `releases.win.json`입니다.

버전에 `-`가 들어가면(예: `0.3.0-beta.1`) 프리릴리스로 게시되어 설정에서 베타 버전을 켠 사용자만 받습니다.

## 로컬 패키징

태그를 올리기 전에 저장소 루트에서 `build.bat <버전>`을 실행하면 워크플로와 같은 검증과 패키징을 로컬에서 합니다. 결과는 `releases/`에 생기며 이 폴더와 `publish/`는 git이 무시합니다.

```powershell
.\build.bat 0.2.0
```

vpk 버전은 `.config/dotnet-tools.json`에 고정되어 있습니다. 앱이 쓰는 Velopack 버전(Siakun.AutoUpdate가 의존하는 버전)과 맞춰 두고, 올릴 때는 Setup.exe로 설치한 앱에서 업데이트와 버전 전환을 직접 확인합니다.
