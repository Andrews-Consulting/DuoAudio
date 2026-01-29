# DuoAudio Installer

This directory contains the installer setup for the DuoAudio application.

## Files

- `DuoAudioInstaller.iss` - Inno Setup installer script
- `publish/` - Published application files
- `installer/` - Output directory for the compiled installer (will be created)

## Option 1: Using Inno Setup (Recommended)

Inno Setup is a free, professional installer creator for Windows programs.

### Installation

1. Download Inno Setup from: https://jrsoftware.org/isdl.php
2. Install Inno Setup on your system

### Compiling the Installer

Once Inno Setup is installed, you can compile the installer using one of these methods:

#### Method 1: Command Line
```cmd
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" DuoAudioInstaller.iss
```

#### Method 2: Using Inno Setup Compiler GUI
1. Open Inno Setup Compiler
2. File → Open Script
3. Select `DuoAudioInstaller.iss`
4. Click "Compile" or press F9

The installer will be created in the `installer/` directory as `DuoAudio-Setup.exe`.

### Installer Features

- Installs to `C:\Program Files\DuoAudio`
- Creates desktop shortcut (optional)
- Creates Start Menu shortcut
- Creates Quick Launch shortcut (Windows 7 and earlier)
- Includes all application dependencies
- Includes documentation files
- Uninstaller included

## Option 2: Using PowerShell Installer (Alternative)

If you don't want to install Inno Setup, you can use the PowerShell installer script.

### Running the PowerShell Installer

1. Right-click on `Install-DuoAudio.ps1`
2. Select "Run with PowerShell"
3. Follow the prompts

Or run from command line:
```powershell
powershell -ExecutionPolicy Bypass -File Install-DuoAudio.ps1
```

### PowerShell Installer Features

- Installs to `C:\Program Files\DuoAudio`
- Creates desktop shortcut
- Creates Start Menu shortcut
- Includes all application files
- Simple and straightforward

## Application Files

The installer includes all necessary files from the `publish/` directory:

- `DuoAudio.exe` - Main application executable
- All required DLL dependencies
- Localization files (cs, de, es, fr, it, ja, ko, pl, pt-BR, ru, tr, zh-Hans, zh-Hant)
- Documentation files (README.md, USER_GUIDE.md, TEST_RESULTS.md)

## Distribution

The compiled installer (`DuoAudio-Setup.exe`) can be distributed to end users. Users simply need to:

1. Run the installer
2. Follow the installation wizard
3. Launch the application from the desktop shortcut or Start Menu

## Uninstallation

Users can uninstall the application through:

- Windows Settings → Apps → DuoAudio → Uninstall
- Start Menu → DuoAudio → Uninstall DuoAudio

## Troubleshooting

### Inno Setup Not Found

If you get an error that Inno Setup is not found:
1. Ensure Inno Setup is installed
2. Check the installation path (default: `C:\Program Files (x86)\Inno Setup 6\`)
3. Update the path in the compile command if installed elsewhere

### PowerShell Execution Policy

If you get an error running the PowerShell script:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

Then try running the installer again.

## Version Information

- Application Name: DuoAudio
- Version: 1.0.0
- Target Framework: .NET 9.0 Windows
- Platform: Windows x64
