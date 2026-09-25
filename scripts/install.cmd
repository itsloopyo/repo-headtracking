@echo off
:: ============================================
:: R.E.P.O. Head Tracking - Install
:: ============================================
:: Thin wrapper - install body lives in cameraunlock-core/scripts/install-body-bepinex.cmd,
:: staged into the release ZIP's shared/ by Copy-SharedBundle. To change
:: install behaviour edit the body, not this wrapper. Everything below the
:: CONFIG BLOCK is copied verbatim from
:: cameraunlock-core/scripts/templates/install-wrapper-bepinex.cmd.
:: ============================================

:: --- CONFIG BLOCK ---
set "GAME_ID=repo"
set "MOD_DISPLAY_NAME=R.E.P.O. Head Tracking"
set "MOD_DLLS=REPOHeadTracking.dll CameraUnlock.Core.dll CameraUnlock.Core.Unity.dll"
set "MOD_INTERNAL_NAME=REPOHeadTracking"
set "MOD_VERSION=0.0.0"
set "STATE_FILE=.headtracking-state.json"
set "FRAMEWORK_TYPE=BepInEx"
:: x64 or x86 - selects the vendored BepInEx zip.
set "BEPINEX_ARCH=x64"
:: Override the vendor zip filename (Thunderstore packs ship their own name).
set "BEPINEX_VENDOR_ZIP_NAME=BepInEx_win_x64.zip"
:: Thunderstore wrapper directory to flatten into the game root. Empty for
:: regular BepInEx.
set "BEPINEX_SUBFOLDER="
:: Subfolder under BepInEx\plugins\ to deploy into. Empty lays the DLLs flat.
set "PLUGIN_SUBFOLDER="
:: Post-install help text. `&echo ` starts each further line.
set "MOD_CONTROLS=Toggle tracking: End or Ctrl+Shift+Y  ^|  Cycle tracking mode: Page Up or Ctrl+Shift+G  ^|  Yaw mode: Page Down or Ctrl+Shift+H"
:: Not used by this mod. Set blank so a value another mod's wrapper left in
:: the same console does not reach the body.
set "IL2CPP_VENDOR_DIR_NAME="
set "IL2CPP_VENDOR_ZIP_NAME="
set "IL2CPP_PLUGIN_DIR_NAME="
set "IL2CPP_MOD_DLLS="
:: --- END CONFIG BLOCK ---

:: Pin delayed expansion off before `%*` is expanded on the `call` below.
:: Under `cmd /V:ON`, or with DelayedExpansion=1 in
:: HKCU\Software\Microsoft\Command Processor, cmd.exe eats a `!` out of the
:: expanded line, and a real game path like C:\Games\Oh! My Game reaches the
:: body already mangled. The body pins expansion off at its own outer scope
:: too, but that is one `call` too late to save the argument it was handed.
setlocal disabledelayedexpansion

set "WRAPPER_DIR=%~dp0"
set "_BODY=%WRAPPER_DIR%shared\install-body-bepinex.cmd"
if not exist "%_BODY%" set "_BODY=%WRAPPER_DIR%..\cameraunlock-core\scripts\install-body-bepinex.cmd"
if not exist "%_BODY%" (
    echo ERROR: install-body-bepinex.cmd not found in shared\ or ..\cameraunlock-core\scripts\.
    echo If this is a release ZIP, re-download it from GitHub ^(corrupt installer^).
    echo If this is the dev tree, run: git submodule update --init --recursive
    exit /b 1
)
call "%_BODY%" %*
exit /b %errorlevel%
