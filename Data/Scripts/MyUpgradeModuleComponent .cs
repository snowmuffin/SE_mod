using Sandbox.Definitions;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRage.Game.Definitions;
using VRage.Game.Components;
using Sandbox.Game.World;


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
