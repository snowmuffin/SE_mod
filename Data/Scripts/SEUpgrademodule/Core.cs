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
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation)]
    class Upgradecore : MySessionComponentBase
    {
        bool m_init = false;
        IMyEntity Entity;
		MyObjectBuilder_SessionComponent m_objectBuilder;
        public static ConcurrentDictionary<long, UpgradeLogic> Upgrades = new ConcurrentDictionary<long, UpgradeLogic>();

        private ConcurrentDictionary<long, List<UpgradeLogic>> m_cachedGrids = new ConcurrentDictionary<long, List<UpgradeLogic>>();
        private PrintLoadBalancer printLoadBalancer = new PrintLoadBalancer();
        private NetworkLoadBalancer networkLoadBalancer = new NetworkLoadBalancer();
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            m_objectBuilder = sessionComponent;
        }

        private void init()
        {

            // 데미지 핸들러 등록
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(1, HandleDamage);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(5856, UpgradeMessageHandler);
            MyAPIGateway.Missiles.OnMissileCollided += missileCollisionHandler;
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
                // 미사일이 이미 처리된 경우, 더 이상 진행하지 않음
                if (processedMissiles.Contains(missile.EntityId))
                {
                    return;
                }

                // 미사일을 처음 처리한 경우 HashSet에 추가
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
            // 초기화되지 않았다면 초기화 진행
            if (!m_init)
            {
                init();
            }
            else
            {

                // 캐시된 그리드를 클리어
                m_cachedGrids.Clear();
            }


            // 로드 밸런서 업데이트
            printLoadBalancer.Update();
            networkLoadBalancer.Update();

        }
        public override void UpdateAfterSimulation()
        {
        }
        // UpgradeLogic 인스턴스 등록

		private void UpgradeMessageHandler(ushort channel, byte[] message, ulong recipient, bool reliable)
		{
			long ID = BitConverter.ToInt64(message, 0);
			int value1 = BitConverter.ToInt32(message, 8);
            int value2 = BitConverter.ToInt32(message, 12);
            int value3 = BitConverter.ToInt32(message, 16);
            
			if(!MyAPIGateway.Multiplayer.IsServer)
			{
				//Log.writeLine("<ShieldGeneratorGameLogic> Sync received.");
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
                // 공격자 정보를 가져오기
                attackerEntity = MyAPIGateway.Entities.GetEntityById(attackerId);
                attackerGrid = attackerEntity as IMyCubeGrid;
                if (attackerGrid == null && attackerEntity is IMyCubeBlock)
                {
                    attackerGrid = ((IMyCubeBlock)attackerEntity).CubeGrid;
                }

                // 공격자의 캐시 확인 및 로직 적용
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
                        
                        // 리스트에 값이 있을 때만 최소값 계산
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

                // 방어 레벨 계산 (최소값을 찾음)
                if (m_cachedGrids.ContainsKey(slimBlock.CubeGrid.EntityId))
                {
                    List<UpgradeLogic> cachedUpgradeLogics = m_cachedGrids[slimBlock.CubeGrid.EntityId];

                    // 리스트에 값이 있을 때만 최소값 계산
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
                // 예외 처리 (로깅 또는 HUD 알림)
            }
        }

                

		public static bool Filter(IMyTerminalBlock block) 
		{

			return (block.CustomName.Contains("[Upgrade]"));
		}

    }
}
