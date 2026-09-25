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

    $icon = $entries | Where-Object { $_.FullName.Replace('\', '/') -eq 'assets/icon.png' } | Select-Object -First 1
    $iconStream = $icon.Open()
    try {
        $signature = [byte[]]::new(8)
        if ($iconStream.Read($signature, 0, $signature.Length) -ne $signature.Length `
            -or $signature[0] -ne 0x89 `
            -or $signature[1] -ne 0x50 `
            -or $signature[2] -ne 0x4E `
            -or $signature[3] -ne 0x47) {
            throw 'Packaged icon does not have a valid PNG signature.'
        }
    }
    finally {
        $iconStream.Dispose()
    }
}
finally {
    $archive.Dispose()
}

Write-Host "Validated Sentinel HUD package $ExpectedVersion at $PackagePath"
