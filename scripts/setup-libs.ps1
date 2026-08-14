#!/usr/bin/env pwsh
#Requires -Version 5.1
# Populate lib/ with build references from REPO FILES ONLY:
#   - BepInEx.dll / 0Harmony.dll extracted from the committed vendor zip
#   - Unity reference assemblies compiled from the checked-in stub sources
# No game installation and no network access. This is exactly what a CI
# runner does, so a local build cannot drift from the CI build.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$libPath = Join-Path $projectRoot "lib"

Write-Host "Setting up build references (stubs + vendored loader)..." -ForegroundColor Cyan

$trackedSources = @("UnityStubs.cs", "UnityUIStubs.cs")
foreach ($src in $trackedSources) {
    if (-not (Test-Path (Join-Path $libPath $src))) {
        throw "lib/$src not found. It is tracked in git - the checkout is incomplete."
    }
}

$vendorZip = Join-Path $projectRoot "vendor\bepinex\BepInEx_win_x64.zip"
if (-not (Test-Path $vendorZip)) {
    throw "Vendored BepInEx zip missing: $vendorZip. Run 'pixi run update-deps' and commit the result."
}

# Wipe to the tracked stub sources so a local run starts from the same empty
# lib/ a fresh CI checkout has. Without this, a stale DLL left behind by an
# earlier run can satisfy a reference the stubs no longer provide. Everything
# this run needs is checked for above, so the wipe cannot strand lib/ empty.
Get-ChildItem -Path $libPath -Force |
    Where-Object { $trackedSources -notcontains $_.Name } |
    Remove-Item -Recurse -Force

$vendorTemp = Join-Path $libPath "_vendor-extract"
Expand-Archive -Path $vendorZip -DestinationPath $vendorTemp -Force
foreach ($dll in @("BepInEx.dll", "0Harmony.dll")) {
    $src = Join-Path $vendorTemp "BepInEx\core\$dll"
    if (-not (Test-Path $src)) { throw "$dll not found in $vendorZip" }
    Copy-Item $src $libPath -Force
    Write-Host "  $dll (vendored)" -ForegroundColor Green
}
Remove-Item -Recurse -Force $vendorTemp

function Build-StubAssembly {
    param(
        [Parameter(Mandatory=$true)][string]$AssemblyName,
        [Parameter(Mandatory=$true)][string]$SourceFile,
        [string[]]$References = @()
    )

    $refItems = ($References | ForEach-Object {
        "    <Reference Include=`"$_`"><HintPath>$_.dll</HintPath></Reference>"
    }) -join "`n"

    $projContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <AssemblyName>$AssemblyName</AssemblyName>
    <NoWarn>CS0169;CS0649;CS0067;CS0660;CS0661</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$SourceFile" />
$refItems
  </ItemGroup>
</Project>
"@
    $projPath = Join-Path $libPath "Stub_$AssemblyName.csproj"
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($projPath, $projContent, $utf8NoBom)

    dotnet build $projPath -c Release -o $libPath --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Failed to build $AssemblyName stub" }
    Remove-Item $projPath -Force
    Write-Host "  $AssemblyName.dll (stub)" -ForegroundColor Green
}

Build-StubAssembly -AssemblyName "UnityEngine" -SourceFile "UnityStubs.cs"
Build-StubAssembly -AssemblyName "UnityEngine.UI" -SourceFile "UnityUIStubs.cs" -References @("UnityEngine")

# Module shells: the mod and CameraUnlock.Core.Unity reference these assembly
# names, but every member they use is declared in UnityStubs.cs.
$emptySourcePath = Join-Path $libPath "EmptyStub.cs"
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($emptySourcePath, "// Empty stub assembly`n", $utf8NoBom)

foreach ($moduleName in @(
    "UnityEngine.CoreModule", "UnityEngine.IMGUIModule", "UnityEngine.UIModule",
    "UnityEngine.InputLegacyModule", "UnityEngine.TextRenderingModule",
    "UnityEngine.AnimationModule", "UnityEngine.PhysicsModule"
)) {
    Build-StubAssembly -AssemblyName $moduleName -SourceFile "EmptyStub.cs"
}

Remove-Item $emptySourcePath -Force
Remove-Item (Join-Path $libPath "*.deps.json") -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $libPath "*.pdb") -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $libPath "obj") -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Setup complete (no game install, no network)" -ForegroundColor Green
