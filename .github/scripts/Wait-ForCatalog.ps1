param(
    [Parameter(Mandatory)][string]$ExpectedVersion,
    [Parameter(Mandatory)][string]$ExpectedAsset,
    [int]$TimeoutMinutes = 10
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'CatalogPolicy.ps1')
$catalogUrl = 'https://raw.githubusercontent.com/MarshalTitan/Sentinel/main/repo.json'
$deadline = [DateTime]::UtcNow.AddMinutes($TimeoutMinutes)
$headers = @{ 'Cache-Control' = 'no-cache'; 'Pragma' = 'no-cache' }
$lastState = 'No public catalog response received.'

for ($attempt = 1; $attempt -le 120 -and [DateTime]::UtcNow -lt $deadline; $attempt++) {
    try {
        # Parse response text explicitly so an array-returning web cmdlet cannot leave a nested catalog.
        $response = Invoke-WebRequest -Uri "$catalogUrl`?release=$env:GITHUB_RUN_ID-$attempt" -Headers $headers -TimeoutSec 20
        $catalog = @(ConvertFrom-Json -InputObject $response.Content)
        if (Test-SentinelHudCatalogEntry -Catalog $catalog -ExpectedVersion $ExpectedVersion -ExpectedAsset $ExpectedAsset) {
            Write-Host "Verified Sentinel HUD $ExpectedVersion and all three release URLs in the permanent public central catalog."
            exit 0
        }
        $entries = @($catalog | Where-Object InternalName -eq 'SentinelHUD')
        $lastState = "Public catalog has $($entries.Count) HUD entries; observed versions: $($entries.AssemblyVersion -join ', ')."
    }
    catch {
        $lastState = $_.Exception.Message
    }

    if ($attempt -eq 1 -or $attempt % 12 -eq 0) {
        Write-Host "Catalog attempt $($attempt): $lastState"
    }
    Start-Sleep -Seconds 5
}

throw "The permanent public central catalog did not verify Sentinel HUD $ExpectedVersion within $TimeoutMinutes minutes. Last result: $lastState"
