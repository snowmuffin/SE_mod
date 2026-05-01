# SE Prime Block Mod

**Legacy:** use **[SE_Overclock_mod](../SE_Overclock_mod/)** for new installs. This folder remains as reference; see `MIGRATED_TO_UNIFIED.md`.

Premium block definitions for Space Engineers (batteries, solar, logistics, warfare-related blocks, etc.).

## Scripts

- **Prefab cargo loot**: On dedicated server / single-player host, when a prefab grid spawns, up to `PrefabLootMaxCargoContainers` cargo containers may receive rolls of **Prime_Matter** (chances and amounts come from `SmallGridRare` / `LargeGridRare` in the world config XML).
- **Config file** (world storage): `Prime_blockConfig.xml` — see `Data/Scripts/Prime_block/MyConfig.cs` for the schema. Important fields:
  - `ExcludeGrids`: substring list matched against prefab name and grid custom name (default includes `respawn`).
  - `PrefabLootMaxCargoContainers`: cap on cargo rolls per spawn (default **5** if missing or invalid).

## Related

The companion **SE Upgrade Module Mod** lives in `../SE_Upgrade_module_mod`. Enable **both** if you use this mod’s **`FactionTypes_Economy.sbc`**: Trader offers reference upgrade components that only exist when the Upgrade mod is loaded. Economy definitions are not duplicated in the Upgrade mod.

## License

See [LICENSE](../LICENSE) in the repository root.
