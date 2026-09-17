#Requires -Version 5.1
<#
.SYNOPSIS
  Provision skin-tokens.cpp CLI (build from pinned commit) + SkinTokens-GGUF F16 models
  into the product InstallLayout (%LocalAppData%/3DGod/...).
  End users do not run this manually; CI / Setup Assistant invoke it.
#>
param(
    [string]$SourceDir = "",
    [string]$CacheDir = "",
    [string]$Commit = "43e885af2eadee9c40aa85849b71528d1c958293",
    [string]$RepoUrl = "https://github.com/localai-org/skin-tokens.cpp.git",
    [switch]$SkipBuild,
    [switch]$SkipModels,
    [switch]$SkipIfPresent
)

$ErrorActionPreference = "Stop"

function Write-Section([string]$t) {
    Write-Host ""
    Write-Host ("=" * 72)
    Write-Host $t
    Write-Host ("=" * 72)
}

$componentsBin = Join-Path $env:LOCALAPPDATA "3DGod\Components\skintokens-cpp\bin"
$modelDir = Join-Path $env:LOCALAPPDATA "3DGod\Models\skintokens-cpp\F16"
$cliExe = Join-Path $componentsBin "skintokens-cli.exe"

if (-not $CacheDir) {
    if ($env:RUNNER_TEMP) { $CacheDir = Join-Path $env:RUNNER_TEMP "3dgod-skintokens-cpp-cache" }
    else { $CacheDir = Join-Path $env:TEMP "3dgod-skintokens-cpp-cache" }
}
New-Item -ItemType Directory -Force -Path $CacheDir | Out-Null

Write-Section "PROVISION skin-tokens.cpp"
Write-Host "COMMIT=$Commit"
Write-Host "CLI_TARGET=$cliExe"
Write-Host "MODEL_DIR=$modelDir"
Write-Host "CACHE=$CacheDir"

if ($SkipIfPresent -and (Test-Path $cliExe) -and (Test-Path (Join-Path $modelDir "tokenrig.gguf"))) {
    Write-Host "Already present – SkipIfPresent"
    Write-Host "SKINTOKENS_CPP_PROVISION=PRESENT"
    exit 0
}

# --- Models (HF LFS SHA-256 oids) ---
$models = @(
    @{ Name = "mesh-encoder.gguf"; Sha = "532710809e3db6c54389dd6489c2aa3c768244868b9c197198d06fa664214527"; Size = 57813184 },
    @{ Name = "skin-vae.gguf"; Sha = "dcd5859ac89bae62bcfd6b7823f9d0e2393b22364bcb19f84c1109af8e07455e"; Size = 244014784 },
    @{ Name = "tokenrig.gguf"; Sha = "933529dc0e550fd499e2e1ae1c574c6d5852047c04832e7207304e184901ed82"; Size = 948562656 }
)
$modelRepo = "LocalAI-io/SkinTokens-GGUF"
$modelRev = "main"

if (-not $SkipModels) {
    Write-Section "DOWNLOAD SkinTokens-GGUF F16"
    New-Item -ItemType Directory -Force -Path $modelDir | Out-Null
    $modelCache = Join-Path $CacheDir "models-F16"
    New-Item -ItemType Directory -Force -Path $modelCache | Out-Null
    foreach ($m in $models) {
        $url = "https://huggingface.co/$modelRepo/resolve/$modelRev/F16/$($m.Name)"
        $cached = Join-Path $modelCache $m.Name
        $dest = Join-Path $modelDir $m.Name
        if ((Test-Path $cached) -and ((Get-Item $cached).Length -eq $m.Size)) {
            $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $cached).Hash.ToLowerInvariant()
            if ($hash -ne $m.Sha) { Remove-Item -Force $cached }
        }
        if (-not (Test-Path $cached) -or ((Get-Item $cached).Length -ne $m.Size)) {
            Write-Host "Downloading $($m.Name) ($($m.Size) bytes)…"
            & curl.exe -L --fail --retry 5 --retry-delay 5 -o $cached $url
            if ($LASTEXITCODE -ne 0) { throw "Model download failed: $($m.Name)" }
        }
        $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $cached).Hash.ToLowerInvariant()
        if ($hash -ne $m.Sha) { throw "SHA-256 mismatch for $($m.Name): got $hash expected $($m.Sha)" }
        Copy-Item -Force $cached $dest
        Write-Host "OK $($m.Name) sha=$hash"
    }
    Write-Host "MODELS_READY=$modelDir"
}

if ($SkipBuild) {
    if (-not (Test-Path $cliExe)) { throw "SkipBuild set but CLI missing at $cliExe" }
    Write-Host "SKINTOKENS_CPP_PROVISION=MODELS_ONLY"
    exit 0
}

# --- Build CLI from pinned upstream ---
Write-Section "BUILD skin-tokens.cpp (CPU, Vulkan OFF)"
if (-not $SourceDir) {
    $SourceDir = Join-Path $CacheDir "src\skin-tokens.cpp"
}
if (-not (Test-Path (Join-Path $SourceDir ".git"))) {
    New-Item -ItemType Directory -Force -Path (Split-Path $SourceDir -Parent) | Out-Null
    if (Test-Path $SourceDir) { Remove-Item -Recurse -Force $SourceDir }
    git clone --filter=blob:none --no-checkout $RepoUrl $SourceDir
    Push-Location $SourceDir
    try {
        git fetch --depth 1 origin $Commit
        git checkout $Commit
        git submodule update --init --recursive
    } finally { Pop-Location }
} else {
    Push-Location $SourceDir
    try {
        $head = (git rev-parse HEAD).Trim()
        if ($head -ne $Commit) {
            git fetch --depth 1 origin $Commit
            git checkout $Commit
            git submodule update --init --recursive
        }
    } finally { Pop-Location }
}

# nlohmann_json via vcpkg
$vcpkgRoot = Join-Path $CacheDir "vcpkg"
if (-not (Test-Path (Join-Path $vcpkgRoot "vcpkg.exe"))) {
    git clone --depth 1 https://github.com/microsoft/vcpkg.git $vcpkgRoot
    & (Join-Path $vcpkgRoot "bootstrap-vcpkg.bat") -disableMetrics
    if ($LASTEXITCODE -ne 0) { throw "vcpkg bootstrap failed" }
}
& (Join-Path $vcpkgRoot "vcpkg.exe") install nlohmann-json:x64-windows --disable-metrics
if ($LASTEXITCODE -ne 0) { throw "vcpkg install nlohmann-json failed" }

# Enter MSVC developer environment (GitHub windows-latest often needs this for VS generator).
$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
$vsPath = $null
if (Test-Path $vswhere) {
    $vsPath = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
    if ($vsPath) {
        Write-Host "VS_INSTALL=$vsPath"
        $devShell = Join-Path $vsPath "Common7\Tools\Microsoft.VisualStudio.DevShell.dll"
        if (Test-Path $devShell) {
            Import-Module $devShell
            Enter-VsDevShell -VsInstallPath $vsPath -SkipAutomaticLocation -DevCmdArguments "-arch=x64 -host_arch=x64"
        } else {
            $vcvars = Join-Path $vsPath "VC\Auxiliary\Build\vcvars64.bat"
            if (Test-Path $vcvars) {
                cmd /c "`"$vcvars`" && set" | ForEach-Object {
                    if ($_ -match '^(.*?)=(.*)$') {
                        [System.Environment]::SetEnvironmentVariable($matches[1], $matches[2])
                    }
                }
            }
        }
    }
}
if (-not (Get-Command cl -ErrorAction SilentlyContinue)) {
    Write-Host "WARN: cl.exe not on PATH after VS setup — cmake may still find VS via generator instance"
}

# Prefer Ninja single-config when available (more reliable on GH runners than VS generator discovery).
$ninja = Get-Command ninja -ErrorAction SilentlyContinue
if (-not $ninja) {
    $ninjaZip = Join-Path $CacheDir "ninja-win.zip"
    $ninjaDir = Join-Path $CacheDir "ninja"
    if (-not (Test-Path (Join-Path $ninjaDir "ninja.exe"))) {
        New-Item -ItemType Directory -Force -Path $ninjaDir | Out-Null
        & curl.exe -L --fail --retry 5 -o $ninjaZip "https://github.com/ninja-build/ninja/releases/download/v1.12.1/ninja-win.zip"
        if ($LASTEXITCODE -ne 0) { throw "ninja download failed" }
        Expand-Archive -Force -Path $ninjaZip -DestinationPath $ninjaDir
    }
    $env:Path = "$ninjaDir;$env:Path"
}

$buildDir = Join-Path $CacheDir "build-release"
$distDir = Join-Path $CacheDir "dist"
if (Test-Path $buildDir) { Remove-Item -Recurse -Force $buildDir }
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null

$toolchain = Join-Path $vcpkgRoot "scripts\buildsystems\vcpkg.cmake"
$cmakeArgs = @(
    "-S", $SourceDir,
    "-B", $buildDir,
    "-G", "Ninja",
    "-DCMAKE_BUILD_TYPE=Release",
    "-DCMAKE_TOOLCHAIN_FILE=$toolchain",
    "-DVCPKG_TARGET_TRIPLET=x64-windows",
    "-DSKINTOKENS_ENABLE_VULKAN=OFF",
    "-DSKINTOKENS_BUILD_TESTS=OFF",
    "-DSKINTOKENS_DYNAMIC_BACKENDS=ON",
    "-DSKINTOKENS_CPU_ALL_VARIANTS=OFF"
)
# Ensure C/CXX compilers are the MSVC ones when cl is available.
if (Get-Command cl -ErrorAction SilentlyContinue) {
    $cmakeArgs += @("-DCMAKE_C_COMPILER=cl", "-DCMAKE_CXX_COMPILER=cl")
}
Write-Host "cmake $($cmakeArgs -join ' ')"
& cmake @cmakeArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "Ninja configure failed — falling back to Visual Studio generator with explicit instance"
    if (-not $vsPath) { throw "cmake configure failed and no VS install found via vswhere" }
    if (Test-Path $buildDir) { Remove-Item -Recurse -Force $buildDir }
    New-Item -ItemType Directory -Force -Path $buildDir | Out-Null
    $cmakeArgs = @(
        "-S", $SourceDir,
        "-B", $buildDir,
        "-G", "Visual Studio 17 2022",
        "-A", "x64",
        "-DCMAKE_GENERATOR_INSTANCE=$vsPath",
        "-DCMAKE_TOOLCHAIN_FILE=$toolchain",
        "-DVCPKG_TARGET_TRIPLET=x64-windows",
        "-DSKINTOKENS_ENABLE_VULKAN=OFF",
        "-DSKINTOKENS_BUILD_TESTS=OFF",
        "-DSKINTOKENS_DYNAMIC_BACKENDS=ON",
        "-DSKINTOKENS_CPU_ALL_VARIANTS=OFF"
    )
    & cmake @cmakeArgs
    if ($LASTEXITCODE -ne 0) { throw "cmake configure failed" }
    & cmake --build $buildDir --config Release --parallel
    if ($LASTEXITCODE -ne 0) { throw "cmake build failed" }
    if (Test-Path $distDir) { Remove-Item -Recurse -Force $distDir }
    & cmake --install $buildDir --prefix $distDir --config Release
    if ($LASTEXITCODE -ne 0) { throw "cmake install failed" }
} else {
    & cmake --build $buildDir --parallel
    if ($LASTEXITCODE -ne 0) { throw "cmake build failed" }
    if (Test-Path $distDir) { Remove-Item -Recurse -Force $distDir }
    & cmake --install $buildDir --prefix $distDir
    if ($LASTEXITCODE -ne 0) { throw "cmake install failed" }
}

$builtCli = Get-ChildItem -Path $distDir -Filter "skintokens-cli.exe" -Recurse | Select-Object -First 1
if (-not $builtCli) {
    $builtCli = Get-ChildItem -Path $buildDir -Filter "skintokens-cli.exe" -Recurse |
        Where-Object { $_.FullName -match '\\Release\\' } |
        Select-Object -First 1
}
if (-not $builtCli) { throw "skintokens-cli.exe not found after build" }

New-Item -ItemType Directory -Force -Path $componentsBin | Out-Null
# Copy CLI + sibling DLLs / ggml backends from the same directory tree
$builtDir = $builtCli.Directory.FullName
Copy-Item -Force (Join-Path $builtDir "*") $componentsBin
# Also copy from dist bin/lib if present
foreach ($sub in @("bin", "lib", "bin\Release", "lib\Release")) {
    $p = Join-Path $distDir $sub
    if (Test-Path $p) {
        Get-ChildItem $p -File -ErrorAction SilentlyContinue | ForEach-Object {
            Copy-Item -Force $_.FullName $componentsBin
        }
    }
}

if (-not (Test-Path $cliExe)) {
    Copy-Item -Force $builtCli.FullName $cliExe
}
if (-not (Test-Path $cliExe)) { throw "Failed to install CLI to $cliExe" }

# Mirror into worker dist for packaging / local source hint
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$workerDist = Join-Path $repoRoot "workers\skintokens-cpp\dist\bin"
New-Item -ItemType Directory -Force -Path $workerDist | Out-Null
Copy-Item -Force (Join-Path $componentsBin "*") $workerDist

$marker = Join-Path (Split-Path $componentsBin -Parent) "provision.json"
@{
    upstreamCommit = $Commit
    upstreamRepo = $RepoUrl
    cli = $cliExe
    modelDir = $modelDir
    provisionedUtc = (Get-Date).ToUniversalTime().ToString("o")
    device = "cpu"
} | ConvertTo-Json | Set-Content -LiteralPath $marker -Encoding UTF8

Write-Host "CLI_READY=$cliExe"
Write-Host "SKINTOKENS_CPP_PROVISION=SUCCESS"
exit 0
