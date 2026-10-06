$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'CatalogPolicy.ps1')
$version = '0.8.4.3'
$asset = "https://github.com/MarshalTitan/SentinelHUD/releases/download/v$version/SentinelHUD.zip"
$entry = @{
    InternalName = 'SentinelHUD'
    AssemblyVersion = $version
    TestingAssemblyVersion = $version
    DownloadLinkInstall = $asset
    DownloadLinkUpdate = $asset
    DownloadLinkTesting = $asset
}
function Assert-CatalogResult([object[]]$Catalog, [bool]$Expected) {
    $actual = Test-SentinelHudCatalogEntry -Catalog $Catalog -ExpectedVersion $version -ExpectedAsset $asset
    if ($actual -ne $Expected) { throw "Catalog validation returned $actual, expected $Expected." }
}
Assert-CatalogResult @($entry) $true
Assert-CatalogResult @(@{ InternalName = 'OtherPlugin' }, $entry) $true
Assert-CatalogResult @() $false
Assert-CatalogResult @($entry, $entry) $false
foreach ($field in @('AssemblyVersion','TestingAssemblyVersion','DownloadLinkInstall','DownloadLinkUpdate','DownloadLinkTesting')) {
    $invalid = $entry.Clone()
    $invalid[$field] = 'stale'
    Assert-CatalogResult @($invalid) $false
}
$parsed = @(ConvertFrom-Json -InputObject (ConvertTo-Json -InputObject @(@{ InternalName = 'OtherPlugin' }, $entry)))
Assert-CatalogResult $parsed $true
Write-Host 'PASS: 10 catalog policy cases, including stale versions/URLs, missing/duplicate entries and a parsed mixed-plugin array.'
