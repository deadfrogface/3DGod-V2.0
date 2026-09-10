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
    [string]$VelopackToolVersion = "1.2.0",
    [switch]$SkipVpk,
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$publish = Join-Path $root "artifacts/publish/win-x64"
$releases = Join-Path $root "artifacts/releases/$Channel"

Write-Host "== 3D God Velopack release =="
Write-Host "Version: $Version  Channel: $Channel  PackId: $PackId  SelfContained: $SelfContained"

if (Test-Path $publish) { Remove-Item -Recurse -Force $publish }
New-Item -ItemType Directory -Force -Path $publish | Out-Null

$publishArgs = @(
    "publish", (Join-Path $root "3DGodCreator.App/3DGodCreator.App.csproj"),
    "-c", "Release",
    "-r", "win-x64",
    "-o", $publish,
    "/p:Version=$Version"
)
if ($SelfContained) {
    $publishArgs += @("--self-contained", "true")
} else {
    $publishArgs += @("--self-contained", "false")
}

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}

# Ensure resources land in the package even when publish -o bypasses Build OutputPath copies.
function Copy-Tree([string]$srcName) {
    $src = Join-Path $root $srcName
    $dst = Join-Path $publish $srcName
    if (-not (Test-Path $src)) {
        Write-Warning "Missing source tree: $src"
        return
    }
    if (Test-Path $dst) { Remove-Item -Recurse -Force $dst }
    Copy-Item -Recurse -Force $src $dst
    Write-Host "Copied $srcName -> $dst"
}
Copy-Tree "assets"
Copy-Tree "presets"
Copy-Tree "blender_embed"

if (-not (Test-Path (Join-Path $publish "3DGodCreator.App.exe"))) {
    Write-Error "Publish output missing 3DGodCreator.App.exe"
    exit 2
}
if (-not (Test-Path (Join-Path $publish "assets"))) {
    Write-Error "Publish output missing assets/ - refusing to pack incomplete installer"
    exit 2
}
if (-not (Test-Path (Join-Path $publish "presets"))) {
    Write-Error "Publish output missing presets/ - refusing to pack incomplete installer"
    exit 2
}

if ($SkipVpk) {
    Write-Host "SkipVpk set - publish only at $publish"
    exit 0
}

New-Item -ItemType Directory -Force -Path $releases | Out-Null

Write-Host "Ensuring vpk tool $VelopackToolVersion is available..."
dotnet tool update -g vpk --version $VelopackToolVersion 2>$null
if ($LASTEXITCODE -ne 0) {
    dotnet tool install -g vpk --version $VelopackToolVersion
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install vpk tool"
        exit $LASTEXITCODE
    }
}

$vpkCmd = Get-Command vpk -ErrorAction SilentlyContinue
if (-not $vpkCmd) {
    # Newly installed global tools may not be on PATH in this session
    $dotnetTools = Join-Path $env:USERPROFILE ".dotnet\tools"
    $env:Path = "$dotnetTools;$env:Path"
    $vpkCmd = Get-Command vpk -ErrorAction SilentlyContinue
}
if (-not $vpkCmd) {
    Write-Error "vpk not found on PATH after install"
    exit 2
}

# Add -y for non-interactive vpk prompts
$vpkArgs = @(
    "pack",
    "--packId", $PackId,
    "--packVersion", $Version,
    "--packDir", $publish,
    "--mainExe", "3DGodCreator.App.exe",
    "--packTitle", "3D God Creator",
    "--outputDir", $releases,
    "-y"
)

Write-Host "Running: vpk $($vpkArgs -join ' ')"
& vpk @vpkArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "vpk pack failed (exit $LASTEXITCODE)."
    exit $LASTEXITCODE
}

Write-Host "Release artifacts: $releases"
Get-ChildItem $releases | ForEach-Object { Write-Host " - $($_.Name)" }
Write-Host "NOTE: Clean-Windows install smoke is verified in CI via scripts/ci/Invoke-CleanInstallSmoke.ps1"
