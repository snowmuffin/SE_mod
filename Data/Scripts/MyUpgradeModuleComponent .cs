using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic; // List<T>와 HashSet<T>를 위해 필요
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Definitions;
using VRage.Game.ModAPI; // IMyInventory와 IMyInventoryItem을 위해 추가
using VRage.ModAPI; // IMyEntity 등을 위해 필요
using VRage.ObjectBuilders;
using VRage.Utils;

[MyObjectBuilderDefinition]
public class MyObjectBuilder_CustomItemDefinition : MyObjectBuilder_PhysicalItemDefinition
{
    // 필드 정의
    public string CustomData = "Default custom data";
    public string UniqueId = Guid.NewGuid().ToString(); // 아이템 고유 ID 생성
}

[MyDefinitionType(typeof(MyObjectBuilder_CustomItemDefinition))]
public class MyCustomItemDefinition : MyPhysicalItemDefinition
{
    public string CustomData { get; set; } = "Default custom data";
    public string UniqueId { get; set; } = Guid.NewGuid().ToString(); // 고유 ID

    protected override void Init(MyObjectBuilder_DefinitionBase builder)
    {
        base.Init(builder);
        MyAPIGateway.Utilities.ShowMessage("CustomItemDisplayNameChanger", "Session Init Started");
        MyLog.Default.WriteLine("CustomItemDisplayNameChanger: Session Init Started");

        var customItemBuilder = builder as MyObjectBuilder_CustomItemDefinition;
        if (customItemBuilder != null)
        {
            CustomData = customItemBuilder.CustomData;
            UniqueId = customItemBuilder.UniqueId; // 정의에서 Unique ID를 가져옴
        }
    }
}

[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
public class ItemCountHandler : MySessionComponentBase
{
    private bool _initialized = false;

    public override void UpdateAfterSimulation()
    {
        if (!_initialized)
        {
            // 서버에서만 코드 실행
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            // 월드에 존재하는 아이템의 수를 조회하는 메서드 호출
            int itemCount = GetItemCountInWorld("Upgrademodule");
            MyAPIGateway.Utilities.ShowMessage("ItemCountHandler", $"Total Upgrademodule items in the world: {itemCount}");
            _initialized = true;
        }
    }

    // 특정 SubtypeId를 가진 아이템의 수를 조회하는 메서드
    private int GetItemCountInWorld(string subtypeId)
    {
        VRage.MyFixedPoint totalCount = 0; // MyFixedPoint 사용

        // 모든 엔티티를 순회
        var entities = new HashSet<IMyEntity>(); // HashSet 사용
        MyAPIGateway.Entities.GetEntities(entities);

        foreach (var entity in entities)
        {
            // 엔티티가 MyFloatingObject인지 확인
            var floatingObject = entity as MyFloatingObject;
            if (floatingObject != null)
            {
                var content = floatingObject.Item.Content;

                // 아이템의 SubtypeId를 비교하여 일치하는지 확인
                if (content.GetId().SubtypeId == MyStringHash.GetOrCompute(subtypeId))
                {
                    totalCount += floatingObject.Item.Amount;
                }
            }

            // 엔티티가 IMyCubeGrid인지 확인
            var grid = entity as IMyCubeGrid;
            if (grid != null)
            {
                var blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks);

                foreach (var block in blocks)
                {
                    var fatBlock = block.FatBlock;
                    if (fatBlock != null)
                    {
                        var terminalBlock = fatBlock as IMyTerminalBlock;
                        if (terminalBlock != null)
                        {
                            // 각 블록이 인벤토리를 가지고 있는지 확인
                            int inventoryCount = terminalBlock.InventoryCount;
                            for (int i = 0; i < inventoryCount; i++)
                            {
                                var inventory = terminalBlock.GetInventory(i) as IMyInventory;
                                if (inventory != null)
                                {
                                    var items = inventory.GetItems();

                                    // 인벤토리 내의 각 아이템을 순회하여 SubtypeId가 일치하는 아이템 찾기
                                    foreach (var item in items)
                                    {
                                        var contentId = item.Content.GetId();
                                        if (contentId.SubtypeId == MyStringHash.GetOrCompute(subtypeId))
                                        {
                                            totalCount += item.Amount;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // 모든 플레이어의 인벤토리를 포함하도록 추가된 부분
        var players = new List<IMyPlayer>();
        MyAPIGateway.Players.GetPlayers(players);

        foreach (var player in players)
        {
            var character = player.Character;
            if (character != null)
            {
                int inventoryCount = character.InventoryCount;
                for (int i = 0; i < inventoryCount; i++)
                {
                    var inventory = character.GetInventory(i) as IMyInventory;
                    if (inventory != null)
                    {
                        var items = inventory.GetItems();

                        foreach (var item in items)
                        {
                            var contentId = item.Content.GetId();
                            if (contentId.SubtypeId == MyStringHash.GetOrCompute(subtypeId))
                            {
                                totalCount += item.Amount;
                            }
                        }
                    }
                }
            }
        }

        return totalCount.ToIntSafe(); // MyFixedPoint를 int로 변환하여 반환
    }
}
