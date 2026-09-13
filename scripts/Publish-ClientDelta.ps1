param(
    [Parameter(Mandatory = $true)]
    [string] $ClientRoot,

    [Parameter(Mandatory = $true)]
    [string] $OutputRoot,

    [Parameter(Mandatory = $true)]
    [string] $Version
)

$ErrorActionPreference = "Stop"

$clientRootPath = (Resolve-Path $ClientRoot).Path
$clientOut = Join-Path $OutputRoot "client"
$filesOut = Join-Path $clientOut "files"

New-Item -ItemType Directory -Path $filesOut -Force | Out-Null

$manifestFiles = @()
Get-ChildItem -Path $clientRootPath -Recurse -File | Where-Object {
    $_.Name -ne ".aaemu-client-manifest.json"
} | ForEach-Object {
    $relativePath = $_.FullName.Substring($clientRootPath.Length + 1) -replace "\\", "/"
    $hash = (Get-FileHash -Path $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $flatName = $relativePath -replace "/", "_"

    Copy-Item -Path $_.FullName -Destination (Join-Path $filesOut $flatName) -Force

    $manifestFiles += [PSCustomObject]@{
        path   = $relativePath
        sha256 = $hash
        size   = $_.Length
    }
}

$manifest = [PSCustomObject]@{
    version = $Version
    files   = $manifestFiles
}

$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $clientOut "manifest.json")
Write-Host "Client delta package written to $clientOut"
