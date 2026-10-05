$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
dotnet build (Join-Path $repo 'AAEmu.Launcher\AAEmu.Launcher.csproj') -c Release --no-restore -v minimal
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
$dir = Join-Path ([IO.Path]::GetTempPath()) ('HawkPlayerSmoke-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $dir | Out-Null
$build = Join-Path $repo 'AAEmu.Launcher\bin\Release'
Get-ChildItem -LiteralPath $build -File | Where-Object { $_.Extension -in '.exe','.dll' -or $_.Name -eq 'AAEmu.Launcher.exe.config' } | Copy-Item -Destination $dir
New-Item -ItemType Directory -Path (Join-Path $dir 'HawkSkater') | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'AAEmu.Launcher\HawkSkater\deployment.json'),(Join-Path $repo 'AAEmu.Launcher\HawkSkater\wow-5875.json') -Destination (Join-Path $dir 'HawkSkater')
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
foreach ($test in 'HawkSkaterFormSmoke','HawkNativeSmoke') {
    & $compiler /nologo /target:exe "/out:$dir\$test.exe" "/r:$dir\AAEmu.Launcher.exe" "/r:$dir\Newtonsoft.Json.dll" /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Net.Http.dll (Join-Path $PSScriptRoot "$test.cs")
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
    Copy-Item -LiteralPath (Join-Path $dir 'AAEmu.Launcher.exe.config') -Destination (Join-Path $dir "$test.exe.config")
}
& (Join-Path $dir 'HawkSkaterFormSmoke.exe') (Join-Path $dir 'player-form.png')
if ($LASTEXITCODE -ne 0) { throw 'Player form test failed' }
& (Join-Path $dir 'HawkNativeSmoke.exe') (Join-Path $dir 'HawkSkater\wow-5875.json')
if ($LASTEXITCODE -ne 0) { throw 'Native installer tests failed' }
Write-Host "Test render: $dir\player-form.png"
