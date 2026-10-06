function Test-SentinelHudCatalogEntry {
    param(
        [AllowEmptyCollection()][object[]]$Catalog,
        [Parameter(Mandatory)][string]$ExpectedVersion,
        [Parameter(Mandatory)][string]$ExpectedAsset
    )

    $entries = @($Catalog | Where-Object InternalName -eq 'SentinelHUD')
    if ($entries.Count -ne 1) { return $false }
    $entry = $entries[0]
    return $entry.AssemblyVersion -eq $ExpectedVersion `
        -and $entry.TestingAssemblyVersion -eq $ExpectedVersion `
        -and $entry.DownloadLinkInstall -eq $ExpectedAsset `
        -and $entry.DownloadLinkUpdate -eq $ExpectedAsset `
        -and $entry.DownloadLinkTesting -eq $ExpectedAsset
}
