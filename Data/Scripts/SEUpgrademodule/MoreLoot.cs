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
using System.Collections.Concurrent;


namespace SEUpgrademodule
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MoreLoot : MySessionComponentBase
    {
        public static ConcurrentDictionary<long, UpgradeLogic> cockpits = new ConcurrentDictionary<long, UpgradeLogic>();
        IMyCubeGrid Grid = null;
        List<IMySlimBlock> GridBlocks = new List<IMySlimBlock>();
        List<IMyCargoContainer> Container = new List<IMyCargoContainer>();

        // 업그레이드 레벨을 관리할 리스트
        List<UpgradeLevel> PUpLevels = new List<UpgradeLevel>();
        List<UpgradeLevel> AUpLevels = new List<UpgradeLevel>();
        List<UpgradeLevel> DUpLevels = new List<UpgradeLevel>();

        int MaxContainers = 7;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                Config.Load();

                int maxLevel = 10;
                double k = 1.0; // 지수 스케일링 상수 (필요에 따라 조정 가능)

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
                        MaxItemsSmall = Config.Instance.SmallGridAdvanced.MaxAmount - (level - 1),
                        MinItemsLarge = Config.Instance.LargeGridAdvanced.MinAmount,
                        MaxItemsLarge = Config.Instance.LargeGridAdvanced.MaxAmount - (level - 1)
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
                        MaxItemsSmall = Config.Instance.SmallGridAdvanced.MaxAmount - (level - 1),
                        MinItemsLarge = Config.Instance.LargeGridAdvanced.MinAmount,
                        MaxItemsLarge = Config.Instance.LargeGridAdvanced.MaxAmount - (level - 1)
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
                        MaxItemsSmall = Config.Instance.SmallGridAdvanced.MaxAmount - (level - 1),
                        MinItemsLarge = Config.Instance.LargeGridAdvanced.MinAmount,
                        MaxItemsLarge = Config.Instance.LargeGridAdvanced.MaxAmount - (level - 1)
                    });
                }

                MyVisualScriptLogicProvider.PrefabSpawnedDetailed += NewSpawn;
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
            allUpgradeLevels.AddRange(PUpLevels);
            allUpgradeLevels.AddRange(AUpLevels);
            allUpgradeLevels.AddRange(DUpLevels);

            int maxLevel = 10;
            double k = 1.0; // 지수 스케일링 상수 (필요에 따라 조정)

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

        private void NewSpawn(long entityId, string prefabName)
        {
            try
            {
                Grid = null;
                Grid = MyAPIGateway.Entities.GetEntityById(entityId) as IMyCubeGrid;
                if (Grid != null && Grid.Physics != null)
                {
                    if (Config.Instance.ExcludeGrids.Contains(prefabName.ToLower()) || Config.Instance.ExcludeGrids.Contains(Grid.CustomName.ToLower()))
                    {
                        return;
                    }
                    Container.Clear();
                    GridBlocks.Clear();
                    Grid.GetBlocks(GridBlocks);

                    foreach (var block in GridBlocks)
                    {
                        if (block.FatBlock != null && block.FatBlock is IMyCargoContainer)
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
                    }

                    MyLog.Default.WriteLine("SEUpgrademodule: Valid Grid " + Grid.CustomName + " spawned with " + Container.Count + " possible Cargos");

                    Container.ShuffleList();
                    int addedLoot = 0;
                    foreach (IMyCargoContainer cargo in Container)
                    {
                        if (AddLoot(cargo) && ++addedLoot >= MaxContainers) break;
                    }

                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("SEUpgrademodule: " + e);
            }
        }

        public static MyObjectBuilder_PhysicalObject GetBuilder(string category, string name)
        {
            switch (category)
            {
                case "MyObjectBuilder_Component":
                    return new MyObjectBuilder_Component() { SubtypeName = name };
                case "MyObjectBuilder_AmmoMagazine":
                    return new MyObjectBuilder_AmmoMagazine() { SubtypeName = name };
                case "MyObjectBuilder_Ingot":
                    return new MyObjectBuilder_Ingot() { SubtypeName = name };
                case "MyObjectBuilder_Ore":
                    return new MyObjectBuilder_Ore() { SubtypeName = name };
                case "MyObjectBuilder_ConsumableItem":
                    return new MyObjectBuilder_ConsumableItem() { SubtypeName = name };
                case "MyObjectBuilder_PhysicalGunObject":
                    return new MyObjectBuilder_PhysicalGunObject() { SubtypeName = name };
                default:
                    return new MyObjectBuilder_PhysicalObject() { SubtypeName = name };
            }
        }

        protected override void UnloadData()
        {
            MyVisualScriptLogicProvider.PrefabSpawnedDetailed -= NewSpawn; //Make sure to unregister
        }

        protected struct Item
        {
            public MyObjectBuilder_Component builder;
            public int minItemsSmall;
            public int minItemsLarge;
            public int maxItemsSmall;
            public int maxItemsLarge;
            public double chanceSmall;
            public double chanceLarge;
            public string Name; // 추가: 이름을 저장하기 위해
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
