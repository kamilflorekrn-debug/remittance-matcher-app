; Inno Setup script - USER package (binary-only)
#define AppName "Getinge Remittance Matcher"
#define AppVersion "1.0.0"
#define AppPublisher "Kamil Florek"
#define AppExeName "RemittanceMatcherApp.exe"
#define SourceDir "C:\Users\kamil\Desktop\Kamil\Codex\Remittance Advice\RemittanceMatcherApp\Dist\User"
#define LicensePath "C:\Users\kamil\Desktop\Kamil\Codex\Remittance Advice\RemittanceMatcherApp\Installer\License-PL.txt"
#define SetupIconPath "C:\Users\kamil\Desktop\Kamil\Codex\Remittance Advice\RemittanceMatcherApp\Assets\getingeremittanceikomn.ico"

[Setup]
AppId={{2B7C7B7F-2C85-4B6A-A5E8-6E0D61E4E739}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\Getinge Remittance Matcher
DisableDirPage=no
DefaultGroupName=Getinge Remittance Matcher
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
LicenseFile={#LicensePath}
OutputDir={#SourceDir}
OutputBaseFilename=GetingeRemittanceMatcher-Setup-User
SetupIconFile={#SetupIconPath}

[Languages]
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"

[Tasks]
Name: "desktopicon"; Description: "Utwórz skrót na pulpicie"; GroupDescription: "Skróty:"; Flags: unchecked
Name: "startmenuicon"; Description: "Utwórz skróty w menu Start"; GroupDescription: "Skróty:"; Flags: unchecked
Name: "taskbaricon"; Description: "Utwórz skrót na pasku zadań"; GroupDescription: "Skróty:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "*.tmp,*.e32.tmp"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar"; Tasks: taskbaricon

[Icons]
Name: "{group}\Getinge Remittance Matcher"; Filename: "{app}\{#AppExeName}"; Tasks: startmenuicon
Name: "{group}\Opis aplikacji"; Filename: "{app}\support\Opis.txt"; Tasks: startmenuicon
Name: "{group}\Odinstaluj Getinge Remittance Matcher"; Filename: "{uninstallexe}"; Tasks: startmenuicon
Name: "{group}\Odinstaluj (helper EXE)"; Filename: "{app}\GetingeRemittanceUninstall.exe"; Tasks: startmenuicon
Name: "{autodesktop}\Getinge Remittance Matcher"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\Getinge Remittance Matcher.lnk"; Filename: "{app}\{#AppExeName}"; Tasks: taskbaricon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Uruchom aplikację"; Flags: nowait postinstall skipifsilent
Filename: "{sys}\ie4uinit.exe"; Parameters: "-show"; Flags: runhidden skipifsilent
