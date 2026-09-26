# Builds and exercises the presentation in a temporary copy. No real settings,
# game executables, addon manifests, registry associations, or network startup.
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    dotnet build AAEmu.Launcher/AAEmu.Launcher.csproj -c Release --no-restore -v minimal
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    $testDir = Join-Path ([IO.Path]::GetTempPath()) ('JasonLauncherSmoke-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $testDir | Out-Null
    Copy-Item -Path 'AAEmu.Launcher/bin/Release/*' -Destination $testDir -Recurse
    Copy-Item -LiteralPath (Join-Path $testDir 'AAEmu.Launcher.exe.config') -Destination (Join-Path $testDir 'Check.exe.config')
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    & $compiler /nologo /target:exe "/out:$testDir\Check.exe" "/r:$testDir\AAEmu.Launcher.exe" /r:System.Drawing.dll /r:System.Windows.Forms.dll (Join-Path $PSScriptRoot 'LauncherPresentationSmoke.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
    Push-Location $testDir
    try {
        & (Join-Path $testDir 'Check.exe')
        if ($LASTEXITCODE -ne 0) { throw 'Presentation smoke tests failed' }
    } finally { Pop-Location }
    Write-Host "Test renders retained in $testDir"
} finally { Pop-Location }
