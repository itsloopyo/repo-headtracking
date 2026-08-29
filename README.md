# R.E.P.O. Head Tracking

![R.E.P.O. running with this mod](https://raw.githubusercontent.com/itsloopyo/repo-headtracking/main/assets/readme-clip.gif)

An unofficial head tracking mod for R.E.P.O. that moves the view with your head while your mouse or controller keeps aiming, driven by a webcam, phone, or any OpenTrack compatible tracker, with no VR headset required.

## Features

- **Decoupled look and aim** - head tracking moves the camera; aim stays on your mouse/controller
- **6DOF positional tracking** - lean and peek with head position
- **Flashlight follows your head** - the light goes where you look, not where your body aims, and leads your head a little so it lands on what your eyes are on rather than on the centre of the screen

## Requirements

- A legitimately purchased copy of [R.E.P.O. on Steam](https://store.steampowered.com/app/3241660/REPO/).
- A head tracking source: [OpenTrack](https://github.com/opentrack/opentrack) with a webcam or VR headset, or a phone tracking app such as [Headcam](https://headcam.app) that sends OpenTrack UDP.
- Windows 10 or 11, 64-bit.

## Installation

1. Download `REPOHeadTracking-v<version>-installer.zip` from the [Releases page](https://github.com/itsloopyo/repo-headtracking/releases).
2. Extract it anywhere.
3. Double-click `install.cmd`. It finds your Steam copy of R.E.P.O., installs BepInEx 5 if it is not already there, and deploys the plugin.
4. Configure OpenTrack (or your phone app) to send UDP output to `127.0.0.1` port `4242`.
5. Launch the game.

If the installer cannot find your game, point it at the install folder in either
of these ways:

```powershell
# Environment variable
$env:REPO_PATH = "D:\Games\steamapps\common\REPO"
.\install.cmd

# Or pass the path directly
.\install.cmd "D:\Games\steamapps\common\REPO"
```

### Manual Installation

For placing files by hand:

1. Install [BepInEx 5 (x64)](https://github.com/BepInEx/BepInEx/releases) into the R.E.P.O. folder (the one containing `REPO.exe`) and launch the game once so it creates its directories.
2. Copy these three DLLs from the release ZIP's `plugins/` folder into `<game>\BepInEx\plugins\`:
   - `REPOHeadTracking.dll`
   - `CameraUnlock.Core.dll`
   - `CameraUnlock.Core.Unity.dll`

The Nexus ZIP (`REPOHeadTracking-v<version>-nexus.zip`) contains only that
`BepInEx/plugins/` subtree, so you can extract it straight into the game folder
if you already run BepInEx.

## Setting Up OpenTrack

In OpenTrack, set **Output** to `UDP over network`, open its options, and set the
destination to IP `127.0.0.1`, port `4242`. That matches the mod's default
`UDPPort`. Start tracking in OpenTrack before or after launching the game; the
mod picks up data whenever it arrives.

### VR Headset Setup

A Quest or other PC-VR headset makes a very accurate tracker:

1. Connect the headset to the PC with Air Link, a Link cable, or Virtual Desktop.
2. Launch SteamVR and make sure the headset is tracking.
3. In OpenTrack, set **Input** to `SteamVR`, then set the output to UDP as above.

### Webcam Setup

1. In OpenTrack, set **Input** to `neuralnet tracker`.
2. Open its options and pick your webcam, then set the resolution and frame rate the camera actually supports.
3. Sit at your normal playing distance, face the camera, and start tracking.

### Phone App Setup

Phone apps that already smooth their own pose data (such as Headcam) can send
straight to the mod: point the app at this PC's local IP address on port `4242`
and skip OpenTrack entirely. If you want OpenTrack's curve mapping and filters,
have the app send to OpenTrack instead, and let OpenTrack forward to
`127.0.0.1:4242`.

## Controls

Two equivalent binding sets. Use whichever your keyboard has; the chords exist
for keyboards with no navigation cluster.

| Action              | Nav-cluster | Chord          |
|---------------------|-------------|----------------|
| Toggle tracking     | `End`       | `Ctrl+Shift+Y` |
| Cycle tracking mode | `Page Up`   | `Ctrl+Shift+G` |
| Toggle yaw mode     | `Page Down` | `Ctrl+Shift+H` |

There is no recentre key. Your tracker app owns the centre: use its own
control (opentrack's Center bind, the CENTER button in Headcam, SteamVR's
reset) and the mod applies whatever pose it receives.

Cycling the tracking mode steps through: normal head tracking, then rotation
only, then position only, then back to normal.

## Configuration

The config file is written on first launch to
`<game>\BepInEx\config\com.cameraunlock.repo.headtracking.cfg`. Edit it with any
text editor while the game is closed.

```ini
[General]
## Whether head tracking is enabled when the game starts
EnabledOnStartup = true
## Whether to show a notification when the plugin initializes
ShowStartupNotification = true
## Yaw mode: true = horizon-locked yaw, false = camera-local yaw
WorldSpaceYaw = true
## Point the flashlight where you are looking rather than where you are aiming
FlashlightFollowsHead = true

[UI]
## Notify when the tracker connection is lost or restored
ShowConnectionNotifications = true

[Keybindings]
ToggleKey = End
CycleTrackingModeKey = PageUp
YawModeKey = PageDown

[Network]
## UDP port to listen on for OpenTrack data (1024 - 65535)
UDPPort = 4242

[Sensitivity]
## Rotation multipliers (0.1 - 3.0). Raise to move the view further per degree of head movement.
YawSensitivity = 1
PitchSensitivity = 1
RollSensitivity = 1
InvertYaw = false
## Defaults to true: R.E.P.O.'s camera pitches opposite to the tracker convention
InvertPitch = true
InvertRoll = false

[Smoothing]
## 0 = most responsive, 1 = heavy smoothing. Both cover rotation and position.
## The mod picks one per connection from the packet source address.
## Tracker running on this machine (loopback):
LocalSmoothing = 0
## Tracker on a remote network device, e.g. a phone over WiFi:
RemoteSmoothing = 0.15

[Position]
## Positional tracking (lean in, out, and side to side)
PositionEnabled = true
## Position multipliers (0.0 - 5.0)
PositionSensitivityX = 1
PositionSensitivityY = 1
PositionSensitivityZ = 1
## Maximum displacement in meters. Z is asymmetric so you can lean further
## forward than back without clipping through your own body.
PositionLimitX = 0.3
PositionLimitY = 0.2
PositionLimitZ = 0.4
PositionLimitZBack = 0.1
## Distance in meters from your neck pivot to the point the tracker reports.
## Cancels the sideways arc your head traces when you turn it.
TrackerPivotForward = 0.08
```

`WorldSpaceYaw = true` turns the view around the world's up-axis whatever the
camera is pitched at, so the horizon stays level. Set it to `false` for
camera-local yaw, which turns around the camera's own up-axis. `Page Down` flips
it at runtime; the config value is what the mod starts with next launch.

## Troubleshooting

**Mod not loading**

- Launch the game once after installing so BepInEx creates its folders, then check that `<game>\BepInEx\plugins\` holds all three DLLs.
- Open `<game>\BepInEx\LogOutput.log` and look for `R.E.P.O. Head Tracking ... initializing`.
- If `LogOutput.log` does not exist at all, BepInEx itself is not loading. Re-run `install.cmd`.

**No tracking response**

- Confirm your tracker is sending UDP to `127.0.0.1` port `4242`, and that `UDPPort` in the config matches.
- If a phone app is sending over WiFi, use the PC's local IP rather than `127.0.0.1`, and allow the game through Windows Firewall on private networks.
- Press `End` (or `Ctrl+Shift+Y`) to make sure tracking is not toggled off.

**Jittery or unstable tracking**

- Raise `RemoteSmoothing` toward `0.3` if your tracker is a phone or other network device, or `LocalSmoothing` if it runs on this PC. Both cover rotation and lean.
- For webcam tracking, add light on your face and avoid a bright window behind you.
- On WiFi phone tracking, move closer to the router or switch to 5 GHz.

**View drifts off-center, or rotates the wrong way**

- Centre it in your tracker app: opentrack's Center bind, the CENTER button in
  Headcam, SteamVR's reset. The mod keeps no centre of its own and applies the
  pose the tracker sends.
- If an axis moves the opposite way to your head, flip the matching `InvertYaw` / `InvertPitch` / `InvertRoll` value in the config.
- If yaw feels wrong only when looking steeply up or down, press `Page Down` to switch between horizon-locked and camera-local yaw.

## Updating

Download the new release and run `install.cmd` again. Your config is preserved.

## Uninstalling

Run `uninstall.cmd`. This removes the mod DLLs. BepInEx is only removed if the
installer put it there. Use `uninstall.cmd /force` to remove it anyway.

## Building from Source

Prerequisites: [pixi](https://pixi.sh) and the .NET SDK. A game install is not
required; the build uses Unity reference stubs.

```powershell
git clone --recursive https://github.com/itsloopyo/repo-headtracking.git
cd repo-headtracking
pixi run build      # build the plugin
pixi run install    # build and deploy to a local R.E.P.O. install
pixi run package    # produce the release ZIPs
```

## License

This mod's own code is MIT licensed - see [LICENSE](LICENSE) for details.

The clip at the top of this page is R.E.P.O. gameplay footage and is not covered
by that licence; it remains the property of its rights holders and ships in
neither release ZIP. Bundled third-party components keep their own licences.
Both are set out in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Credits

- R.E.P.O. by [semiwork](https://store.steampowered.com/app/3241660/REPO/).
- [BepInEx](https://github.com/BepInEx/BepInEx) - mod loader (LGPL-2.1).
- [HarmonyX](https://github.com/BepInEx/HarmonyX) - runtime patching (MIT).
- [OpenTrack](https://github.com/opentrack/opentrack) - head tracking protocol (ISC).
- Full details in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Disclaimer

This mod is not affiliated with, endorsed by, or supported by semiwork. R.E.P.O.
is a co-op game; other players in your lobby are unaffected by this mod, but use
it at your own risk.

## Community and Support

- [Discord](https://discord.com/invite/dxyZdyFNT9) - setup help, bug reports, and new-release announcements
- [Lopari](https://lopari.app) - free Windows launcher with one-click install and launch of head-tracking mods
- [Headcam](https://headcam.app) - free app that turns your phone into a head tracker
