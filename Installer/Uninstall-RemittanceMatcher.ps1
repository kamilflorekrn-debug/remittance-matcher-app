param(
    [string]$InstallDir = "$env:LOCALAPPDATA\Programs\Getinge Remittance Matcher"
)

$ErrorActionPreference = 'Stop'

$shortcutName = 'Getinge Remittance Matcher.lnk'
$startMenuLink = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Getinge\$shortcutName"
$desktopLink = Join-Path ([Environment]::GetFolderPath('Desktop')) $shortcutName

if (Test-Path $startMenuLink) { Remove-Item -LiteralPath $startMenuLink -Force }
if (Test-Path $desktopLink) { Remove-Item -LiteralPath $desktopLink -Force }

$startMenuDir = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Getinge'
if (Test-Path $startMenuDir -and -not (Get-ChildItem -Path $startMenuDir -Force | Select-Object -First 1)) {
    Remove-Item -LiteralPath $startMenuDir -Force
}

if (Test-Path $InstallDir) {
    Remove-Item -LiteralPath $InstallDir -Recurse -Force
}

Write-Host "Odinstalowanie zakończone."
