# Third-Party Notices

R.E.P.O. Head Tracking is itself MIT licensed (copyright itsloopyo /
CameraUnlock, see `LICENSE`). It bundles or links the third-party components
listed below; none of them change the license of this mod's own code.

## BepInEx

- **Version:** 5.4.23.5 (win x64)
- **License:** LGPL-2.1
- **Upstream:** https://github.com/BepInEx/BepInEx
- **Usage:** Mod loader that hosts the head tracking plugin.
- **Bundled:** yes. Bundled in the release ZIP and used as the install-time source.

Source availability per LGPL-2.1 section 6 is satisfied by the upstream
repository link above. The binary is not modified.

---

## HarmonyX (0Harmony)

- **Version:** 2.9.0 (as shipped inside BepInEx 5.4.23.5)
- **License:** MIT
- **Upstream:** https://github.com/BepInEx/HarmonyX
- **Usage:** Referenced at build time and loaded at runtime by BepInEx.
- **Bundled:** yes. Ships inside the vendored BepInEx archive in the release ZIP.

Copyright (c) 2017 Andreas Pardeike, (c) 2020 BepInEx contributors

---

## Mono.Cecil

- **License:** MIT
- **Upstream:** https://github.com/jbevain/cecil
- **Usage:** IL reading/writing used by BepInEx's preloader.
- **Bundled:** yes, inside the vendored BepInEx zip; bundled in the release ZIP
  and used as the install-time source.

## MonoMod

- **License:** MIT
- **Upstream:** https://github.com/MonoMod/MonoMod
- **Usage:** runtime detouring used by BepInEx / HarmonyX.
- **Bundled:** yes, inside the vendored BepInEx zip; bundled in the release ZIP
  and used as the install-time source.

## OpenTrack

- **Version:** n/a (wire protocol only)
- **License:** ISC
- **Upstream:** https://github.com/opentrack/opentrack
- **Usage:** Protocol-compatible UDP receiver for pose data.
- **Bundled:** no. No OpenTrack code is bundled or linked.

---
