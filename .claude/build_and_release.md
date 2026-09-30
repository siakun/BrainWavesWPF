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

배포는 Velopack 설치 패키지로 하고, 앱은 Siakun.AutoUpdate로 GitHub 릴리스를 조회해 스스로 업데이트합니다.

1. `BrainWaves/BrainWaves/BrainWaves.csproj`의 `Version`을 올려 커밋합니다. 버전은 이 값 한 곳에서만 정합니다.
2. 같은 버전의 태그 `v<버전>`을 그 커밋에 붙여 push합니다.
3. `.github/workflows/release.yml`이 태그와 `Version`이 같은지 확인한 뒤, 직전 릴리스를 기준으로 delta를 만들고 Setup.exe, 포터블 zip, nupkg, `releases.win.json`을 GitHub 릴리스로 게시합니다.

버전에 `-`가 들어가면(예: `0.3.0-beta.1`) 프리릴리스로 게시되어 설정에서 베타 버전을 켠 사용자만 받습니다.
Actions에서 워크플로를 수동으로 실행하면 릴리스를 만들지 않고 패키지를 아티팩트로만 올립니다.

## 로컬 패키징

워크플로와 같은 명령으로 패키지를 만들어 확인할 수 있습니다. 출력 폴더는 저장소 밖에 둡니다.

```powershell
dotnet tool restore
dotnet publish BrainWaves/BrainWaves/BrainWaves.csproj -c Release -r win-x64 --self-contained true -o <출력>/publish
dotnet vpk pack --packId BrainWaves --packVersion <버전> --packDir <출력>/publish --mainExe BrainWaves.exe --packTitle BrainWaves --icon BrainWaves/BrainWaves/Resources/BrainWaves.ico --outputDir <출력>/releases
```

vpk 버전은 `.config/dotnet-tools.json`에 고정되어 있습니다. 앱이 쓰는 Velopack 버전(Siakun.AutoUpdate가 의존하는 버전)과 맞춰 두고, 올릴 때는 Setup.exe로 설치한 앱에서 업데이트와 버전 전환을 직접 확인합니다.
