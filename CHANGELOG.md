# Changelog

All notable changes to this project are documented here.

## [Unreleased]

### Added
- A setting set to `default` in `CameraUnlock.ini` takes its value from `Defaults.ini`, which every head tracking mod that keeps its settings in `CameraUnlock.ini` reads. Head tracking mods that keep their settings in another file do not read it, and neither do earlier versions of this mod. Writing a value in place of `default` changes that setting for this game only. When the mod saves a setting that a hotkey changed in game, it writes the new value in place of `default`, so that setting no longer follows `Defaults.ini` in this game until you set it to `default` again.
- `Defaults.ini` is `%AppData%\CameraUnlock\Defaults.ini` on Windows; `$XDG_CONFIG_HOME/CameraUnlock/Defaults.ini` on Linux, or `~/.config/CameraUnlock/Defaults.ini` where `XDG_CONFIG_HOME` is not set, under Wine and Proton too; and `~/Library/Application Support/CameraUnlock/Defaults.ini` on macOS. The mod's log, where it writes one, names the file it read.
- When the mod starts and finds no `Defaults.ini`, it creates one holding the built-in values, unless Windows runs the game as a packaged app, or the game runs on Linux or macOS without Wine or Proton. The mod never changes `Defaults.ini` after that.
- `General / FlashlightMultiplier` sets how far the flashlight turns relative to your
  head. It was fixed at 1.5 with no way to change it. `1.0` matches the view, `0`
  leaves the beam on the aim
- Initial 6DOF head tracking implementation for R.E.P.O.

### Fixed
- Log `OpenTrack connection established` / `lost` regardless of the on-screen notification setting. It is the only evidence in `BepInEx/LogOutput.log` that tracker packets ever arrived, and a user who had turned notifications off sent a log that could not answer "did the tracker reach the game"

### Changed
- Settings move to `BepInEx\config\CameraUnlock.ini`. Earlier versions of the mod kept these settings in `com.cameraunlock.repo.headtracking.cfg`, in the same folder. The first time this version starts and finds no `CameraUnlock.ini`, it reads your settings from `com.cameraunlock.repo.headtracking.cfg` and writes them into `CameraUnlock.ini`. It never changes `com.cameraunlock.repo.headtracking.cfg`, and does not read it again while `CameraUnlock.ini` exists.
- A setting that the defaults the README shows set to `default` is written as `default` when you never changed it from the default earlier versions used, because `com.cameraunlock.repo.headtracking.cfg` does not hold it or holds that default. It then follows `Defaults.ini`, so it takes the value `Defaults.ini` gives it, or the built-in value where `Defaults.ini` gives none, which can differ from the default earlier versions used. A setting you changed is written with the value imported for it, or as `default` where that value equals its default at that start.
- `RotationEnabled` and `PositionEnabled` are one setting here, the tracking mode, so both are written as `default` or neither is.
- Comments, and keys the mod never read, are not carried over. Nor are these, where your old file had them:
  - A sensitivity, scale, deadzone, response curve or axis inversion you changed from its default. Set these in your tracker instead.
  - A neck pivot distance you changed from its default. The neck pivot is not a setting now.
  - A hotkey set to Ctrl, Shift or Alt on its own. That key goes down before the key of any chord made with it, so the hotkey is left unbound, and it keeps its Ctrl+Shift chord where it has one.
- An older version of the mod reads `com.cameraunlock.repo.headtracking.cfg` and never reads `CameraUnlock.ini`, so a setting you change after updating is not in `com.cameraunlock.repo.headtracking.cfg`.
- Deleting only `CameraUnlock.ini` makes the next start read `com.cameraunlock.repo.headtracking.cfg` again. To go back to the defaults, replace everything in `CameraUnlock.ini` with the defaults the README shows. Every setting they set to `default` then follows `Defaults.ini`.
- BepInEx's ConfigurationManager no longer lists these settings. Edit `BepInEx\config\CameraUnlock.ini` with any text editor.
- Hotkeys are written as key names, and each hotkey lists every key that triggers it, the Ctrl+Shift chord included: `ToggleKey=End, Ctrl+Shift+Y`.
- A hotkey bound to a plain key no longer fires while Ctrl and Shift are both held, so Ctrl+Shift with that key reaches only a binding that names the chord.
- The tracking mode and the yaw mode are saved to `CameraUnlock.ini` the moment a hotkey changes them, so the game starts in them next time. Turning head tracking on or off with `End` is not saved: the mod starts with head tracking on or off as `EnableOnStartup` says.
- The flashlight settings are `[Light] LightFollowsHead` and `[Light] LightMultiplier`, and the notification switches are under `[Notifications]`.
- On Linux and macOS without Wine or Proton, this version reads its settings and saves none: it creates no `CameraUnlock.ini`, reads your settings from `com.cameraunlock.repo.headtracking.cfg` again at every start while there is no `CameraUnlock.ini`, and a change made in game lasts until the game closes.
- Since the dev pre-release, `FlashlightMultiplier` is read from the config (e353d45). The dev build ignored the key and turned the beam by a fixed 1.5, so a value you set there now takes effect.
- Removed the in-game recentre control. Your tracker app owns the centre now: centre it there (opentrack's Center bind, the CENTER button in Headcam, SteamVR's reset) and the mod applies the pose it receives as absolute. A second centre inside the mod could only drift out of step with the tracker's. The `Home` key, the `Ctrl+Shift+T` chord and the `Keybindings / RecenterKey` config entry are gone
- Replaced the single `Smoothing` config key with `LocalSmoothing` (default 0.0) and `RemoteSmoothing` (default 0.15), selected per connection from the packet source address
- Removed the `PositionSmoothing` key: position now uses the same connection-selected value as rotation
- Removed the hidden 0.15 baseline smoothing floor, so local trackers get zero-latency tracking by default
- Moved installation to native launcher-manifest delivery (`delivery_mode: manifest`, schema version 2), so the launcher deploys the loader, plugin files, and the BepInEx config seed from metadata. install.cmd and uninstall.cmd are retained for manual installs and pre-v2 migration.

### Removed
- The neck pivot settings. A distance you changed from the default is not carried over.
- The sensitivity, scale, deadzone, response curve and axis inversion settings. Set these in your tracker app instead.
- With these settings at their shipped defaults the camera moves as it did before.
