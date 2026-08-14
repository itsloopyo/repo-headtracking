#!/usr/bin/env pwsh
#Requires -Version 5.1
<#
.SYNOPSIS
    Automated release workflow for R.E.P.O. Head Tracking mod.
.PARAMETER Version
    major | minor | patch | nightly | X.Y.Z
.EXAMPLE
    pixi run release patch
#>
param(
    [Parameter(Position=0)]
    [string]$Version = "",
    # Ship a release even when there are no user-facing commits since the
    # last tag (writes a maintenance changelog entry instead of aborting).
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$csprojPath = Join-Path $projectDir "src\REPOHeadTracking\REPOHeadTracking.csproj"

# Dev-channel rolling pre-release dispatch.
if ($Version -eq 'nightly') {
    & (Join-Path $PSScriptRoot 'release-nightly.ps1')
    exit $LASTEXITCODE
}

Import-Module (Join-Path $projectDir "cameraunlock-core\powershell\ReleaseWorkflow.psm1") -Force

# Mirrors New-ChangelogFromCommits' insertion so a -Force maintenance entry
# lands in the same place with the same shape.
function Add-MaintenanceChangelogEntry {
    param([string]$Path, [string]$NewVersion)
    $date = Get-Date -Format 'yyyy-MM-dd'
    $entry = "## [$NewVersion] - $date`n`n### Changed`n`n- Maintenance release (no user-facing changes).`n`n"
    $changelog = Get-Content $Path -Raw
    if ($changelog -match '(?s)(# Changelog.*?)(## \[)') {
        $changelog = $changelog -replace '(?s)(# Changelog.*?\n\n)', "`$1$entry"
    } else {
        $changelog = $changelog -replace '(?s)(# Changelog.*?\n)', "`$1$entry"
    }
    $changelog = $changelog.TrimEnd() + "`n"
    Set-Content $Path $changelog -NoNewline
}

Write-Host "=== R.E.P.O. Head Tracking Release ===" -ForegroundColor Cyan

$currentVersion = Get-CsprojVersion $csprojPath

if ([string]::IsNullOrWhiteSpace($Version)) {
    Write-Host "Current version: $currentVersion" -ForegroundColor White
    Write-Host "Usage: pixi run release <major|minor|patch|nightly|X.Y.Z>" -ForegroundColor Yellow
    exit 0
}

try {
    $Version = Resolve-ReleaseVersion -Argument $Version -CurrentVersion $currentVersion
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$tagName = "v$Version"

$currentBranch = git rev-parse --abbrev-ref HEAD
if ($currentBranch -ne "main") {
    Write-Host "Error: Must be on 'main' branch to release (currently on '$currentBranch')" -ForegroundColor Red
    exit 1
}

$status = git status --porcelain
if ($status) {
    Write-Host "Error: Working directory has uncommitted changes" -ForegroundColor Red
    Write-Host $status -ForegroundColor Gray
    exit 1
}

if (git tag -l $tagName) {
    Write-Host "Error: Tag '$tagName' already exists" -ForegroundColor Red
    exit 1
}

Write-Host "New version: $Version" -ForegroundColor Green

# Generate CHANGELOG from commits since last tag. This is the gate that
# aborts when there are no user-facing commits, so run it BEFORE mutating
# any version files or building - a failure here then leaves a clean tree
# instead of stranding a half-applied version bump with no tag.
$changelogPath = Join-Path $projectDir "CHANGELOG.md"
Write-Host "Generating CHANGELOG..." -ForegroundColor Cyan
$hasExistingTags = git tag -l 2>$null
if (-not $hasExistingTags) {
    $date = Get-Date -Format 'yyyy-MM-dd'
    Set-Content $changelogPath "# Changelog`n`n## [$Version] - $date`n`nFirst release.`n"
    Write-Host "  First release - wrote initial CHANGELOG entry" -ForegroundColor Gray
} else {
    try {
        New-ChangelogFromCommits -ChangelogPath $changelogPath -Version $Version -ArtifactPaths @(
            "src/REPOHeadTracking/", "cameraunlock-core", "scripts/", "README.md", "CHANGELOG.md", "LICENSE", ".github/"
        )
    } catch {
        if (-not $Force) {
            Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "No user-facing changes to release. Re-run with -Force for a maintenance release." -ForegroundColor Yellow
            exit 1
        }
        Write-Host "No user-facing commits since last tag - writing maintenance entry (-Force)." -ForegroundColor Yellow
        Add-MaintenanceChangelogEntry -Path $changelogPath -NewVersion $Version
    }
}

Set-CsprojVersion $csprojPath $Version

$pluginPath = Join-Path $projectDir "src\REPOHeadTracking\Core\REPOHeadTrackingPlugin.cs"
$pluginContent = Get-Content $pluginPath -Raw
$updatedPlugin = $pluginContent -replace 'PluginVersion = "[^"]+"', "PluginVersion = `"$Version`""
if ($updatedPlugin -eq $pluginContent) {
    Write-Host "Error: PluginVersion constant not found in $pluginPath" -ForegroundColor Red
    exit 1
}
$updatedPlugin | Set-Content $pluginPath -NoNewline
Write-Host "  Updated REPOHeadTrackingPlugin.cs" -ForegroundColor Gray

# install.cmd stamps MOD_VERSION into .headtracking-state.json and the packager
# copies the script verbatim, so without this every legacy install records the
# version this line was last edited to rather than the one it deployed. Raw
# read/write keeps the CRLF endings a .cmd needs.
$installCmdPath = Join-Path $projectDir "scripts\install.cmd"
$installCmd = Get-Content $installCmdPath -Raw
$updatedInstallCmd = $installCmd -replace '(?m)^set "MOD_VERSION=[^"]*"', "set `"MOD_VERSION=$Version`""
if ($updatedInstallCmd -eq $installCmd) {
    Write-Host "Error: MOD_VERSION line not found in $installCmdPath" -ForegroundColor Red
    exit 1
}
$updatedInstallCmd | Set-Content $installCmdPath -NoNewline
Write-Host "  Updated install.cmd MOD_VERSION" -ForegroundColor Gray

# Build through pixi so the release build runs the same setup -> restore ->
# build chain a developer and CI run, not a bare dotnet build that assumes
# lib/ is already populated.
Write-Host "Building release..." -ForegroundColor Cyan
Push-Location $projectDir
pixi run build
if ($LASTEXITCODE -ne 0) { Write-Host "Build failed!" -ForegroundColor Red; Pop-Location; exit 1 }
Pop-Location

Write-Host "Committing..." -ForegroundColor Cyan
git add $csprojPath $pluginPath $installCmdPath $changelogPath
git commit -m "Release v$Version"
if ($LASTEXITCODE -ne 0) { Write-Host "Commit failed!" -ForegroundColor Red; exit 1 }

git tag -a $tagName -m "Release $tagName"
git push origin main
git push origin $tagName

Write-Host "Release $tagName initiated!" -ForegroundColor Green
