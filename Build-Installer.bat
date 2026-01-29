@echo off
REM DuoAudio Installer Builder
REM This batch file compiles the Inno Setup installer

echo ========================================
echo   DuoAudio Installer Builder
echo ========================================
echo.

REM Check if Inno Setup is installed
set "INNO_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist "%INNO_PATH%" (
    set "INNO_PATH=C:\Program Files\Inno Setup 6\ISCC.exe"
)

if not exist "%INNO_PATH%" (
    echo ERROR: Inno Setup compiler not found.
    echo.
    echo Please install Inno Setup from: https://jrsoftware.org/isdl.php
    echo.
    echo After installation, this script will automatically detect it.
    echo.
    pause
    exit /b 1
)

REM Check if the installer script exists
if not exist "%~dp0DuoAudioInstaller.iss" (
    echo ERROR: DuoAudioInstaller.iss not found.
    echo Please ensure you are running this from the correct directory.
    pause
    exit /b 1
)

REM Check if the publish directory exists
if not exist "%~dp0publish" (
    echo ERROR: publish directory not found.
    echo Please ensure the application has been published.
    pause
    exit /b 1
)

REM Create installer output directory if it doesn't exist
if not exist "%~dp0installer" (
    echo Creating installer output directory...
    mkdir "%~dp0installer"
)

REM Compile the installer
echo.
echo Compiling installer using Inno Setup...
echo Compiler: %INNO_PATH%
echo Script: %~dp0DuoAudioInstaller.iss
echo Output: %~dp0installer\
echo.

"%INNO_PATH%" "%~dp0DuoAudioInstaller.iss"

REM Check the exit code
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Installer compilation failed with error code: %ERRORLEVEL%
    echo.
    echo Please check the error messages above for details.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================
echo   Installer Build Complete!
echo ========================================
echo.
echo The installer has been created in the installer directory:
echo   %~dp0installer\
echo.
echo You can now distribute DuoAudio-Setup.exe to users.
echo.
pause
