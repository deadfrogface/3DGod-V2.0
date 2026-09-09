#Requires -Version 5.1
<#
.SYNOPSIS
  Build a Velopack release for 3D God Creator (PHASE 55).
  AI model checkpoints and worker venvs are NOT included in the base installer.
#>
param(
    [string]$Version = "2.0.0",
    [ValidateSet("stable", "beta", "dev")]
    [string]$Channel = "stable",
    [string]$PackId = "ThreeDGodCreator",
    [string]$VelopackToolVersion = "0.0.1298",
    [switch]$SkipVpk
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$publish = Join-Path $root "artifacts/publish/win-x64"
$releases = Join-Path $root "artifacts/releases/$Channel"

Write-Host "== 3D God Velopack release =="
Write-Host "Version: $Version  Channel: $Channel  PackId: $PackId"

if (Test-Path $publish) { Remove-Item -Recurse -Force $publish }
New-Item -ItemType Directory -Force -Path $publish | Out-Null

dotnet publish (Join-Path $root "3DGodCreator.App/3DGodCreator.App.csproj") `
    -c Release -r win-x64 --self-contained false `
    -o $publish /p:Version=$Version

if ($SkipVpk) {
    Write-Host "SkipVpk set - publish only at $publish"
    exit 0
}

New-Item -ItemType Directory -Force -Path $releases | Out-Null

$vpkArgs = @(
    "tool", "run", "vpk", "--version", $VelopackToolVersion, "pack",
    "--packId", $PackId,
    "--packVersion", $Version,
    "--packDir", $publish,
    "--mainExe", "3DGodCreator.App.exe",
    "--packTitle", "3D God Creator",
    "--outputDir", $releases
)

Write-Host "Running: dotnet $($vpkArgs -join ' ')"
& dotnet @vpkArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "vpk pack failed (exit $LASTEXITCODE). Install tool: dotnet tool install -g vpk --version $VelopackToolVersion"
    exit $LASTEXITCODE
}

Write-Host "Release artifacts: $releases"
Write-Host 'GATED_EXTERNAL_DEPENDENCY: Clean VM install/start/project-create is NOT verified on this machine.'
Write-Host "Manual gate: fresh Windows VM, run Setup.exe from $releases, launch app, create project."
