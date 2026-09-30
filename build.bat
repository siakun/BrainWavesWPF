@echo off
setlocal enabledelayedexpansion

:: INTENT: .github/workflows/release.yml과 같은 검증과 패키징을 로컬에서 한다.
::   태그를 올리기 전에 패키지와 릴리스 내역을 확인하려는 용도이며, 게시는 태그를 push하면 GitHub Actions가 한다.
::   한쪽을 고치면 다른 쪽도 함께 고친다.

echo ========================================
echo BrainWaves Release Build (Velopack)
echo ========================================
echo.

:: Set paths
set PROJECT_PATH=BrainWaves\BrainWaves\BrainWaves.csproj
set ICON_PATH=BrainWaves\BrainWaves\Resources\BrainWaves.ico
set PUBLISH_DIR=publish
set RELEASE_DIR=releases

:: Version (필수 입력)
if "%1"=="" (
    echo [ERROR] Version is required!
    echo.
    echo Usage:
    echo   CMD:        build.bat ^<version^>
    echo   PowerShell: .\build.bat ^<version^>
    echo.
    echo Example:
    echo   CMD:        build.bat 0.2.0
    echo   PowerShell: .\build.bat 0.2.0
    echo.
    pause
    exit /b 1
)
set VERSION=%1
set RELEASE_NOTES=docs\releases\%VERSION%.md

:: 프로젝트와 패키지 버전이 다르거나 업데이트 내역이 없으면 기존 출력물을 지우기 전에 중단한다.
for /f "delims=" %%v in ('dotnet msbuild "%PROJECT_PATH%" -getProperty:Version -nologo') do set PROJECT_VERSION=%%v
if not "%VERSION%"=="%PROJECT_VERSION%" (
    echo [ERROR] Version must match the project version: %PROJECT_VERSION%
    exit /b 1
)
if not exist "%RELEASE_NOTES%" (
    echo [ERROR] Release notes are missing: %RELEASE_NOTES%
    exit /b 1
)

echo Version: %VERSION%
echo.

:: Clean previous builds
echo [1/4] Cleaning previous builds...
if exist "%PUBLISH_DIR%" rd /s /q "%PUBLISH_DIR%"
if exist "%RELEASE_DIR%" rd /s /q "%RELEASE_DIR%"
mkdir "%PUBLISH_DIR%"
mkdir "%RELEASE_DIR%"

:: Publish application
echo.
echo [2/4] Publishing application...
dotnet publish "%PROJECT_PATH%" ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -o "%PUBLISH_DIR%"

if !errorlevel! neq 0 (
    echo.
    echo [ERROR] Publish failed!
    goto :error
)

:: Check vpk tool
:: INTENT: CI와 같은 vpk를 쓰도록 .config/dotnet-tools.json에 고정한 버전을 복원한다.
::   전역에 깔린 vpk는 PC마다 버전이 달라 같은 소스에서도 다른 패키지가 나온다.
echo.
echo [3/4] Restoring vpk tool...
dotnet tool restore
if !errorlevel! neq 0 (
    echo [ERROR] Failed to restore vpk tool!
    goto :error
)

:: Pack with Velopack
echo.
echo [4/4] Packing with Velopack...
dotnet vpk pack ^
    --packId "BrainWaves" ^
    --packVersion "%VERSION%" ^
    --packDir "%PUBLISH_DIR%" ^
    --mainExe "BrainWaves.exe" ^
    --packTitle "BrainWaves" ^
    --icon "%ICON_PATH%" ^
    --releaseNotes "%RELEASE_NOTES%" ^
    --outputDir "%RELEASE_DIR%"

if !errorlevel! neq 0 (
    echo.
    echo [ERROR] Velopack packing failed!
    goto :error
)

:: Remove unnecessary files
del "%RELEASE_DIR%\assets.win.json" 2>nul
del "%RELEASE_DIR%\RELEASES" 2>nul

:: Rename files (MS style)
echo.
echo Renaming files to MS style...
ren "%RELEASE_DIR%\BrainWaves-win-Setup.exe" "BrainWaves-Setup-%VERSION%-x64.exe"
ren "%RELEASE_DIR%\BrainWaves-win-Portable.zip" "BrainWaves-Portable-%VERSION%-x64.zip"

:: Show build results
echo.
echo ========================================
echo Build completed successfully!
echo ========================================
echo.
echo Output: %RELEASE_DIR%\
echo.
echo Files created:
dir /b "%RELEASE_DIR%"
echo.

:: Display folder size
for /f "usebackq" %%a in (`powershell -Command "(Get-ChildItem -Path '%RELEASE_DIR%' -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB -as [int]"`) do set sizemb=%%a
echo Total Size: !sizemb! MB

echo.
echo To release on GitHub:
echo   Push the tag v%VERSION%. GitHub Actions builds and publishes the same files.
echo.
echo Press any key to open releases folder...
pause >nul
start "" "%RELEASE_DIR%"
goto :end

:error
echo.
echo ========================================
echo Build failed! Check the error messages above.
echo ========================================
pause
exit /b 1

:end
endlocal
exit /b 0
