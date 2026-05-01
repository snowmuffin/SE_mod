# SE Prime Block Mod

Premium block definitions for Space Engineers (batteries, solar, logistics, warfare-related blocks, etc.). This folder is a **standalone Workshop mod**: copy `SE_Prime_Block_mod` into your Space Engineers `Mods` directory.

## Scripts

- **Prefab cargo loot**: On dedicated server / single-player host, when a prefab grid spawns, up to `PrefabLootMaxCargoContainers` cargo containers may receive rolls of **Prime_Matter** (chances and amounts come from `SmallGridRare` / `LargeGridRare` in the world config XML).
- **Config file** (world storage): `Prime_blockConfig.xml` — see `Data/Scripts/Prime_block/MyConfig.cs` for the schema. Important fields:
  - `ExcludeGrids`: substring list matched against prefab name and grid custom name (default includes `respawn`).
  - `PrefabLootMaxCargoContainers`: cap on cargo rolls per spawn (default **5** if missing or invalid).

## Related

The companion **SE Upgrade Module Mod** lives in `../SE_Upgrade_module_mod`. Both mods can be enabled together; they use separate namespaces and config file names.

## License

See [LICENSE](../LICENSE) in the repository root.
