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
        public static Configuration config = new Configuration ();
        public int m_PowerEfficiencyUpgradeLevel = 0;
        public int m_AttackUpgradeLevel = 0;
        public int m_DefenseUpgradeLevel = 0;
        public int m_SpeedModuleLevel = 0;
        public int m_FortressModuleLevel = 0;
        public int m_BerserkerModuleLevel = 0;
        public int updateCounter=0;
        bool m_closed = false;
        MyObjectBuilder_EntityBase m_objectBuilder;
        bool m_init = false;
        UpgradeModuleSummary savemessage = new UpgradeModuleSummary();

        // ■■■ 디버그 메시지 출력 주기를 제어하기 위한 프레임 카운터 (예: 120프레임마다 1회)
        private int debugCounter = 0;
        private const int DEBUG_INTERVAL = 120; // 대략 2초(60 FPS 기준)마다 한번만 출력

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            m_objectBuilder = objectBuilder;

            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            Sandbox.ModAPI.IMyTerminalBlock terminalBlock = Entity as Sandbox.ModAPI.IMyTerminalBlock;
            terminalBlock.AppendingCustomInfo += UpdateBlockInfo;
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

                    for (int i = 0; i < 8; i++)
                    {
                        message[i] = messageID[i];
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        message[i + 8] = messageValue1[i];
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        message[i + 12] = messageValue2[i];
                    }
                    for (int i = 0; i < 4; i++)
                    {
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
                if (block == null) return;
                if (info == null)  return;

                info.Clear();
                info.AppendLine("");
                info.AppendLine($"Attack_Level:{m_AttackUpgradeLevel}");
                info.AppendLine($"Defense_Level:{m_DefenseUpgradeLevel}");
                info.AppendLine($"PowerEfficiency_Level:{m_PowerEfficiencyUpgradeLevel}");
                info.AppendLine($"Speed_Level:{m_SpeedModuleLevel}");

                IMyGridTerminalSystem tsystem = null;
                if (block.CubeGrid != null)
                    tsystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(block.CubeGrid);
            }
            catch (Exception e)
            {
                // 예외 무시 또는 로깅
            }
        }

        public override void Close()
        {
            m_closed = true;
            Sandbox.ModAPI.IMyTerminalBlock terminalBlock = Entity as Sandbox.ModAPI.IMyTerminalBlock;
            terminalBlock.AppendingCustomInfo -= UpdateBlockInfo;

            if (Upgradecore.Upgrades.ContainsKey(Entity.EntityId))
            {
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
            if (!Entity.Storage.ContainsKey(UpgradeModuleSummary.StorageGuid))
                return;

            var data = Entity.Storage.GetValue(UpgradeModuleSummary.StorageGuid);
            try
            {
                var storagedata = MyAPIGateway.Utilities.SerializeFromBinary<UpgradeModuleSummary>(
                    Convert.FromBase64String(data)
                );
                m_PowerEfficiencyUpgradeLevel = storagedata.PowerEfficiencyUpgradeLevel;
                m_AttackUpgradeLevel = storagedata.AttackUpgradeLevel;
                m_DefenseUpgradeLevel = storagedata.DefenseUpgradeLevel;
                // 추가 데이터 로드 가능
            }
            catch (Exception e)
            {
                SaveStorage();
            }
        }

        private void SaveStorage()
        {
            if (Entity.Storage == null)
                InitStorage();

            var storageData = new UpgradeModuleSummary
            {
                PowerEfficiencyUpgradeLevel = m_PowerEfficiencyUpgradeLevel,
                AttackUpgradeLevel = m_AttackUpgradeLevel,
                DefenseUpgradeLevel = m_DefenseUpgradeLevel
            };

            var data = MyAPIGateway.Utilities.SerializeToBinary(storageData);
            Entity.Storage.SetValue(UpgradeModuleSummary.StorageGuid, Convert.ToBase64String(data));
        }

        public static bool IsOwnedByNPC(long ownerId)
        {
            if (ownerId == 0) return false;

            var session = MyAPIGateway.Session;
            if (session?.Factions == null) return false;

            var faction = session.Factions.TryGetPlayerFaction(ownerId);
            if (faction == null) return false;

            return faction.IsEveryoneNpc();
        }

        public override void UpdateBeforeSimulation()
        {



            IMyCubeBlock cubeBlock = Entity as IMyCubeBlock;
            if (!m_init)
            {
                m_init = true;
            }
            updateCounter++;
            if (updateCounter < 1800)
            {
                return; // 아직 간격이 도달하지 않았으므로 반환
            }
            updateCounter = 0;
            if (cubeBlock == null || cubeBlock.CubeGrid == null)
                return;

            IMyTerminalBlock terminalBlock = Entity as IMyTerminalBlock;
            if (terminalBlock == null || !terminalBlock.CustomName.Contains("[Upgrade]"))
                return;

            IMyInventory inventory = cubeBlock.GetInventory(0);
            if (inventory == null)
                return;

            ResetUpgradeLevels();

            // ■■■ 인벤토리 아이템 추출
            List<VRage.Game.ModAPI.Ingame.MyInventoryItem> items = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
            inventory.GetItems(items);

            foreach (var item in items)
            {
                UpdateUpgradeLevel(item, "PowerEfficiencyUpgradeModule_Level", ref m_PowerEfficiencyUpgradeLevel);
                UpdateUpgradeLevel(item, "AttackUpgradeModule_Level", ref m_AttackUpgradeLevel);
                UpdateUpgradeLevel(item, "DefenseUpgradeModule_Level", ref m_DefenseUpgradeLevel);
                UpdateUpgradeLevel(item, "SpeedModule_Level", ref m_SpeedModuleLevel);
                UpdateUpgradeLevel(item, "BerserkerModule_Level", ref m_BerserkerModuleLevel);
                UpdateUpgradeLevel(item, "FortressModule_Level", ref m_FortressModuleLevel);
            }

            // ■■■ 종합 레벨 계산
            m_PowerEfficiencyUpgradeLevel -= m_SpeedModuleLevel + m_BerserkerModuleLevel + m_FortressModuleLevel;
            m_AttackUpgradeLevel += m_BerserkerModuleLevel;
            m_DefenseUpgradeLevel += m_FortressModuleLevel - m_BerserkerModuleLevel;
            m_SpeedModuleLevel -= m_FortressModuleLevel;

            // ■■■ NPC 보정 적용
            if (cubeBlock != null && IsOwnedByNPC(cubeBlock.OwnerId))
            {
                int beforePower = m_PowerEfficiencyUpgradeLevel;
                int beforeAttack = m_AttackUpgradeLevel;
                int beforeDefense = m_DefenseUpgradeLevel;
                int beforeSpeed = m_SpeedModuleLevel;

                m_PowerEfficiencyUpgradeLevel =
                    (m_PowerEfficiencyUpgradeLevel + Upgradecore.NpcOffsetPower) * Upgradecore.NpcMultiplierPower;
                m_AttackUpgradeLevel =
                    (m_AttackUpgradeLevel + Upgradecore.NpcOffsetAttack) * Upgradecore.NpcMultiplierAttack;
                m_DefenseUpgradeLevel =
                    (m_DefenseUpgradeLevel + Upgradecore.NpcOffsetDefence) * Upgradecore.NpcMultiplierDefence;
                m_SpeedModuleLevel =
                    (m_SpeedModuleLevel + Upgradecore.NpcOffsetSpeed) * Upgradecore.NpcMultiplierSpeed;


            }

            // ■■■ 나머지 로직
            savemessage.DefenseUpgradeLevel = m_DefenseUpgradeLevel;
            savemessage.AttackUpgradeLevel = m_AttackUpgradeLevel;
            savemessage.PowerEfficiencyUpgradeLevel = m_PowerEfficiencyUpgradeLevel;
            ApplyThrustPowerMultiplier(cubeBlock.CubeGrid, m_PowerEfficiencyUpgradeLevel, m_SpeedModuleLevel);

            terminalBlock.RefreshCustomInfo();
        }

        private void ResetUpgradeLevels()
        {
            m_PowerEfficiencyUpgradeLevel = 0;
            m_AttackUpgradeLevel = 0;
            m_DefenseUpgradeLevel = 0;
            m_SpeedModuleLevel = 0;
            m_FortressModuleLevel = 0;
            m_BerserkerModuleLevel = 0;
        }

        private void UpdateUpgradeLevel(VRage.Game.ModAPI.Ingame.MyInventoryItem item, string prefix, ref int upgradeLevel)
        {
            if (item.Type.SubtypeId.ToString().StartsWith(prefix))
            {
                string levelStr = item.Type.SubtypeId.ToString().Replace(prefix, "");

                int level;
                if (int.TryParse(levelStr, out level) && level > upgradeLevel)
                {
                    upgradeLevel = level;
                }
            }
        }

        private void ApplyThrustPowerMultiplier(IMyCubeGrid grid, int powerEfficiencyLevel, int SpeedModuleLevel)
        {
            IMyGridTerminalSystem tsystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
            if (tsystem == null)
                return;

            List<IMyThrust> thrusts = new List<IMyThrust>();
            tsystem.GetBlocksOfType(thrusts);

            float powerMultiplier = (float)Math.Pow(1 - 0.02, powerEfficiencyLevel);
            foreach (var thrust in thrusts)
            {
                thrust.ThrustMultiplier = (float)Math.Pow(1.15, SpeedModuleLevel);
                thrust.PowerConsumptionMultiplier = powerMultiplier;
            }
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
                string levelStr = subtype.Substring(prefix.Length);
                if (int.TryParse(levelStr, out level))
                {
                    return level;
                }
            }
            return 0;
        }
    }
}
