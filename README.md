# R.E.P.O. Head Tracking

![R.E.P.O. running with this mod](https://raw.githubusercontent.com/itsloopyo/repo-headtracking/main/assets/readme-clip.gif)

An unofficial head tracking mod for R.E.P.O. that moves the view with your head while your mouse or controller keeps aiming, driven by a webcam, phone, or any OpenTrack compatible tracker, with no VR headset required.

## Features

- **Decoupled look and aim** - head tracking moves the camera; aim stays on your mouse/controller
- **6DOF positional tracking** - lean and peek with head position
- **Works with any OpenTrack compatible tracker** - free options available for PC, iOS and Android

Settings live in `BepInEx\config\CameraUnlock.ini`. BepInEx's ConfigurationManager does not list them; edit the file with any text editor. See [Configuration](#configuration).

## Requirements

- A legitimately purchased copy of [R.E.P.O. on Steam](https://store.steampowered.com/app/3241660/REPO/).
- A head tracking source: [OpenTrack](https://github.com/opentrack/opentrack) with a webcam or VR headset, or a phone tracking app such as [Headcam](https://headcam.app) that sends OpenTrack UDP.
- Windows 10 or 11, 64-bit.

## Installation

### Lopari

Download [Lopari](https://lopari.app), choose **R.E.P.O.**, and click
**Play with head tracking**.

### Standalone Installer

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

The mod listens for OpenTrack pose data on UDP port `4242`, on every network
interface. One datagram is six little-endian 64-bit floats in the order
`x, y, z, yaw, pitch, roll`: position in centimetres, rotation in degrees, 48
bytes in total. Anything that sends that to that port drives the view.
OpenTrack's **UDP over network** output sends exactly this, and the steps below
set it up.

1. Install [OpenTrack](https://github.com/opentrack/opentrack/releases).
2. Pick a tracker under **Input**, using the notes below.
3. Set **Output** to **UDP over network**, host `127.0.0.1`, port `4242`.
4. Press **Start**. Tracking and the game can start in either order.

### Webcam

OpenTrack ships a `neuralnet tracker` input that reads a plain webcam. Select it
under **Input**, pick your camera in its settings, and use the output settings
above. How well it tracks depends on your camera and your lighting, so try it
before buying anything.

### Phone

A phone app can reach the mod directly, with no OpenTrack on the PC, if it sends
the datagram described above. Point it at this PC's IP address (run `ipconfig`
to find it) on port `4242`. Not every phone tracker speaks this protocol, so
check yours for an OpenTrack or UDP output option first. [Headcam](https://headcam.app)
sends it, and I wrote it so decent tracking is free for anyone who already owns
a phone.

Sending direct works when the app filters its own signal on the device. The
mod's smoothing is sized to take the edge off a clean signal rather than to
rescue a noisy one, so a raw feed sent direct will jitter. If it does, point the
app at OpenTrack's **UDP over network** *input* on some other port, say 5252,
and let OpenTrack's filters and curves clean it up before its output forwards to
`127.0.0.1:4242`.

Anything arriving from outside `127.0.0.0/8` counts as a remote connection and
is smoothed with `RemoteSmoothing` rather than `LocalSmoothing`. That includes a
tracker on this very PC that sends to the machine's own LAN address, because the
mod reads the source address and not the machine.

### Headset or other hardware

If your device has an OpenTrack input driver, select it under **Input** and use
the same output settings. OpenTrack's own **Input** list is the authority on
what it can read; the mod only ever sees what OpenTrack sends.

### Centring

Centring belongs to your tracker. The mod subtracts no centre of its own: it
applies the pose it receives exactly as it arrives, so a stream of zeros holds
the view where the game itself puts it. Press the centre control in your tracker
(OpenTrack's **Center** bind, or the CENTER button in Headcam) and the tracker
zeroes its own output, which leaves the view centred with the mod doing nothing.

That is why there is no centre hotkey here and nothing to re-centre in game. Two
centres in series would drift apart, because each side re-centres at moments the
other cannot see, and you would end up pressing twice to centre once. If the
view sits off to one side, centre it in the tracker.

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
only, then position only, then back to normal. The mode and the yaw mode are
saved the moment you change them, so the game starts in them next time.
Turning head tracking off with `End` lasts until the game closes: the mod
starts with head tracking on or off as `EnableOnStartup` says.

Every hotkey is a list in the config file, so you can rebind a key, add one or
remove one there. See [Configuration](#configuration).

## Configuration

<!-- cameraunlock:config -->
The mod reads its settings from `BepInEx\config\CameraUnlock.ini` in the game folder, and creates the file when it starts and finds none. Edit it with any text editor.

A setting set to `default` takes its value from `Defaults.ini`, which every head tracking mod that keeps its settings in `CameraUnlock.ini` reads. Head tracking mods that keep their settings in another file do not read it. Writing a value in place of `default` changes that setting for this game only. When the mod saves a setting that a hotkey changed in game, it writes the new value in place of `default`, so that setting no longer follows `Defaults.ini` in this game until you set it to `default` again.

`Defaults.ini` is `%AppData%\CameraUnlock\Defaults.ini` on Windows; `$XDG_CONFIG_HOME/CameraUnlock/Defaults.ini` on Linux, or `~/.config/CameraUnlock/Defaults.ini` where `XDG_CONFIG_HOME` is not set, under Wine and Proton too; and `~/Library/Application Support/CameraUnlock/Defaults.ini` on macOS. The mod's log, where it writes one, names the file it read.

When the mod starts and finds no `Defaults.ini`, it creates one holding the built-in values, unless Windows runs the game as a packaged app, or the game runs on Linux or macOS without Wine or Proton. The mod never changes `Defaults.ini` after that. Edit it with any text editor.

On Linux and macOS without Wine or Proton, this version reads its settings and saves none: it creates no `CameraUnlock.ini` and a change made in game lasts until the game closes.

BepInEx's ConfigurationManager does not list these settings.

The built-in value of each setting set to `default` below:

- `UdpPort=4242`
- `EnableOnStartup=true`
- `WorldSpaceYaw=true`
- `RotationEnabled=true`
- `LocalSmoothing=0.0`
- `RemoteSmoothing=0.15`
- `PositionEnabled=true`
- `PositionLimitX=0.3`
- `PositionLimitY=0.2`
- `PositionLimitYDown=0.2`
- `PositionLimitZ=0.4`
- `PositionLimitZBack=0.1`
- `ToggleKey=End, Ctrl+Shift+Y`
- `CycleTrackingModeKey=PageUp, Ctrl+Shift+G`
- `YawModeKey=PageDown, Ctrl+Shift+H`
- `LightFollowsHead=true`
- `LightMultiplier=1.5`

With every setting at its default, the file reads:

```ini
; R.E.P.O. head tracking settings.
; Comments start with ; and go on their own line. Text after a value is part of the value.
; Hotkeys are key names such as End, PageUp or Ctrl+Shift+Y. Separate several with commas; leave empty for none.
; A setting set to default takes its value from Defaults.ini, which every head tracking mod
; that keeps its settings in CameraUnlock.ini reads: %AppData%\CameraUnlock\Defaults.ini on
; Windows, $XDG_CONFIG_HOME/CameraUnlock/Defaults.ini (normally ~/.config/CameraUnlock) on
; Linux, under Wine and Proton too, and ~/Library/Application Support/CameraUnlock/Defaults.ini
; on macOS. The log names the file it read. Write a value instead of default to change that
; setting for this game only.

[CameraUnlock]
; Written by the mod. Leave this section in place.
ConfigFormat=1

[Network]
; UDP port the mod receives tracker data on (OpenTrack protocol).
UdpPort=default

[General]
; true: head tracking is on when the game starts. ToggleKey turns it on and off.
EnableOnStartup=default
; true: yaw turns around the world's up axis. false: around the camera's own up axis.
WorldSpaceYaw=default
; true: turning your head turns the view.
; Tracking mode at startup, with PositionEnabled. The mode hotkey changes both.
RotationEnabled=default

[Smoothing]
; Smoothing when the tracker runs on this PC. 0 is the least, 1 the most.
LocalSmoothing=default
; Smoothing when the tracker is another device on the network, such as a phone.
; 0 is the least, 1 the most.
RemoteSmoothing=default

[Position]
; true: moving your head moves the view.
; Tracking mode at startup, with RotationEnabled. The mode hotkey changes both.
PositionEnabled=default
; How far, in metres, leaning left or right can move the view.
PositionLimitX=default
; How far, in metres, raising your head can move the view.
PositionLimitY=default
; How far, in metres, lowering your head can move the view.
PositionLimitYDown=default
; How far, in metres, leaning forward can move the view.
PositionLimitZ=default
; How far, in metres, leaning back can move the view.
PositionLimitZBack=default

[Hotkeys]
; Turns head tracking on and off.
ToggleKey=default
; Changes the tracking mode: rotation and position, rotation only, position only.
CycleTrackingModeKey=default
; Switches yaw between the world's up axis and the camera's own (WorldSpaceYaw).
YawModeKey=default

[Light]
; true: a light you carry points where you look instead of where you aim.
LightFollowsHead=default
; How far the light turns for each degree your head turns.
; 1 matches the view, 0 keeps the light on your aim.
LightMultiplier=default

[Notifications]
; true: show head tracking's state and hotkeys on screen when the game starts.
ShowStartupNotification=true
; true: show on screen when the tracker's data starts or stops arriving, and when
; the UDP port is in use by another app.
ShowConnectionNotifications=true
```
<!-- /cameraunlock:config -->

## Troubleshooting

**Mod not loading**

- Launch the game once after installing so BepInEx creates its folders, then check that `<game>\BepInEx\plugins\` holds all three DLLs.
- Open `<game>\BepInEx\LogOutput.log` and look for `R.E.P.O. Head Tracking ... initializing`.
- If `LogOutput.log` does not exist at all, BepInEx itself is not loading. Re-run `install.cmd`.

**No tracking response**

- Confirm your tracker is sending UDP to `127.0.0.1` port `4242`, and that `UdpPort` in `CameraUnlock.ini` matches.
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
- If an axis moves the opposite way to your head, invert that axis in your tracker app. The mod has no sensitivity or inversion settings: the tracker shapes the pose.
- If yaw feels wrong only when looking steeply up or down, press `Page Down` to switch between horizon-locked and camera-local yaw.

## Updating

Download the new release and run `install.cmd` again. `CameraUnlock.ini` is kept.

## Uninstalling

Run `uninstall.cmd`. This removes the mod DLLs and leaves `BepInEx\config\CameraUnlock.ini`
in place. BepInEx is only removed if the
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

## Community & Support

- Discord: [Loop's Head Tracking Hangout](https://discord.com/invite/dxyZdyFNT9) - setup help, bug reports, and new-release announcements
- [Lopari](https://lopari.app) - free Windows launcher with one-click install and launch for the released head-tracking mods
- [Headcam](https://headcam.app) - free app that turns your iPhone or Android phone into the head tracker
