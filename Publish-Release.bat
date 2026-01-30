@echo off
REM DuoAudio Release Publisher
REM This script publishes the application using framework-dependent deployment
REM This requires .NET 9 runtime to be installed on the target machine

echo ========================================
echo   DuoAudio Release Publisher
echo   (Framework-Dependent Deployment)
echo ========================================
echo.
echo NOTE: This deployment requires .NET 9 Runtime
echo       to be installed on the target machine.
echo.

REM Clean and recreate publish directory
if exist "publish" (
    echo Cleaning existing publish directory...
    rmdir /s /q "publish"
)
mkdir "publish"

REM Publish the application (framework-dependent)
echo Publishing application (framework-dependent)...
cd DuoAudio

REM Use framework-dependent deployment (no --self-contained flag)
REM This relies on .NET 9 runtime being present on the target machine
dotnet publish -c Release -r win-x64 --self-contained false -o "../publish/temp"

if errorlevel 1 (
    echo ERROR: Publish failed!
    cd ..
    pause
    exit /b 1
)

cd ..

REM Move files from temp to publish root
echo Organizing published files...
move "publish\temp\*" "publish\" >nul 2>&1
for /d %%D in ("publish\temp\*") do (
    move "%%D" "publish\" >nul 2>&1
)

REM Remove temp directory
rmdir /s /q "publish\temp"

echo.
echo ========================================
echo   Publish completed successfully!
echo ========================================
echo.
echo Files included in the installer:
echo   - DuoAudio.exe (main executable)
echo   - DuoAudio.dll (application assembly)
echo   - NAudio*.dll (3rd party audio library)
echo   - Runtime configuration files
echo.
echo NOTE: Target machine must have .NET 9 Runtime installed.
echo       Download from: https://dotnet.microsoft.com/download/dotnet/9.0
echo.
pause
