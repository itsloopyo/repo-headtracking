# Changelog

All notable changes to this project are documented here.

## [Unreleased]

### Added
- Initial 6DOF head tracking implementation for R.E.P.O.

### Fixed
- Log `OpenTrack connection established` / `lost` regardless of the on-screen notification setting. It is the only evidence in `BepInEx/LogOutput.log` that tracker packets ever arrived, and a user who had turned notifications off sent a log that could not answer "did the tracker reach the game"

### Changed
- Removed the in-game recentre control. Your tracker app owns the centre now: centre it there (opentrack's Center bind, the CENTER button in Headcam, SteamVR's reset) and the mod applies the pose it receives as absolute. A second centre inside the mod could only drift out of step with the tracker's. The `Home` key, the `Ctrl+Shift+T` chord and the `Keybindings / RecenterKey` config entry are gone
- Replaced the single `Smoothing` config key with `LocalSmoothing` (default 0.0) and `RemoteSmoothing` (default 0.15), selected per connection from the packet source address
- Removed the `PositionSmoothing` key: position now uses the same connection-selected value as rotation
- Removed the hidden 0.15 baseline smoothing floor, so local trackers get zero-latency tracking by default
- Moved installation to native launcher-manifest delivery (`delivery_mode: manifest`, schema version 2), so the launcher deploys the loader, plugin files, and the BepInEx config seed from metadata. install.cmd and uninstall.cmd are retained for manual installs and pre-v2 migration.
