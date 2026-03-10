; Inno Setup script - DEVELOPER package (source + binary)
#define AppName "Getinge Remittance Matcher Developer"
#define AppVersion "1.0.0"
#define AppPublisher "Kamil Florek"
#define AppExeName "RemittanceMatcherApp.exe"
#define ProjectRoot "C:\Users\kamil\Desktop\Kamil\Codex\Remittance Advice\RemittanceMatcherApp"
#define SourceDir "C:\Users\kamil\Desktop\Kamil\Codex\Remittance Advice\RemittanceMatcherApp\Dist\Developer"
#define LicensePath "C:\Users\kamil\Desktop\Kamil\Codex\Remittance Advice\RemittanceMatcherApp\Installer\License-PL.txt"
#define SetupIconPath "C:\Users\kamil\Desktop\Kamil\Codex\Remittance Advice\RemittanceMatcherApp\Assets\getingeremittanceikomn.ico"

[Setup]
AppId={{6F02EF7D-249D-4DB7-B46C-2D44BFD4126A}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\Getinge Remittance Matcher Developer
DisableDirPage=no
DefaultGroupName=Getinge Remittance Matcher Developer
UninstallDisplayIcon={app}\app\{#AppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
LicenseFile={#LicensePath}
OutputDir={#ProjectRoot}\publish
OutputBaseFilename=GetingeRemittanceMatcher-Setup-Developer
SetupIconFile={#SetupIconPath}

[Languages]
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"

[Tasks]
Name: "desktopicon"; Description: "Utwórz skrót na pulpicie"; GroupDescription: "Skróty:"; Flags: unchecked
Name: "startmenuicon"; Description: "Utwórz skróty w menu Start"; GroupDescription: "Skróty:"; Flags: unchecked
Name: "taskbaricon"; Description: "Utwórz skrót na pasku zadań"; GroupDescription: "Skróty:"; Flags: unchecked

[Files]
; Runtime app (ready to run)
Source: "{#SourceDir}\app\*"; DestDir: "{app}\app"; Excludes: "*.tmp,*.e32.tmp"; Flags: ignoreversion recursesubdirs createallsubdirs
; Editable source tree
Source: "{#SourceDir}\source\*"; DestDir: "{app}\source"; Excludes: "*.tmp,*.e32.tmp"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar"; Tasks: taskbaricon

[Icons]
Name: "{group}\Getinge Remittance Matcher Developer"; Filename: "{app}\app\{#AppExeName}"; IconFilename: "{app}\app\getingeremittanceikomn.ico"; Tasks: startmenuicon
Name: "{group}\Kod źródłowy (folder)"; Filename: "{app}\source"; Tasks: startmenuicon
Name: "{group}\Odinstaluj Getinge Remittance Matcher Developer"; Filename: "{uninstallexe}"; Tasks: startmenuicon
Name: "{group}\Odinstaluj (helper EXE)"; Filename: "{app}\app\GetingeRemittanceUninstall.exe"; Tasks: startmenuicon
Name: "{autodesktop}\Getinge Remittance Matcher Developer"; Filename: "{app}\app\{#AppExeName}"; IconFilename: "{app}\app\getingeremittanceikomn.ico"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\Getinge Remittance Matcher Developer.lnk"; Filename: "{app}\app\{#AppExeName}"; IconFilename: "{app}\app\getingeremittanceikomn.ico"; Tasks: taskbaricon

[Run]
Filename: "{app}\app\{#AppExeName}"; Description: "Uruchom aplikację"; Flags: nowait postinstall skipifsilent
Filename: "{sys}\ie4uinit.exe"; Parameters: "-show"; Flags: runhidden skipifsilent
