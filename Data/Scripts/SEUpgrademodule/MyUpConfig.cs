using System;
using System.Collections.Generic;
using ProtoBuf;
using System.Xml.Serialization;
using VRageMath;
using VRage.Game;
using System.Text;

namespace SEUpgrademodule
{
    [ProtoContract]
    [Serializable]
    public class MyUpConfig
    {
        [ProtoMember(1)]
        public Item SmallGridBasic;
        [ProtoMember(2)]
        public Item LargeGridBasic;
        [ProtoMember(3)]
        public Item SmallGridAdvanced;
        [ProtoMember(4)]
        public Item LargeGridAdvanced;
        [ProtoMember(5)]
        public List<string> ExcludeGrids;
        [ProtoMember(6)]
        public Boolean DisableGrindSubgridDamage = true;

    }

    [ProtoContract]
    [Serializable]
    public class Item
    {
        [XmlAttribute]
        public float Chance;
        [XmlAttribute]
        public int MinAmount;
        [XmlAttribute]
        public int MaxAmount;
    }
}