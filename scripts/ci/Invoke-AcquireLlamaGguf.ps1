#Requires -Version 5.1
<#
.SYNOPSIS
  Download the pinned Apache-2.0 Qwen2.5-0.5B-Instruct Q4_K_M GGUF for LLamaSharp CI proof.
  Writes license.json sidecar. Does not upload the model as a CI artifact.
#>
param(
    [string]$ModelDir = "",
    [switch]$SkipIfPresent
)

$ErrorActionPreference = "Stop"

# Pinned commercial-clear (Apache-2.0) GGUF — record name/revision/license/hash.
$ModelId = "Qwen/Qwen2.5-0.5B-Instruct-GGUF"
$FileName = "qwen2.5-0.5b-instruct-q4_k_m.gguf"
$Revision = "9217f5db79a29953eb74d5343926648285ec7e67"
$Sha256 = "74a4da8c9fdbcd15bd1f6d01d621410d31c6fc00986f5eb687824e7b93d7a9db"
$SizeBytes = 491400032
$LicenseId = "apache-2.0"
$Url = "https://huggingface.co/$ModelId/resolve/$Revision/$FileName"

if (-not $ModelDir) {
    if ($IsWindows -or $env:OS -match "Windows") {
        $ModelDir = Join-Path $env:LOCALAPPDATA "3DGod\Models\llama"
    } else {
        $xdg = if ($env:XDG_DATA_HOME) { $env:XDG_DATA_HOME } else { Join-Path $HOME ".local/share" }
        $ModelDir = Join-Path $xdg "3DGod/Models/llama"
    }
}

New-Item -ItemType Directory -Force -Path $ModelDir | Out-Null
$dest = Join-Path $ModelDir $FileName
$license = Join-Path $ModelDir "license.json"

Write-Host "LLAMA_GGUF_DIR=$ModelDir"
Write-Host "LLAMA_GGUF_URL=$Url"
Write-Host "LLAMA_GGUF_SHA256=$Sha256"
Write-Host "LLAMA_GGUF_REVISION=$Revision"
Write-Host "LLAMA_GGUF_LICENSE=$LicenseId"

if ($SkipIfPresent -and (Test-Path $dest) -and ((Get-Item $dest).Length -eq $SizeBytes)) {
    Write-Host "GGUF already present with expected size – verifying hash…"
} elseif (Test-Path $dest) {
    Remove-Item -Force $dest
}

if (-not (Test-Path $dest) -or (Get-Item $dest).Length -ne $SizeBytes) {
    Write-Host "Downloading GGUF ($SizeBytes bytes)…"
    # Resume-friendly curl; fail hard on HTTP errors.
    & curl.exe -L --fail --retry 5 --retry-delay 5 -o $dest $Url
    if ($LASTEXITCODE -ne 0) { throw "GGUF download failed (exit $LASTEXITCODE)" }
}

$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $dest).Hash.ToLowerInvariant()
if ($hash -ne $Sha256) {
    throw "GGUF SHA-256 mismatch: got $hash expected $Sha256"
}
Write-Host "LLAMA_GGUF_HASH_OK=$hash"

$sidecar = @{
    accepted = $true
    licenseId = $LicenseId
    modelId = $ModelId
    fileName = $FileName
    revision = $Revision
    sha256 = $Sha256
    sizeBytes = $SizeBytes
    sourceUrl = $Url
    acceptedUtc = (Get-Date).ToUniversalTime().ToString("o")
} | ConvertTo-Json
Set-Content -LiteralPath $license -Value $sidecar -Encoding UTF8
Write-Host "WROTE $license"
Write-Host "LLAMA_GGUF_ACQUIRE=SUCCESS"
