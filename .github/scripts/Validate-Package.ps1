param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedVersion
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $PackagePath -PathType Leaf)) {
    throw "Package not found: $PackagePath"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PackagePath))
try {
    $entries = @($archive.Entries)
    $names = @($entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    $required = @(
        'SentinelHUD.dll',
        'SentinelHUD.json',
        'SentinelHUD.deps.json',
        'SentinelHUD.Core.dll',
        'SentinelCore.dll',
        'SentinelCore.Dalamud.dll',
        'SentinelCore.UI.dll',
        'assets/icon.png'
    )

    foreach ($name in $required) {
        if ($names -notcontains $name) {
            throw "Package is missing required entry '$name'."
        }
    }

    if ($names | Where-Object { $_ -match '(^|/)(obj|bin|tests|external)/' }) {
        throw 'Package contains a source, test, external, or build-directory entry.'
    }

    $manifestEntry = $entries | Where-Object FullName -eq 'SentinelHUD.json' | Select-Object -First 1
    $reader = [System.IO.StreamReader]::new($manifestEntry.Open())
    try {
        $manifest = $reader.ReadToEnd() | ConvertFrom-Json
    }
    finally {
        $reader.Dispose()
    }

    if ($manifest.InternalName -ne 'SentinelHUD') {
        throw "Unexpected InternalName '$($manifest.InternalName)'."
    }
    if ($manifest.AssemblyVersion -ne $ExpectedVersion) {
        throw "Manifest version '$($manifest.AssemblyVersion)' does not match '$ExpectedVersion'."
    }
    if ([int]$manifest.DalamudApiLevel -ne 15) {
        throw "Unexpected Dalamud API level '$($manifest.DalamudApiLevel)'."
    }
    if ($manifest.RepoUrl -ne 'https://github.com/MarshalTitan/SentinelHUD') {
        throw "Unexpected RepoUrl '$($manifest.RepoUrl)'."
    }

    $icon = $entries | Where-Object FullName -eq 'assets/icon.png' | Select-Object -First 1
    if ($icon.Length -lt 1024) {
        throw 'Packaged icon is unexpectedly small.'
    }
}
finally {
    $archive.Dispose()
}

Write-Host "Validated Sentinel HUD package $ExpectedVersion at $PackagePath"
