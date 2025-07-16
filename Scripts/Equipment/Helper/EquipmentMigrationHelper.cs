using System.Collections.Generic;
using UnityEngine;
using OctoberStudio.Save;

namespace OctoberStudio.Equipment
{
    /// <summary>
    /// Helper class to migrate old equipment save data to new UID-based system
    /// </summary>
    public static class EquipmentMigrationHelper
    {
        /// <summary>
        /// Migrate old save data (quantity-based) to new save data (UID-based)
        /// </summary>
        public static bool MigrateToUIDSystem(EquipmentSave equipmentSave)
        {
            if (equipmentSave == null)
            {
                Debug.LogError("[EquipmentMigrationHelper] EquipmentSave is null!");
                return false;
            }

            bool migrationPerformed = false;
            var itemsToMigrate = new List<(EquipmentType type, int id, int level, int quantity)>();

            // Check if we have old format data (items with quantity > 1)
            foreach (var item in equipmentSave.inventory)
            {
                // This is a fictional check - in the new system there's no quantity field
                // But if we had old data, we would identify it here
                
                // For now, we'll just ensure all items have valid UIDs
                if (string.IsNullOrEmpty(item.uid))
                {
                    item.uid = UIDGenerator.GenerateInventoryItemUID();
                    migrationPerformed = true;
                    Debug.Log($"[EquipmentMigrationHelper] Generated UID for item: {item.equipmentType} {item.equipmentId}");
                }
            }

            // Ensure equipped items have UIDs
            for (int i = 0; i < equipmentSave.equippedItems.Length; i++)
            {
                var equipped = equipmentSave.equippedItems[i];
                if (equipped != null && equipped.equipmentId != -1 && string.IsNullOrEmpty(equipped.uid))
                {
                    // For equipped items without UID, we need to find matching inventory item
                    // or create a temporary UID
                    var matchingInventoryItem = equipmentSave.inventory.Find(inv => 
                        inv.equipmentType == equipped.equipmentType && 
                        inv.equipmentId == equipped.equipmentId && 
                        inv.level == equipped.level);

                    if (matchingInventoryItem != null)
                    {
                        equipped.uid = matchingInventoryItem.uid;
                    }
                    else
                    {
                        // Create a new inventory item for the equipped item
                        var newItem = new EquipmentSave.InventoryItem(equipped.equipmentType, equipped.equipmentId, equipped.level);
                        equipped.uid = newItem.uid;
                        Debug.Log($"[EquipmentMigrationHelper] Generated UID for equipped item: {equipped.equipmentType} {equipped.equipmentId}");
                    }
                    
                    migrationPerformed = true;
                }
            }

            if (migrationPerformed)
            {
                Debug.Log("[EquipmentMigrationHelper] Migration completed successfully!");
                
                // Force sync to save the changes
                equipmentSave.ForceSync();
                
                // Save the migrated data
                if (GameController.SaveManager != null)
                {
                    GameController.SaveManager.Save(false);
                }
            }
            else
            {
                Debug.Log("[EquipmentMigrationHelper] No migration needed - data is already in correct format");
            }

            return migrationPerformed;
        }

        /// <summary>
        /// Validate that all items have proper UIDs
        /// </summary>
        public static ValidationResult ValidateSaveData(EquipmentSave equipmentSave)
        {
            var result = new ValidationResult();
            
            if (equipmentSave == null)
            {
                result.AddError("EquipmentSave is null");
                return result;
            }

            // Validate inventory items
            var seenUIDs = new HashSet<string>();
            
            foreach (var item in equipmentSave.inventory)
            {
                if (string.IsNullOrEmpty(item.uid))
                {
                    result.AddError($"Inventory item missing UID: {item.equipmentType} {item.equipmentId}");
                }
                else if (!UIDGenerator.IsValidUID(item.uid))
                {
                    result.AddError($"Invalid UID format: {item.uid}");
                }
                else if (seenUIDs.Contains(item.uid))
                {
                    result.AddError($"Duplicate UID found: {item.uid}");
                }
                else
                {
                    seenUIDs.Add(item.uid);
                }
            }

            // Validate equipped items
            for (int i = 0; i < equipmentSave.equippedItems.Length; i++)
            {
                var equipped = equipmentSave.equippedItems[i];
                if (equipped != null && equipped.equipmentId != -1)
                {
                    if (string.IsNullOrEmpty(equipped.uid))
                    {
                        result.AddError($"Equipped item missing UID: {equipped.equipmentType} {equipped.equipmentId}");
                    }
                    else if (!UIDGenerator.IsValidUID(equipped.uid))
                    {
                        result.AddError($"Equipped item has invalid UID: {equipped.uid}");
                    }
                }
            }

            result.TotalInventoryItems = equipmentSave.inventory.Count;
            result.UniqueUIDs = seenUIDs.Count;

            return result;
        }

        /// <summary>
        /// Clean up orphaned equipped items (equipped items not found in inventory)
        /// </summary>
        public static int CleanupOrphanedEquippedItems(EquipmentSave equipmentSave)
        {
            int cleanedCount = 0;
            
            for (int i = 0; i < equipmentSave.equippedItems.Length; i++)
            {
                var equipped = equipmentSave.equippedItems[i];
                if (equipped != null && equipped.equipmentId != -1 && !string.IsNullOrEmpty(equipped.uid))
                {
                    // Check if this equipped item exists in inventory
                    var inventoryItem = equipmentSave.GetItemByUID(equipped.uid);
                    if (inventoryItem == null)
                    {
                        Debug.LogWarning($"[EquipmentMigrationHelper] Orphaned equipped item found: {equipped.equipmentType} UID:{equipped.uid}");
                        
                        // Create inventory item for the orphaned equipped item
                        var newInventoryItem = new EquipmentSave.InventoryItem(equipped.equipmentType, equipped.equipmentId, equipped.level);
                        newInventoryItem.uid = equipped.uid; // Use the same UID
                        equipmentSave.inventory.Add(newInventoryItem);
                        
                        cleanedCount++;
                    }
                }
            }

            if (cleanedCount > 0)
            {
                equipmentSave.ForceSync();
                Debug.Log($"[EquipmentMigrationHelper] Created {cleanedCount} inventory items for orphaned equipped items");
            }

            return cleanedCount;
        }

        /// <summary>
        /// Generate backup of save data before migration
        /// </summary>
        public static void CreateBackup(EquipmentSave equipmentSave)
        {
            if (equipmentSave == null) return;

            try
            {
                var backupData = JsonUtility.ToJson(equipmentSave, true);
                var timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupKey = $"EquipmentBackup_{timestamp}";
                
                PlayerPrefs.SetString(backupKey, backupData);
                PlayerPrefs.Save();
                
                Debug.Log($"[EquipmentMigrationHelper] Backup created: {backupKey}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EquipmentMigrationHelper] Failed to create backup: {ex.Message}");
            }
        }

        /// <summary>
        /// Validation result structure
        /// </summary>
        public class ValidationResult
        {
            public List<string> Errors { get; } = new List<string>();
            public List<string> Warnings { get; } = new List<string>();
            public int TotalInventoryItems { get; set; }
            public int UniqueUIDs { get; set; }

            public bool IsValid => Errors.Count == 0;

            public void AddError(string error)
            {
                Errors.Add(error);
                Debug.LogError($"[EquipmentMigrationHelper] Validation Error: {error}");
            }

            public void AddWarning(string warning)
            {
                Warnings.Add(warning);
                Debug.LogWarning($"[EquipmentMigrationHelper] Validation Warning: {warning}");
            }

            public string GetSummary()
            {
                var summary = $"Validation Summary:\n";
                summary += $"- Total Items: {TotalInventoryItems}\n";
                summary += $"- Unique UIDs: {UniqueUIDs}\n";
                summary += $"- Errors: {Errors.Count}\n";
                summary += $"- Warnings: {Warnings.Count}\n";
                summary += $"- Status: {(IsValid ? "VALID" : "INVALID")}";
                
                return summary;
            }
        }

        /// <summary>
        /// Perform full migration and validation process
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void PerformFullMigration()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[EquipmentMigrationHelper] Migration can only be performed in Play Mode");
                return;
            }

            var equipmentSave = GameController.SaveManager?.GetSave<EquipmentSave>("Equipment");
            if (equipmentSave == null)
            {
                Debug.LogError("[EquipmentMigrationHelper] Could not get EquipmentSave");
                return;
            }

            Debug.Log("[EquipmentMigrationHelper] Starting full migration process...");

            // 1. Create backup
            CreateBackup(equipmentSave);

            // 2. Validate current data
            var preValidation = ValidateSaveData(equipmentSave);
            Debug.Log($"[EquipmentMigrationHelper] Pre-migration validation:\n{preValidation.GetSummary()}");

            // 3. Perform migration
            bool migrated = MigrateToUIDSystem(equipmentSave);

            // 4. Clean up orphaned items
            int cleanedItems = CleanupOrphanedEquippedItems(equipmentSave);

            // 5. Final validation
            var postValidation = ValidateSaveData(equipmentSave);
            Debug.Log($"[EquipmentMigrationHelper] Post-migration validation:\n{postValidation.GetSummary()}");

            // 6. Summary
            Debug.Log($"[EquipmentMigrationHelper] Migration completed!");
            Debug.Log($"- Migration performed: {migrated}");
            Debug.Log($"- Items cleaned: {cleanedItems}");
            Debug.Log($"- Final status: {(postValidation.IsValid ? "SUCCESS" : "FAILED")}");
        }
    }
}