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

        /// <summary>Max cargo containers to roll loot for per prefab NPC spawn (default 7).</summary>
        [ProtoMember(9)]
        public int PrefabLootMaxCargoContainers;

        /// <summary>How many cockpit blocks to try for cockpit loot per spawn (default 1).</summary>
        [ProtoMember(10)]
        public int PrefabLootMaxCockpitAttempts;

        /// <summary>Max speed (m/s) reachable at SpeedModule level 10 (default 200).</summary>
        [ProtoMember(11)]
        public float SpeedModuleMaxSpeed = 200f;
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