param([string]$Version = '2026.10.05.8', [string]$OutputRoot = (Join-Path $PSScriptRoot '..\artifacts'), [string]$ClientRelease, [string]$DeploymentFile, [string]$SkateContent)
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^[A-Za-z0-9._-]+$' -or $Version -in '.', '..') { throw 'Invalid version' }
$repo = Split-Path $PSScriptRoot -Parent
dotnet build (Join-Path $repo 'AAEmu.Launcher\AAEmu.Launcher.csproj') -c Release --no-restore -v minimal
if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed' }
$package = Join-Path $OutputRoot "JasonGamesLauncher-HawkSkater-$Version"
if (Test-Path -LiteralPath $package) { throw 'Choose a fresh output/version' }
New-Item -ItemType Directory -Path $package -Force | Out-Null
& (Join-Path $PSScriptRoot 'Install-JasonGamesLauncher.ps1') -Destination (Join-Path $package 'App') -NoShortcut
if ($ClientRelease) {
    $native = Get-Content -LiteralPath (Join-Path $ClientRelease 'manifest.json') -Raw | ConvertFrom-Json
    if ($native.schema -ne 1 -or $native.platform -ne 'windows-x86_64') { throw 'Only a native Windows release can be bundled in the player launcher' }
    $bundle = Join-Path $package 'App\HawkSkater\Release'; New-Item -ItemType Directory -Path $bundle -Force | Out-Null
    foreach ($file in $native.files) {
        if ($file.path -notmatch '^(bin|runtime|tools|licenses)/' -or $file.path -match '(^|/)\.\.?(/|$)|[:\\]') { throw 'Unsafe runtime file path' }
        $original = Join-Path $ClientRelease $file.path
        if ((Get-Item -LiteralPath $original).Length -ne $file.size -or (Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sha256) { throw "Invalid native release file: $($file.path)" }
        $target = Join-Path $bundle $file.path; New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $original -Destination $target
    }
    Copy-Item -LiteralPath (Join-Path $ClientRelease 'manifest.json') -Destination $bundle
}
if ($SkateContent) {
    $content = Get-Content -LiteralPath (Join-Path $SkateContent 'manifest.json') -Raw | ConvertFrom-Json
    if ($content.schema -ne 1 -or $content.kind -ne 'skate-content') { throw 'Invalid Skate content pack' }
    $bundle = Join-Path $package 'App\HawkSkater\SkateContent'; New-Item -ItemType Directory -Path $bundle -Force | Out-Null
    foreach ($file in $content.files) {
        if ($file.path -notmatch '^(skate-data/assets/private/|skate-audio/[A-Za-z0-9_-]+\.wav$)' -or $file.path -match '(^|/)\.\.?(/|$)|[:\\]') { throw 'Unsafe Skate file path' }
        $original = Join-Path $SkateContent $file.path
        if ((Get-Item -LiteralPath $original).Length -ne $file.size -or (Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sha256) { throw 'Invalid Skate content checksum' }
        $target = Join-Path $bundle $file.path; New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $original -Destination $target
    }
    Copy-Item -LiteralPath (Join-Path $SkateContent 'manifest.json') -Destination $bundle
}
if ($DeploymentFile) {
    $deployment = Get-Content -LiteralPath $DeploymentFile -Raw | ConvertFrom-Json
    if ($deployment.platform -ne 'windows-x86_64' -or $deployment.schema -ne 1) { throw 'Invalid publisher configuration' }
    Copy-Item -LiteralPath $DeploymentFile -Destination (Join-Path $package 'App\HawkSkater\deployment.json')
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install-JasonGamesLauncher.ps1') -Destination (Join-Path $package 'Install.ps1')
Copy-Item -LiteralPath (Join-Path $repo 'docs\jason-hawk-skater.md') -Destination (Join-Path $package 'README.md')
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE') -Destination (Join-Path $package 'LICENSE')
Compress-Archive -Path (Join-Path $package '*') -DestinationPath "$package.zip"
Write-Host "Launcher package: $package.zip"
Write-Host 'Native runtime, authorized Skate content and externally downloaded WoW assets remain separate.'
