using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Common.Utils;
using VRageMath;
using VRage;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;
using VRage.Game.Entity.UseObject;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Voxels;
using System.Linq;
using System.Linq.Expressions;
namespace SEUpgrademodule
{ 

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Cockpit), false)]
    public class UpgradeLogic : MyGameLogicComponent
    {
        public int m_PowerEfficiencyUpgradeLevel = 0;
        public int m_AttackUpgradeLevel = 0;
        public int m_DefenseUpgradeLevel = 0;
        bool m_closed = false;
        MyObjectBuilder_EntityBase m_objectBuilder;
        bool m_init = false;
        UpgradeModuleSummary savemessage = new UpgradeModuleSummary();
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            m_objectBuilder = objectBuilder;
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            
            if (!MoreLoot.cockpits.ContainsKey(Entity.EntityId))
            {
                MoreLoot.cockpits.TryAdd(Entity.EntityId, this);
            }
        }
		public void UpdatePrintBalanced()
		{
			if (!m_closed && Entity.InScene)
			{
				Sandbox.ModAPI.IMyTerminalBlock terminalBlock = Entity as Sandbox.ModAPI.IMyTerminalBlock;
				terminalBlock.RefreshCustomInfo();
			}
		}
		public void UpdateNetworkBalanced()
		{
			if (!m_closed && Entity.InScene)
			{

				if (MyAPIGateway.Multiplayer.MultiplayerActive && MyAPIGateway.Multiplayer.IsServer)
				{
					byte[] message = new byte[20];
					byte[] messageID = BitConverter.GetBytes(Entity.EntityId);
					byte[] messageValue1 = BitConverter.GetBytes(m_PowerEfficiencyUpgradeLevel);
                    byte[] messageValue2 = BitConverter.GetBytes(m_AttackUpgradeLevel);
                    byte[] messageValue3 = BitConverter.GetBytes(m_DefenseUpgradeLevel);

					for (int i = 0; i < 8; i++) {
						message[i] = messageID[i];
					}

					for (int i = 0; i < 4; i++) {
						message[i + 8] = messageValue1[i];
					}
					for (int i = 0; i < 4; i++) {
						message[i + 12] = messageValue2[i];
					}
					for (int i = 0; i < 4; i++) {
						message[i + 16] = messageValue3[i];
					}
					MyAPIGateway.Multiplayer.SendMessageToOthers(5856, message, true);
					
				}

			}
		}
		public void UpdateBlockInfo(Sandbox.ModAPI.IMyTerminalBlock block, StringBuilder info)
		{
			try
			{
				if (block == null)
					return;

				if (info == null)
					return;

				info.Clear();

				info.AppendLine("");

				IMyGridTerminalSystem tsystem = null;

				if (block.CubeGrid != null)
					tsystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(block.CubeGrid);


			}
			catch (Exception e)
			{ }
		}
		public override void Close()
		{
			m_closed = true;

			Sandbox.ModAPI.IMyTerminalBlock terminalBlock = Entity as Sandbox.ModAPI.IMyTerminalBlock;

			terminalBlock.AppendingCustomInfo -= UpdateBlockInfo;

			if (Upgradecore.Upgrades.ContainsKey(Entity.EntityId)) {
				Upgradecore.Upgrades.Remove(Entity.EntityId);
			}
		}
        private void InitStorage()
        {
            if (Entity.Storage == null)
            {
                Entity.Storage = new MyModStorageComponent();
            }
        }

        private void LoadStorage()
        {
            // 저장소 로드
            if (!Entity.Storage.ContainsKey(UpgradeModuleSummary.StorageGuid))
                return;

            var data = Entity.Storage.GetValue(UpgradeModuleSummary.StorageGuid);
            try
            {
                var storagedata = MyAPIGateway.Utilities.SerializeFromBinary<UpgradeModuleSummary>(Convert.FromBase64String(data));
                m_PowerEfficiencyUpgradeLevel = storagedata.PowerEfficiencyUpgradeLevel;
                m_AttackUpgradeLevel = storagedata.AttackUpgradeLevel;
                m_DefenseUpgradeLevel = storagedata.DefenseUpgradeLevel;
                // 추가 데이터 로드 가능
            }
            catch (Exception e)
            {
                // 저장 데이터가 손상된 경우 복구
                SaveStorage();
            }
        }

        private void SaveStorage()
        {
            if (Entity.Storage == null)
                InitStorage();

            // 저장할 데이터 생성
            var storageData = new UpgradeModuleSummary
            {
                PowerEfficiencyUpgradeLevel = m_PowerEfficiencyUpgradeLevel,
                AttackUpgradeLevel = m_AttackUpgradeLevel, // 오타 수정
                DefenseUpgradeLevel = m_DefenseUpgradeLevel
            };

            var data = MyAPIGateway.Utilities.SerializeToBinary(storageData);
            Entity.Storage.SetValue(UpgradeModuleSummary.StorageGuid, Convert.ToBase64String(data));
        }


        public override void UpdateBeforeSimulation()
        {
            IMyCubeBlock cubeBlock = Entity as IMyCubeBlock;
            if (!m_init)
            {
                m_init = true;
            }

            if (cubeBlock == null)
                return;

            IMyCubeGrid grid = cubeBlock.CubeGrid;
            if (grid == null)
                return;

            IMyTerminalBlock tm =  Entity as IMyTerminalBlock;

            if (!tm.CustomName.Contains("[MainCockpit]")) 
                return;


            IMyInventory inventory = cubeBlock.GetInventory(0);
            if (inventory == null)
                return;

            List<VRage.Game.ModAPI.Ingame.MyInventoryItem> items = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
            inventory.GetItems(items); 
            m_PowerEfficiencyUpgradeLevel = 0;
            m_AttackUpgradeLevel = 0;
            m_DefenseUpgradeLevel = 0;
            foreach (var item in items)
            {
                // PowerEfficiencyUpgradeModule_Level 확인
                if (item.Type.SubtypeId.ToString().StartsWith("PowerEfficiencyUpgradeModule_Level"))
                {
                    string levelStr = item.Type.SubtypeId.ToString().Replace("PowerEfficiencyUpgradeModule_Level", "");
                    int level;
                    if (int.TryParse(levelStr, out level) && level > m_PowerEfficiencyUpgradeLevel)
                    {
                        m_PowerEfficiencyUpgradeLevel = level;

                        
                    }
                }
                // AttackUpgradeModule_Level 확인
                else if (item.Type.SubtypeId.ToString().StartsWith("AttackUpgradeModule_Level"))
                {
                    string levelStr = item.Type.SubtypeId.ToString().Replace("AttackUpgradeModule_Level", "");
                    int level;
                    if (int.TryParse(levelStr, out level) && level > m_AttackUpgradeLevel)
                    {
                        m_AttackUpgradeLevel = level;

                    }
                }
                // DefenseUpgradeModule_Level 확인
                else if (item.Type.SubtypeId.ToString().StartsWith("DefenseUpgradeModule_Level"))
                {
                    string levelStr = item.Type.SubtypeId.ToString().Replace("DefenseUpgradeModule_Level", "");
                    int level;
                    if (int.TryParse(levelStr, out level) && level > m_DefenseUpgradeLevel)
                    {
                        m_DefenseUpgradeLevel = level;

                    }

                }
            }
            IMyGridTerminalSystem tsystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid (grid);
            List<IMyThrust> thrusts = new List<IMyThrust>();
            float PowerMultiplier = (float)Math.Pow(1 - 0.02, m_PowerEfficiencyUpgradeLevel);
            if (tsystem != null) 
            {
                tsystem.GetBlocksOfType<IMyThrust>(thrusts);

                foreach(var thrust in thrusts)
                {
                    thrust.PowerConsumptionMultiplier = PowerMultiplier;
                }
                
            }
            savemessage.DefenseUpgradeLevel = m_DefenseUpgradeLevel;
            savemessage.AttackUpgradeLevel = m_AttackUpgradeLevel;
            savemessage.PowerEfficiencyUpgradeLevel = m_PowerEfficiencyUpgradeLevel;
        }
		public override void UpdateOnceBeforeFrame()
		{
			InitStorage();
			LoadStorage();
			SaveStorage();
		}
        private int ParseLevelFromSubtype(string subtype, string prefix)
        {
            int level = 0;
            if (subtype.StartsWith(prefix))
            {
                string levelStr = subtype.Substring(prefix.Length); // 접두사 제거 후 레벨 숫자 추출
                if (int.TryParse(levelStr, out level))
                {
                    return level;
                }
            }
            return 0;
        }
    }
}
