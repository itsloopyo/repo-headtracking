#!/usr/bin/env pwsh
#Requires -Version 5.1
# Packaging for R.E.P.O. Head Tracking. Thin wrapper over the shared BepInEx
# packager; produces both ZIPs in release/:
#   REPOHeadTracking-v{version}-installer.zip  (GitHub: install.cmd + shared/ +
#                                               plugins/ + vendored BepInEx + docs
#                                               + stamped launcher-manifest.json)
#   REPOHeadTracking-v{version}-nexus.zip      (extract-to-game-folder)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = 'SilentlyContinue'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

foreach ($script in @("install.cmd", "uninstall.cmd")) {
    if (-not (Test-Path (Join-Path $scriptDir $script))) {
        throw "Required script not found: scripts/$script"
    }
}

# The shared packager skips vendor/ when it is absent (mods that ship no
# loader). This mod's launcher-manifest.json declares the BepInEx archive, so
# a missing vendor zip would ship a package the launcher cannot provision.
$vendorZip = Join-Path $projectDir "vendor\bepinex\BepInEx_win_x64.zip"
if (-not (Test-Path $vendorZip)) {
    throw "Bundled BepInEx vendor zip missing: $vendorZip. Run 'pixi run update-deps' and commit the result."
}

& (Join-Path $projectDir "cameraunlock-core\scripts\package-bepinex-mod.ps1") `
    -ModName 'REPOHeadTracking' `
    -CsprojPath 'src\REPOHeadTracking\REPOHeadTracking.csproj' `
    -BuildOutputDir 'src\REPOHeadTracking\bin\Release\net472' `
    -ModDlls @('REPOHeadTracking.dll', 'CameraUnlock.Core.dll', 'CameraUnlock.Core.Unity.dll') `
    -ProjectRoot $projectDir `
    -CreateNexusZip
