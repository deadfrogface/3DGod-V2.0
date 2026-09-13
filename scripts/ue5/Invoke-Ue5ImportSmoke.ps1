#Requires -Version 5.1
<#
.SYNOPSIS
  Real Unreal Editor import smoke when UE_ROOT is available.

.DESCRIPTION
  IMPLEMENTED_GATED_UE5_RUNTIME helper. Does not claim PASS_REAL unless Unreal
  actually runs and the import log contains 3DGOD_UE5_IMPORT_OK (and optional
  skeleton/materials/morph/LOD markers).

.EXITCODES
  0 = PASS_REAL markers present
  2 = GATED_UE5_RUNTIME (UE_ROOT / editor / project missing)
  3 = log file missing after run
  4 = ran without required success markers
#>
param(
    [Parameter(Mandatory = $true)][string]$AssetPath,
    [string]$UeRoot = $env:UE_ROOT,
    [string]$ProjectPath,
    [string]$DestinationPath = '/Game/Imported/ThreeDGodImport',
    [string]$LogPath = $(Join-Path $env:TEMP "3dgod-ue5-import.log")
)

$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$PyScript = Join-Path $ScriptDir 'python/ue5_import_asset.py'

if ([string]::IsNullOrWhiteSpace($UeRoot) -or -not (Test-Path -LiteralPath $UeRoot)) {
    Write-Host "GATED_UE5_RUNTIME – UE_ROOT not set or missing: '$UeRoot'"
    exit 2
}

# Best-effort UE version folder check (Engine/Build/Build.version or similar).
$versionCandidates = @(
    (Join-Path $UeRoot 'Engine/Build/Build.version'),
    (Join-Path $UeRoot 'Engine/Source/Runtime/Launch/Resources/Version.h')
)
$versionHit = $versionCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if ($versionHit) {
    Write-Host "UE version metadata found: $versionHit"
} else {
    Write-Host "UE version metadata not found under $UeRoot (continuing if editor binary exists)."
}

$editor = @(
    (Join-Path $UeRoot 'Engine/Binaries/Win64/UnrealEditor-Cmd.exe'),
    (Join-Path $UeRoot 'Engine/Binaries/Linux/UnrealEditor-Cmd'),
    (Join-Path $UeRoot 'Engine/Binaries/Win64/UnrealEditor.exe'),
    (Join-Path $UeRoot 'Engine/Binaries/Linux/UnrealEditor')
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

if (-not $editor) {
    Write-Host "GATED_UE5_RUNTIME – UnrealEditor-Cmd executable not found under $UeRoot"
    exit 2
}

if (-not (Test-Path -LiteralPath $AssetPath)) {
    throw "Asset not found: $AssetPath"
}

if ([string]::IsNullOrWhiteSpace($ProjectPath) -or -not (Test-Path -LiteralPath $ProjectPath)) {
    Write-Host "GATED_UE5_RUNTIME – provide -ProjectPath to a .uproject for real import automation."
    exit 2
}

if (-not (Test-Path -LiteralPath $PyScript)) {
    throw "Missing Unreal Python helper: $PyScript"
}

if (Test-Path -LiteralPath $LogPath) {
    Remove-Item -LiteralPath $LogPath -Force
}

$env:THREEDGOD_UE5_ASSET = (Resolve-Path -LiteralPath $AssetPath).Path
$env:THREEDGOD_UE5_DEST = $DestinationPath
$env:THREEDGOD_UE5_LOG_MARKERS = '1'

$argList = @(
    "`"$ProjectPath`"",
    '-unattended',
    '-nop4',
    '-nosplash',
    "-log=`"$LogPath`"",
    "-ExecutePythonScript=`"$PyScript`""
)

Write-Host "Launching Unreal Editor Cmd for import smoke..."
Write-Host "$editor $($argList -join ' ')"
$p = Start-Process -FilePath $editor -ArgumentList $argList -Wait -PassThru -NoNewWindow
if ($null -eq $p) {
    Write-Host "GATED_UE5_RUNTIME – failed to start UnrealEditor-Cmd"
    exit 2
}

if (-not (Test-Path -LiteralPath $LogPath)) {
    Write-Host "UE5 log missing – cannot claim PASS_REAL (exit 3)"
    exit 3
}

$log = Get-Content -LiteralPath $LogPath -Raw -ErrorAction Stop

if ($log -notmatch '3DGOD_UE5_IMPORT_OK') {
    Write-Host "UE5 ran but 3DGOD_UE5_IMPORT_OK was not found – not PASS_REAL (exit 4)."
    if ($log -match '3DGOD_UE5_IMPORT_FAIL') {
        Write-Host "Import failure marker present in log."
    }
    if ($p.ExitCode -ne 0) {
        Write-Host "UE5 editor process exit code: $($p.ExitCode)"
    }
    exit 4
}

Write-Host "PASS_REAL – 3DGOD_UE5_IMPORT_OK present."
foreach ($opt in @('3DGOD_UE5_SKELETON_OK', '3DGOD_UE5_MATERIALS_OK', '3DGOD_UE5_MORPH_OK', '3DGOD_UE5_LOD_OK')) {
    if ($log -match [regex]::Escape($opt)) {
        Write-Host "Optional marker: $opt"
    }
}

exit 0
