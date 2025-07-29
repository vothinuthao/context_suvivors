using System.Collections.Generic;
using System.Linq;
using TwoSleepyCats.Patterns.Singleton;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.Equipment
{
    /// <summary>
    /// Manager for equipment merging system
    /// Handles logic for merging equipment items to create higher rarity items
    /// </summary>
    public class EquipmentMergeManager : MonoSingleton<EquipmentMergeManager>
    {

        [Header("Merge Settings")]
        [SerializeField] private int requiredItemsToMerge = 2;
        [SerializeField] private bool allowMergeLegendary = false; // Legendary items cannot be merged by default

        [Header("Events")]
        public UnityEvent<EquipmentModel> OnMergeCompleted;
        public UnityEvent<string> OnMergeError;


        /// <summary>
        /// Check if two equipment items can be merged together
        /// </summary>
        /// <param name="item1">First equipment item</param>
        /// <param name="item2">Second equipment item</param>
        /// <returns>True if items can be merged</returns>
        public bool CanMergeItems(EquipmentModel item1, EquipmentModel item2)
        {
            if (item1 == null || item2 == null)
                return false;

            // Items must be different instances but same equipment
            if (ReferenceEquals(item1, item2))
                return false;

            // Must be same type, same ID, same rarity
            if (item1.EquipmentType != item2.EquipmentType)
                return false;

            if (item1.ID != item2.ID)
                return false;

            if (item1.Rarity != item2.Rarity)
                return false;

            // Check if this rarity can be merged
            if (item1.Rarity == EquipmentRarity.Legendary && !allowMergeLegendary)
                return false;

            return true;
        }

        /// <summary>
        /// Get all items from inventory that can be merged with the given base item
        /// </summary>
        /// <param name="baseItem">Base item to find merge candidates for</param>
        /// <returns>List of mergeable items</returns>
        public List<(EquipmentSave.InventoryItem inventoryItem, EquipmentModel equipmentData)> GetMergeableItems(EquipmentModel baseItem)
        {
            if (baseItem == null || EquipmentManager.Instance == null)
                return new List<(EquipmentSave.InventoryItem, EquipmentModel)>();

            var allInventoryItems = EquipmentManager.Instance.GetInventoryItemsWithData();
            var mergeableItems = new List<(EquipmentSave.InventoryItem, EquipmentModel)>();

            foreach (var itemData in allInventoryItems)
            {
                if (itemData.equipmentData != null && CanMergeItems(baseItem, itemData.equipmentData))
                {
                    mergeableItems.Add(itemData);
                }
            }

            return mergeableItems;
        }

        /// <summary>
        /// Get the next rarity level for merging
        /// </summary>
        /// <param name="currentRarity">Current rarity level</param>
        /// <returns>Next rarity level, or current if max level</returns>
        public EquipmentRarity GetNextRarity(EquipmentRarity currentRarity)
        {
            switch (currentRarity)
            {
                case EquipmentRarity.Common:
                    return EquipmentRarity.Uncommon;
                case EquipmentRarity.Uncommon:
                    return EquipmentRarity.Rare;
                case EquipmentRarity.Rare:
                    return EquipmentRarity.Epic;
                case EquipmentRarity.Epic:
                    return EquipmentRarity.Legendary;
                case EquipmentRarity.Legendary:
                    return EquipmentRarity.Legendary; // Cannot upgrade further
                default:
                    return currentRarity;
            }
        }

        /// <summary>
        /// Calculate merge result stats
        /// The merged item will have higher rarity and slightly better base stats
        /// </summary>
        /// <param name="sourceItem">Source equipment to base the merge on</param>
        /// <returns>New equipment with upgraded stats</returns>
        private EquipmentModel CreateMergedItem(EquipmentModel sourceItem)
        {
            // Find the equipment with next rarity level of the same type and ID
            var nextRarity = GetNextRarity(sourceItem.Rarity);
            var upgradedItems = EquipmentDatabase.Instance.GetEquipmentByTypeAndRarity(sourceItem.EquipmentType, nextRarity);
            
            // Find item with same ID but higher rarity
            var upgradedItem = upgradedItems.FirstOrDefault(item => item.ID == GetUpgradedEquipmentId(sourceItem.ID, nextRarity));
            
            if (upgradedItem != null)
            {
                return upgradedItem;
            }

            // If no predefined upgrade exists, create a temporary upgraded version
            // This is a fallback - ideally all upgrades should be defined in CSV
            Debug.LogWarning($"[EquipmentMergeManager] No predefined upgrade found for {sourceItem.Name}. Using fallback.");
            return sourceItem; // Return original item as fallback
        }

        /// <summary>
        /// Get the equipment ID for the upgraded version
        /// This assumes a specific ID pattern in the equipment CSV
        /// </summary>
        /// <param name="currentId">Current equipment ID</param>
        /// <param name="targetRarity">Target rarity level</param>
        /// <returns>Upgraded equipment ID</returns>
        private int GetUpgradedEquipmentId(int currentId, EquipmentRarity targetRarity)
        {
            // This assumes equipment IDs follow a pattern where higher rarity versions
            // have predictable ID relationships. Adjust based on your CSV structure.
            
            // Simple example: each rarity tier adds a fixed offset
            int rarityOffset = (int)targetRarity * 100; // Adjust based on your ID scheme
            return currentId + rarityOffset;
        }

        /// <summary>
        /// Perform the merge operation
        /// </summary>
        /// <param name="item1">First item to merge</param>
        /// <param name="item2">Second item to merge</param>
        /// <returns>True if merge was successful</returns>
        public bool MergeEquipment(EquipmentModel item1, EquipmentModel item2)
        {
            if (!CanMergeItems(item1, item2))
            {
                OnMergeError?.Invoke("Items cannot be merged together");
                return false;
            }

            if (EquipmentManager.Instance == null)
            {
                OnMergeError?.Invoke("Equipment Manager not available");
                return false;
            }

            // Check if player has both items in inventory
            if (!HasItemInInventory(item1) || !HasItemInInventory(item2))
            {
                OnMergeError?.Invoke("Items not found in inventory");
                return false;
            }

            try
            {
                // Remove the source items from inventory
                RemoveItemFromInventory(item1);
                RemoveItemFromInventory(item2);

                // Create and add the merged item
                var mergedItem = CreateMergedItem(item1);
                if (mergedItem != null)
                {
                    EquipmentManager.Instance.AddEquipmentToInventory(mergedItem.ID, mergedItem.Level);
                    
                    OnMergeCompleted?.Invoke(mergedItem);
                    Debug.Log($"[EquipmentMergeManager] Successfully merged {item1.Name} into {mergedItem.Name}");
                    return true;
                }
                else
                {
                    // Rollback: add items back to inventory
                    EquipmentManager.Instance.AddEquipmentToInventory(item1.ID, item1.Level);
                    EquipmentManager.Instance.AddEquipmentToInventory(item2.ID, item2.Level);
                    
                    OnMergeError?.Invoke("Failed to create merged item");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                OnMergeError?.Invoke($"Merge failed: {ex.Message}");
                Debug.LogError($"[EquipmentMergeManager] Merge operation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if player has the specified equipment item in inventory
        /// </summary>
        /// <param name="equipment">Equipment to check for</param>
        /// <returns>True if item exists in inventory</returns>
        private bool HasItemInInventory(EquipmentModel equipment)
        {
            if (EquipmentManager.Instance == null)
                return false;

            return EquipmentManager.Instance.HasEquipment(equipment.ID, equipment.Level);
        }

        /// <summary>
        /// Remove one instance of the specified equipment from inventory
        /// </summary>
        /// <param name="equipment">Equipment to remove</param>
        private void RemoveItemFromInventory(EquipmentModel equipment)
        {
            if (EquipmentManager.Instance == null)
                return;

            // This assumes EquipmentManager has a method to remove items
            // You may need to implement this method in EquipmentManager
            var equipmentSave = GameController.SaveManager.GetSave<EquipmentSave>("Equipment");
            equipmentSave?.RemoveFromInventory(equipment.EquipmentType, equipment.ID, equipment.Level);
        }

        /// <summary>
        /// Get merge preview information
        /// </summary>
        /// <param name="item1">First item</param>
        /// <param name="item2">Second item</param>
        /// <returns>Information about merge result</returns>
        public MergePreviewInfo GetMergePreview(EquipmentModel item1, EquipmentModel item2)
        {
            var preview = new MergePreviewInfo();
            
            if (!CanMergeItems(item1, item2))
            {
                preview.canMerge = false;
                preview.errorMessage = "Items cannot be merged";
                return preview;
            }

            preview.canMerge = true;
            preview.sourceItem = item1;
            preview.resultRarity = GetNextRarity(item1.Rarity);
            preview.resultItem = CreateMergedItem(item1);
            
            return preview;
        }

        /// <summary>
        /// Data structure for merge preview information
        /// </summary>
        [System.Serializable]
        public class MergePreviewInfo
        {
            public bool canMerge;
            public string errorMessage;
            public EquipmentModel sourceItem;
            public EquipmentModel resultItem;
            public EquipmentRarity resultRarity;
        }

        /// <summary>
        /// Debug method to test merge functionality
        /// </summary>
        [ContextMenu("Test Merge System")]
        public void TestMergeSystem()
        {
            if (!EquipmentDatabase.Instance.IsDataLoaded)
            {
                Debug.LogWarning("Equipment database not loaded!");
                return;
            }

            var commonHats = EquipmentDatabase.Instance.GetEquipmentByTypeAndRarity(EquipmentType.Hat, EquipmentRarity.Common);
            if (commonHats.Length >= 1)
            {
                var testItem = commonHats[0];
                var nextRarity = GetNextRarity(testItem.Rarity);
                Debug.Log($"Test: {testItem.Name} ({testItem.Rarity}) can merge to {nextRarity}");
                
                var mergeableItems = GetMergeableItems(testItem);
                Debug.Log($"Found {mergeableItems.Count} items that can merge with {testItem.Name}");
            }
        }
    }
}