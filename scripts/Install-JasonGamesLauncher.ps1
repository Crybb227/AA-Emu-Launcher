param(
    [string]$Destination = (Join-Path $env:LOCALAPPDATA 'JasonGamesLauncher\App'),
    [string]$BuildDirectory = (Join-Path $PSScriptRoot 'App'),
    [switch]$NoShortcut
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $BuildDirectory)) { $BuildDirectory = Join-Path $PSScriptRoot '..\AAEmu.Launcher\bin\Release' }
$buildRoot = (Resolve-Path -LiteralPath $BuildDirectory).Path
if (-not (Test-Path -LiteralPath (Join-Path $buildRoot 'AAEmu.Launcher.exe'))) { throw 'Build Release before installing.' }
$destinationRoot = [IO.Path]::GetFullPath($Destination)
if ($destinationRoot -eq [IO.Path]::GetPathRoot($destinationRoot)) { throw 'Choose an application subfolder.' }
if (Get-Process -Name AAEmu.Launcher -ErrorAction SilentlyContinue) { throw 'Close Jason Games Launcher before installing.' }
New-Item -ItemType Directory -Path $destinationRoot -Force | Out-Null
# Runtime binaries/resources only. Never copy settings from a used build folder.
$runtimeFiles = Get-ChildItem -LiteralPath $buildRoot -File | Where-Object { $_.Extension -in '.exe', '.dll' -or $_.Name -like '*.exe.config' -or $_.Name -eq 'documents.folders' }
foreach ($file in $runtimeFiles) { Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $destinationRoot $file.Name) -Force }
foreach ($folder in 'lng', 'Res', 'HawkSkater') {
    $sourceFolder = Join-Path $buildRoot $folder
    foreach ($file in Get-ChildItem -LiteralPath $sourceFolder -Recurse -File) {
        if ($file.FullName -match '[\\/]__pycache__[\\/]' -or $file.Extension -eq '.pyc') { continue }
        $relative = $file.FullName.Substring($buildRoot.Length + 1)
        if ($folder -eq 'HawkSkater' -and $relative -notmatch '^HawkSkater[\\/](deployment\.json|wow-5875\.json|Release[\\/].+|SkateContent[\\/].+)$') { continue }
        $target = Join-Path $destinationRoot $relative
        New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }
}
$templateRoot = Join-Path $PSScriptRoot '..\AAEmu.Launcher'
if (-not (Test-Path -LiteralPath $templateRoot)) { $templateRoot = $buildRoot }
$template = Join-Path $templateRoot 'settings.aelcf'
function Install-CleanTemplate([string]$Source, [string]$Target) {
    $data = Get-Content -LiteralPath $Source -Raw | ConvertFrom-Json
    foreach ($name in 'lastLoginUser', 'lastLoginPass') {
        if ($data.PSObject.Properties.Name -contains $name) { $data.$name = '' }
    }
    if ($data.PSObject.Properties.Name -contains 'userHistory') { $data.userHistory = @() }
    [IO.File]::WriteAllText($Target, ($data | ConvertTo-Json -Depth 30), (New-Object Text.UTF8Encoding $false))
}
if (-not (Test-Path -LiteralPath (Join-Path $destinationRoot 'settings.aelcf'))) {
    Install-CleanTemplate $template (Join-Path $destinationRoot 'settings.aelcf')
}
if (-not (Test-Path -LiteralPath (Join-Path $destinationRoot 'aaemu.info.aelcf'))) {
    Install-CleanTemplate (Join-Path $templateRoot 'aaemu.info.aelcf') (Join-Path $destinationRoot 'aaemu.info.aelcf')
}
if (-not $NoShortcut) {
    $shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) 'Jason Games Launcher.lnk'
    $shortcut = (New-Object -ComObject WScript.Shell).CreateShortcut($shortcutPath)
    $shortcut.TargetPath = Join-Path $destinationRoot 'AAEmu.Launcher.exe'
    $shortcut.WorkingDirectory = $destinationRoot
    $shortcut.Save()
}
Write-Host "Installed Jason Games Launcher in $destinationRoot"
Write-Host 'JasonHawkSkater uses the native Windows runtime with automatic asset setup.'
