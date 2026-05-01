# SE Overclock (Unified: Upgrade + Prime)

**Workshop title:** *SE Overclock* — single Space Engineers package combining:

- **Upgrade modules**: cockpit `[Upgrade]` inventory, damage/missile hooks, NPC prefab upgrade loot (`SEUpgrademodule` scripts).
- **Prime blocks**: premium cubes, components, economy (`Prime_block` scripts for config only; prefab **Prime Matter** rolls run inside `SEUpgrademodule.MoreLoot` so only one session handler is registered).

## Legacy mods

Use **either** this unified mod **or** the older split mods (`SE_Upgrade_module_mod`, `SE_Prime_Block_mod`), not both, to avoid duplicate definitions and double prefab loot.

## Mod layout (`Data/`)

Definitions are under [Data/](Data/). Scripts live under [Data/Scripts/](Data/Scripts/).

| Area | Files / folders | Role |
|------|-----------------|------|
| Block GUI categories | `BlockCategories_Overclock.sbc` | Ship tool tab (Prime grinders/welders) + Production tab (`ModuleAmplificationStation`). |
| Cube blocks (Prime + tuned vanilla) | `CubeBlocks_*.sbc` | Premium / altered blocks (battery, decorative pack, energy, grinder, logistics, oxygen, prototech, solar, warfare, welder). |
| Block grouping | `BlockVariantGroups.sbc` | Variant links for Prime ship tools. |
| Components & blueprints | `Components.sbc`, `Blueprints.sbc`, `BlueprintClasses.sbc` | Prime Matter, upgrade chips, merged recipes. |
| Items & storage | `PhysicalItems.sbc`, `EntityComponents.sbc` | Ores/ingots; `UpgradeModuleSummary` ModStorage GUID. |
| Economy | `FactionTypes_Economy.sbc` | NPC store entries for mod items. |
| Voxels | `VoxelMaterialChanges.sbc`, `VoxelMaterials_asteroids.sbc` | Cerium / Lanthanum asteroid materials. |
| **Scripts** | `Scripts/SEUpgrademodule/` | Session/runtime: config, core, logic, prefab loot (upgrade + Prime Matter), network helpers. |
| **Scripts** | `Scripts/Prime_block/` | XML-only side: loads `Prime_blockConfig.xml` (no second prefab session handler). |

**Assets:** [Textures/](Textures/), [Models/](Models/). Optional branding source: [Textures/Marketing/Upgrade-Chip-Logo.png](Textures/Marketing/Upgrade-Chip-Logo.png) (not referenced by SBC; Workshop preview uses repo `tools/steamcmd/workshop_preview.png`).

### After changing `BlockCategories_Overclock.sbc`

In-game smoke check (host or DS): **Build screen → Ship tools** lists Prime grinders/welders; **Production** lists `ModuleAmplificationStation`. If either is missing, confirm this mod is the only one defining those categories and that the XML loads without errors in the log.

## Config files (world storage)

| File | Purpose |
|------|---------|
| `SEUpgrademoduleConfig.xml` | Upgrade loot caps, NPC multipliers, exclude prefab names, cockpit rescan. |
| `Prime_blockConfig.xml` | Prime Matter prefab chances (`SmallGridRare` / `LargeGridRare`), `PrefabLootMaxCargoContainers`, excludes. |

Both are loaded on dedicated server / single-player host.

## Steam Workshop

- **Listing:** [SE Overclock](https://steamcommunity.com/sharedfiles/filedetails/?id=3717639172) (`publishedfileid` **3717639172**).
- **Publish / update:** [tools/steamcmd/Deploy-Overclock.ps1](../tools/steamcmd/Deploy-Overclock.ps1). Set `$env:STEAM_OVERCLOCK_PUBLISHED_ID = "3717639172"` for a clean machine, or rely on an existing `se_overclock_workshop.vdf` (see steamcmd README). `modinfo.sbmi` already references this Workshop id.

## License

MIT — see [LICENSE](../LICENSE) in the repository root.
