param(
    [string]$WorkDirectory = (Join-Path $env:LOCALAPPDATA 'JasonGamesLauncher\Build\HawkWindows'),
    [switch]$Dev
)
$ErrorActionPreference = 'Stop'
$work = [IO.Path]::GetFullPath($WorkDirectory)
New-Item -ItemType Directory -Path $work -Force | Out-Null
# Build dependencies are isolated here, not installed into the player's machine.
$env:CARGO_HOME = Join-Path $work 'cargo'
$env:RUSTUP_HOME = Join-Path $work 'rustup'
$env:PATH = "$env:CARGO_HOME\bin;$env:PATH"
$env:CARGO_BUILD_JOBS = '2'
$installer = Join-Path $work 'rustup-init.exe'
if (-not (Test-Path "$env:CARGO_HOME\bin\cargo.exe")) {
    Invoke-WebRequest 'https://static.rust-lang.org/rustup/dist/x86_64-pc-windows-msvc/rustup-init.exe' -OutFile $installer
    & $installer -y --no-modify-path --profile minimal --default-toolchain 1.98.1 --default-host x86_64-pc-windows-msvc
    if ($LASTEXITCODE -ne 0) { throw 'Windows Rust toolchain setup failed' }
}
$source = Join-Path $work 'source'
if (-not (Test-Path (Join-Path $source 'Cargo.toml'))) {
    New-Item -ItemType Directory -Path $source -Force | Out-Null
    $archive = Join-Path $work 'source.tar'
    $linuxArchive = (& wsl -d dml-arch -- wslpath -a $archive.Replace('\', '/')).Trim()
    & wsl -d dml-arch -- git -C /home/dml/games/world-of-skatecraft archive --format=tar -o $linuxArchive HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Source archive failed' }
    & tar -xf $archive -C $source
    if ($LASTEXITCODE -ne 0) { throw 'Source extraction failed' }
}
Push-Location $source
try {
    & "$env:CARGO_HOME\bin\cargo.exe" fetch --locked --target x86_64-pc-windows-msvc
    if ($LASTEXITCODE -ne 0) { throw 'Pinned client dependencies could not be fetched' }
    & (Join-Path $PSScriptRoot 'hawk-keyboard\Apply-Keyboard.ps1') -Source $source -CargoHome $env:CARGO_HOME
    & "$env:CARGO_HOME\bin\cargo.exe" fmt --all
    if ($LASTEXITCODE -ne 0) { throw 'Client source formatting failed' }
    $arguments = @('build', '--locked', '--release', '-p', 'benilla')
    if (-not $Dev) { $arguments += '--no-default-features' }
    & "$env:CARGO_HOME\bin\cargo.exe" @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Native Windows client build failed' }
    Write-Host "Native Windows client: $source\target\release\benilla.exe"
} finally { Pop-Location }
