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
        public NpcMultiplier NpcMultiplier;
        [ProtoMember(6)]
        public NpcOffset NpcOffset;
        [ProtoMember(7)]
        public List<string> ExcludeGrids;
        [ProtoMember(8)]
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
    public class NpcMultiplier
    {
        [XmlAttribute]
        public int Attack;
        [XmlAttribute]
        public int Defence;
        [XmlAttribute]
        public int Power;
        [XmlAttribute]
        public int Speed;
    }
    public class NpcOffset
    {
        [XmlAttribute]
        public int Attack;
        [XmlAttribute]
        public int Defence;
        [XmlAttribute]
        public int Power;
        [XmlAttribute]
        public int Speed;
    }
}