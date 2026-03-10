; Inno Setup script for Getinge Remittance Matcher
#define AppName "Getinge Remittance Matcher"
#define AppVersion "1.0.0"
#define AppPublisher "Kamil Florek"
#define AppExeName "RemittanceMatcherApp.exe"
#define SourceDir "C:\Users\kamil\Desktop\Kamil\Codex\Remittance Advice\RemittanceMatcherApp\publish"

[Setup]
AppId={{2B7C7B7F-2C85-4B6A-A5E8-6E0D61E4E739}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\Getinge Remittance Matcher
DefaultGroupName=Getinge Remittance Matcher
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
OutputDir={#SourceDir}
OutputBaseFilename=GetingeRemittanceMatcher-Setup
SetupIconFile=C:\Users\kamil\Desktop\Kamil\Codex\Remittance Advice\RemittanceMatcherApp\Assets\getingeremittanceikomn.ico

[Languages]
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"

[Tasks]
Name: "desktopicon"; Description: "Utwórz skrót na pulpicie"; GroupDescription: "Dodatkowe skróty:"; Flags: unchecked
Name: "taskbaricon"; Description: "Utwórz skrót na pasku zadań"; GroupDescription: "Dodatkowe skróty:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "GetingeRemittanceMatcher-Setup.exe"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar"; Tasks: taskbaricon

[Icons]
Name: "{group}\Getinge Remittance Matcher"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Opis aplikacji"; Filename: "{app}\Opis.txt"
Name: "{group}\Odinstaluj Getinge Remittance Matcher"; Filename: "{uninstallexe}"
Name: "{group}\Odinstaluj (helper EXE)"; Filename: "{app}\GetingeRemittanceUninstall.exe"
Name: "{autodesktop}\Getinge Remittance Matcher"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\Getinge Remittance Matcher.lnk"; Filename: "{app}\{#AppExeName}"; Tasks: taskbaricon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Uruchom aplikację"; Flags: nowait postinstall skipifsilent
