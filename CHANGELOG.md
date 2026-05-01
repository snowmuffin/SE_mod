# Changelog

All notable changes to this repository are recorded here. Each mod also has its own `metadata.mod` `<ModVersion>`; bump that when you publish to Steam Workshop.

## [Unreleased]

### SE_Overclock_mod
- **`Data/BlockCategories_Overclock.sbc`**: single file for ShipTools + Production GUI categories (Production wrapped with the same `Category` schema as ShipTools); removed `BlockCategories_PrimeShipTools.sbc` and `BlockCategories_UpgradeProduction.sbc`.
- README **Mod layout** (definitions/scripts/assets map) and in-game smoke-check notes for block categories.
- **`Upgrade-Chip-Logo.png`** moved to **`Textures/Marketing/`** (still not referenced by SBC).

### Repository
- Renamed **`SE_Unified_mod` → `SE_Overclock_mod`**; `Deploy-Overclock.ps1` / docs / examples point at the new folder (Workshop title remains *SE Overclock*).

## [2.0.0] - 2026-05-01

### SE_Overclock_mod (new)
- Single mod merging upgrade + Prime data; merged `Components`, `BlueprintClasses`, `Blueprints`; Prime cube SBCs and `FactionTypes_Economy`; split `BlockCategories_*` files.
- One prefab loot path: removed `Prime_block/MoreLoot.cs`; Prime Matter rolls in `SEUpgrademodule/MoreLoot.cs` with unioned exclude lists from both XML configs.
- `metadata.mod` **2.0.0**, `modinfo.sbmi` Workshop id **0** until first upload.

### Legacy
- `SE_Upgrade_module_mod` / `SE_Prime_Block_mod`: `MIGRATED_TO_UNIFIED.md`; must not be enabled with `SE_Overclock_mod`.

## [1.0.3] - 2026-05-01

### SE_Upgrade_module_mod
- `metadata.mod` **1.0.2**: removed duplicate economy SBC (`FactionTypes_Economy.sbc`); Trader integration is owned by the Prime mod when both are used together.
- Removed dead **`GetBuilder`** from `MoreLoot.cs`.

### SE_Prime_Block_mod
- `metadata.mod` **1.0.4**: single `Data/BlockVariantGroups.sbc` (moved from `Data/Scripts/`); fixed duplicate `Prime_MatterComponents` blueprint class; removed unused **`GetBuilder`** from `MoreLoot.cs`.

### Repository
- Root README **Mod coupling**; Prime README notes economy dependency on Upgrade definitions.
- `Deploy-Workshop.ps1`: interactive login and optional `STEAM_GUARD`; `workshop_preview.png`; steamcmd README updates.

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
