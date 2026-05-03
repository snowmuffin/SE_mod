using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRageMath;
using VRage;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage.Utils;
using VRage.Game.Components;
using VRage.Game;
using VRage.Game.ModAPI;
using System.Collections.Concurrent;

namespace SEUpgrademodule
{
    [Serializable]
    public class ConfigurationMessage
    {
        public ulong sender;
    }

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    class Upgradecore : MySessionComponentBase
    {
        bool m_init = false;

        public static ConcurrentDictionary<long, UpgradeLogic> Upgrades = new ConcurrentDictionary<long, UpgradeLogic>();

        public static int NpcMultiplierAttack = 1;
        public static int NpcMultiplierDefence = 1;
        public static int NpcMultiplierPower = 1;
        public static int NpcMultiplierSpeed = 1;
        public static int NpcOffsetAttack = 1;
        public static int NpcOffsetDefence = 1;
        public static int NpcOffsetPower = 1;
        public static int NpcOffsetSpeed = 1;

        private ConcurrentDictionary<long, List<UpgradeLogic>> m_cachedGrids = new ConcurrentDictionary<long, List<UpgradeLogic>>();
        private PrintLoadBalancer printLoadBalancer = new PrintLoadBalancer();
        private NetworkLoadBalancer networkLoadBalancer = new NetworkLoadBalancer();

        private void init()
        {
  
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(UpgradeSessionConstants.ChannelUpgradeSync, UpgradeMessageHandler);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(UpgradeSessionConstants.ChannelConfigRequest, HandleConfigRequest);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(UpgradeSessionConstants.ChannelConfigResponse, HandleConfigResponse);

            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, HandleDamage);

            loadConfigFile();

            foreach (var kv in Upgradecore.Upgrades)
            {
                UpgradeLogic basec = null;
                kv.Value.Entity.Components.TryGet<UpgradeLogic>(out basec);
                if (basec == null)
                {
                    kv.Value.Entity.Components.Add<UpgradeLogic>(kv.Value);
                    kv.Value.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                    kv.Value.OnAddedToScene();
                }
            }
            m_init = true;
        }

        public override void UpdateBeforeSimulation()
        {
            // 모드가 로드되고 한 번만 초기화
            if (!m_init)
            {
                init();
            }
            else
            {
                m_cachedGrids.Clear();
            }

            ApplyPerGridMaxSpeed();

            printLoadBalancer.Update();
            networkLoadBalancer.Update();
        }

        private void ApplyPerGridMaxSpeed()
        {
            var gridSpeedLevels = new Dictionary<long, int>();
            foreach (var kv in Upgradecore.Upgrades)
            {
                var logic = kv.Value;
                if (logic?.Entity == null || !logic.Entity.InScene) continue;
                var block = logic.Entity as IMyCubeBlock;
                if (block?.CubeGrid == null) continue;
                long gridId = block.CubeGrid.EntityId;
                int current;
                if (!gridSpeedLevels.TryGetValue(gridId, out current) || logic.m_SpeedModuleLevel > current)
                    gridSpeedLevels[gridId] = logic.m_SpeedModuleLevel;
            }

            float globalMax = 100f * MyAPIGateway.Session.SessionSettings.SpeedMultiplier;

            foreach (var kv in gridSpeedLevels)
            {
                if (kv.Value <= 0) continue;
                IMyEntity entity = MyAPIGateway.Entities.GetEntityById(kv.Key);
                IMyCubeGrid grid = entity as IMyCubeGrid;
                if (grid?.Physics == null || grid.Physics.IsStatic) continue;
                var physics = grid.Physics as MyPhysicsComponentBase;
                if (physics == null) continue;
                float t = Math.Min((float)kv.Value / UpgradeLogicConstants.SpeedModuleMaxLevel, 1f);
                float maxSpeed = 100f + (globalMax - 100f) * t;
                Vector3 vel = physics.LinearVelocity;
                float speed = vel.Length();
                if (speed > maxSpeed)
                    physics.LinearVelocity = vel * (maxSpeed / speed);
            }
        }

		private void UpgradeMessageHandler(ushort channel, byte[] message, ulong recipient, bool reliable)
		{
            if (message == null || message.Length < UpgradeSessionConstants.UpgradeSyncMessageByteLength)
                return;
			long ID = BitConverter.ToInt64(message, 0);
			int value1 = BitConverter.ToInt32(message, 8);
            int value2 = BitConverter.ToInt32(message, 12);
            int value3 = BitConverter.ToInt32(message, 16);
            int value4 = BitConverter.ToInt32(message, 20);

			if(!MyAPIGateway.Multiplayer.IsServer)
			{


				foreach(var LogicKV in Upgradecore.Upgrades)
				{
					if(ID == LogicKV.Key)
					{
                        LogicKV.Value.m_PowerEfficiencyUpgradeLevel = value1;
                        LogicKV.Value.m_AttackUpgradeLevel = value2;
                        LogicKV.Value.m_DefenseUpgradeLevel = value3;
                        LogicKV.Value.m_SpeedModuleLevel = value4;
					}
				}
			}
		}

        private List<UpgradeLogic> GetGridUpgradeLogics(IMyCubeGrid grid)
        {
            List<UpgradeLogic> logics;
            if (m_cachedGrids.TryGetValue(grid.EntityId, out logics))
                return logics;

            logics = new List<UpgradeLogic>();
            IMyGridTerminalSystem tsystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
            if (tsystem != null)
            {
                List<IMyTerminalBlock> cockpits = new List<IMyTerminalBlock>();
                tsystem.GetBlocksOfType<IMyCockpit>(cockpits, Filter);
                foreach (var cockpit in cockpits)
                {
                    UpgradeLogic logic = ((IMyTerminalBlock)cockpit).GameLogic.GetAs<UpgradeLogic>();
                    if (logic != null)
                        logics.Add(logic);
                }
            }
            m_cachedGrids.TryAdd(grid.EntityId, logics);
            return logics;
        }

        void HandleDamage(object target, ref MyDamageInformation info)
        {
            IMySlimBlock slimBlock = target as IMySlimBlock;
            if (slimBlock == null)
                return;

            try
            {
                float damageMultiplier = 1f;

                IMyEntity attackerEntity = MyAPIGateway.Entities.GetEntityById(info.AttackerId);
                IMyCubeGrid attackerGrid = attackerEntity as IMyCubeGrid;
                if (attackerGrid == null && attackerEntity is IMyCubeBlock)
                    attackerGrid = ((IMyCubeBlock)attackerEntity).CubeGrid;

                if (attackerGrid != null)
                {
                    List<UpgradeLogic> attackerLogics = GetGridUpgradeLogics(attackerGrid);
                    if (attackerLogics.Count > 0)
                    {
                        int minAttackLevel = attackerLogics[0].m_AttackUpgradeLevel;
                        foreach (var logic in attackerLogics)
                            if (logic.m_AttackUpgradeLevel < minAttackLevel)
                                minAttackLevel = logic.m_AttackUpgradeLevel;
                        damageMultiplier *= (float)Math.Pow(1.02, minAttackLevel);
                    }
                }

                List<UpgradeLogic> defenderLogics = GetGridUpgradeLogics(slimBlock.CubeGrid);
                if (defenderLogics.Count > 0)
                {
                    int minDefenseLevel = defenderLogics[0].m_DefenseUpgradeLevel;
                    foreach (var logic in defenderLogics)
                        if (logic.m_DefenseUpgradeLevel < minDefenseLevel)
                            minDefenseLevel = logic.m_DefenseUpgradeLevel;
                    damageMultiplier *= (float)Math.Pow(0.98, minDefenseLevel);
                }

                info.Amount *= damageMultiplier;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"Exception in HandleDamage: {e.Message}");
            }
        }

        public void loadConfigFile()
        {
            if (MyAPIGateway.Multiplayer.IsServer || !MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                Config.Load();
                NpcMultiplierAttack = Config.Instance.NpcMultiplier.Attack;
                NpcMultiplierDefence = Config.Instance.NpcMultiplier.Defence;
                NpcMultiplierPower = Config.Instance.NpcMultiplier.Power;
                NpcMultiplierSpeed = Config.Instance.NpcMultiplier.Speed;
                NpcOffsetAttack = Config.Instance.NpcOffset.Attack;
                NpcOffsetDefence = Config.Instance.NpcOffset.Defence;
                NpcOffsetPower = Config.Instance.NpcOffset.Power;
                NpcOffsetSpeed = Config.Instance.NpcOffset.Speed;


            }
            else
            {
                // 클라이언트는 서버에 요청
                RequestConfigFromServer();
            }
        }

        private void RequestConfigFromServer()
        {
            var configRequest = new ConfigurationMessage
            {
                sender = MyAPIGateway.Multiplayer.MyId
            };

            string requestXml = MyAPIGateway.Utilities.SerializeToXML(configRequest);
            byte[] requestBytes = Encoding.Unicode.GetBytes(requestXml);

            MyAPIGateway.Multiplayer.SendMessageTo(UpgradeSessionConstants.ChannelConfigRequest, requestBytes, MyAPIGateway.Multiplayer.ServerId, true);
        }

        private void HandleConfigRequest(ushort channel, byte[] message, ulong sender, bool reliable)
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                try
                {
                    string requestXml = Encoding.Unicode.GetString(message);
                    ConfigurationMessage configRequest = MyAPIGateway.Utilities.SerializeFromXML<ConfigurationMessage>(requestXml);

                    var configResponse = new MyUpConfig
                    {
                        NpcMultiplier = Config.Instance.NpcMultiplier,
                        NpcOffset = Config.Instance.NpcOffset
                    };

                    string responseXml = MyAPIGateway.Utilities.SerializeToXML(configResponse);
                    byte[] responseBytes = Encoding.Unicode.GetBytes(responseXml);

                    MyAPIGateway.Multiplayer.SendMessageTo(UpgradeSessionConstants.ChannelConfigResponse, responseBytes, sender, reliable);

   
                }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLine("SEUpgrademodule HandleConfigRequest: " + ex.Message);
                }
            }
        }

        private void HandleConfigResponse(ushort channel, byte[] message, ulong sender, bool reliable)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
            {
                try
                {
                    string responseXml = Encoding.Unicode.GetString(message);
                    MyUpConfig configResponse = MyAPIGateway.Utilities.SerializeFromXML<MyUpConfig>(responseXml);

                    if (configResponse != null)
                    {
                        NpcMultiplierAttack = configResponse.NpcMultiplier.Attack;
                        NpcMultiplierDefence = configResponse.NpcMultiplier.Defence;
                        NpcMultiplierPower = configResponse.NpcMultiplier.Power;
                        NpcMultiplierSpeed = configResponse.NpcMultiplier.Speed;
                        NpcOffsetAttack = configResponse.NpcOffset.Attack;
                        NpcOffsetDefence = configResponse.NpcOffset.Defence;
                        NpcOffsetPower = configResponse.NpcOffset.Power;
                        NpcOffsetSpeed = configResponse.NpcOffset.Speed;

                    }
                }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLine("SEUpgrademodule HandleConfigResponse: " + ex.Message);
                }
            }
        }

        public static bool Filter(IMyTerminalBlock block) 
        {
            return block.CustomName.Contains("[Upgrade]");
        }
    }
}
