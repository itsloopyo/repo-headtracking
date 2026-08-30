#!/usr/bin/env pwsh
#Requires -Version 5.1
# Populate lib/ with build references from REPO FILES ONLY:
#   - BepInEx.dll / 0Harmony.dll extracted from the committed vendor zip
#   - Unity reference assemblies compiled from the shared stub sources in
#     cameraunlock-core/csharp/stubs
# No game installation and no network access. This is exactly what a CI
# runner does, so a local build cannot drift from the CI build.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$libPath = Join-Path $projectRoot "lib"

Write-Host "Setting up build references (stubs + vendored loader)..." -ForegroundColor Cyan

$vendorZip = Join-Path $projectRoot "vendor\bepinex\BepInEx_win_x64.zip"
if (-not (Test-Path $vendorZip)) {
    throw "Vendored BepInEx zip missing: $vendorZip. Run 'pixi run update-deps' and commit the result."
}

$stubBuilder = Join-Path $projectRoot "cameraunlock-core/csharp/stubs/build-unity-stubs.ps1"
if (-not (Test-Path $stubBuilder)) {
    throw "Shared stub builder missing: $stubBuilder. Run 'git submodule update --init'."
}

# Wipe lib/ so a local run starts from the same empty directory a fresh CI
# checkout has. Without this, a stale DLL left behind by an earlier run can
# satisfy a reference the stubs no longer provide. Everything this run needs
# lives outside lib/ and is checked for above, so the wipe cannot strand it.
if (Test-Path $libPath) {
    Get-ChildItem -Path $libPath -Force | Remove-Item -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $libPath | Out-Null
}

$vendorTemp = Join-Path $libPath "_vendor-extract"
Expand-Archive -Path $vendorZip -DestinationPath $vendorTemp -Force
foreach ($dll in @("BepInEx.dll", "0Harmony.dll")) {
    $src = Join-Path $vendorTemp "BepInEx\core\$dll"
    if (-not (Test-Path $src)) { throw "$dll not found in $vendorZip" }
    Copy-Item $src $libPath -Force
    Write-Host "  $dll (vendored)" -ForegroundColor Green
}
Remove-Item -Recurse -Force $vendorTemp

# The shared builder's default -EmptyModule set on net472 is exactly the module
# list this mod's csproj and CameraUnlock.Core.Unity reference, so no override.
& $stubBuilder -OutputPath $libPath -TargetFramework net472
if ($LASTEXITCODE -ne 0) { throw "Stub build failed" }

Write-Host "Setup complete (no game install, no network)" -ForegroundColor Green
