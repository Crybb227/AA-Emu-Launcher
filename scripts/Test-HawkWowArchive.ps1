param([Parameter(Mandatory=$true)][string]$Archive)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
dotnet build (Join-Path $repo 'AAEmu.Launcher\AAEmu.Launcher.csproj') -c Release --no-restore -v minimal
if($LASTEXITCODE -ne 0){throw 'Build failed'}
$work=Join-Path $env:LOCALAPPDATA ('JasonGamesLauncher\Build\WowArchiveTest-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
$build=Join-Path $repo 'AAEmu.Launcher\bin\Release'
Copy-Item (Join-Path $build 'AAEmu.Launcher.exe'),(Join-Path $build 'Newtonsoft.Json.dll') -Destination $work
Copy-Item (Join-Path $build 'AAEmu.Launcher.exe.config') (Join-Path $work 'Check.exe.config')
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe "/out:$work\Check.exe" "/r:$work\AAEmu.Launcher.exe" /r:System.Net.Http.dll (Join-Path $PSScriptRoot 'HawkWowArchiveSmoke.cs')
if($LASTEXITCODE -ne 0){throw 'Test compilation failed'}
& (Join-Path $work 'Check.exe') ([IO.Path]::GetFullPath($Archive)) (Join-Path $repo 'AAEmu.Launcher\HawkSkater\wow-5875.json')
if($LASTEXITCODE -ne 0){throw 'Archive import test failed'}
