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
            if (slimBlock == null) return;

            long attackerId = info.AttackerId;
            IMyEntity attackerEntity = null;
            IMyCubeGrid attackerGrid = null;
            float damageMultiplier = 1f;

            try
            {
                DebugLog($"HandleDamage triggered. AttackerId: {attackerId}");

                attackerEntity = MyAPIGateway.Entities.GetEntityById(attackerId);
                if (attackerEntity is IMyCubeGrid)
                {
                    attackerGrid = attackerEntity as IMyCubeGrid;
                }
                else if (attackerEntity is IMyCubeBlock)
                {

                    attackerGrid = (attackerEntity as IMyCubeBlock).CubeGrid;
                }

                if (attackerGrid != null)
                {
                    List<UpgradeLogic> attackerUpgradeLogics;
                    if (!m_cachedGrids.TryGetValue(attackerGrid.EntityId, out attackerUpgradeLogics))
                    {
                        DebugLog("Attacker not found in cache, fetching data.");
                        DebugLog($"Attacker Grid found: {attackerGrid.DisplayName}");

                        IMyGridTerminalSystem tsystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(attackerGrid);
                        if (tsystem != null)
                        {
                            List<IMyCockpit> cockpits = new List<IMyCockpit>();
                            tsystem.GetBlocksOfType<IMyCockpit>(cockpits, Filter);

                            DebugLog($"Found {cockpits.Count} cockpits in attacker grid.");

                            attackerUpgradeLogics = new List<UpgradeLogic>();
                            foreach (var cockpit in cockpits)
                            {
                                var upgradeLogic = cockpit.GameLogic.GetAs<UpgradeLogic>();
                                if (upgradeLogic != null)
                                {
                                    attackerUpgradeLogics.Add(upgradeLogic);
                                }
                            }

                            m_cachedGrids[attackerGrid.EntityId] = attackerUpgradeLogics;
                            DebugLog("Attacker upgrade logic cached.");
                        }
                    }

                    if (attackerUpgradeLogics != null && attackerUpgradeLogics.Count > 0)
                    {
                        int maxAttackLevel = attackerUpgradeLogics.Max(u => u.m_AttackUpgradeLevel);
                        DebugLog($"Max attack level: {maxAttackLevel}");

                        damageMultiplier *= ComputeMultiplier(1.02f, maxAttackLevel);
                    }
                }

                // 방어 레벨 계산
                List<UpgradeLogic> defenderUpgradeLogics;
                if (!m_cachedGrids.TryGetValue(slimBlock.CubeGrid.EntityId, out defenderUpgradeLogics))
                {
                    DebugLog("Defense grid not found in cache, fetching data.");

                    IMyGridTerminalSystem tsystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(slimBlock.CubeGrid as IMyCubeGrid);
                    if (tsystem != null)
                    {
                        List<IMyCockpit> cockpits = new List<IMyCockpit>();
                        tsystem.GetBlocksOfType<IMyCockpit>(cockpits, Filter);

                        DebugLog($"Found {cockpits.Count} cockpits in defense grid.");

                        defenderUpgradeLogics = new List<UpgradeLogic>();
                        foreach (var cockpit in cockpits)
                        {
                            var upgradeLogic = cockpit.GameLogic.GetAs<UpgradeLogic>();
                            if (upgradeLogic != null)
                            {
                                defenderUpgradeLogics.Add(upgradeLogic);
                            }
                        }

                        m_cachedGrids[slimBlock.CubeGrid.EntityId] = defenderUpgradeLogics;
                        DebugLog("Defense upgrade logic cached.");
                    }
                }

                if (defenderUpgradeLogics == null || defenderUpgradeLogics.Count == 0)
                {
                    DebugLog("Defense upgrades not found in cache after attempted fetch.");
                    return; // 캐시에 정보가 없으면 더 이상 처리하지 않음
                }

                int maxDefenseLevel = defenderUpgradeLogics.Max(u => u.m_DefenseUpgradeLevel);
                DebugLog($"Max defense level: {maxDefenseLevel}");

                damageMultiplier *= ComputeMultiplier(0.98f, maxDefenseLevel);
                info.Amount *= damageMultiplier;

                DebugLog($"Final damage: {info.Amount}");
            }
            catch (Exception e)
            {
                // 예외 처리 (로깅 또는 HUD 알림)
                MyAPIGateway.Utilities.ShowMessage("ERROR", $"Exception in HandleDamage: {e.Message}");
            }
        }

        // 멀티플라이어 계산을 위한 헬퍼 메서드
        float ComputeMultiplier(float baseValue, int level)
        {
            float multiplier = 1f;
            for (int i = 0; i < level; i++)
            {
                multiplier *= baseValue;
            }
            return multiplier;
        }

        // 최적화된 DebugLog 메서드
        const bool DEBUG_MODE = false;

        void DebugLog(string message)
        {
            if (DEBUG_MODE)
            {
                MyAPIGateway.Utilities.ShowMessage("DEBUG", message);
            }
        }


		public static bool Filter(IMyTerminalBlock block) 
		{

			return (block.CustomName.Contains("[Upgrade]"));
		}

    }
}
