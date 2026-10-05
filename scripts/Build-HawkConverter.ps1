param([string]$WorkDirectory = (Join-Path $env:LOCALAPPDATA 'JasonGamesLauncher\Build\HawkConverter'))
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$work = [IO.Path]::GetFullPath($WorkDirectory)
$source = Join-Path $work 'source'
$revision = '4488651c35c44365faa1ba38eed5758b6ebde714'
New-Item -ItemType Directory -Path $work -Force | Out-Null
if (-not (Test-Path (Join-Path $source '.git'))) {
    git clone https://github.com/SK8-ENGINE/skate-3-rust-engine.git $source
    if ($LASTEXITCODE -ne 0) { throw 'Converter source download failed' }
}
if ((git -C $source rev-parse HEAD) -ne $revision) { throw "Converter source must be pinned to $revision; use a fresh work directory." }
if (-not ((Get-Content (Join-Path $source 'LICENSE') -Raw) -match 'GNU GENERAL PUBLIC LICENSE')) { throw 'Converter redistribution licence missing' }
$venv = Join-Path $work 'venv'
if (-not (Test-Path (Join-Path $venv 'Scripts\python.exe'))) { python -m venv $venv }
$python = Join-Path $venv 'Scripts\python.exe'
& $python -m pip install --only-binary=:all: PyInstaller==6.22.3 numpy==2.5.3 Pillow==12.3.0
if ($LASTEXITCODE -ne 0) { throw 'Converter build dependencies failed' }
$decoderZip = Join-Path $work 'vgmstream-win64.zip'
if (-not (Test-Path $decoderZip)) { Invoke-WebRequest 'https://github.com/vgmstream/vgmstream/releases/download/r2117/vgmstream-win64.zip' -OutFile $decoderZip }
$release = Invoke-RestMethod 'https://api.github.com/repos/vgmstream/vgmstream/releases/tags/r2117'
$asset = $release.assets | Where-Object { $_.name -eq 'vgmstream-win64.zip' }
if ($asset.digest -and $asset.digest -ne ('sha256:' + (Get-FileHash $decoderZip -Algorithm SHA256).Hash.ToLowerInvariant())) { throw 'Decoder archive checksum mismatch' }
$decoder = Join-Path $work 'decoder'
if (-not (Test-Path $decoder)) { Expand-Archive -LiteralPath $decoderZip -DestinationPath $decoder }
$audio = Join-Path $work 'audio'; New-Item -ItemType Directory -Path $audio -Force | Out-Null
foreach ($name in 'skate_audio.py','eb_extract.py') {
    Copy-Item -LiteralPath "\\wsl.localhost\dml-arch\home\dml\games\world-of-skatecraft\tools\$name" -Destination $audio
}
$rust = Join-Path $env:LOCALAPPDATA 'JasonGamesLauncher\Build\HawkWindows'
$env:CARGO_HOME = Join-Path $rust 'cargo'; $env:RUSTUP_HOME = Join-Path $rust 'rustup'
& "$env:CARGO_HOME\bin\rustc.exe" --edition 2021 --crate-type cdylib -O (Join-Path $source 'tools\asset_pipeline\refpack_native.rs') -o (Join-Path $work 'refpack.dll')
if ($LASTEXITCODE -ne 0) { throw 'Native RefPack decoder build failed' }
Push-Location $work
try {
    & $python -m PyInstaller --noconfirm --onedir --name skate-convert --distpath (Join-Path $work 'dist') --workpath (Join-Path $work 'pyi-build') --paths $source --paths $audio --hidden-import skate_audio --hidden-import eb_extract --hidden-import wave --collect-all numpy --collect-all PIL --add-data "$source\tools;tools" --add-data "$audio;audio" --add-data "$decoder;decoder" --add-binary "$work\refpack.dll;tools/asset_pipeline" (Join-Path $PSScriptRoot 'hawk-convert.py')
    if ($LASTEXITCODE -ne 0) { throw 'Standalone converter build failed' }
} finally { Pop-Location }
Write-Host "Standalone converter: $work\dist\skate-convert\skate-convert.exe"
Write-Host 'GPL converter corresponding sources and all dependency licence notices must accompany distribution.'
