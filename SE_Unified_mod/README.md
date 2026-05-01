# SE Overclock (Unified: Upgrade + Prime)

**Workshop title:** *SE Overclock* — single Space Engineers package combining:

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

Publish with [tools/steamcmd/Deploy-Overclock.ps1](../tools/steamcmd/Deploy-Overclock.ps1) (uses Workshop title **SE Overclock**). After the first upload, set `STEAM_OVERCLOCK_PUBLISHED_ID` to the new FileID and update `modinfo.sbmi`.

## License

MIT — see [LICENSE](../LICENSE) in the repository root.
