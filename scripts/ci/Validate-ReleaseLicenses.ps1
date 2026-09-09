#Requires -Version 5.1
<#
.SYNOPSIS
  Release CI gate: MODEL_LICENSES.json, manifests, THIRD_PARTY_NOTICES.txt (PHASE 57).
#>
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")

Write-Host "Running release license gate tests..."
dotnet test (Join-Path $root "3DGodCreator.Core.Tests/3DGodCreator.Core.Tests.csproj") `
    -c $Configuration `
    --filter "FullyQualifiedName~PackagingAndLicenseGateTests" `
    --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Error "Release license gate FAILED"
    exit $LASTEXITCODE
}

Write-Host "Release license gate PASSED"
