# Changelog

All notable changes to this project are documented here.

## [Unreleased]

### Added
- Initial 6DOF head tracking implementation for R.E.P.O.

### Changed
- Moved installation to native launcher-manifest delivery (`delivery_mode: manifest`, schema version 2), so the launcher deploys the loader, plugin files, and the BepInEx config seed from metadata. install.cmd and uninstall.cmd are retained for manual installs and pre-v2 migration.
