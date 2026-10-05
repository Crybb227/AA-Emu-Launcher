param(
    [string]$WorkDirectory = (Join-Path $env:LOCALAPPDATA 'JasonGamesLauncher\Build\HawkWindows'),
    [Parameter(Mandatory=$true)][string]$SkateAssets,
    [switch]$FullWorkspace
)
$ErrorActionPreference = 'Stop'
$work = [IO.Path]::GetFullPath($WorkDirectory)
$env:CARGO_HOME = Join-Path $work 'cargo'
$env:RUSTUP_HOME = Join-Path $work 'rustup'
$env:CARGO_BUILD_JOBS = '2'
$env:PATH = "$env:CARGO_HOME\bin;${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer;$env:PATH"
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$installation = (& $vswhere -latest -products Microsoft.VisualStudio.Product.Community -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath).Trim()
if (-not $installation) { $installation = (& $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath).Trim() }
if (-not $installation) { throw 'MSVC x64 build tools are required on the publisher machine' }
$vcvars = Join-Path $installation 'VC\Auxiliary\Build\vcvars64.bat'
cmd /d /c "call `"$vcvars`" >nul && set" | ForEach-Object {
    if ($_ -match '^([^=]+)=(.*)$') { [Environment]::SetEnvironmentVariable($matches[1], $matches[2], 'Process') }
}
if ($LASTEXITCODE -ne 0) { throw 'MSVC developer environment setup failed' }
$env:WOW_DATA = ''
$env:SKATE_KEYBOARD_TEST_ASSETS = (Resolve-Path -LiteralPath $SkateAssets).Path
Push-Location (Join-Path $work 'source')
try {
    & "$env:CARGO_HOME\bin\cargo.exe" fmt --all --check
    if ($LASTEXITCODE -ne 0) { throw 'Client formatting check failed' }
    if ($FullWorkspace) {
        & "$env:CARGO_HOME\bin\cargo.exe" test --locked --release --workspace --no-default-features --lib
        if ($LASTEXITCODE -ne 0) { throw 'Workspace player unit tests failed' }
    }
} finally { Pop-Location }
$harness = Join-Path $work 'keyboard-tests'
New-Item -ItemType Directory -Path (Join-Path $harness 'src') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'hawk-keyboard\test-harness\Cargo.toml') -Destination $harness
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'hawk-keyboard\test-harness\lib.rs') -Destination (Join-Path $harness 'src')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'hawk-keyboard\keyboard.rs') -Destination (Join-Path $harness 'src')
if (-not (Test-Path (Join-Path $harness 'Cargo.lock'))) {
    Copy-Item -LiteralPath (Join-Path $work 'source\Cargo.lock') -Destination $harness
}
$env:CARGO_TARGET_DIR = Join-Path $work 'source\target'
Push-Location $harness
try {
    # First run adds only the harness package to the copied, pinned client lock.
    & "$env:CARGO_HOME\bin\cargo.exe" test --offline --release --lib
    if ($LASTEXITCODE -ne 0) { throw 'Exact production keyboard module unit tests failed' }
    & "$env:CARGO_HOME\bin\cargo.exe" test --locked --offline --release engine_push_and_ollie -- --ignored --nocapture
    if ($LASTEXITCODE -ne 0) { throw 'Measured keyboard engine movement/braking/ollie test failed' }
} finally { Pop-Location }
