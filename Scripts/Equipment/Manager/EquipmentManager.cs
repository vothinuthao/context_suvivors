using Common.Scripts.Equipment;
using UnityEngine;
using UnityEngine.Events;
using OctoberStudio.Save;
using System.Collections.Generic;
using System.Linq;

namespace OctoberStudio.Equipment
{
    public class EquipmentManager : MonoBehaviour
    {
        private static EquipmentSave equipmentSave;
        private static EquipmentManager instance;

        public static EquipmentManager Instance => instance;

        public UnityEvent<EquipmentType> OnEquipmentChanged;
        public UnityEvent OnInventoryChanged;
        
        public EquipmentDatabase Database => EquipmentDatabase.Instance;

        [Header("Debug Info")]
        [SerializeField, ReadOnly] private bool databaseReady = false;
        [SerializeField, ReadOnly] private int totalEquipmentInDatabase = 0;

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            equipmentSave = GameController.SaveManager.GetSave<EquipmentSave>("Equipment");
            
            if (EquipmentDatabase.Instance != null)
            {
                EquipmentDatabase.Instance.OnDataLoaded += OnDatabaseLoaded;
                EquipmentDatabase.Instance.OnLoadingError += OnDatabaseError;
                
                if (EquipmentDatabase.Instance.IsDataLoaded)
                {
                    OnDatabaseLoaded();
                }
            }
        }

        private void OnDestroy()
        {
            if (EquipmentDatabase.Instance != null)
            {
                EquipmentDatabase.Instance.OnDataLoaded -= OnDatabaseLoaded;
                EquipmentDatabase.Instance.OnLoadingError -= OnDatabaseError;
            }
        }

        private void OnDatabaseLoaded()
        {
            databaseReady = true;
            totalEquipmentInDatabase = EquipmentDatabase.Instance.TotalEquipmentCount;
        }

        private void OnDatabaseError(string error)
        {
            databaseReady = false;
        }

        public EquipmentModel GetEquippedItem(EquipmentType type)
        {
            if (!databaseReady)
            {
                return null;
            }

            var equippedItem = equipmentSave.GetEquippedItem(type);
            if (equippedItem == null || equippedItem.equipmentId == -1)
                return null;

            return EquipmentDatabase.Instance.GetEquipmentByGlobalId(equippedItem.equipmentId);
        }

        public string GetEquippedItemUID(EquipmentType type)
        {
            return equipmentSave.GetEquippedItemUID(type);
        }

        public bool EquipItemByUID(string itemUID)
        {
            if (!databaseReady)
            {
                return false;
            }

            var inventoryItem = equipmentSave.GetItemByUID(itemUID);
            if (inventoryItem == null)
            {
                return false;
            }

            return EquipItem(inventoryItem.equipmentType, inventoryItem.equipmentId, inventoryItem.level, itemUID);
        }

        public bool EquipItem(EquipmentType type, int globalEquipmentId, int level = 1, string specificUID = "")
        {
            if (!databaseReady)
            {
                return false;
            }

            var equipmentData = EquipmentDatabase.Instance.GetEquipmentByGlobalId(globalEquipmentId);
            if (equipmentData == null)
            {
                return false;
            }

            if (equipmentData.EquipmentType != type)
            {
                return false;
            }

            EquipmentSave.InventoryItem itemToEquip = null;

            if (!string.IsNullOrEmpty(specificUID))
            {
                itemToEquip = equipmentSave.GetItemByUID(specificUID);
                if (itemToEquip == null)
                {
                    return false;
                }
            }
            else
            {
                var matchingItems = equipmentSave.GetItemsByTypeAndId(type, globalEquipmentId, level);
                if (matchingItems.Count > 0)
                {
                    itemToEquip = matchingItems[0];
                }
                else
                {
                    return false;
                }
            }

            var currentEquipped = equipmentSave.GetEquippedItem(type);
            if (currentEquipped.equipmentId != -1 && !string.IsNullOrEmpty(currentEquipped.uid))
            {
                equipmentSave.AddToInventory(type, currentEquipped.equipmentId, currentEquipped.level);
            }

            equipmentSave.SetEquippedItem(type, globalEquipmentId, level, itemToEquip.uid);
            
            equipmentSave.RemoveFromInventory(itemToEquip.uid);

            OnEquipmentChanged?.Invoke(type);
            OnInventoryChanged?.Invoke();

            UpdatePlayerStats();

            return true;
        }

        public bool EquipItem(EquipmentModel equipment, int level = 1, string specificUID = "")
        {
            if (equipment == null) return false;
            return EquipItem(equipment.EquipmentType, equipment.ID, level, specificUID);
        }

        public void UnequipItem(EquipmentType type)
        {
            var equippedItem = equipmentSave.GetEquippedItem(type);
            if (equippedItem.equipmentId == -1)
                return;

            equipmentSave.AddToInventory(type, equippedItem.equipmentId, equippedItem.level);
            
            equipmentSave.UnequipItem(type);

            OnEquipmentChanged?.Invoke(type);
            OnInventoryChanged?.Invoke();

            UpdatePlayerStats();
        }

        public EquipmentSave.InventoryItem AddEquipmentToInventory(int globalEquipmentId, int level = 1)
        {
            if (!databaseReady)
            {
                return null;
            }

            var equipmentData = EquipmentDatabase.Instance.GetEquipmentByGlobalId(globalEquipmentId);
            if (equipmentData == null)
            {
                return null;
            }

            var newItem = equipmentSave.AddToInventory(equipmentData.EquipmentType, globalEquipmentId, level);
            OnInventoryChanged?.Invoke();

            return newItem;
        }

        public List<EquipmentSave.InventoryItem> AddMultipleEquipmentToInventory(int globalEquipmentId, int level = 1, int count = 1)
        {
            if (!databaseReady)
            {
                return new List<EquipmentSave.InventoryItem>();
            }

            var equipmentData = EquipmentDatabase.Instance.GetEquipmentByGlobalId(globalEquipmentId);
            if (equipmentData == null)
            {
                return new List<EquipmentSave.InventoryItem>();
            }

            var addedItems = equipmentSave.AddMultipleToInventory(equipmentData.EquipmentType, globalEquipmentId, level, count);
            OnInventoryChanged?.Invoke();

            return addedItems;
        }

        public EquipmentSave.InventoryItem AddEquipmentToInventory(EquipmentModel equipment, int level = 1)
        {
            if (equipment == null) return null;
            return AddEquipmentToInventory(equipment.ID, level);
        }

        public bool RemoveEquipmentFromInventory(string itemUID)
        {
            var removed = equipmentSave.RemoveFromInventory(itemUID);
            if (removed)
            {
                OnInventoryChanged?.Invoke();
            }
            return removed;
        }

        public bool RemoveEquipmentFromInventory(EquipmentType type, int equipmentId, int level = 1)
        {
            var removed = equipmentSave.RemoveFromInventory(type, equipmentId, level);
            if (removed)
            {
                OnInventoryChanged?.Invoke();
            }
            return removed;
        }

        public List<EquipmentSave.InventoryItem> GetInventoryItems()
        {
            equipmentSave ??= GameController.SaveManager.GetSave<EquipmentSave>("Equipment");
            return equipmentSave.inventory.ToList();
        }

        public List<(EquipmentSave.InventoryItem inventoryItem, EquipmentModel equipmentData)> GetInventoryItemsWithData()
        {
            if (!databaseReady)
            {
                return new List<(EquipmentSave.InventoryItem, EquipmentModel)>();
            }

            equipmentSave ??= GameController.SaveManager.GetSave<EquipmentSave>("Equipment");
            var inventoryItems = equipmentSave.inventory;
            var result = new List<(EquipmentSave.InventoryItem, EquipmentModel)>();

            foreach (var item in inventoryItems)
            {
                var equipmentData = EquipmentDatabase.Instance.GetEquipmentByGlobalId(item.equipmentId);
                result.Add((item, equipmentData));
            }

            return result;
        }

        public List<EquipmentSave.InventoryItem> GetInventoryItemsByType(EquipmentType type)
        {
            return equipmentSave.inventory.Where(i => i.equipmentType == type).ToList();
        }

        public EquipmentSave.InventoryItem GetItemByUID(string uid)
        {
            return equipmentSave.GetItemByUID(uid);
        }

        public bool IsItemEquipped(string itemUID)
        {
            return equipmentSave.IsItemEquipped(itemUID);
        }

        public EquipmentStats GetTotalEquipmentStats()
        {
            if (!databaseReady)
                return new EquipmentStats();

            var totalStats = new EquipmentStats();

            for (int i = 0; i < 6; i++)
            {
                var equippedData = GetEquippedItem((EquipmentType)i);
                if (equippedData != null)
                {
                    var stats = equippedData.GetTotalStats();
                    totalStats += stats;
                }
            }

            return totalStats;
        }

        public EquipmentModel[] GetEquipmentsByType(EquipmentType type)
        {
            if (!databaseReady)
                return new EquipmentModel[0];

            return EquipmentDatabase.Instance.GetEquipmentsByType(type);
        }

        public EquipmentModel GetRandomLoot(EquipmentRarity rarity)
        {
            if (!databaseReady)
                return null;

            return EquipmentDatabase.Instance.GetRandomEquipmentByRarity(rarity);
        }

        public bool HasEquipment(int globalEquipmentId, int level = 1)
        {
            var items = equipmentSave.GetItemsByTypeAndId(EquipmentType.Hat, globalEquipmentId, level);
            return items.Count > 0;
        }

        public Dictionary<EquipmentType, int> GetInventoryStatistics()
        {
            return equipmentSave.GetInventoryStats();
        }

        public void SortInventoryByDate(bool newestFirst = true)
        {
            equipmentSave.SortInventoryByDate(newestFirst);
            OnInventoryChanged?.Invoke();
        }

        public void SortInventoryByType()
        {
            equipmentSave.SortInventoryByType();
            OnInventoryChanged?.Invoke();
        }

        private void UpdatePlayerStats()
        {
            if (PlayerBehavior.Player != null)
            {
                PlayerBehavior.Player.RecalculateStatsFromEquipment();
            }
        }

        [ContextMenu("Reload Equipment Database")]
        public void ReloadDatabase()
        {
            if (EquipmentDatabase.Instance != null)
            {
                EquipmentDatabase.Instance.ReloadEquipmentData();
            }
        }

        [ContextMenu("Add Test Equipment")]
        public void AddTestEquipment()
        {
            if (!databaseReady)
            {
                return;
            }

            AddEquipmentToInventory(0, 1);
            AddEquipmentToInventory(6, 1);
            AddEquipmentToInventory(12, 1);
            AddEquipmentToInventory(18, 1);
            AddEquipmentToInventory(24, 1);
        }

        [ContextMenu("Log Equipment Status")]
        public void LogEquipmentStatus()
        {
            for (int i = 0; i < 6; i++)
            {
                var type = (EquipmentType)i;
                var equipped = GetEquippedItem(type);
                var equippedUID = GetEquippedItemUID(type);
            }

            var stats = GetInventoryStatistics();
        }
    }
}