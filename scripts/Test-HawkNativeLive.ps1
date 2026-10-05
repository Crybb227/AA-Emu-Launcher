param([Parameter(Mandatory=$true)][string]$ClientRelease, [string]$ExistingWork, [string]$SkateContent)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$work = Join-Path $env:LOCALAPPDATA ('JasonGamesLauncher\Build\NativeLive-' + [Guid]::NewGuid().ToString('N'))
if ($ExistingWork) {
    $work = (Resolve-Path -LiteralPath $ExistingWork).Path
    $buildRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'JasonGamesLauncher\Build'))
    if ((Split-Path $work -Parent) -ne $buildRoot -or (Split-Path $work -Leaf) -notmatch '^NativeLive-[a-f0-9]{32}$') { throw 'Reuse only a test-owned NativeLive workspace' }
} else { New-Item -ItemType Directory -Path $work | Out-Null }
$bytes = New-Object byte[] 32; $random = [Security.Cryptography.RandomNumberGenerator]::Create(); $random.GetBytes($bytes); $random.Dispose()
[IO.File]::WriteAllText((Join-Path $work 'token'), [Convert]::ToBase64String($bytes))
$openssl = 'C:\Program Files\Git\usr\bin\openssl.exe'
& $openssl req -x509 -newkey rsa:2048 -nodes -days 1 -keyout (Join-Path $work 'key.pem') -out (Join-Path $work 'cert.pem') -subj /CN=localhost -addext 'subjectAltName=DNS:localhost' 2>$null
if ($LASTEXITCODE -ne 0) { throw 'Test certificate failed' }
& $openssl x509 -in (Join-Path $work 'cert.pem') -outform DER -out (Join-Path $work 'cert.cer')
$python = (Get-Command python).Source
if (Test-Path -LiteralPath (Join-Path $work 'port')) { Remove-Item -LiteralPath (Join-Path $work 'port') }
$helper = Start-Process -FilePath $python -ArgumentList @((Join-Path $PSScriptRoot 'hawk-live-api.py'), '--work', $work) -WindowStyle Hidden -PassThru
try {
    for ($i=0; $i -lt 30 -and -not (Test-Path (Join-Path $work 'port')); $i++) { Start-Sleep -Milliseconds 200 }
    if (-not (Test-Path (Join-Path $work 'port'))) { throw 'Local HTTPS test helper failed' }
    $build = Join-Path $repo 'AAEmu.Launcher\bin\Release'
    Get-ChildItem -LiteralPath $build -File | Where-Object { $_.Extension -in '.exe','.dll' } | Copy-Item -Destination $work
    Copy-Item -LiteralPath (Join-Path $build 'AAEmu.Launcher.exe.config') -Destination (Join-Path $work 'Check.exe.config')
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    & $compiler /nologo /target:exe "/out:$work\Check.exe" "/r:$work\AAEmu.Launcher.exe" "/r:$work\Newtonsoft.Json.dll" /r:System.Net.Http.dll (Join-Path $PSScriptRoot 'HawkNativeLive.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Native live test compilation failed' }
    if (-not $SkateContent) { $SkateContent = Join-Path $env:LOCALAPPDATA 'JasonGamesLauncher\Build\HawkConverter\native-assets-test2' }
    & (Join-Path $work 'Check.exe') $work $ClientRelease (Join-Path $repo 'AAEmu.Launcher\HawkSkater\wow-5875.json') $SkateContent
    if ($LASTEXITCODE -ne 0) { throw "Native live test failed. Evidence retained in $work" }
} finally { if (-not $helper.HasExited) { Stop-Process -Id $helper.Id }; $helper.Dispose() }
Write-Host "Native live evidence: $work"
