# Space Engineers Mods Collection

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Steam Workshop](https://img.shields.io/badge/Steam-Workshop-blue.svg)](https://steamcommunity.com/sharedfiles/filedetails/?id=3341019311)

This repository contains two comprehensive mods for Space Engineers: **SE Upgrade Module Mod** and **SE Prime Block Mod**. These mods introduce advanced upgrade systems and premium blocks, significantly enhancing your gameplay experience by providing more options and greater customization.

## Table of Contents
- [Project Structure](#project-structure)
- [SE Upgrade Module Mod](#se-upgrade-module-mod)
- [SE Prime Block Mod](#se-prime-block-mod)
- [Installation](#installation)
- [Usage](#usage)
- [Development Information](#development-information)
- [Contributors](#contributors)
- [License](#license)
- [Support & Contact](#support--contact)
- [Changelog](#changelog)
- [Mod coupling](#mod-coupling)

## Mod coupling

- **Economy (`FactionTypes_Economy.sbc`)** lives only in **SE Prime Block Mod**. Its Trader (and other) lists reference **Prime_Matter** (defined in Prime) and **tier‑1 upgrade components** (defined in Upgrade). For a consistent economy, **subscribe to both mods** and list **both** in the world’s mod list. Upgrade alone no longer ships a duplicate Trader economy patch.
- **Scripts** stay in separate assemblies (one folder per Workshop item). Shared behaviour is aligned by convention (prefab spawn guards, config patterns), not by a shared DLL.

## Project Structure

```
SE_mod/
├── SE_Prime_Block_mod/          # Prime Block Mod
│   ├── metadata.mod             # Mod metadata
│   ├── Data/                    # Game data definitions
│   │   ├── Components.sbc       # Component definitions
│   │   ├── CubeBlocks_*.sbc     # Various block definitions
│   │   └── Scripts/             # C# scripts
│   └── Textures/                # Texture files
└── SE_Upgrade_module_mod/       # Upgrade Module Mod
    ├── metadata.mod             # Mod metadata
    ├── modinfo.sbmi             # Steam Workshop info
    ├── Data/                    # Game data definitions
    │   ├── Components.sbc       # Upgrade module definitions
    │   ├── PhysicalItems.sbc    # Cerium/Lanthanum ores/ingots
    │   └── Scripts/             # C# scripts
    ├── Models/                  # 3D models
    └── Textures/                # Textures and icons
```

---

## SE Upgrade Module Mod

### Overview
The **SE Upgrade Module Mod** enhances your Space Engineers experience by introducing sophisticated upgrade modules that can be applied to various grid types within the game. These modules focus on improving attack, defense, and energy efficiency, with the potential for upgrades up to level 10, offering progressive enhancements to your gameplay.

### Features

#### Module Types
- **Attack Module**: Increases offensive capabilities up to LV10, boosting damage output.
- **Defense Module**: Enhances defensive strength up to LV10, improving resilience against attacks.
- **Energy Efficiency Module**: Optimizes energy consumption up to LV10, allowing for longer operation times.
- **Speed Module**: Increases movement speed with a trade-off on power efficiency.
- **Berserker Module**: Amplifies attack power at the cost of reduced defense and power efficiency.
- **Fortress Module**: Strengthens defense while reducing movement speed and power efficiency.

#### Leveling System
- Each module type can be upgraded to a maximum of level 10 by combining lower-level modules.
- Higher-level modules provide more significant benefits and can be crafted from multiple lower-tier modules.

#### Grid Enhancement
- Players can add "[Upgrade]" to the names of cockpit-type blocks (including beds) to enable module enhancements.
- The mod automatically detects and applies the highest level of each module type within the block's inventory.

#### NPC Grid Enhancement
- Some non-player character (NPC) grids can also receive enhancements.
- Enhanced grids showcase their total enhancement level in their names, calculated as the sum of all applied module levels (plus configured NPC offsets). Cargo prefab loot rolls use upgrade module **levels 1–3** only for probability; cockpit NPC rolls use the full **1–10** range.

#### Crafting Materials
- **Cerium Ore**: Essential for crafting level 1 modules.
- **Cerium Ingot**: Refined from Cerium Ore for crafting.
- **Lanthanum Ore**: Necessary for crafting level 1 modules.
- **Lanthanum Ingot**: Refined from Lanthanum Ore for crafting.

#### Technical Implementation
- The mod features a **real-time damage system** that allows attack and defense modules to impact damage calculations dynamically.
- **Missile enhancements** are also influenced by attack modules, creating a more engaging combat experience.
- Full **multiplayer support** ensures that upgrades are synchronized across server-client interactions.
- **Performance optimizations** include load balancers to maintain performance stability during gameplay.

Optional tuning (world storage `SEUpgrademoduleConfig.xml`): `PrefabLootMaxCargoContainers`, `PrefabLootMaxCockpitAttempts` cap how many cargo blocks receive rolls and how many cockpits are tried per prefab spawn (defaults: 7 and 1).

### How to Use

1. **Setup Your Blocks**
   - Rename your cockpit-type blocks (including beds) to include "[Upgrade]" in their name.
   - Access the block's inventory and insert desired upgrade modules.

2. **Apply Enhancements**
   - The mod automatically applies the highest level of each module type to the block.
   - Combine multiple lower-level modules to create higher-tier upgrades.

3. **NPC Grid Enhancements**
   - Certain NPC grids will automatically receive enhancements, which will be reflected in their names.

---

## SE Prime Block Mod

### Overview
The **SE Prime Block Mod** introduces a range of premium blocks designed to enhance performance and functionality within Space Engineers. These blocks provide superior capabilities, allowing players to optimize their builds for various gameplay scenarios.

### Core Features

#### Block Categories
- **Energy Systems**: Advanced batteries and solar panels that offer greater efficiency and capacity.
- **Industrial Equipment**: High-performance machinery and components for enhanced production and processing capabilities.
- **Defense Structures**: Upgraded walls, turrets, and shields that provide better protection against threats.
- **Utility Blocks**: Enhanced blocks that improve overall grid functionality and efficiency.

Prefab cargo loot uses `Prime_blockConfig.xml`; optional `PrefabLootMaxCargoContainers` (default 5) limits rolls per spawn. See [SE_Prime_Block_mod/README.md](SE_Prime_Block_mod/README.md).

### How to Use
1. **Install the Mod**: Follow the installation instructions below to add the mod to your game.
2. **Access the Blocks**: Find the new premium blocks in your build menu.
3. **Construction**: Utilize these blocks to create more powerful and efficient grids.

## Installation
To install the mods, follow these steps:

1. **Download the Mods**:
   - Clone this repository or download it as a ZIP file.
   
   ```bash
   git clone <YOUR_REPOSITORY_URL_HERE>
   ```

2. **Upload to Space Engineers**:
   - Navigate to your Space Engineers installation directory.
   - Copy each mod folder (`SE_Upgrade_module_mod`, `SE_Prime_Block_mod`) into the `Mods` directory within your Space Engineers folder (each mod is its own folder under `Mods`).

3. **Activate the Mods**:
   - Start Space Engineers and enable the mods in the game settings under the Mods section.

## Contributing
We welcome contributions to enhance the SE_mod project. If you have suggestions, improvements, or bug fixes, please fork the repository and submit a pull request. Follow existing code style and keep changes focused.

## Versioning
- Bump `<ModVersion>` in each mod’s `metadata.mod` when releasing.
- Record user-facing changes in [CHANGELOG.md](CHANGELOG.md).
- To push Workshop updates from the command line, see [tools/steamcmd/README.md](tools/steamcmd/README.md) (SteamCMD `workshop_build_item` + VDF examples).

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Support & Contact
Open an issue on this GitHub repository for support or feature requests.

## Changelog
See [CHANGELOG.md](CHANGELOG.md).

---

Thank you for checking out the Space Engineers Mods Collection.
