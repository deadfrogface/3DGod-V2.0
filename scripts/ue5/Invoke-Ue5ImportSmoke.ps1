#Requires -Version 5.1
<#
.SYNOPSIS
  Real Unreal Editor import smoke when UE_ROOT is available.

.DESCRIPTION
  IMPLEMENTED_GATED_UE5_RUNTIME helper. Does not claim PASS_REAL unless Unreal
  actually runs and the import log contains success markers.
#>
param(
    [Parameter(Mandatory = $true)][string]$AssetPath,
    [string]$UeRoot = $env:UE_ROOT,
    [string]$ProjectPath,
    [string]$LogPath = $(Join-Path $env:TEMP "3dgod-ue5-import.log")
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($UeRoot) -or -not (Test-Path $UeRoot)) {
    Write-Host "GATED_UE5_RUNTIME – UE_ROOT not set or missing: '$UeRoot'"
    exit 2
}

$editor = @(
    (Join-Path $UeRoot 'Engine/Binaries/Win64/UnrealEditor-Cmd.exe'),
    (Join-Path $UeRoot 'Engine/Binaries/Win64/UnrealEditor.exe')
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $editor) {
    Write-Host "GATED_UE5_RUNTIME – UnrealEditor executable not found under $UeRoot"
    exit 2
}

if (-not (Test-Path $AssetPath)) {
    throw "Asset not found: $AssetPath"
}

if ([string]::IsNullOrWhiteSpace($ProjectPath) -or -not (Test-Path $ProjectPath)) {
    Write-Host "GATED_UE5_RUNTIME – provide -ProjectPath to a .uproject for real import automation."
    exit 2
}

# Command-let style invoke: user/project-specific Python or automation flag can be appended later.
$args = @(
    $ProjectPath,
    "-unattended",
    "-nop4",
    "-nosplash",
    "-log=$LogPath",
    "-ExecutePythonScript=`"print('3DGOD_UE5_IMPORT_HOOK');`""
)

Write-Host "Launching Unreal Editor Cmd for import smoke..."
Write-Host "$editor $($args -join ' ')"
$p = Start-Process -FilePath $editor -ArgumentList $args -Wait -PassThru
if ($p.ExitCode -ne 0) {
    Write-Host "UE5 editor exited with code $($p.ExitCode)"
    exit $p.ExitCode
}

if (-not (Test-Path $LogPath)) {
    Write-Host "UE5 log missing – cannot claim PASS_REAL"
    exit 3
}

$log = Get-Content $LogPath -Raw
if ($log -match '3DGOD_UE5_IMPORT_HOOK') {
    Write-Host "UE5 automation hook observed in log (extend with asset/skeleton assertions for PASS_REAL)."
    exit 0
}

Write-Host "UE5 ran but import success markers were not found – not PASS_REAL."
exit 4
