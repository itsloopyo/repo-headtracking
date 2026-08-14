[CmdletBinding()]
param([switch]$AllowDirty)
$ErrorActionPreference = 'Stop'
$ProjectRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Import-Module (Join-Path $ProjectRoot 'cameraunlock-core\powershell\NightlyRelease.psm1') -Force

$csproj = Join-Path $ProjectRoot 'src\REPOHeadTracking\REPOHeadTracking.csproj'
$version = (Select-String -Path $csproj -Pattern '<Version>([^<]+)</Version>').Matches[0].Groups[1].Value

Publish-NightlyBuild `
    -ModId 'repo' `
    -ModName 'REPOHeadTracking' `
    -Version $version `
    -ProjectRoot $ProjectRoot `
    -BuildCommand 'pixi run build' `
    -AllowDirty:$AllowDirty
