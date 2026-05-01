# Changelog

All notable changes to this repository are recorded here. Each mod also has its own `metadata.mod` `<ModVersion>`; bump that when you publish to Steam Workshop.

## [Unreleased]

### Repository
- `tools/steamcmd/Deploy-Workshop.ps1` for two-mod Workshop deploy; `workshop_preview.png` placeholder; Korean deploy notes in README.

## [1.0.2] - 2026-05-01

### Repository
- Added [tools/steamcmd/](tools/steamcmd/) with VDF examples, PowerShell helper (`STEAM_USER` / optional `STEAM_PASS`), and README for Workshop updates via SteamCMD; root README links to it; `.gitignore` excludes local `se_*_workshop.vdf` copies.

## [1.0.1] - 2026-05-01

### SE_Upgrade_module_mod
- Documented prefab loot behavior in the root README (cargo rolls use module levels 1–3; cockpit NPC rolls use 1–10).
- Added `PrefabLootMaxCargoContainers` and `PrefabLootMaxCockpitAttempts` to world XML config with safe defaults and migration for older saves.
- Fixed prefab spawn handler: null/static/`MarkedForClose` ordering, removed accidental mutation of loot limits across spawns, null-safe exclude lists, null `IMyGridTerminalSystem` guard.
- NPC grid `[LV…]` label now shows the computed total level without incorrectly applying only the attack multiplier.
- Centralized multiplayer channel IDs and upgrade sync payload size in `UpgradeModConstants.cs`; inventory rescan interval constant for cockpit logic; load balancer frame periods in `LoadBalancerConstants`.
- Config load/save and multiplayer config handlers now log exception messages on failure.

### SE_Prime_Block_mod
- `metadata.mod` version **1.0.3**.
- Added `PrefabLootMaxCargoContainers` to world XML config (default 5) with migration for older files.
- Aligned prefab spawn filtering with the upgrade mod (static grids, respawn prefab name, null-safe excludes).
- Config load failures now log the exception message.

### Repository
- Root `README.md`: removed erroneous outer markdown code fence, clarified install paths, added versioning and changelog links.
- Added this `CHANGELOG.md` and [SE_Prime_Block_mod/README.md](SE_Prime_Block_mod/README.md).
