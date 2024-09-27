using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.Game; // MyVisualScriptLogicProvider를 위해 추가
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Definitions;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace SE_UpgradeModuleMod
{
    public static class ModConstants
    {
        public const ushort ModMessageId = 12345; // 임의의 고유 ID를 설정하세요
    }

    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CustomItem : MyObjectBuilder_PhysicalObject
    {
        // 아이템 인스턴스에 저장할 필드 정의
        public string UniqueId = ""; // 아이템 고유 ID
    }

    [MyDefinitionType(typeof(MyObjectBuilder_CustomItemDefinition))]
    public class MyCustomItemDefinition : MyPhysicalItemDefinition
    {
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            // 아이템 정의 초기화
            var customBuilder = builder as MyObjectBuilder_CustomItemDefinition;
            if (customBuilder != null)
            {
                // 필요한 경우 정의에서 데이터를 가져옵니다.
            }
        }
    }

    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CustomItemDefinition : MyObjectBuilder_PhysicalItemDefinition
    {
        // 아이템 정의에 저장할 필드 정의
    }

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ItemCountHandler : MySessionComponentBase
    {
        private bool _initialized = false;
        private HashSet<string> existingUniqueIds = new HashSet<string>(); // 기존 UniqueId를 저장하는 집합
        private System.IO.TextWriter _logFile;

        public override void LoadData()
        {
            _logFile = MyAPIGateway.Utilities.WriteFileInLocalStorage("ItemCountHandler.log", typeof(ItemCountHandler));
            Log("LoadData called.");

            // 메시지 핸들러 등록
            MyAPIGateway.Utilities.RegisterMessageHandler(ModConstants.ModMessageId, ReceivedMessageHandler);

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                Log("Server detected. Registering OnEntityAdded event.");
                // 엔티티 추가 이벤트 등록
                MyAPIGateway.Entities.OnEntityAdd += OnEntityAdded;
            }
        }

        protected override void UnloadData()
        {
            Log("UnloadData called.");

            // 메시지 핸들러 해제
            MyAPIGateway.Utilities.UnregisterMessageHandler(ModConstants.ModMessageId, ReceivedMessageHandler);

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                Log("Server detected. Unregistering OnEntityAdded event.");
                // 엔티티 추가 이벤트 해제
                MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdded;
            }

            _logFile?.Flush();
            _logFile?.Close();
            _logFile = null;
        }

        public override void UpdateAfterSimulation()
        {
            if (!_initialized)
            {
                Log("First UpdateAfterSimulation called.");

                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    Log("Server detected. Collecting UniqueIds.");
                    // 아이템의 UniqueId를 수집하고 클라이언트에 전송
                    GetItemUniqueIdsInWorld("Upgrademodule");
                }

                _initialized = true;
                Log("Initialization complete.");
            }
        }

        // 로그 작성 메서드
        private void Log(string message)
        {
            _logFile?.WriteLine($"{DateTime.Now}: {message}");
            _logFile?.Flush();
        }

        // 엔티티가 추가될 때 호출되는 메서드
        private void OnEntityAdded(IMyEntity entity)
        {
            Log($"OnEntityAdded called for entity {entity.DisplayName}.");

            if (!MyAPIGateway.Multiplayer.IsServer)
            {
                Log("Not a server, exiting OnEntityAdded.");
                return;
            }

            // 새로운 아이템이 생성되었는지 확인
            var floatingObject = entity as MyFloatingObject;
            if (floatingObject != null)
            {
                Log($"Floating object detected: {floatingObject.Name}.");

                var content = floatingObject.Item.Content;

                // 아이템의 SubtypeId를 비교하여 대상 아이템인지 확인
                if (content.GetId().SubtypeId == MyStringHash.GetOrCompute("Upgrademodule"))
                {
                    Log("Upgrademodule item detected.");

                    // 아이템의 UniqueId를 확인
                    var customItem = content as MyObjectBuilder_CustomItem;
                    if (customItem != null)
                    {
                        Log($"Custom item found with UniqueId: {customItem.UniqueId}.");

                        if (string.IsNullOrEmpty(customItem.UniqueId) || existingUniqueIds.Contains(customItem.UniqueId))
                        {
                            // 새로운 UniqueId 생성
                            string newUniqueId;
                            do
                            {
                                newUniqueId = Guid.NewGuid().ToString();
                            } while (existingUniqueIds.Contains(newUniqueId));

                            customItem.UniqueId = newUniqueId;
                            existingUniqueIds.Add(newUniqueId);

                            Log($"Assigned new UniqueId: {newUniqueId}.");

                            // 클라이언트에 메시지 전송
                            SendMessageToClients($"New UniqueId assigned: {newUniqueId}");
                        }
                    }
                    else
                    {
                        Log("Failed to cast content to MyObjectBuilder_CustomItem.");
                    }
                }
            }
        }

        private void GetItemUniqueIdsInWorld(string subtypeId)
        {
            Log($"Collecting UniqueIds for items with SubtypeId: '{subtypeId}'.");
            HashSet<string> uniqueIds = new HashSet<string>();

            // 모든 엔티티를 순회
            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);
            Log($"Total entities found: {entities.Count}.");

            // ... (생략된 코드: FloatingObject와 그리드 인벤토리 검사 부분)

            // 모든 플레이어의 인벤토리를 검사
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            Log($"Total players found: {players.Count}.");

            foreach (var player in players)
            {
                Log($"Checking player: DisplayName='{player.DisplayName}', PlayerId={player.IdentityId}.");

                var character = player.Character;
                if (character != null)
                {
                    int inventoryCount = character.InventoryCount;
                    Log($"Player character has {inventoryCount} inventories.");

                    for (int i = 0; i < inventoryCount; i++)
                    {
                        var inventory = character.GetInventory(i) as IMyInventory;
                        if (inventory != null)
                        {
                            var items = inventory.GetItems();
                            Log($"Inventory {i} has {items.Count} items.");

                            foreach (var item in items)
                            {
                                var contentId = item.Content.GetId();
                                if (contentId.SubtypeId == MyStringHash.GetOrCompute(subtypeId))
                                {
                                    var customItem = item.Content as MyObjectBuilder_CustomItem;
                                    if (customItem != null)
                                    {
                                        if (!string.IsNullOrEmpty(customItem.UniqueId))
                                        {
                                            uniqueIds.Add(customItem.UniqueId);
                                            Log($"Found UniqueId in player inventory: '{customItem.UniqueId}'.");
                                        }
                                        else
                                        {
                                            Log("Custom item in player inventory has no UniqueId. Assigning new UniqueId.");

                                            // 서버에서만 아이템 수정
                                            if (MyAPIGateway.Multiplayer.IsServer)
                                            {
                                                // 새로운 UniqueId 생성
                                                string newUniqueId;
                                                do
                                                {
                                                    newUniqueId = Guid.NewGuid().ToString();
                                                } while (existingUniqueIds.Contains(newUniqueId));

                                                customItem.UniqueId = newUniqueId;
                                                existingUniqueIds.Add(newUniqueId);
                                                uniqueIds.Add(newUniqueId);

                                                Log($"Assigned new UniqueId to item in player inventory: '{newUniqueId}'.");

                                                // 아이템 변경 사항을 적용하기 위해 인벤토리 아이템을 업데이트합니다.
                                                var index = items.IndexOf(item);
                                                if (index >= 0)
                                                {
                                                    // 아이템을 업데이트하여 변경 사항을 적용
                                                    items[index] = item;
                                                    Log($"Updated item in inventory at index {index}.");
                                                }
                                                else
                                                {
                                                    Log("Failed to find item index in inventory.");
                                                }
                                            }
                                            else
                                            {
                                                Log("Not a server. Cannot assign UniqueId.");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Log($"Failed to cast item content to MyObjectBuilder_CustomItem. Actual type: '{item.Content.GetType().Name}'.");
                                    }
                                }
                            }
                        }
                        else
                        {
                            Log($"Inventory {i} is null.");
                        }
                    }
                }
                else
                {
                    Log("Player character is null.");
                }
            }

            Log($"Total UniqueIds collected: {uniqueIds.Count}");
            // 수집된 UniqueId를 클라이언트에 전송
            foreach (var id in uniqueIds)
            {
                SendMessageToClients($"Item UniqueId: {id}");
            }

            // 총 아이템 수량도 클라이언트에 전송
            SendMessageToClients($"Total Upgrademodule items: {uniqueIds.Count}");
        }

        // 클라이언트에 메시지를 전송하는 메서드
        private void SendMessageToClients(string message)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(message);
            MyAPIGateway.Multiplayer.SendMessageToOthers(ModConstants.ModMessageId, bytes);
            Log($"Sent message to clients: {message}");
        }

        // 클라이언트에서 메시지를 수신하고 채팅에 출력하는 메서드
        private void ReceivedMessageHandler(object data)
        {
            byte[] byteData = data as byte[];
            if (byteData != null)
            {
                var message = System.Text.Encoding.UTF8.GetString(byteData);

                if (!MyAPIGateway.Multiplayer.IsServer)
                {
                    // 클라이언트에서만 채팅 메시지 출력
                    MyAPIGateway.Utilities.ShowMessage("ItemCountHandler", message);
                }

                // 로그에도 메시지 기록
                Log($"Received message: {message}");
            }
            else
            {
                Log("Received data is not byte array.");
            }
        }
    }
}
