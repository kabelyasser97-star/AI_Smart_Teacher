; AI Smart Teacher - Inno Setup script (Phase 11)
; Requires Inno Setup 6+. Build portable output first:
;   powershell -ExecutionPolicy Bypass -File .\publish\build-portable-win-x64.ps1
; Then compile: ISCC.exe .\installer\AI_Smart_Teacher.iss

#define MyAppName "AI Smart Teacher"
#define MyAppVersion "0.9.0"
#define MyAppPublisher "AI Smart Teacher"
#define MyAppExeName "AI_Smart_Teacher.exe"

[Setup]
AppId={{A6B9A63D-9A9A-4E66-A8D3-7D11E2D7C001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\publish\installer
OutputBaseFilename=AI_Smart_Teacher_Setup
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayName={#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish\portable-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; User data is stored under %LocalAppData%\AI_Smart_Teacher (database/backups/logs/attachments).
; Keep user data on uninstall by design. Remove only if explicitly required by policy.
