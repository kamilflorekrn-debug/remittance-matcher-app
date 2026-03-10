param(
    [string]$InstallDir = "$env:LOCALAPPDATA\Programs\Getinge Remittance Matcher",
    [switch]$NoDesktopShortcut
)

$ErrorActionPreference = 'Stop'

$sourceDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not (Test-Path (Join-Path $sourceDir 'RemittanceMatcherApp.exe'))) {
    throw "Nie znaleziono RemittanceMatcherApp.exe obok instalatora. Uruchom instalator z folderu publish."
}

New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null

Write-Host "Kopiowanie plików do: $InstallDir"
Get-ChildItem -Path $sourceDir -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $InstallDir $_.Name) -Force
}
Get-ChildItem -Path $sourceDir -Directory | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $InstallDir $_.Name) -Recurse -Force
}

$exePath = Join-Path $InstallDir 'RemittanceMatcherApp.exe'
$shortcutName = 'Getinge Remittance Matcher.lnk'
$startMenuDir = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Getinge'
New-Item -ItemType Directory -Path $startMenuDir -Force | Out-Null

$ws = New-Object -ComObject WScript.Shell
$startShortcut = $ws.CreateShortcut((Join-Path $startMenuDir $shortcutName))
$startShortcut.TargetPath = $exePath
$startShortcut.WorkingDirectory = $InstallDir
$startShortcut.IconLocation = "$exePath,0"
$startShortcut.Save()

if (-not $NoDesktopShortcut) {
    $desktopDir = [Environment]::GetFolderPath('Desktop')
    $desktopShortcut = $ws.CreateShortcut((Join-Path $desktopDir $shortcutName))
    $desktopShortcut.TargetPath = $exePath
    $desktopShortcut.WorkingDirectory = $InstallDir
    $desktopShortcut.IconLocation = "$exePath,0"
    $desktopShortcut.Save()
}

$uninstallSource = Join-Path $sourceDir 'Uninstall-RemittanceMatcher.ps1'
if (Test-Path $uninstallSource) {
    Copy-Item -LiteralPath $uninstallSource -Destination (Join-Path $InstallDir 'Uninstall-RemittanceMatcher.ps1') -Force
}

Write-Host "Instalacja zakończona."
Write-Host "Uruchomienie: $exePath"
Write-Host "Odinstalowanie: $InstallDir\Uninstall-RemittanceMatcher.ps1"
