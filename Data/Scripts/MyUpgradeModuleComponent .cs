using Sandbox.Definitions;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRage.Game.Definitions;
using VRage.Game.Components;
using Sandbox.Game.World;
using Sandbox.Game.Entities;
using VRage.ModAPI;

[MyObjectBuilderDefinition]
public class MyObjectBuilder_CustomItemDefinition : MyObjectBuilder_PhysicalItemDefinition
{
    // 필드 정의
    public string CustomData = "Default custom data";
}

[MyDefinitionType(typeof(MyObjectBuilder_CustomItemDefinition))]
public class MyCustomItemDefinition : MyPhysicalItemDefinition
{
    public string CustomData { get; set; } = "Default custom data";

    protected override void Init(MyObjectBuilder_DefinitionBase builder)
    {
        base.Init(builder);
        MyAPIGateway.Utilities.ShowMessage("CustomItemDisplayNameChanger", "Session Init Started");
        MyLog.Default.WriteLine("CustomItemDisplayNameChanger: Session Init Started");

        var customItemBuilder = builder as MyObjectBuilder_CustomItemDefinition;
        if (customItemBuilder != null)
        {
            CustomData = customItemBuilder.CustomData;
        }
    }
}

[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
public class ItemSpawnHandler : MySessionComponentBase
{
    private bool _initialized = false;

    public override void UpdateAfterSimulation()
    {
        if (!_initialized)
        {
            // 한 번만 초기화 (세션 시작 시)
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdded;
            _initialized = true;
            MyAPIGateway.Utilities.ShowMessage("ItemSpawnHandler", "Item spawn handler initialized");
        }
    }

    protected override void UnloadData()
    {
        // 세션 종료 시 이벤트 제거
        MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdded;
    }

    private void OnEntityAdded(IMyEntity entity)
    {
        // 엔티티가 월드에 추가될 때 실행됨
        var floatingObject = entity as MyFloatingObject;
        if (floatingObject != null)
        {
            var content = floatingObject.Item.Content;
            var definitionId = content.GetId(); // 아이템의 DefinitionId 가져오기
            var definition = MyDefinitionManager.Static.GetPhysicalItemDefinition(definitionId);

            if (definition != null)
            {
                MyAPIGateway.Utilities.ShowMessage("ItemSpawnHandler", $"Entity added: {definition.DisplayNameText}");

                // 추가된 엔티티가 커스텀 아이템인지 확인
                if (definitionId.TypeId == typeof(MyObjectBuilder_CustomItemDefinition))
                {
                    MyAPIGateway.Utilities.ShowMessage("ItemSpawnHandler", $"Custom item spawned: {definition.DisplayNameText}");
                }
            }
        }
    }
}
