# Space Engineers Mods Collection

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Steam Workshop](https://img.shields.io/badge/Steam-Workshop-blue.svg)](https://steamcommunity.com/sharedfiles/filedetails/?id=3341019311)

This repository contains two comprehensive mods for Space Engineers: **SE Upgrade Module Mod** and **SE Prime Block Mod**. These mods provide advanced upgrade systems and premium blocks to enhance your gameplay experience.

## 📋 Table of Contents
- [Project Structure](#-project-structure)
- [SE Upgrade Module Mod](#-se-upgrade-module-mod)
- [SE Prime Block Mod](#️-se-prime-block-mod)
- [Development Information](#️-development-information)
- [Contributors](#-contributors)
- [License](#-license)
- [Support & Contact](#-support--contact)

## 📁 Project Structure

```
SE_mod/
├── SE_Prime_Block_mod/          # Prime Block Mod
│   ├── metadata.mod             # Mod metadata (v1.0.2)
│   ├── Data/                    # Game data definitions
│   │   ├── Components.sbc       # Component definitions
│   │   ├── CubeBlocks_*.sbc    # Various block definitions
│   │   └── Scripts/             # C# scripts
│   └── Textures/                # Texture files
└── SE_Upgrade_module_mod/       # Upgrade Module Mod
    ├── metadata.mod             # Mod metadata (v1.0)
    ├── modinfo.sbmi             # Steam Workshop info
    ├── Data/                    # Game data definitions
    │   ├── Components.sbc       # Upgrade module definitions
    │   ├── PhysicalItems.sbc    # Cerium/Lanthanum ores/ingots
    │   └── Scripts/             # C# scripts
    ├── Models/                  # 3D models
    └── Textures/                # Textures and icons
```

---

## 🎮 SE Upgrade Module Mod

### 📄 Overview
The **SE Upgrade Module Mod** enhances your Space Engineers experience by adding sophisticated upgrade modules for attack, defense, and energy efficiency. These modules can be upgraded up to level 10, providing progressive enhancements to your grids.

### 🌟 Features

#### 📦 Module Types
- **Attack Module**: Enhance offensive capabilities up to LV10
- **Defense Module**: Boost defensive strength up to LV10
- **Energy Efficiency Module**: Improve energy usage up to LV10
- **Speed Module**: Increases movement speed (reduces power efficiency)
- **Berserker Module**: Increases attack power, reduces defense/power efficiency
- **Fortress Module**: Increases defense, reduces speed/power efficiency

#### ⚙️ Leveling System
- Modules can be upgraded up to level 10
- Higher level modules can be crafted by combining multiple lower level modules
- Each module type has 10 distinct levels with progressive benefits

#### 🏗️ Grid Enhancement
- Add "[Upgrade]" to the name of cockpit-type blocks (including beds) within your grid
- Insert modules into the inventory of these blocks to apply enhancements
- Only the highest level of each module type is applied
- Automatic detection and application system

#### 🤖 NPC Grid Enhancement
- Some NPC grids can also receive enhancements
- Enhanced grids will display their level in the grid name
- The level is the sum of all applied module levels
- Dynamic scaling for balanced gameplay

#### 🔬 Crafting Materials
- **Cerium Ore**: Used as a crafting material for level 1 modules
- **Cerium Ingot**: Refined from Cerium Ore, used for crafting
- **Lanthanum Ore**: Used as a crafting material for level 1 modules  
- **Lanthanum Ingot**: Refined from Lanthanum Ore, used for crafting

#### 💻 Technical Implementation
- **Real-time Damage System**: Attack/defense modules affect damage calculations in real-time
- **Missile Enhancement**: Missile explosion damage is also affected by attack modules
- **Multiplayer Support**: Server-client synchronization system
- **Performance Optimization**: Load balancers for optimal performance

### 🚀 How to Use

1. **Setup Your Blocks**
   - Rename your cockpit-type blocks (including beds) to include "[Upgrade]" in their name
   - Open the block's inventory and insert the desired upgrade modules

2. **Apply Enhancements**
   - The mod automatically applies the highest level of each module type to the block
   - Combine multiple lower level modules to create higher tier upgrades

3. **NPC Grid Enhancements**
   - Certain NPC grids receive automatic enhancements
   - Enhanced NPC grids display their total enhancement level in their names

### 🙌 Special Thanks
- **NyaNyaNyang**: Provided valuable modeling resources for this project

### 🔗 Links
- [Steam Workshop Page](https://steamcommunity.com/sharedfiles/filedetails/?id=3341019311)

---

## 🏗️ SE Prime Block Mod

### 📄 Overview
The **SE Prime Block Mod** introduces premium blocks with enhanced performance and functionality to Space Engineers. These advanced blocks provide superior capabilities across various categories.

### 🌟 Core Features

#### 🔧 Block Categories
- **Energy Systems**: Enhanced batteries and solar panels
- **Industrial Equipment**: Advanced grinders and welders
- **Logistics Systems**: Improved transport and storage solutions
- **Combat Systems**: Enhanced weapon platforms
- **Life Support**: Efficient oxygen generators
- **Prototech**: Futuristic technology blocks

#### ⚙️ Advanced Systems
- **Configurable Drop Rates**: Customizable drop rates for different grid sizes
- **Grid Exclusion System**: Exclude specific grids (e.g., respawn pods) from effects
- **Subgrid Protection**: Optional protection for subgrids during grinding operations
- **Rarity Classification**: Items categorized as Common, Rare, or Exotic tiers

### 🎯 Loot Drop System

| Grid Type | Rarity | Drop Chance | Item Count |
|-----------|---------|-------------|------------|
| **Small Grids** | Common | 20% | 3-4 items |
| | Rare | 5% | 2-3 items |
| | Exotic | 5% | 1-2 items |
| **Large Grids** | Common | 15% | 6-20 items |
| | Rare | 7% | 3-10 items |
| | Exotic | 4% | 2-5 items |

---

## 🛠️ Development Information

### 📋 System Requirements
- **Game**: Space Engineers
- **Platform**: PC (Steam)
- **Multiplayer**: ✅ Supported
- **Dedicated Server**: ✅ Supported

### 🏗️ Technology Stack
- **Language**: C# (.NET Framework)
- **Game Engine**: Space Engineers ModAPI
- **File Formats**: 
  - `.sbc` (Space Engineers Block Configuration)
  - `.cs` (C# Scripts)
  - `.dds` (DirectDraw Surface Textures)
  - `.mwm` (Space Engineers 3D Models)

### 📁 Source Code Structure

<details>
<summary><strong>SE Upgrade Module Mod</strong></summary>

```
Data/Scripts/SEUpgrademodule/
├── Core.cs                  # Main mod logic and initialization
├── Logic.cs                 # Upgrade calculation and application
├── Config.cs                # Configuration management system
├── MoreLoot.cs              # Loot generation and distribution
├── MyUpConfig.cs            # User configuration interface
├── NetworkLoadBalancer.cs   # Network optimization and sync
├── PrintLoadBalancer.cs     # Output optimization and logging
└── StorageData.cs           # Persistent data storage
```
</details>

<details>
<summary><strong>SE Prime Block Mod</strong></summary>

```
Data/Scripts/Prime_block/
├── Config.cs                # Configuration management system
├── MoreLoot.cs              # Advanced loot generation system
└── MyConfig.cs              # User configuration interface
```
</details>

---

## 🤝 Contributors

| Mod | Role | Contributor |
|-----|------|-------------|
| **SE Upgrade Module Mod** | Lead Developer | snowmuffin |
| | 3D Modeling Resources | NyaNyaNyang |
| **SE Prime Block Mod** | Lead Developer | snowmuffin |

---

## 📜 License

This project is licensed under the [MIT License](LICENSE).

```
MIT License
Copyright (c) 2024 snowmuffin

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
```

---

## 📧 Support & Contact

- **🐛 Bug Reports**: [GitHub Issues](https://github.com/snowmuffin/SE_mod/issues)
- **💬 Discussions**: [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3341019311)
- **📝 Documentation**: This README file

---

## 🔄 Version History

| Mod | Version | Status | Release Notes |
|-----|---------|--------|---------------|
| **SE Upgrade Module Mod** | v1.0 | ✅ Stable | Initial release with core functionality |
| **SE Prime Block Mod** | v1.0.2 | ✅ Latest | Bug fixes and performance improvements |

---

## ⚠️ Known Issues & Troubleshooting

| Issue | Severity | Description | Workaround |
|-------|----------|-------------|------------|
| Performance Impact | ⚠️ Medium | Potential FPS drops on very large grids | Limit module usage on mega-structures |
| Mod Compatibility | ⚠️ Medium | Conflicts with other upgrade mods | Use one upgrade mod at a time |
| Network Sync | 🔴 High | Occasional desync in high-latency multiplayer | Restart affected clients |

---

## 🚀 Future Plans & Roadmap

### 🔮 Planned Features
- [ ] **New Module Types**: Additional specialized upgrade modules
- [ ] **Enhanced UI**: Improved user interface for better mod management
- [ ] **Performance Optimization**: Advanced algorithms for better performance
- [ ] **Prime Block Expansion**: Additional premium block categories
- [ ] **Cross-Mod Integration**: Better compatibility with popular SE mods
- [ ] **Advanced Configuration**: More granular configuration options

### 📅 Development Timeline
- **Short Term** (Next Update): Bug fixes and stability improvements
- **Medium Term** (Q3-Q4 2024): New features and UI enhancements
- **Long Term** (2025+): Major feature expansions and new mod integration

---

*This README is actively maintained and reflects the current state of both mods. Last updated: August 2024*
