; DuoAudio Installer Script
; This script creates an installer for the DuoAudio application
; Uses framework-dependent deployment - requires .NET 9 runtime to be installed

#define AppName "DuoAudio"
#define AppVersion "1.0.0"
#define AppPublisher "DuoAudio"
#define AppExeName "DuoAudio.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
AppId={{A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=installer
OutputBaseFilename=DuoAudio-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"

[Files]
; Main application executable
Source: "publish\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\{#AppName}.dll"; DestDir: "{app}"; Flags: ignoreversion

; Only 3rd party dependencies that are NOT part of .NET runtime
; NAudio libraries - these must be bundled as they are not system libraries
Source: "publish\NAudio.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\NAudio.Asio.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\NAudio.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\NAudio.Midi.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\NAudio.Wasapi.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\NAudio.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\NAudio.WinMM.dll"; DestDir: "{app}"; Flags: ignoreversion

; Runtime configuration
Source: "publish\{#AppName}.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\{#AppName}.deps.json"; DestDir: "{app}"; Flags: ignoreversion

; Debug symbols (optional, can be removed for production)
Source: "publish\{#AppName}.pdb"; DestDir: "{app}"; Flags: ignoreversion

; Documentation files
Source: "README.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "DuoAudio\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "DuoAudio\USER_GUIDE.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "DuoAudio\TEST_RESULTS.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

 ; Check if .NET 9 runtime is installed
 ; The app requires .NET 9.0 or higher to be installed on the system
 [Code]
 function IsDotNet9Installed(): Boolean;
 var
   ResultCode: Integer;
   TempFile: String;
   Lines: TArrayOfString;
   i: Integer;
 begin
   Result := false;

   // Create temporary file to capture dotnet output
   TempFile := ExpandConstant('{tmp}') + '\dotnet_runtimes.txt';

   // Run dotnet --list-runtimes and redirect output to file
   if Exec('cmd.exe', '/C dotnet --list-runtimes > "' + TempFile + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
   begin
     // Read the file and check for .NET 9.0
     if LoadStringsFromFile(TempFile, Lines) then
     begin
       for i := 0 to GetArrayLength(Lines) - 1 do
       begin
         if Pos('Microsoft.NETCore.App 9.0', Lines[i]) > 0 then
         begin
           Result := true;
           Break;
         end;
       end;
     end;
     // Clean up temp file
     DeleteFile(TempFile);
   end;
 end;

 function InitializeSetup(): Boolean;
 var
   ErrorCode: Integer;
   dotnetInstalled: Boolean;
 begin
   Result := true;

   // Check for .NET 9.0 using dotnet command (most reliable)
   dotnetInstalled := IsDotNet9Installed();

   if not dotnetInstalled then
   begin
     if MsgBox('This application requires .NET 9.0 Runtime to be installed.' + #13#10 +
               'Would you like to download it now?' + #13#10 + #13#10 +
               'Without .NET 9.0 Runtime, the application will not run.',
               mbConfirmation, MB_YESNO) = IDYES then
     begin
       ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/9.0', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
     end;
   end;
 end;
