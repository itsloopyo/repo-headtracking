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

$modName = 'REPOHeadTracking'
$buildOutputDir = 'src/REPOHeadTracking/bin/Release/net472'
$modDlls = @('REPOHeadTracking.dll', 'CameraUnlock.Core.dll', 'CameraUnlock.Core.Unity.dll')

# Notices that must reach the root of both published ZIPs. cameraunlock-core is
# MIT under a different copyright holder than this mod's own LICENSE, so its
# text has to travel with the binary it is compiled into rather than being
# treated as covered by ours.
$coreLicenseEntry = 'licenses/cameraunlock-core-LICENSE.txt'
$coreLicenseSource = Join-Path $projectDir 'cameraunlock-core\LICENSE'
$requiredZipEntries = @('LICENSE', 'THIRD-PARTY-NOTICES.md', $coreLicenseEntry)

foreach ($script in @("install.cmd", "uninstall.cmd")) {
    if (-not (Test-Path (Join-Path $scriptDir $script))) {
        throw "Required script not found: scripts/$script"
    }
}

# Fatal rather than skipped: a missing licence is a compliance failure, and a
# guarded copy would turn it into a green build.
foreach ($noticeDoc in @('LICENSE', 'THIRD-PARTY-NOTICES.md')) {
    if (-not (Test-Path (Join-Path $projectDir $noticeDoc))) {
        throw "Required notice file not found: $noticeDoc. Every published ZIP is a binary distribution and must carry it."
    }
}
if (-not (Test-Path $coreLicenseSource)) {
    throw "cameraunlock-core LICENSE not found at $coreLicenseSource. Run 'git submodule update --init' - its MIT notice must ship with the binary it is compiled into."
}

# The shared packager skips vendor/ when it is absent (mods that ship no
# loader). This mod's launcher-manifest.json declares the BepInEx archive, so
# a missing vendor zip would ship a package the launcher cannot provision.
$vendorZip = Join-Path $projectDir "vendor\bepinex\BepInEx_win_x64.zip"
if (-not (Test-Path $vendorZip)) {
    throw "Bundled BepInEx vendor zip missing: $vendorZip. Run 'pixi run update-deps' and commit the result."
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Add-ZipEntry {
    param([string]$ZipPath, [string]$SourceFile, [string]$EntryName)

    $zip = [System.IO.Compression.ZipFile]::Open($ZipPath, 'Update')
    try {
        $existing = $zip.GetEntry($EntryName)
        if ($existing) { $existing.Delete() }
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $SourceFile, $EntryName) | Out-Null
    } finally {
        $zip.Dispose()
    }
}

function Assert-ZipCarriesNotices {
    param([string]$ZipPath, [string[]]$EntryNames)

    $zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        $present = $zip.Entries | ForEach-Object { $_.FullName.Replace('\', '/') }
    } finally {
        $zip.Dispose()
    }

    foreach ($entry in $EntryNames) {
        if ($present -notcontains $entry) {
            throw "$(Split-Path -Leaf $ZipPath) is missing $entry. Every published ZIP is a binary distribution and must carry the notices of everything in it."
        }
    }
}

$result = & (Join-Path $projectDir "cameraunlock-core\scripts\package-bepinex-mod.ps1") `
    -ModName $modName `
    -CsprojPath 'src\REPOHeadTracking\REPOHeadTracking.csproj' `
    -BuildOutputDir 'src\REPOHeadTracking\bin\Release\net472' `
    -ModDlls $modDlls `
    -ProjectRoot $projectDir

$version = [System.IO.Path]::GetFileNameWithoutExtension($result.GithubZip) -replace '^.*-v', '' -replace '-installer$', ''
$releaseDir = Join-Path $projectDir 'release'

# Injected here rather than staged by the shared packager: that script lives at
# whatever cameraunlock-core commit this mod pins, so its behaviour is frozen
# until the pointer moves, and a licence obligation cannot wait on a bump.
Add-ZipEntry -ZipPath $result.GithubZip -SourceFile $coreLicenseSource -EntryName $coreLicenseEntry
Write-Host "  $coreLicenseEntry" -ForegroundColor Green
Assert-ZipCarriesNotices -ZipPath $result.GithubZip -EntryNames $requiredZipEntries

# The Nexus ZIP is built here for the same reason: the shared packager's version
# ships the plugin DLLs alone, and a binary distribution with no LICENSE and no
# THIRD-PARTY-NOTICES.md satisfies neither the MIT nor the LGPL notices of what
# is compiled into it.
Write-Host ""
Write-Host "=== Creating NexusMods ZIP ===" -ForegroundColor Magenta

$nexusStagingDir = Join-Path $releaseDir 'staging-nexus'
if (Test-Path $nexusStagingDir) { Remove-Item -Recurse -Force $nexusStagingDir }
$nexusPluginsDir = Join-Path $nexusStagingDir 'BepInEx\plugins'
New-Item -ItemType Directory -Path $nexusPluginsDir -Force | Out-Null

foreach ($dll in $modDlls) {
    Copy-Item (Join-Path (Join-Path $projectDir $buildOutputDir) $dll) -Destination $nexusPluginsDir -Force
    Write-Host "  BepInEx/plugins/$dll" -ForegroundColor Green
}

foreach ($noticeDoc in @('LICENSE', 'THIRD-PARTY-NOTICES.md', 'README.md')) {
    Copy-Item (Join-Path $projectDir $noticeDoc) -Destination $nexusStagingDir -Force
    Write-Host "  $noticeDoc" -ForegroundColor Green
}

$nexusLicensesDir = Join-Path $nexusStagingDir 'licenses'
New-Item -ItemType Directory -Path $nexusLicensesDir -Force | Out-Null
Copy-Item $coreLicenseSource -Destination (Join-Path $nexusLicensesDir 'cameraunlock-core-LICENSE.txt') -Force
Write-Host "  $coreLicenseEntry" -ForegroundColor Green

$nexusZipPath = Join-Path $releaseDir "$modName-v$version-nexus.zip"
if (Test-Path $nexusZipPath) { Remove-Item $nexusZipPath -Force }
Push-Location $nexusStagingDir
try { Compress-Archive -Path '.\*' -DestinationPath $nexusZipPath -Force }
finally { Pop-Location }
Remove-Item -Recurse -Force $nexusStagingDir

Assert-ZipCarriesNotices -ZipPath $nexusZipPath -EntryNames $requiredZipEntries

Write-Host ("  $nexusZipPath ({0:N1} KB)" -f ((Get-Item $nexusZipPath).Length / 1KB)) -ForegroundColor Green
