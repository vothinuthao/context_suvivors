using Common.Scripts.Equipment;
using UnityEngine;
using UnityEngine.Events;
using OctoberStudio.Save;

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
        /// Equip item by global ID
        /// </summary>
        public bool EquipItem(EquipmentType type, int globalEquipmentId, int level = 1)
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

            // Check if player has this item in inventory
            var hasItem = equipmentSave.inventory.Exists(item => 
                item.equipmentType == type && 
                item.equipmentId == globalEquipmentId && 
                item.level == level);

            if (!hasItem)
            {
                Debug.LogWarning($"[EquipmentManager] Player doesn't have equipment {globalEquipmentId} in inventory!");
                return false;
            }

            // Unequip current item if any
            var currentEquipped = equipmentSave.GetEquippedItem(type);
            if (currentEquipped.equipmentId != -1)
            {
                equipmentSave.AddToInventory(type, currentEquipped.equipmentId, currentEquipped.level);
            }

            // Equip the new item
            equipmentSave.SetEquippedItem(type, globalEquipmentId, level);
            
            // Remove from inventory
            equipmentSave.RemoveFromInventory(type, globalEquipmentId, level, 1);

            // Trigger events
            OnEquipmentChanged?.Invoke(type);
            OnInventoryChanged?.Invoke();

            // Update player stats
            UpdatePlayerStats();

            Debug.Log($"[EquipmentManager] Equipped {equipmentData.Name} to {type} slot");
            return true;
        }

        /// <summary>
        /// Equip item by EquipmentModel reference
        /// </summary>
        public bool EquipItem(EquipmentModel equipment, int level = 1)
        {
            if (equipment == null) return false;
            return EquipItem(equipment.EquipmentType, equipment.ID, level);
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
        public bool AddEquipmentToInventory(int globalEquipmentId, int level = 1, int quantity = 1)
        {
            if (!databaseReady)
            {
                Debug.LogWarning("[EquipmentManager] Database not ready yet!");
                return false;
            }

            var equipmentData = EquipmentDatabase.Instance.GetEquipmentByGlobalId(globalEquipmentId);
            if (equipmentData == null)
            {
                Debug.LogError($"[EquipmentManager] Equipment with ID {globalEquipmentId} not found!");
                return false;
            }

            equipmentSave.AddToInventory(equipmentData.EquipmentType, globalEquipmentId, level, quantity);
            OnInventoryChanged?.Invoke();

            Debug.Log($"[EquipmentManager] Added {quantity}x {equipmentData.Name} to inventory");
            return true;
        }

        /// <summary>
        /// Add equipment to inventory by EquipmentModel reference
        /// </summary>
        public bool AddEquipmentToInventory(EquipmentModel equipment, int level = 1, int quantity = 1)
        {
            if (equipment == null) return false;
            return AddEquipmentToInventory(equipment.ID, level, quantity);
        }

        /// <summary>
        /// Add equipment to inventory (legacy method)
        /// </summary>
        public void AddEquipmentToInventory(EquipmentType type, int equipmentId, int level = 1, int quantity = 1)
        {
            equipmentSave.AddToInventory(type, equipmentId, level, quantity);
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Get all inventory items with their EquipmentModel data
        /// </summary>
        public (EquipmentSave.InventoryItem inventoryItem, EquipmentModel equipmentData)[] GetInventoryItemsWithData()
        {
            if (!databaseReady)
            {
                Debug.LogWarning("[EquipmentManager] Database not ready yet!");
                return new (EquipmentSave.InventoryItem, EquipmentModel)[0];
            }

            equipmentSave ??= GameController.SaveManager.GetSave<EquipmentSave>("Equipment");
            var inventoryItems = equipmentSave.inventory;
            var result = new (EquipmentSave.InventoryItem, EquipmentModel)[inventoryItems.Count];

            for (int i = 0; i < inventoryItems.Count; i++)
            {
                var item = inventoryItems[i];
                var equipmentData = EquipmentDatabase.Instance.GetEquipmentByGlobalId(item.equipmentId);
                result[i] = (item, equipmentData);
            }

            return result;
        }

        /// <summary>
        /// Get all inventory items (legacy method)
        /// </summary>
        public EquipmentSave.InventoryItem[] GetInventoryItems()
        {
            equipmentSave ??= GameController.SaveManager.GetSave<EquipmentSave>("Equipment");
            return equipmentSave.inventory.ToArray();
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
            var item = equipmentSave.inventory.Find(i => 
                i.equipmentId == globalEquipmentId && 
                i.level == level);
            
            return item != null && item.quantity > 0;
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
            AddEquipmentToInventory(0, 1, 1);  // Common Hat
            AddEquipmentToInventory(6, 1, 1);  // Uncommon Armor
            AddEquipmentToInventory(12, 1, 1); // Rare Ring
            AddEquipmentToInventory(18, 1, 1); // Epic Necklace
            AddEquipmentToInventory(24, 1, 1); // Legendary Belt

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
                Debug.Log($"  {type}: {(equipped?.Name ?? "None")}");
            }
        }
    }
}