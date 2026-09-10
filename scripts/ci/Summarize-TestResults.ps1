#Requires -Version 5.1
param(
    [Parameter(Mandatory = $true)]
    [string] $TrxPath,
    [switch] $FailOnTestFailures
)

$ErrorActionPreference = "Stop"

function Write-Section([string] $title) {
    Write-Host ""
    Write-Host ("=" * 72)
    Write-Host $title
    Write-Host ("=" * 72)
}

function Get-GateReason([string] $message) {
    if ([string]::IsNullOrWhiteSpace($message)) { return $null }
    $clean = $message -replace '^\$XunitDynamicSkip\$', ''
    if ($clean -match '(GATED_[A-Z0-9_]+)') {
        return @{
            Category = $Matches[1]
            Reason   = $clean.Trim()
        }
    }
    return $null
}

if (-not (Test-Path -LiteralPath $TrxPath)) {
    Write-Host "::error::TRX file not found: $TrxPath"
    exit 2
}

[xml] $trx = Get-Content -LiteralPath $TrxPath -Raw
$ns = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
$ns.AddNamespace("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")

$counters = $trx.SelectSingleNode("//t:ResultSummary/t:Counters", $ns)
$total = [int]$counters.total
$passed = [int]$counters.passed
$failed = [int]$counters.failed
$skipped = [int]$counters.notExecuted
if ($counters.error) { $failed += [int]$counters.error }

$unitResults = $trx.SelectNodes("//t:UnitTestResult", $ns)
$gated = New-Object System.Collections.Generic.List[object]
$gatedByCategory = @{}
$misclassifiedFails = 0
$skippedFromOutcomes = 0

foreach ($r in $unitResults) {
    $outcome = $r.outcome
    $message = ""
    $msgNode = $r.SelectSingleNode("t:Output/t:ErrorInfo/t:Message", $ns)
    if ($msgNode) { $message = $msgNode.InnerText }

    # SkippableFact often records outcome=Skipped while Counters.notExecuted stays 0.
    $isSkipOutcome = ($outcome -eq "NotExecuted" -or $outcome -eq "Skipped")
    if ($isSkipOutcome) { $skippedFromOutcomes++ }

    $gate = Get-GateReason $message

    if ($null -ne $gate -and $isSkipOutcome) {
        $gated.Add([pscustomobject]@{
            Test     = $r.testName
            Category = $gate.Category
            Reason   = $gate.Reason
        }) | Out-Null
        if (-not $gatedByCategory.ContainsKey($gate.Category)) {
            $gatedByCategory[$gate.Category] = New-Object System.Collections.Generic.List[string]
        }
        $gatedByCategory[$gate.Category].Add($gate.Reason) | Out-Null
    }
    elseif ($null -ne $gate -and $outcome -eq "Failed") {
        # Should not happen with SkippableFact; treat as CI defect (not a green PASS).
        $misclassifiedFails++
        Write-Host "::error::GATED_* surfaced as Failed instead of Skipped: $($r.testName) :: $($gate.Reason)"
    }
}

if ($skippedFromOutcomes -gt $skipped) { $skipped = $skippedFromOutcomes }

$gatedCount = $gated.Count
$skippedNonGated = [Math]::Max(0, $skipped - $gatedCount)

Write-Section "CI TEST SUMMARY"
Write-Host "BUILD_STATUS: SUCCESS (tests executed after successful Release build)"
Write-Host "TESTS_TOTAL:   $total"
Write-Host "TESTS_PASSED:  $passed"
Write-Host "TESTS_FAILED:  $failed"
Write-Host "TESTS_SKIPPED: $skipped"
Write-Host "TESTS_GATED:   $gatedCount  (explicit GATED_* - NOT counted as PASS)"
Write-Host "TESTS_SKIPPED_OTHER: $skippedNonGated"

if ($gatedCount -gt 0) {
    Write-Section "GATED CATEGORIES (not PASS)"
    foreach ($cat in ($gatedByCategory.Keys | Sort-Object)) {
        $items = $gatedByCategory[$cat]
        Write-Host ""
        Write-Host "$cat : $($items.Count)"
        foreach ($reason in ($items | Select-Object -Unique)) {
            Write-Host "  - $reason"
        }
    }

    Write-Section "GATED TESTS DETAIL"
    foreach ($g in $gated) {
        Write-Host ("[{0}] {1}" -f $g.Category, $g.Test)
        Write-Host ("         {0}" -f $g.Reason)
    }
}

Write-Host ""
Write-Host "NOTE: GATED_* outcomes are environment/hardware/license/runtime gates."
Write-Host "They must never be reported as PASS."

if ($misclassifiedFails -gt 0) {
    Write-Host "::error::CI FAILED: $misclassifiedFails GATED_* test(s) misclassified as Failed"
    exit 1
}

if ($FailOnTestFailures -and $failed -gt 0) {
    Write-Host "::error::CI FAILED: $failed test(s) failed"
    exit 1
}

if ($failed -gt 0) {
    exit 1
}

Write-Host "CI_TEST_STATUS: SUCCESS"
exit 0
