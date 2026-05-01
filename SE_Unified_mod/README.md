# SE Unified Mod (Upgrade + Prime)

Single Space Engineers Workshop package combining:

- **Upgrade modules**: cockpit `[Upgrade]` inventory, damage/missile hooks, NPC prefab upgrade loot (`SEUpgrademodule` scripts).
- **Prime blocks**: premium cubes, components, economy (`Prime_block` scripts for config only; prefab **Prime Matter** rolls run inside `SEUpgrademodule.MoreLoot` so only one session handler is registered).

## Legacy mods

Use **either** this unified mod **or** the older split mods (`SE_Upgrade_module_mod`, `SE_Prime_Block_mod`), not both, to avoid duplicate definitions and double prefab loot.

## Config files (world storage)

| File | Purpose |
|------|---------|
| `SEUpgrademoduleConfig.xml` | Upgrade loot caps, NPC multipliers, exclude prefab names, cockpit rescan. |
| `Prime_blockConfig.xml` | Prime Matter prefab chances (`SmallGridRare` / `LargeGridRare`), `PrefabLootMaxCargoContainers`, excludes. |

Both are loaded on dedicated server / single-player host.

## Steam Workshop

Publish `SE_Unified_mod` as a **new** Workshop item (`modinfo.sbmi` uses placeholder id `0`). After upload, set the returned id in `modinfo.sbmi` or use [tools/steamcmd](../tools/steamcmd/README.md).

## License

MIT — see [LICENSE](../LICENSE) in the repository root.
