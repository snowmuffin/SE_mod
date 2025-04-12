using System;
using System.Collections.Generic;
using System.Linq;
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
    public class Configuration
    {
        public Multiplier Multiplier;
        public Offset Offset;
    }

    public class Multiplier
    {
        public int Attack;
        public int Defence;
        public int Power;
        public int Speed;
    }

    public class Offset
    {
        public int Attack;
        public int Defence;
        public int Power;
        public int Speed;
    }

    public class UpgradecoreConfig
    {
        public static Configuration ConfigInstance { get; set; } = new Configuration
        {
            Multiplier = new Multiplier
            {
                Attack = 1,
                Defence = 1,
                Power = 1,
                Speed = 1
            },
            Offset = new Offset
            {
                Attack = 0,
                Defence = 0,
                Power = 0,
                Speed = 0
            }
        };

        public static void ApplyConfig(Configuration configResponse)
        {
            ConfigInstance.Multiplier.Attack = configResponse.Multiplier.Attack;
            ConfigInstance.Multiplier.Defence = configResponse.Multiplier.Defence;
            ConfigInstance.Multiplier.Power = configResponse.Multiplier.Power;
            ConfigInstance.Multiplier.Speed = configResponse.Multiplier.Speed;
            ConfigInstance.Offset.Attack = configResponse.Offset.Attack;
            ConfigInstance.Offset.Defence = configResponse.Offset.Defence;
            ConfigInstance.Offset.Power = configResponse.Offset.Power;
            ConfigInstance.Offset.Speed = configResponse.Offset.Speed;
        }
    }

    [Serializable]
    public class ConfigurationMessage
    {
        public ulong sender;
    }

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation)]
    class Upgradecore : MySessionComponentBase
    {
        bool m_init = false;
        IMyEntity Entity;
		MyObjectBuilder_SessionComponent m_objectBuilder;

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

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            m_objectBuilder = sessionComponent;
        }

        private void init()
        {
  
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(5856, UpgradeMessageHandler);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(5853, HandleConfigRequest);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(5854, HandleConfigResponse);

            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, HandleDamage);
            MyAPIGateway.Missiles.OnMissileCollided += missileCollisionHandler;

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

        HashSet<long> processedMissiles = new HashSet<long>();

        void missileCollisionHandler(IMyMissile missile)
        {
            try
            {
                if (processedMissiles.Contains(missile.EntityId))
                {
                    return;
                }
                processedMissiles.Add(missile.EntityId);

                IMyEntity attackerEntity = MyAPIGateway.Entities.GetEntityById(missile.LauncherId);
                IMyCubeGrid attackergrid = attackerEntity as IMyCubeGrid;
                IMyCubeBlock attackerblock = attackerEntity as IMyCubeBlock;

                if (attackergrid == null && attackerblock != null)
                {
                    attackergrid = attackerblock.CubeGrid;
                }

                if (attackergrid != null)
                {
                    if (!m_cachedGrids.ContainsKey(attackergrid.EntityId))
                    {
                        IMyGridTerminalSystem tsystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(attackergrid);
                        List<IMyTerminalBlock> cockpits = new List<IMyTerminalBlock>();
                        List<UpgradeLogic> UpgradeLogics = new List<UpgradeLogic>();

                        if (tsystem != null)
                        {
                            tsystem.GetBlocksOfType<IMyCockpit>(cockpits, Filter);

                            foreach (var cockpit in cockpits)
                            {
                                UpgradeLogics.Add(((IMyTerminalBlock)cockpit).GameLogic.GetAs<UpgradeLogic>());
                            }

                            m_cachedGrids.TryAdd(attackergrid.EntityId, UpgradeLogics);
                        }
                    }

                    // 공격자 업그레이드 정보 로드
                    if (m_cachedGrids.ContainsKey(attackergrid.EntityId))
                    {
                        List<UpgradeLogic> cachedattackerUpgradeLogics = m_cachedGrids[attackergrid.EntityId];
                        int maxAttackLevel = 0;

                        foreach (var upgradeLogic in cachedattackerUpgradeLogics)
                        {
                            if (upgradeLogic.m_AttackUpgradeLevel > maxAttackLevel)
                                maxAttackLevel = upgradeLogic.m_AttackUpgradeLevel;
                        }
                        missile.ExplosionDamage *= (float)Math.Pow(1 + 0.02, maxAttackLevel);
                    }
                }
            }
            catch (Exception e)
            {
                // 예외 처리
            }
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

            printLoadBalancer.Update();
            networkLoadBalancer.Update();
        }

        public override void UpdateAfterSimulation()
        {
            


        }

		private void UpgradeMessageHandler(ushort channel, byte[] message, ulong recipient, bool reliable)
		{
			long ID = BitConverter.ToInt64(message, 0);
			int value1 = BitConverter.ToInt32(message, 8);
            int value2 = BitConverter.ToInt32(message, 12);
            int value3 = BitConverter.ToInt32(message, 16);

			if(!MyAPIGateway.Multiplayer.IsServer)
			{


				foreach(var LogicKV in Upgradecore.Upgrades) 
				{
					if(ID == LogicKV.Key) 
					{
                        LogicKV.Value.m_PowerEfficiencyUpgradeLevel = value1;
                        LogicKV.Value.m_AttackUpgradeLevel = value2;
                        LogicKV.Value.m_DefenseUpgradeLevel = value3;
					}
				}
			}
		}

        void HandleDamage(object target, ref MyDamageInformation info)
        {
            IMySlimBlock slimBlock = target as IMySlimBlock;
            long attackerId = info.AttackerId;
            IMyEntity attackerEntity = null;
            IMyCubeGrid attackerGrid = null;
            float damageMultiplier = 1f;
            try
            {
                attackerEntity = MyAPIGateway.Entities.GetEntityById(attackerId);
                attackerGrid = attackerEntity as IMyCubeGrid;
                if (attackerGrid == null && attackerEntity is IMyCubeBlock)
                {
                    attackerGrid = ((IMyCubeBlock)attackerEntity).CubeGrid;
                }

                if (attackerGrid != null)
                {
                    if (!m_cachedGrids.ContainsKey(attackerGrid.EntityId))
                    {
                        IMyGridTerminalSystem tsystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(attackerGrid);
                        List<IMyTerminalBlock> cockpits = new List<IMyTerminalBlock>();
                        List<UpgradeLogic> UpgradeLogics = new List<UpgradeLogic>();

                        if (tsystem != null)
                        {
                            tsystem.GetBlocksOfType<IMyCockpit>(cockpits, Filter);

                            foreach (var cockpit in cockpits)
                            {
                                UpgradeLogics.Add(((IMyTerminalBlock)cockpit).GameLogic.GetAs<UpgradeLogic>());
                            }
                            m_cachedGrids.TryAdd(attackerGrid.EntityId, UpgradeLogics);
                        }
                    }

                    // 공격 레벨 계산 (최소값을 찾음)
                    if (m_cachedGrids.ContainsKey(attackerGrid.EntityId))
                    {
                        List<UpgradeLogic> cachedattackerUpgradeLogics = m_cachedGrids[attackerGrid.EntityId];

                        if (cachedattackerUpgradeLogics.Count > 0)
                        {
                            int minAttackLevel = cachedattackerUpgradeLogics[0].m_AttackUpgradeLevel;

                            foreach (var upgradeLogic in cachedattackerUpgradeLogics)
                            {
                                if (upgradeLogic.m_AttackUpgradeLevel < minAttackLevel)
                                {
                                    minAttackLevel = upgradeLogic.m_AttackUpgradeLevel;
                                }
                            }
                            damageMultiplier *= (float)Math.Pow(1 + 0.02, minAttackLevel);
                        }
                    }
                }

                // 방어 레벨 계산 (최소값을 찾음)
                if (!m_cachedGrids.ContainsKey(slimBlock.CubeGrid.EntityId))
                {
                    IMyGridTerminalSystem tsystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(slimBlock.CubeGrid as IMyCubeGrid);
                    List<IMyTerminalBlock> cockpits = new List<IMyTerminalBlock>();
                    List<UpgradeLogic> UpgradeLogics = new List<UpgradeLogic>();

                    if (tsystem != null)
                    {
                        tsystem.GetBlocksOfType<IMyCockpit>(cockpits, Filter);
                        foreach (var cockpit in cockpits)
                        {
                            UpgradeLogics.Add(((IMyTerminalBlock)cockpit).GameLogic.GetAs<UpgradeLogic>());
                        }
                        m_cachedGrids.TryAdd(slimBlock.CubeGrid.EntityId, UpgradeLogics);
                    }
                }

                if (m_cachedGrids.ContainsKey(slimBlock.CubeGrid.EntityId))
                {
                    List<UpgradeLogic> cachedUpgradeLogics = m_cachedGrids[slimBlock.CubeGrid.EntityId];

                    if (cachedUpgradeLogics.Count > 0)
                    {
                        int minDefenseLevel = cachedUpgradeLogics[0].m_DefenseUpgradeLevel;
                        foreach (var upgradeLogic in cachedUpgradeLogics)
                        {
                            if (upgradeLogic.m_DefenseUpgradeLevel < minDefenseLevel)
                            {
                                minDefenseLevel = upgradeLogic.m_DefenseUpgradeLevel;
                            }
                        }
                        damageMultiplier *= (float)Math.Pow(1 - 0.02, minDefenseLevel);
                    }
                }
                info.Amount *= damageMultiplier;

            }
            catch (Exception e)
            {
                // 예외 처리
            }
        }

        public void loadConfigFile()
        {
            // 서버 혹은 싱글플레이 시 실제 Config 로드
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

            MyAPIGateway.Multiplayer.SendMessageTo(5853, requestBytes, MyAPIGateway.Multiplayer.ServerId, true);
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

                    MyAPIGateway.Multiplayer.SendMessageTo(5854, responseBytes, sender, reliable);

   
                }
                catch (Exception ex)
                {
                    // 예외 처리
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
                    // 예외 처리
                }
            }
        }

        public static bool Filter(IMyTerminalBlock block) 
        {
            return block.CustomName.Contains("[Upgrade]");
        }
    }
}
