using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.Game;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using ProtoBuf;
using System.ComponentModel;
namespace SE_UpgradeModuleMod
{
    public class MyModStorageComponent
    {
        public static readonly Guid StorageGuid = new Guid("37S8C85H-ERTC-4W6T-69UF-B432BAB5BGDB");
        
        [ProtoMember(1)]
        public Guid entity_id { get; set; }
        public MyModStorageComponent()
        {
        }

        // Serialize 및 Deserialize 필요 시 오버라이드
        public override void Serialize()
        {
            base.Serialize();
            // Serialize 로직 추가 (필요 시)
        }

        public override void Deserialize()
        {
            base.Deserialize();
            // Deserialize 로직 추가 (필요 시)
        }
    }

}
