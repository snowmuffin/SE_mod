using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;


namespace Prime_block
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MoreLoot : MySessionComponentBase
    {

        IMyCubeGrid Grid = null;
        List<IMySlimBlock> GridBlocks = new List<IMySlimBlock>();
        List<IMyCargoContainer> Container = new List<IMyCargoContainer>();
        

        Item Prime_Matter;
        int MaxContainers = 5;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                Config.Load();

                
                Prime_Matter = new Item
                {
                    builder = new MyObjectBuilder_Component() { SubtypeName = "Prime_Matter" },
                    chanceSmall = Config.Instance.SmallGridRare.Chance,
                    chanceLarge = Config.Instance.LargeGridRare.Chance,
                    minItemsSmall = Config.Instance.SmallGridRare.MinAmount,
                    maxItemsSmall = Config.Instance.SmallGridRare.MaxAmount,
                    minItemsLarge = Config.Instance.LargeGridRare.MinAmount,
                    maxItemsLarge = Config.Instance.LargeGridRare.MaxAmount
                };
                
                MyVisualScriptLogicProvider.PrefabSpawnedDetailed += NewSpawn;
            }
        }


        private bool AddLoot(IMyCargoContainer container)
        {
            bool added = false;

            bool isLarge = container.CubeGrid.GridSizeEnum == MyCubeSize.Large;
            IMyInventory inventory = container.GetInventory();

            try
            {
                
                if (MyUtils.GetRandomDouble(0, 1) <= (isLarge ? Prime_Matter.chanceLarge : Prime_Matter.chanceSmall))
                {
                    int amount = MyUtils.GetRandomInt((isLarge ? Prime_Matter.minItemsLarge : Prime_Matter.minItemsSmall), (isLarge ? Prime_Matter.maxItemsLarge : Prime_Matter.maxItemsSmall));
                    MyLog.Default.WriteLine("Prime_block: Added " + amount + "x Rare Tech to " + container.CustomName);
                    inventory.AddItems(amount, Prime_Matter.builder);
                    added = true;
                }
                
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Prime_block:  FAILED " + e);
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
                    if(Config.Instance.ExcludeGrids.Contains(prefabName.ToLower()) || Config.Instance.ExcludeGrids.Contains(Grid.CustomName.ToLower()))
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

                    MyLog.Default.WriteLine("Prime_block: Valid Grid " + Grid.CustomName + " spawned with " + Container.Count + " possible Cargos");

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
                MyLog.Default.WriteLine("Prime_block: " + e);
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
                default: return new MyObjectBuilder_PhysicalObject() { SubtypeName = name };
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
        }
    }
}