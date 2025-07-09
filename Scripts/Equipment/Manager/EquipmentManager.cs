using Common.Scripts.Equipment;
using UnityEngine;
using UnityEngine.Events;
using OctoberStudio.Save;

namespace OctoberStudio.Equipment
{
    public class EquipmentManager : MonoBehaviour
    {
        [SerializeField] private EquipmentDatabase database;
        
        private static EquipmentSave equipmentSave;
        private static EquipmentManager instance;

        public static EquipmentManager Instance => instance;

        public UnityEvent<EquipmentType> OnEquipmentChanged;
        public UnityEvent OnInventoryChanged;
        public EquipmentDatabase Database => database;

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
        }

        public EquipmentModel GetEquippedItem(EquipmentType type)
        {
            var equippedItem = equipmentSave.GetEquippedItem(type);
            if (equippedItem == null || equippedItem.equipmentId == -1)
                return null;

            return database.GetEquipmentById(type, equippedItem.equipmentId);
        }

        public bool EquipItem(EquipmentType type, int equipmentId, int level = 1)
        {
            var hasItem = equipmentSave.inventory.Exists(item => 
                item.equipmentType == type && 
                item.equipmentId == equipmentId && 
                item.level == level);

            if (!hasItem)
                return false;

            var currentEquipped = equipmentSave.GetEquippedItem(type);
            if (currentEquipped.equipmentId != -1)
            {
                equipmentSave.AddToInventory(type, currentEquipped.equipmentId, currentEquipped.level);
            }

            // Equip the new item
            equipmentSave.SetEquippedItem(type, equipmentId, level);
            
            // Remove from inventory
            equipmentSave.RemoveFromInventory(type, equipmentId, level, 1);

            // Trigger events
            OnEquipmentChanged?.Invoke(type);
            OnInventoryChanged?.Invoke();

            // Update player stats
            UpdatePlayerStats();

            return true;
        }

        // Unequip an item
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
        }

        // Add item to inventory (for loot/rewards)
        public void AddEquipmentToInventory(EquipmentType type, int equipmentId, int level = 1, int quantity = 1)
        {
            equipmentSave.AddToInventory(type, equipmentId, level, quantity);
            OnInventoryChanged?.Invoke();
        }

        // Get all inventory items
        // ReSharper disable Unity.PerformanceAnalysis
        public EquipmentSave.InventoryItem[] GetInventoryItems()
        {
            equipmentSave ??= GameController.SaveManager.GetSave<EquipmentSave>("Equipment");
            return equipmentSave.inventory.ToArray();
        }

        // Calculate total equipment bonuses
        public EquipmentStats GetTotalEquipmentStats()
        {
            var totalStats = new EquipmentStats();

            for (int i = 0; i < 6; i++)
            {
                var equippedData = GetEquippedItem((EquipmentType)i);
                if (equippedData != null)
                {
                    totalStats.bonusHP += equippedData.BonusHP;
                    totalStats.bonusDamage += equippedData.BonusDamage;
                    totalStats.bonusSpeed += equippedData.BonusSpeed;
                    totalStats.bonusMagnetRadius += equippedData.BonusMagnetRadius;
                    totalStats.bonusXPMultiplier += equippedData.BonusXPMultiplier;
                    totalStats.bonusCooldownReduction += equippedData.BonusCooldownReduction;
                    totalStats.bonusDamageReduction += equippedData.BonusDamageReduction;
                }
            }

            return totalStats;
        }

        private void UpdatePlayerStats()
        {
            if (PlayerBehavior.Player != null)
            {
                PlayerBehavior.Player.RecalculateStatsFromEquipment();
            }
        }
    }
}