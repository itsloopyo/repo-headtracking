# Changelog

All notable changes to this project are documented here.

## [Unreleased]

### Added
- Initial 6DOF head tracking implementation for R.E.P.O.

### Changed
- Replaced the single `Smoothing` config key with `LocalSmoothing` (default 0.0) and `RemoteSmoothing` (default 0.15), selected per connection from the packet source address
- Removed the `PositionSmoothing` key: position now uses the same connection-selected value as rotation
- Removed the hidden 0.15 baseline smoothing floor, so local trackers get zero-latency tracking by default
- Moved installation to native launcher-manifest delivery (`delivery_mode: manifest`, schema version 2), so the launcher deploys the loader, plugin files, and the BepInEx config seed from metadata. install.cmd and uninstall.cmd are retained for manual installs and pre-v2 migration.
