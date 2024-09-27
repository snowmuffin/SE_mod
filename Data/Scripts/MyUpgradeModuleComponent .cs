using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRage.Game.Definitions;
using VRage.Network;

namespace SE_UpgradeModuleMod
{
    public static class ModConstants
    {
        public const ushort ModMessageId = 12345; // 임의의 고유 ID를 설정하세요
    }

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ItemCountHandler : MySessionComponentBase
    {
        private bool _initialized = false;
        private HashSet<string> existingUniqueIds = new HashSet<string>(); // 기존 UniqueId를 저장하는 집합
        private System.IO.TextWriter _logFile;
        private Dictionary<long, string> entityHashCodeMap = new Dictionary<long, string>(); // EntityId와 HashCode 매핑 저장소
        private const string StorageFileName = "EntityHashCodeMap.xml"; // 매핑 데이터 파일 이름

        public override void LoadData()
        {


            // 메시지 핸들러 등록
            MyAPIGateway.Utilities.RegisterMessageHandler(ModConstants.ModMessageId, ReceivedMessageHandler);

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                // OnEntityAdded 이벤트 핸들러 등록
                MyAPIGateway.Entities.OnEntityAdd += OnEntityAdded;
            }
            else
            {
                
            }

            LoadStorage(); // 기존 저장소 로드
            

            base.LoadData();
        }

        protected override void UnloadData()
        {

            // 메시지 핸들러 해제
            MyAPIGateway.Utilities.UnregisterMessageHandler(ModConstants.ModMessageId, ReceivedMessageHandler);

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                // OnEntityAdded 이벤트 핸들러 해제
                MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdded;
            }

            SaveStorage(); // 저장소 저장
            base.UnloadData();
        }

        public override void UpdateAfterSimulation()
        {
            if (!_initialized)
            {

                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    // 아이템의 UniqueId를 수집하고 클라이언트에 전송
                    GetItemInWorld("Upgrademodule");
                }
                else
                {
                }

                _initialized = true;
            }

            base.UpdateAfterSimulation();
        }



        // 해시코드 생성 메서드
        private string GenerateHashCode(long entityId)
        {
            // GUID를 사용하여 고유한 해시코드 생성
            string hashCode = Guid.NewGuid().ToString();
            return hashCode;
        }

        // 개체가 추가될 때 호출되는 이벤트 핸들러
        private void OnEntityAdded(IMyEntity entity)
        {

            var floatingObject = entity as MyFloatingObject;
            if (floatingObject != null)
            {

                var item = floatingObject.Item;
                // 구조체의 경우, item.Amount > 0 등의 속성으로 체크
                if (item.Amount > 0 && item.Content.SubtypeId == MyStringHash.GetOrCompute("Upgrademodule"))
                {
                    long entityId = floatingObject.EntityId;
                    string hashCode;

                    if (!entityHashCodeMap.ContainsKey(entityId))
                    {
                        hashCode = GenerateHashCode(entityId);
                        entityHashCodeMap.Add(entityId, hashCode);

                        // 클라이언트에 매핑 정보 전송 (선택 사항)
                        SendMessageToClients($"New Upgrademodule added - EntityID: {entityId}, HashCode: {hashCode}");
                    }
                    else
                    {
                        hashCode = entityHashCodeMap[entityId];
                    }
                }
                else
                {
                }
            }
            else
            {
            }
        }

        private void GetItemInWorld(string subtypeId)
        {

            HashSet<string> uniqueIds = new HashSet<string>();

            // 모든 엔티티를 순회
            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);

            foreach (var entity in entities)
            {
                var floatingObject = entity as MyFloatingObject;

                if (floatingObject != null)
                {

                    var item = floatingObject.Item;
                    // 구조체의 경우, item.Amount > 0 등의 속성으로 체크
                    if (item.Amount > 0 && item.Content.SubtypeId == MyStringHash.GetOrCompute(subtypeId))
                    {
                        long entityId = floatingObject.EntityId;
                        string hashCode;

                        if (!entityHashCodeMap.ContainsKey(entityId))
                        {
                            hashCode = GenerateHashCode(entityId);
                            entityHashCodeMap.Add(entityId, hashCode);
                        }
                        else
                        {
                            hashCode = entityHashCodeMap[entityId];
                        }

                        uniqueIds.Add(hashCode);
                    }
                    else
                    {
                    }
                }
                else
                {
                }
            }

            // 모든 플레이어의 인벤토리를 검사
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);

            foreach (var player in players)
            {

                var character = player.Character;
                if (character != null)
                {
                    int inventoryCount = character.InventoryCount;

                    for (int i = 0; i < inventoryCount; i++)
                    {
                        var inventory = character.GetInventory(i) as IMyInventory;
                        if (inventory != null)
                        {
                            var items = inventory.GetItems();

                            foreach (var item in items)
                            {
                                var contentId = item.Content.GetId();
                                if (contentId.SubtypeId == MyStringHash.GetOrCompute(subtypeId))
                                {
                                    long itemId = item.ItemId; // ItemId를 EntityId로 가정 (실제 상황에 맞게 조정 필요)
                                    string hashCode;

                                    if (!entityHashCodeMap.ContainsKey(itemId))
                                    {
                                        hashCode = GenerateHashCode(itemId);
                                        entityHashCodeMap.Add(itemId, hashCode);
                                    }
                                    else
                                    {
                                        hashCode = entityHashCodeMap[itemId];
                                    }

                                    uniqueIds.Add(hashCode);
                                }
                                else
                                {
                                }
                            }
                        }
                        else
                        {
                        }
                    }
                }
                else
                {
                }
            }

            // 총 아이템 수량도 클라이언트에 전송
            SendMessageToClients($"Total Upgrademodule items: {uniqueIds.Count}");
        }

        // 클라이언트에 메시지를 전송하는 메서드
        private void SendMessageToClients(string message)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(message);
            MyAPIGateway.Multiplayer.SendMessageToOthers(ModConstants.ModMessageId, bytes);
        }

        // 클라이언트에서 메시지를 수신하고 채팅에 출력하는 메서드
        private void ReceivedMessageHandler(object data)
        {
            byte[] byteData = data as byte[];
            if (byteData != null)
            {
                var message = Encoding.UTF8.GetString(byteData);

                if (!MyAPIGateway.Multiplayer.IsServer)
                {
                    // 클라이언트에서만 채팅 메시지 출력
                    MyAPIGateway.Utilities.ShowMessage("ItemCountHandler", message);
                }
            }
            else
            {
            }
        }

        // 해시코드 매핑 저장 메서드
        private void SaveStorage()
        {

            // Dictionary를 XML 형식으로 직렬화
            string serializedData = MyAPIGateway.Utilities.SerializeToXML(entityHashCodeMap);

            // LocalStorage에 저장
            using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(StorageFileName, typeof(ItemCountHandler)))
            {
                writer.Write(serializedData);
            }

        }

        // 해시코드 매핑 로드 메서드
        private void LoadStorage()
        {

            // LocalStorage에서 파일 읽기
            using (var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(StorageFileName, typeof(ItemCountHandler)))
            {
                if (reader != null)
                {
                    string serializedData = reader.ReadToEnd();

                    try
                    {
                        // XML을 다시 Dictionary로 역직렬화
                        entityHashCodeMap = MyAPIGateway.Utilities.SerializeFromXML<Dictionary<long, string>>(serializedData);
                    }
                    catch (Exception e)
                    {
                        entityHashCodeMap = new Dictionary<long, string>();
                    }
                }
                else
                {
                    entityHashCodeMap = new Dictionary<long, string>();
                }
            }
        }
    }
}