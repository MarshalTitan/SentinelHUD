param(
    [string]$ExpectedCommit = '300703b360a58fb4b73bf7675d31fe8cab4614cd',
    [string]$ExpectedTag = 'v0.3.1.0',
    [string]$ExpectedPackageVersion = '0.3.1',
    [string]$ExpectedUiPackageHash = 'e1a9ce4e1ce36042c0fcd53f4c23874d918640be10eef16c21f1cd436c6ba747'
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

$vendoredPackagePath = "./.packages/SentinelCore/$ExpectedTag/MarshalTitan.SentinelCore.UI.$ExpectedPackageVersion.nupkg"
if (-not (Test-Path -LiteralPath $vendoredPackagePath)) {
    throw "Vendored Sentinel Core UI package is missing: '$vendoredPackagePath'."
}
$vendoredHash = (Get-FileHash -LiteralPath $vendoredPackagePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($vendoredHash -ne $ExpectedUiPackageHash) {
    throw "Vendored Sentinel Core UI package hash '$vendoredHash' does not match '$ExpectedUiPackageHash'."
}

$packagePath = Join-Path $env:RUNNER_TEMP "MarshalTitan.SentinelCore.UI.$ExpectedPackageVersion.nupkg"
$packageUrl = "https://github.com/MarshalTitan/SentinelCore/releases/download/$ExpectedTag/MarshalTitan.SentinelCore.UI.$ExpectedPackageVersion.nupkg"
Invoke-WebRequest -Uri $packageUrl -OutFile $packagePath
$actualHash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualHash -ne $ExpectedUiPackageHash) {
    throw "Sentinel Core UI package hash '$actualHash' does not match '$ExpectedUiPackageHash'."
}

Write-Host "Verified vendored and published MarshalTitan.SentinelCore.UI $ExpectedPackageVersion at $ExpectedCommit ($actualHash)."
