using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRageMath;
using VRage;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage.Utils;
using VRage.Game.Components;
using VRage.Game;
using VRage.Game.ModAPI;

namespace SEUpgrademodule
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MoreLoot : MySessionComponentBase
    {
        IMyCubeGrid Grid = null;
        List<IMySlimBlock> GridBlocks = new List<IMySlimBlock>();
        List<IMyCargoContainer> Container = new List<IMyCargoContainer>();
        List<IMyTerminalBlock> Cockpit = new List<IMyTerminalBlock>();
        // 업그레이드 레벨을 관리할 리스트
        List<UpgradeLevel> PUpLevels = new List<UpgradeLevel>();
        List<UpgradeLevel> AUpLevels = new List<UpgradeLevel>();
        List<UpgradeLevel> DUpLevels = new List<UpgradeLevel>();
        List<UpgradeLevel> SUpLevels = new List<UpgradeLevel>();
        List<UpgradeLevel> BUpLevels = new List<UpgradeLevel>();
        List<UpgradeLevel> FUpLevels = new List<UpgradeLevel>();

        /// <summary>Prime Matter prefab rolls (merged from legacy Prime_block mod).</summary>
        PrimeMatterRoll _primeMatter;

        struct PrimeMatterRoll
        {
            public MyObjectBuilder_Component Builder;
            public double ChanceSmall;
            public double ChanceLarge;
            public int MinSmall;
            public int MaxSmall;
            public int MinLarge;
            public int MaxLarge;
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                Prime_block.Config.Load();
                Config.Load();

                var pc = Prime_block.Config.Instance;
                _primeMatter = new PrimeMatterRoll
                {
                    Builder = new MyObjectBuilder_Component() { SubtypeName = "Prime_Matter" },
                    ChanceSmall = pc.SmallGridRare.Chance,
                    ChanceLarge = pc.LargeGridRare.Chance,
                    MinSmall = pc.SmallGridRare.MinAmount,
                    MaxSmall = pc.SmallGridRare.MaxAmount,
                    MinLarge = pc.LargeGridRare.MinAmount,
                    MaxLarge = pc.LargeGridRare.MaxAmount
                };

                int maxLevel = 10;
                double k = 0.3f; // 지수 스케일링 상수 (필요에 따라 조정 가능)

                // PUp 레벨 초기화
                for (int level = 1; level <= maxLevel; level++)
                {
                    PUpLevels.Add(new UpgradeLevel
                    {
                        Name = $"PUpLv{level}",
                        Builder = new MyObjectBuilder_Component() { SubtypeName = $"PowerEfficiencyUpgradeModule_Level{level}" },
                        ChanceSmall = Config.Instance.SmallGridAdvanced.Chance,
                        ChanceLarge = Config.Instance.LargeGridAdvanced.Chance,
                        MinItemsSmall = Config.Instance.SmallGridAdvanced.MinAmount,
                        MaxItemsSmall = Config.Instance.SmallGridAdvanced.MaxAmount,
                        MinItemsLarge = Config.Instance.LargeGridAdvanced.MinAmount,
                        MaxItemsLarge = Config.Instance.LargeGridAdvanced.MaxAmount
                    });
                }

                // AUp 레벨 초기화
                for (int level = 1; level <= maxLevel; level++)
                {
                    AUpLevels.Add(new UpgradeLevel
                    {
                        Name = $"AUpLv{level}",
                        Builder = new MyObjectBuilder_Component() { SubtypeName = $"AttackUpgradeModule_Level{level}" },
                        ChanceSmall = Config.Instance.SmallGridAdvanced.Chance,
                        ChanceLarge = Config.Instance.LargeGridAdvanced.Chance,
                        MinItemsSmall = Config.Instance.SmallGridAdvanced.MinAmount,
                        MaxItemsSmall = Config.Instance.SmallGridAdvanced.MaxAmount,
                        MinItemsLarge = Config.Instance.LargeGridAdvanced.MinAmount,
                        MaxItemsLarge = Config.Instance.LargeGridAdvanced.MaxAmount
                    });
                }

                // DUp 레벨 초기화
                for (int level = 1; level <= maxLevel; level++)
                {
                    DUpLevels.Add(new UpgradeLevel
                    {
                        Name = $"DUpLv{level}",
                        Builder = new MyObjectBuilder_Component() { SubtypeName = $"DefenseUpgradeModule_Level{level}" },
                        ChanceSmall = Config.Instance.SmallGridAdvanced.Chance,
                        ChanceLarge = Config.Instance.LargeGridAdvanced.Chance,
                        MinItemsSmall = Config.Instance.SmallGridAdvanced.MinAmount,
                        MaxItemsSmall = Config.Instance.SmallGridAdvanced.MaxAmount,
                        MinItemsLarge = Config.Instance.LargeGridAdvanced.MinAmount,
                        MaxItemsLarge = Config.Instance.LargeGridAdvanced.MaxAmount
                    });
                }

                int specialMaxLevel = 3;
                for (int level = 1; level <= specialMaxLevel; level++)
                {
                    SUpLevels.Add(new UpgradeLevel { Name = $"SUpLv{level}", Builder = new MyObjectBuilder_Component() { SubtypeName = $"SpeedModule_Level{level}" } });
                    BUpLevels.Add(new UpgradeLevel { Name = $"BUpLv{level}", Builder = new MyObjectBuilder_Component() { SubtypeName = $"BerserkerModule_Level{level}" } });
                    FUpLevels.Add(new UpgradeLevel { Name = $"FUpLv{level}", Builder = new MyObjectBuilder_Component() { SubtypeName = $"FortressModule_Level{level}" } });
                }

                MyVisualScriptLogicProvider.PrefabSpawnedDetailed += NewSpawn;
            }
        }

        private static bool IsPrefabSpawnExcluded(string prefabLower, string gridNameLower)
        {
            if (prefabLower.Contains("respawn"))
                return true;
            var up = Config.Instance.ExcludeGrids;
            if (up != null && (up.Contains(prefabLower) || up.Contains(gridNameLower)))
                return true;
            var pr = Prime_block.Config.Instance.ExcludeGrids;
            if (pr != null && (pr.Contains(prefabLower) || pr.Contains(gridNameLower)))
                return true;
            return false;
        }

        private bool TryAddPrimeMatterToCargo(IMyCargoContainer container)
        {
            try
            {
                bool isLarge = container.CubeGrid.GridSizeEnum == MyCubeSize.Large;
                var inv = container.GetInventory();
                if (inv == null)
                    return false;
                double chance = isLarge ? _primeMatter.ChanceLarge : _primeMatter.ChanceSmall;
                if (MyUtils.GetRandomDouble(0, 1) > chance)
                    return false;
                int minA = isLarge ? _primeMatter.MinLarge : _primeMatter.MinSmall;
                int maxA = isLarge ? _primeMatter.MaxLarge : _primeMatter.MaxSmall;
                int amount = MyUtils.GetRandomInt(minA, maxA);
                MyLog.Default.WriteLine($"SE_Overclock: Added {amount}x Prime_Matter to {container.CustomName}");
                inv.AddItems(amount, _primeMatter.Builder);
                return true;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("SEUpgrademodule Prime loot: " + e);
                return false;
            }
        }

        // 지수 스케일링 함수
        private double GetExponentiallyScaledChance(int level, double baseChance, int maxLevel, double k = 1.0)
        {
            // P(l) = baseChance * e^{ -k * (l - 1) }
            return baseChance * Math.Exp(-k * (level - 1));
        }

        private bool AddLoot(IMyCargoContainer container)
        {
            bool added = false;

            bool isLarge = container.CubeGrid.GridSizeEnum == MyCubeSize.Large;
            IMyInventory inventory = container.GetInventory();

            // 모든 업그레이드 레벨 리스트 합치기
            List<UpgradeLevel> allUpgradeLevels = new List<UpgradeLevel>();
            allUpgradeLevels.AddRange(PUpLevels.Take(3));
            allUpgradeLevels.AddRange(AUpLevels.Take(3));
            allUpgradeLevels.AddRange(DUpLevels.Take(3));

            int maxLevel = 3;
            double k = 1.5; // 지수 스케일링 상수 (필요에 따라 조정)

            try
            {
                foreach (var upgrade in allUpgradeLevels)
                {
                    // 업그레이드 이름에서 타입과 레벨 분리
                    // 예: "PUpLv1" -> "PUp", 1
                    string upgradeType = "";
                    int level = 1;

                    if (upgrade.Name.StartsWith("PUpLv"))
                    {
                        upgradeType = "PUp";
                        level = int.Parse(upgrade.Name.Substring(5));
                    }
                    else if (upgrade.Name.StartsWith("AUpLv"))
                    {
                        upgradeType = "AUp";
                        level = int.Parse(upgrade.Name.Substring(5));
                    }
                    else if (upgrade.Name.StartsWith("DUpLv"))
                    {
                        upgradeType = "DUp";
                        level = int.Parse(upgrade.Name.Substring(5));
                    }
                    else
                    {
                        // 알 수 없는 업그레이드 타입
                        continue;
                    }

                    // 기본 확률 선택
                    double baseChance = isLarge ? upgrade.ChanceLarge : upgrade.ChanceSmall;

                    // 지수 스케일링 적용
                    double scaledChance = GetExponentiallyScaledChance(level, baseChance, maxLevel, k);

                    // 확률 검사
                    if (MyUtils.GetRandomDouble(0, 1) <= scaledChance)
                    {
                        int amount = MyUtils.GetRandomInt(
                            isLarge ? upgrade.MinItemsLarge : upgrade.MinItemsSmall,
                            isLarge ? upgrade.MaxItemsLarge : upgrade.MaxItemsSmall
                        );

                        MyLog.Default.WriteLine($"SE_Upgrade_module: Added {amount}x {upgrade.Name} to {container.CustomName}");
                        inventory.AddItems(amount, upgrade.Builder);
                        added = true;
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("SEUpgrademodule: FAILED " + e);
            }

            return added;
        }
        private bool AddLootCockpit(IMyTerminalBlock cockpit)
        {
            bool added = false;

            List<UpgradeLevel> allUpgradeLevels = new List<UpgradeLevel>();
            IMyInventory inventory = cockpit.GetInventory();
            allUpgradeLevels.AddRange(PUpLevels);
            allUpgradeLevels.AddRange(AUpLevels);
            allUpgradeLevels.AddRange(DUpLevels);

            // 각 업그레이드 타입에 대해 처리할 최대 레벨
            int maxLevel = 10;
            double k = 0.7f; // 지수 스케일링 상수 (필요에 따라 조정)

            // 그리드 레벨 합산을 위한 변수
            int totalLevel = Config.Instance.NpcOffset.Power+Config.Instance.NpcOffset.Defence+Config.Instance.NpcOffset.Attack;

            try
            {
                // 디버그 로그: 업그레이드 시작

                // 각 업그레이드 타입별로 레벨을 랜덤하게 선택
                string[] upgradeTypes = { "PUp", "AUp", "DUp" };

                foreach (var selectedUpgradeType in upgradeTypes)
                {
                    List<double> levelWeights = new List<double>();
                    double totalWeight = 0;
            

                    for (int level = 1; level <= maxLevel; level++)
                    {
                        // 지수 스케일링: 레벨이 높을수록 가중치가 낮아짐
                        double weight = Math.Exp(-k * (level - 1));
                        levelWeights.Add(weight);
                        totalWeight += weight;
                    }

                    // 3. 누적 가중치를 이용하여 레벨 선택
                    double randomValue = MyUtils.GetRandomDouble(0, totalWeight);
                    double cumulativeWeight = 0;
                    int selectedLevel = maxLevel;

                    for (int level = 1; level <= maxLevel; level++)
                    {
                        cumulativeWeight += levelWeights[level - 1];
                        if (randomValue <= cumulativeWeight)
                        {
                            selectedLevel = level;
                            break;
                        }
                    }

                    // 4. 선택된 업그레이드 타입과 레벨로 이름 생성
                    string upgradeName = $"{selectedUpgradeType}Lv{selectedLevel}";

                    // 디버그 로그: 선택된 레벨과 타입

                    // 5. 해당 업그레이드를 찾아서 추가
                    var upgrade = allUpgradeLevels.FirstOrDefault(u => u.Name == upgradeName);

                    if (upgrade != null)
                    {
                        // 확률 기반 추가 로직 생략하고, 무조건 추가
                        int amount = 1;

                        // 디버그 로그: 아이템 추가
                        inventory.AddItems(amount, upgrade.Builder);
                        added = true;

                        // 해당 업그레이드 레벨을 합산
                        totalLevel += selectedLevel;
                    }
                    else
                    {
                        // 디버그 로그: 업그레이드 찾기 실패
                    }


                }
                // Speed / Berserker / Fortress: 각 25% 확률, 레벨 1-3 지수 가중치
                const double specialChance = 0.25;
                const double kSpecial = 1.5;
                const int specialMax = 3;

                double[] specialWeights = new double[specialMax];
                double specialTotalWeight = 0;
                for (int i = 0; i < specialMax; i++)
                {
                    specialWeights[i] = Math.Exp(-kSpecial * i);
                    specialTotalWeight += specialWeights[i];
                }

                var specialGroups = new List<UpgradeLevel>[] { SUpLevels, BUpLevels, FUpLevels };
                var specialPrefixes = new string[] { "SUp", "BUp", "FUp" };

                for (int g = 0; g < specialGroups.Length; g++)
                {
                    if (MyUtils.GetRandomDouble(0, 1) > specialChance)
                        continue;

                    double rand = MyUtils.GetRandomDouble(0, specialTotalWeight);
                    double cumulative = 0;
                    int selectedLevel = 1;
                    for (int i = 0; i < specialMax; i++)
                    {
                        cumulative += specialWeights[i];
                        if (rand <= cumulative) { selectedLevel = i + 1; break; }
                    }

                    var upgrade = specialGroups[g].Find(u => u.Name == $"{specialPrefixes[g]}Lv{selectedLevel}");
                    if (upgrade != null)
                    {
                        inventory.AddItems(1, upgrade.Builder);
                        added = true;
                    }
                }

                // 6. 그리드 이름에 'LV'와 총 레벨을 추가 (합계; README와 동일하게 공격 배율만 곱하지 않음)
                if (totalLevel >= 0)
                {
                    var grid = (cockpit as IMyCubeBlock).CubeGrid;
                    if (grid != null && !grid.CustomName.Contains("[LV"))
                    {
                        grid.CustomName += $" [LV{totalLevel}]";
                        MyLog.Default.WriteLine($"SE_Upgrade_module: Updated grid name to {grid.CustomName}");
                    }
                }
                if (!cockpit.CustomName.Contains("[Upgrade]"))
                {
                    
                    cockpit.CustomName += " [Upgrade]";
            
                }
               
            }
            catch (Exception e)
            {
                // 디버그 로그: 예외 처리
                MyLog.Default.WriteLine("SEUpgrademodule: FAILED " + e);
            }

        
            
            return added;
        }
        private void NewSpawn(long entityId, string prefabName)
        {
            try
            {
                Grid = null;
                Grid = MyAPIGateway.Entities.GetEntityById(entityId) as IMyCubeGrid;
                if (Grid == null || Grid.MarkedForClose)
                    return;
                if (Grid.IsStatic)
                    return;
                if (Grid.Physics != null)
                {
                    string prefabLower = prefabName != null ? prefabName.ToLower() : "";
                    string gridNameLower = Grid.CustomName != null ? Grid.CustomName.ToLower() : "";
                    if (IsPrefabSpawnExcluded(prefabLower, gridNameLower))
                    {
                        return;
                    }
                    Container.Clear();
                    Cockpit.Clear();
                    GridBlocks.Clear();
                    Grid.GetBlocks(GridBlocks);

                    foreach (var block in GridBlocks)
                    {
                        if (block.FatBlock != null)
                        {
                            if(block.FatBlock is IMyCargoContainer)
                            {
                                var cargo = block.FatBlock as IMyCargoContainer;
                                if (cargo != null && !cargo.MarkedForClose && cargo.IsWorking)
                                {
                                    var inventory = cargo.GetInventory();
                                    if (cargo.GetInventory() != null)
                                    {
                                        Container.Add(cargo);
                                    }
                                }
                            }
                            else if(block.FatBlock is IMyCockpit)
                            {
                                
                                var cockpit = block.FatBlock as IMyTerminalBlock;
                                if (cockpit != null)
                                {
                                    
                                    if (cockpit.GetInventory() != null)
                                    {
                                        
                                        Cockpit.Add(cockpit);
                                    }
                                }
                            }

                        }
                    }


                    Container.ShuffleList();
                    int addedLoot = 0;
                    int maxCargoLoot = Config.Instance.PrefabLootMaxCargoContainers;
                    Cockpit.ShuffleList();
                    foreach (IMyCargoContainer cargo in Container)
                    {
                        if (AddLoot(cargo) && ++addedLoot >= maxCargoLoot) break;
                    }
                    int primeAdds = 0;
                    int primeCap = Prime_block.Config.Instance.PrefabLootMaxCargoContainers;
                    foreach (IMyCargoContainer cargo in Container)
                    {
                        if (TryAddPrimeMatterToCargo(cargo) && ++primeAdds >= primeCap)
                            break;
                    }
                    int cockpitTries = 0;
                    int maxCockpitLoot = Config.Instance.PrefabLootMaxCockpitAttempts;
                    foreach (IMyTerminalBlock cockpit in Cockpit)
                    {
                        cockpitTries++;
                        if (AddLootCockpit(cockpit))
                            break;
                        if (cockpitTries >= maxCockpitLoot)
                            break;
                    }

                    List<IMyBeacon> beacons = new List<IMyBeacon>();
                    IMyGridTerminalSystem tsystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(Grid);
                    if (tsystem == null)
                        return;
                    tsystem.GetBlocksOfType<IMyBeacon>(beacons);
                    if (beacons != null)
                    {
                        foreach(IMyBeacon beacon in beacons)
                        {
                            beacon.Enabled = true; // 비콘 활성화
                            beacon.CustomName = Grid.CustomName; // 신호 이름 설정
                            beacon.HudText  = Grid.CustomName;
                        }
            

                    }

                    List<IMyRadioAntenna> antennas = new List<IMyRadioAntenna>();
                    tsystem.GetBlocksOfType<IMyRadioAntenna>(antennas);
                    if (antennas != null)
                    {
                        foreach(IMyRadioAntenna antenna in antennas)
                        {
                            antenna.Enabled = true; // 비콘 활성화
                            antenna.CustomName = Grid.CustomName; // 신호 이름 설정
                            antenna.HudText  = Grid.CustomName;
                        }
                    }

                    
                    
                    

                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("SEUpgrademodule: " + e);
            }
        }

        protected override void UnloadData()
        {
            MyVisualScriptLogicProvider.PrefabSpawnedDetailed -= NewSpawn; //Make sure to unregister
        }

        // UpgradeLevel 클래스
        private class UpgradeLevel
        {
            public string Name { get; set; }
            public MyObjectBuilder_Component Builder { get; set; }
            public double ChanceSmall { get; set; }
            public double ChanceLarge { get; set; }
            public int MinItemsSmall { get; set; }
            public int MaxItemsSmall { get; set; }
            public int MinItemsLarge { get; set; }
            public int MaxItemsLarge { get; set; }
        }
    }
}
