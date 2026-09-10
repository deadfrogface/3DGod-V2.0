#Requires -Version 5.1
<#
.SYNOPSIS
  Build Velopack package, install via Setup.exe on a clean path, run --smoke-test, uninstall.
  Fails hard if packaging/install/smoke fails. Does NOT fall back to unpackaged bin/.
#>
param(
    [string]$Version = "2.0.0-ci",
    [string]$Channel = "stable",
    [string]$PackId = "ThreeDGodCreator",
    [string]$InstallRoot = "",
    [int]$SmokeTimeoutSec = 120,
    [switch]$SkipUninstall,
    [switch]$SkipPack
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $root

function Write-Section([string]$t) {
    Write-Host ""
    Write-Host ("=" * 72)
    Write-Host $t
    Write-Host ("=" * 72)
}

Write-Section "CLEAN WINDOWS INSTALLER SMOKE"
Write-Host "Repo: $root"

if (-not $InstallRoot) {
    $stamp = Get-Date -Format "yyyyMMddHHmmss"
    if ($env:RUNNER_TEMP) {
        $InstallRoot = Join-Path $env:RUNNER_TEMP "3dgod-clean-install-$stamp"
    } else {
        $InstallRoot = Join-Path $env:TEMP "3dgod-clean-install-$stamp"
    }
}

# Best-effort cleanup of prior smoke installs that may lock Setup.exe
Get-Process | Where-Object { $_.ProcessName -match "3DGodCreator|ThreeDGodCreator" } |
    Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# Do NOT pre-create InstallRoot: Setup.exe owns that directory lifecycle.
if (Test-Path $InstallRoot) {
    cmd /c "rmdir /s /q `"$InstallRoot`"" | Out-Null
}

Write-Host "INSTALL_ROOT=$InstallRoot"

# 1) Package via repository packaging script (real vpk, not SkipVpk) unless SkipPack.
Write-Section "PACK (Velopack)"
if ($SkipPack) {
    Write-Host "SkipPack set - using existing artifacts/releases/$Channel"
} else {
    & (Join-Path $root "scripts\packaging\Build-VelopackRelease.ps1") `
        -Version $Version `
        -Channel $Channel `
        -PackId $PackId `
        -SelfContained
    if ($LASTEXITCODE -ne 0) {
        Write-Host "::error::Velopack pack FAILED"
        Write-Host "INSTALLER_SMOKE_STATUS=FAILED_PACK"
        exit $LASTEXITCODE
    }
}

$releases = Join-Path $root "artifacts\releases\$Channel"
$setup = Get-ChildItem -Path $releases -Filter "*Setup.exe" -Recurse |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if (-not $setup) {
    Write-Host "::error::No Setup.exe under $releases - installer smoke cannot fall back to unpackaged build"
    Write-Host "INSTALLER_SMOKE_STATUS=FAILED_NO_SETUP"
    Get-ChildItem $releases -Recurse -ErrorAction SilentlyContinue | ForEach-Object { Write-Host $_.FullName }
    exit 3
}
Write-Host "SETUP=$($setup.FullName)"

# 2) Silent install to clean directory (must use Setup.exe).
# IMPORTANT: log file must NOT live inside InstallRoot (Setup renames that directory).
Write-Section "INSTALL (Setup.exe --silent)"
$setupLog = Join-Path (Split-Path $InstallRoot -Parent) ("3dgod-setup-" + [Guid]::NewGuid().ToString("N") + ".log")
$setupArgs = @(
    "--silent",
    "--installto", $InstallRoot,
    "--log", $setupLog
)
Write-Host "Running: $($setup.FullName) $($setupArgs -join ' ')"
$setupProc = Start-Process -FilePath $setup.FullName -ArgumentList $setupArgs -PassThru -Wait -NoNewWindow
Write-Host "Setup exit=$($setupProc.ExitCode)"
if ($setupProc.ExitCode -ne 0) {
    Write-Host "::error::Setup.exe failed with exit $($setupProc.ExitCode)"
    Write-Host "INSTALLER_SMOKE_STATUS=FAILED_INSTALL"
    if (Test-Path $setupLog) { Get-Content $setupLog -Tail 80 }
    exit 4
}

# Setup may auto-launch the GUI app; stop it before our controlled smoke run.
Get-Process -Name "3DGodCreator.App" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Locate installed current/ layout (real app files live under current/)
$currentDir = Join-Path $InstallRoot "current"
$exe = $null
if (Test-Path (Join-Path $currentDir "3DGodCreator.App.exe")) {
    $exe = Get-Item (Join-Path $currentDir "3DGodCreator.App.exe")
}
if (-not $exe) {
    $exe = Get-ChildItem -Path $InstallRoot -Filter "3DGodCreator.App.exe" -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '[\\/]current[\\/]' } |
        Select-Object -First 1
}
if (-not $exe) {
    Write-Host "::error::Setup.exe did not produce installed current\3DGodCreator.App.exe under $InstallRoot"
    Write-Host "INSTALLER_SMOKE_STATUS=FAILED_INSTALL"
    if (Test-Path $setupLog) { Get-Content $setupLog -Tail 80 }
    Get-ChildItem $InstallRoot -Recurse -ErrorAction SilentlyContinue | Select-Object -First 80 FullName | ForEach-Object { Write-Host $_.FullName }
    exit 4
}
Write-Host "INSTALLED_EXE=$($exe.FullName)"
$appDir = $exe.Directory.FullName

# Required installed files
$required = @(
    (Join-Path $appDir "3DGodCreator.App.exe"),
    (Join-Path $appDir "assets"),
    (Join-Path $appDir "presets")
)
foreach ($r in $required) {
    if (-not (Test-Path $r)) {
        Write-Host "::error::Required installed path missing: $r"
        Write-Host "INSTALLER_SMOKE_STATUS=FAILED_FILES"
        exit 5
    }
}

# 3) Launch installed app with --smoke-test (do not fall back to unpackaged bin/)
Write-Section "SMOKE LAUNCH"
$smokeResult = Join-Path $appDir "smoke-result.json"
if (Test-Path $smokeResult) { Remove-Item -Force $smokeResult }

Write-Host "Launching installed exe with --smoke-test"
$p = Start-Process -FilePath $exe.FullName -ArgumentList @("--smoke-test") -PassThru -Wait -NoNewWindow `
    -WorkingDirectory $appDir
Write-Host "Smoke process exit=$($p.ExitCode)"
if ($p.ExitCode -ne 0) {
    Write-Host "::error::Installed app smoke-test exited $($p.ExitCode)"
    Write-Host "INSTALLER_SMOKE_STATUS=FAILED_SMOKE_EXIT"
    exit 6
}

$deadline = (Get-Date).AddSeconds(30)
while (-not (Test-Path $smokeResult) -and (Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 1
}

if (-not (Test-Path $smokeResult)) {
    Write-Host "::error::smoke-result.json missing after installed launch - refusing unpackaged fallback"
    Write-Host "INSTALLER_SMOKE_STATUS=FAILED_SMOKE_MARKER"
    exit 7
}

$json = Get-Content -LiteralPath $smokeResult -Raw | ConvertFrom-Json
Write-Host "SMOKE_RESULT:"
Get-Content -LiteralPath $smokeResult
if (-not $json.ok) {
    Write-Host "::error::Installed smoke reported ok=false"
    Write-Host "INSTALLER_SMOKE_STATUS=FAILED_SMOKE_CHECKS"
    exit 8
}

$configPath = Join-Path $appDir "config.json"
if (-not (Test-Path $configPath)) {
    Write-Host "::error::config.json missing after smoke"
    Write-Host "INSTALLER_SMOKE_STATUS=FAILED_CONFIG"
    exit 9
}
Write-Host "CONFIG_OK=$configPath"

# Process must not still be running hung
$lingering = Get-Process -Name "3DGodCreator.App" -ErrorAction SilentlyContinue
if ($lingering) {
    Write-Host "Stopping lingering smoke processes..."
    $lingering | Stop-Process -Force -ErrorAction SilentlyContinue
}

# 4) Uninstall via Update.exe when present
Write-Section "UNINSTALL"
$update = Get-ChildItem -Path $InstallRoot -Filter "Update.exe" -Recurse -ErrorAction SilentlyContinue |
    Select-Object -First 1
if ($SkipUninstall) {
    Write-Host "SkipUninstall set"
}
elseif ($update) {
    Write-Host "Running: $($update.FullName) uninstall --silent"
    $u = Start-Process -FilePath $update.FullName -ArgumentList @("uninstall", "--silent") -PassThru -Wait -NoNewWindow
    Write-Host "Uninstall exit=$($u.ExitCode)"
}
else {
    Write-Host "WARN: Update.exe not found; removing InstallRoot manually"
    Remove-Item -Recurse -Force $InstallRoot -ErrorAction SilentlyContinue
}

Write-Section "INSTALLER SMOKE SUMMARY"
Write-Host "INSTALLER_SMOKE_STATUS=SUCCESS"
Write-Host "SETUP=$($setup.FullName)"
Write-Host "INSTALLED_EXE=$($exe.FullName)"
Write-Host "SMOKE_RESULT=$smokeResult"
exit 0
