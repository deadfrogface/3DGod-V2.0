#Requires -Version 5.1
<#
.SYNOPSIS
  Root build entry point for 3D God Creator (SAFE REUSE build unification).
#>
param(
    [ValidateSet('validate','restore','build','test','publish','package','smoke','all')]
    [string[]]$Targets = @('all'),
    [string]$Configuration = 'Release',
    [string]$Version = '2.0.0',
    [switch]$SkipVpk,
    [switch]$SelfContained
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
Set-Location $root

function Invoke-Step([string]$Name, [scriptblock]$Body) {
    Write-Host ""
    Write-Host "==> $Name" -ForegroundColor Cyan
    & $Body
    if ($LASTEXITCODE -ne 0 -and $null -ne $LASTEXITCODE) {
        throw "Step failed: $Name (exit $LASTEXITCODE)"
    }
}

$runAll = $Targets -contains 'all'
$sln = Join-Path $root '3DGodCreator.sln'
$testProj = Join-Path $root '3DGodCreator.Core.Tests/3DGodCreator.Core.Tests.csproj'

if ($runAll -or $Targets -contains 'validate') {
    Invoke-Step 'validate' {
        if (-not (Test-Path (Join-Path $root 'global.json'))) { throw 'global.json missing' }
        if (-not (Test-Path $sln)) { throw '3DGodCreator.sln missing' }
        dotnet --info | Out-Host
    }
}

if ($runAll -or $Targets -contains 'restore') {
    Invoke-Step 'restore' { dotnet restore $sln }
}

if ($runAll -or $Targets -contains 'build') {
    Invoke-Step 'build' {
        if ($Targets -notcontains 'restore' -and -not $runAll) {
            dotnet build $sln -c $Configuration -v minimal
        } else {
            dotnet build $sln -c $Configuration --no-restore -v minimal
        }
    }
}

if ($runAll -or $Targets -contains 'test') {
    Invoke-Step 'test' {
        New-Item -ItemType Directory -Force -Path (Join-Path $root 'artifacts/test') | Out-Null
        # Build tests if publish-only callers skipped build.
        dotnet test $testProj -c $Configuration `
            --logger "trx;LogFileName=results.trx" `
            --logger "console;verbosity=normal" `
            --results-directory (Join-Path $root 'artifacts/test')
    }
}

if ($runAll -or $Targets -contains 'publish' -or $Targets -contains 'package') {
    Invoke-Step 'publish+velopack' {
        $script = Join-Path $root 'scripts/packaging/Build-VelopackRelease.ps1'
        $packArgs = @("-Version", $Version)
        if ($SkipVpk) { $packArgs += '-SkipVpk' }
        if ($SelfContained) { $packArgs += '-SelfContained' }
        & $script @packArgs
    }
}

if ($Targets -contains 'smoke') {
    Invoke-Step 'clean-install-smoke' {
        & (Join-Path $root 'scripts/ci/Invoke-CleanInstallSmoke.ps1')
    }
}

Write-Host ""
Write-Host "build.ps1 completed: $($Targets -join ',')" -ForegroundColor Green
