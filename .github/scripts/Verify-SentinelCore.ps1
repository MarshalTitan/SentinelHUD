param(
    [string]$ExpectedCommit = '520f9b9837dc3286c24d6f812b8348a27f378a3e',
    [string]$ExpectedTag = 'v0.3.0.0',
    [string]$ExpectedPackageVersion = '0.3.0',
    [string]$ExpectedUiPackageHash = '121e279f45206d4bc65192c1207b4044d48cf1625fe422fefb2f997f1eca819e'
)

$ErrorActionPreference = 'Stop'

$actualCommit = (git -C external/SentinelCore rev-parse HEAD).Trim()
if ($actualCommit -ne $ExpectedCommit) {
    throw "Sentinel Core submodule is '$actualCommit', expected '$ExpectedCommit'."
}

git -C external/SentinelCore fetch --quiet origin tag $ExpectedTag --no-tags
if ($LASTEXITCODE -ne 0) {
    throw "Could not resolve Sentinel Core tag '$ExpectedTag'."
}
$tagCommit = (git -C external/SentinelCore rev-list -n 1 $ExpectedTag).Trim()
if ($tagCommit -ne $ExpectedCommit) {
    throw "Sentinel Core tag '$ExpectedTag' resolves to '$tagCommit', expected '$ExpectedCommit'."
}

[xml]$coreProps = Get-Content -LiteralPath 'external/SentinelCore/Directory.Build.props' -Raw
$coreVersion = [string]$coreProps.Project.PropertyGroup.PackageVersion
if ($coreVersion -ne $ExpectedPackageVersion) {
    throw "Sentinel Core package version is '$coreVersion', expected '$ExpectedPackageVersion'."
}

[xml]$uiProject = Get-Content -LiteralPath 'external/SentinelCore/src/SentinelCore.UI/SentinelCore.UI.csproj' -Raw
$packageId = [string]$uiProject.Project.PropertyGroup.PackageId
if ($packageId -ne 'MarshalTitan.SentinelCore.UI') {
    throw "Unexpected Sentinel Core UI package ID '$packageId'."
}

$packagePath = Join-Path $env:RUNNER_TEMP 'MarshalTitan.SentinelCore.UI.0.3.0.nupkg'
$packageUrl = "https://github.com/MarshalTitan/SentinelCore/releases/download/$ExpectedTag/MarshalTitan.SentinelCore.UI.$ExpectedPackageVersion.nupkg"
Invoke-WebRequest -Uri $packageUrl -OutFile $packagePath
$actualHash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualHash -ne $ExpectedUiPackageHash) {
    throw "Sentinel Core UI package hash '$actualHash' does not match '$ExpectedUiPackageHash'."
}

Write-Host "Verified MarshalTitan.SentinelCore.UI $ExpectedPackageVersion at $ExpectedCommit ($actualHash)."
