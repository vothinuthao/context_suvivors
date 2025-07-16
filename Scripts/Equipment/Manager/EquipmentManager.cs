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
        
        // Use the new CSV-based database
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
            
            // Subscribe to database events
            if (EquipmentDatabase.Instance != null)
            {
                EquipmentDatabase.Instance.OnDataLoaded += OnDatabaseLoaded;
                EquipmentDatabase.Instance.OnLoadingError += OnDatabaseError;
                
                // Check if data is already loaded
                if (EquipmentDatabase.Instance.IsDataLoaded)
                {
                    OnDatabaseLoaded();
                }
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from database events
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
            Debug.Log($"[EquipmentManager] Database loaded with {totalEquipmentInDatabase} equipment items");
        }

        private void OnDatabaseError(string error)
        {
            databaseReady = false;
            Debug.LogError($"[EquipmentManager] Database loading error: {error}");
        }

        /// <summary>
        /// Get equipped item (using global ID lookup)
        /// </summary>
        public EquipmentModel GetEquippedItem(EquipmentType type)
        {
            if (!databaseReady)
            {
                Debug.LogWarning("[EquipmentManager] Database not ready yet!");
                return null;
            }

            var equippedItem = equipmentSave.GetEquippedItem(type);
            if (equippedItem == null || equippedItem.equipmentId == -1)
                return null;

            // Use global ID lookup instead of type-based index lookup
            return EquipmentDatabase.Instance.GetEquipmentByGlobalId(equippedItem.equipmentId);
        }

        /// <summary>
        /// Get equipped item UID
        /// </summary>
        public string GetEquippedItemUID(EquipmentType type)
        {
            return equipmentSave.GetEquippedItemUID(type);
        }

        /// <summary>
        /// Equip item by UID
        /// </summary>
        public bool EquipItemByUID(string itemUID)
        {
            if (!databaseReady)
            {
                Debug.LogWarning("[EquipmentManager] Database not ready yet!");
                return false;
            }

            var inventoryItem = equipmentSave.GetItemByUID(itemUID);
            if (inventoryItem == null)
            {
                Debug.LogError($"[EquipmentManager] Item with UID {itemUID} not found in inventory!");
                return false;
            }

            return EquipItem(inventoryItem.equipmentType, inventoryItem.equipmentId, inventoryItem.level, itemUID);
        }

        /// <summary>
        /// Equip item by global ID (creates new item if not in inventory)
        /// </summary>
        public bool EquipItem(EquipmentType type, int globalEquipmentId, int level = 1, string specificUID = "")
        {
            if (!databaseReady)
            {
                Debug.LogWarning("[EquipmentManager] Database not ready yet!");
                return false;
            }

            // Verify the equipment exists in database
            var equipmentData = EquipmentDatabase.Instance.GetEquipmentByGlobalId(globalEquipmentId);
            if (equipmentData == null)
            {
                Debug.LogError($"[EquipmentManager] Equipment with ID {globalEquipmentId} not found in database!");
                return false;
            }

            // Verify equipment type matches slot
            if (equipmentData.EquipmentType != type)
            {
                Debug.LogError($"[EquipmentManager] Equipment type mismatch! Expected {type}, got {equipmentData.EquipmentType}");
                return false;
            }

            EquipmentSave.InventoryItem itemToEquip = null;

            // If specific UID provided, find that exact item
            if (!string.IsNullOrEmpty(specificUID))
            {
                itemToEquip = equipmentSave.GetItemByUID(specificUID);
                if (itemToEquip == null)
                {
                    Debug.LogError($"[EquipmentManager] Item with UID {specificUID} not found in inventory!");
                    return false;
                }
            }
            else
            {
                // Find any matching item in inventory
                var matchingItems = equipmentSave.GetItemsByTypeAndId(type, globalEquipmentId, level);
                if (matchingItems.Count > 0)
                {
                    itemToEquip = matchingItems[0]; // Take first matching item
                }
                else
                {
                    Debug.LogWarning($"[EquipmentManager] Player doesn't have equipment {globalEquipmentId} in inventory!");
                    return false;
                }
            }

            // Unequip current item if any
            var currentEquipped = equipmentSave.GetEquippedItem(type);
            if (currentEquipped.equipmentId != -1 && !string.IsNullOrEmpty(currentEquipped.uid))
            {
                // Return currently equipped item to inventory
                equipmentSave.AddToInventory(type, currentEquipped.equipmentId, currentEquipped.level);
            }

            // Equip the new item
            equipmentSave.SetEquippedItem(type, globalEquipmentId, level, itemToEquip.uid);
            
            // Remove from inventory
            equipmentSave.RemoveFromInventory(itemToEquip.uid);

            // Trigger events
            OnEquipmentChanged?.Invoke(type);
            OnInventoryChanged?.Invoke();

            // Update player stats
            UpdatePlayerStats();

            Debug.Log($"[EquipmentManager] Equipped {equipmentData.Name} (UID: {itemToEquip.uid}) to {type} slot");
            return true;
        }

        /// <summary>
        /// Equip item by EquipmentModel reference
        /// </summary>
        public bool EquipItem(EquipmentModel equipment, int level = 1, string specificUID = "")
        {
            if (equipment == null) return false;
            return EquipItem(equipment.EquipmentType, equipment.ID, level, specificUID);
        }

        /// <summary>
        /// Unequip an item
        /// </summary>
        public void UnequipItem(EquipmentType type)
        {
            var equippedItem = equipmentSave.GetEquippedItem(type);
            if (equippedItem.equipmentId == -1)
                return;

            // Move equipped item back to inventory
            equipmentSave.AddToInventory(type, equippedItem.equipmentId, equippedItem.level);
            
            // Clear equipped slot
            equipmentSave.UnequipItem(type);

            // Trigger events
            OnEquipmentChanged?.Invoke(type);
            OnInventoryChanged?.Invoke();

            // Update player stats
            UpdatePlayerStats();

            Debug.Log($"[EquipmentManager] Unequipped item from {type} slot");
        }

        /// <summary>
        /// Add equipment to inventory by global ID
        /// </summary>
        public EquipmentSave.InventoryItem AddEquipmentToInventory(int globalEquipmentId, int level = 1)
        {
            if (!databaseReady)
            {
                Debug.LogWarning("[EquipmentManager] Database not ready yet!");
                return null;
            }

            var equipmentData = EquipmentDatabase.Instance.GetEquipmentByGlobalId(globalEquipmentId);
            if (equipmentData == null)
            {
                Debug.LogError($"[EquipmentManager] Equipment with ID {globalEquipmentId} not found!");
                return null;
            }

            var newItem = equipmentSave.AddToInventory(equipmentData.EquipmentType, globalEquipmentId, level);
            OnInventoryChanged?.Invoke();

            Debug.Log($"[EquipmentManager] Added {equipmentData.Name} (UID: {newItem.uid}) to inventory");
            return newItem;
        }

        /// <summary>
        /// Add multiple equipment to inventory
        /// </summary>
        public List<EquipmentSave.InventoryItem> AddMultipleEquipmentToInventory(int globalEquipmentId, int level = 1, int count = 1)
        {
            if (!databaseReady)
            {
                Debug.LogWarning("[EquipmentManager] Database not ready yet!");
                return new List<EquipmentSave.InventoryItem>();
            }

            var equipmentData = EquipmentDatabase.Instance.GetEquipmentByGlobalId(globalEquipmentId);
            if (equipmentData == null)
            {
                Debug.LogError($"[EquipmentManager] Equipment with ID {globalEquipmentId} not found!");
                return new List<EquipmentSave.InventoryItem>();
            }

            var addedItems = equipmentSave.AddMultipleToInventory(equipmentData.EquipmentType, globalEquipmentId, level, count);
            OnInventoryChanged?.Invoke();

            Debug.Log($"[EquipmentManager] Added {count}x {equipmentData.Name} to inventory");
            return addedItems;
        }

        /// <summary>
        /// Add equipment to inventory by EquipmentModel reference
        /// </summary>
        public EquipmentSave.InventoryItem AddEquipmentToInventory(EquipmentModel equipment, int level = 1)
        {
            if (equipment == null) return null;
            return AddEquipmentToInventory(equipment.ID, level);
        }

        /// <summary>
        /// Remove specific item by UID
        /// </summary>
        public bool RemoveEquipmentFromInventory(string itemUID)
        {
            var removed = equipmentSave.RemoveFromInventory(itemUID);
            if (removed)
            {
                OnInventoryChanged?.Invoke();
                Debug.Log($"[EquipmentManager] Removed item with UID: {itemUID}");
            }
            return removed;
        }

        /// <summary>
        /// Remove item by type and ID (removes first matching item)
        /// </summary>
        public bool RemoveEquipmentFromInventory(EquipmentType type, int equipmentId, int level = 1)
        {
            var removed = equipmentSave.RemoveFromInventory(type, equipmentId, level);
            if (removed)
            {
                OnInventoryChanged?.Invoke();
                Debug.Log($"[EquipmentManager] Removed {type} equipment ID: {equipmentId}");
            }
            return removed;
        }

        /// <summary>
        /// Get all inventory items
        /// </summary>
        public List<EquipmentSave.InventoryItem> GetInventoryItems()
        {
            equipmentSave ??= GameController.SaveManager.GetSave<EquipmentSave>("Equipment");
            return equipmentSave.inventory.ToList();
        }

        /// <summary>
        /// Get all inventory items with their EquipmentModel data
        /// </summary>
        public List<(EquipmentSave.InventoryItem inventoryItem, EquipmentModel equipmentData)> GetInventoryItemsWithData()
        {
            if (!databaseReady)
            {
                Debug.LogWarning("[EquipmentManager] Database not ready yet!");
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

        /// <summary>
        /// Get inventory items by type
        /// </summary>
        public List<EquipmentSave.InventoryItem> GetInventoryItemsByType(EquipmentType type)
        {
            return equipmentSave.inventory.Where(i => i.equipmentType == type).ToList();
        }

        /// <summary>
        /// Get item by UID
        /// </summary>
        public EquipmentSave.InventoryItem GetItemByUID(string uid)
        {
            return equipmentSave.GetItemByUID(uid);
        }

        /// <summary>
        /// Check if item is currently equipped
        /// </summary>
        public bool IsItemEquipped(string itemUID)
        {
            return equipmentSave.IsItemEquipped(itemUID);
        }

        /// <summary>
        /// Calculate total equipment bonuses
        /// </summary>
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

        /// <summary>
        /// Get equipment by type from database
        /// </summary>
        public EquipmentModel[] GetEquipmentsByType(EquipmentType type)
        {
            if (!databaseReady)
                return new EquipmentModel[0];

            return EquipmentDatabase.Instance.GetEquipmentsByType(type);
        }

        /// <summary>
        /// Get random loot by rarity
        /// </summary>
        public EquipmentModel GetRandomLoot(EquipmentRarity rarity)
        {
            if (!databaseReady)
                return null;

            return EquipmentDatabase.Instance.GetRandomEquipmentByRarity(rarity);
        }

        /// <summary>
        /// Check if player has specific equipment
        /// </summary>
        public bool HasEquipment(int globalEquipmentId, int level = 1)
        {
            var items = equipmentSave.GetItemsByTypeAndId(EquipmentType.Hat, globalEquipmentId, level);
            return items.Count > 0;
        }

        /// <summary>
        /// Get inventory statistics
        /// </summary>
        public Dictionary<EquipmentType, int> GetInventoryStatistics()
        {
            return equipmentSave.GetInventoryStats();
        }

        /// <summary>
        /// Sort inventory by creation date
        /// </summary>
        public void SortInventoryByDate(bool newestFirst = true)
        {
            equipmentSave.SortInventoryByDate(newestFirst);
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Sort inventory by type and ID
        /// </summary>
        public void SortInventoryByType()
        {
            equipmentSave.SortInventoryByType();
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Update player stats when equipment changes
        /// </summary>
        private void UpdatePlayerStats()
        {
            if (PlayerBehavior.Player != null)
            {
                PlayerBehavior.Player.RecalculateStatsFromEquipment();
            }
        }

        /// <summary>
        /// Force reload database (for debugging)
        /// </summary>
        [ContextMenu("Reload Equipment Database")]
        public void ReloadDatabase()
        {
            if (EquipmentDatabase.Instance != null)
            {
                EquipmentDatabase.Instance.ReloadEquipmentData();
            }
        }

        /// <summary>
        /// Add some test equipment to inventory (for debugging)
        /// </summary>
        [ContextMenu("Add Test Equipment")]
        public void AddTestEquipment()
        {
            if (!databaseReady)
            {
                Debug.LogWarning("Database not ready!");
                return;
            }

            // Add one of each rarity
            AddEquipmentToInventory(0, 1);  // Common Hat
            AddEquipmentToInventory(6, 1);  // Uncommon Armor
            AddEquipmentToInventory(12, 1); // Rare Ring
            AddEquipmentToInventory(18, 1); // Epic Necklace
            AddEquipmentToInventory(24, 1); // Legendary Belt

            Debug.Log("Added test equipment to inventory");
        }

        /// <summary>
        /// Log equipment manager status
        /// </summary>
        [ContextMenu("Log Equipment Status")]
        public void LogEquipmentStatus()
        {
            Debug.Log($"[EquipmentManager] Status:");
            Debug.Log($"  Database Ready: {databaseReady}");
            Debug.Log($"  Total Equipment in DB: {totalEquipmentInDatabase}");
            Debug.Log($"  Inventory Items: {(equipmentSave?.inventory.Count ?? 0)}");
            
            // Log equipped items
            for (int i = 0; i < 6; i++)
            {
                var type = (EquipmentType)i;
                var equipped = GetEquippedItem(type);
                var equippedUID = GetEquippedItemUID(type);
                Debug.Log($"  {type}: {(equipped?.Name ?? "None")} (UID: {equippedUID})");
            }

            // Log inventory stats
            var stats = GetInventoryStatistics();
            Debug.Log("  Inventory Statistics:");
            foreach (var kvp in stats)
            {
                Debug.Log($"    {kvp.Key}: {kvp.Value} items");
            }
        }
    }
}