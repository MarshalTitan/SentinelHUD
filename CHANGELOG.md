# Changelog

## 0.8.4.0 — Sentinel Modern 2 reference release

- Replaced Sentinel HUD's local Modern preview renderer with the canonical `SentinelModernAppShell` from `MarshalTitan.SentinelCore.UI` 0.3.0.
- Added a compact Core header, six-item primary icon rail, conditional secondary category sidebars, Core page transitions and animated ambient treatment with Dalamud reduced-motion support.
- Adopted Core glass cards, settings rows, switches, status pills and an Appearance-only action dock; removed the local palette, card, navigation, toggle, ambient and motion implementation.
- Retained the persisted Modern theme selection without a configuration schema change and left the Classic renderer unchanged.
- Added retained and tested primary/category navigation state.
- Pinned Sentinel Core `v0.3.0.0` at commit `520f9b9837dc3286c24d6f812b8348a27f378a3e` and added CI verification for the `MarshalTitan.SentinelCore.UI` 0.3.0 package hash and packaged assembly version.
- Preserved all HUD modules, targeting interactions, editor/layout state, Awareness, camera, encounter, anti-AFK, quest convenience, commands, IPC and disposal behavior.
