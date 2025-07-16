using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OctoberStudio.Save;

namespace OctoberStudio.Equipment
{
    /// <summary>
    /// Debug utilities for equipment system
    /// </summary>
    public static class EquipmentDebugUtilities
    {
        /// <summary>
        /// Generate a comprehensive report of the current equipment state
        /// </summary>
        public static string GenerateSystemReport()
        {
            var report = "=== EQUIPMENT SYSTEM REPORT ===\n\n";
            
            // Database Status
            report += "DATABASE STATUS:\n";
            if (EquipmentDatabase.Instance != null)
            {
                report += $"- Status: {(EquipmentDatabase.Instance.IsDataLoaded ? "LOADED" : "NOT LOADED")}\n";
                report += $"- Total Equipment: {EquipmentDatabase.Instance.TotalEquipmentCount}\n";
                
                if (EquipmentDatabase.Instance.IsDataLoaded)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        var type = (EquipmentType)i;
                        var count = EquipmentDatabase.Instance.GetEquipmentCountByType(type);
                        report += $"- {type}: {count} items\n";
                    }
                }
            }
            else
            {
                report += "- Database Instance: NULL\n";
            }

            report += "\n";

            // Manager Status
            report += "MANAGER STATUS:\n";
            if (EquipmentManager.Instance != null)
            {
                report += "- Manager Instance: ACTIVE\n";
                
                var equipmentSave = GameController.SaveManager?.GetSave<EquipmentSave>("Equipment");
                if (equipmentSave != null)
                {
                    report += $"- Save Data: LOADED\n";
                    report += $"- Inventory Items: {equipmentSave.inventory.Count}\n";
                    
                    // Equipped items
                    int equippedCount = 0;
                    for (int i = 0; i < 6; i++)
                    {
                        var equipped = equipmentSave.GetEquippedItem((EquipmentType)i);
                        if (equipped.equipmentId != -1)
                            equippedCount++;
                    }
                    report += $"- Equipped Items: {equippedCount}/6\n";
                }
                else
                {
                    report += "- Save Data: NULL\n";
                }
            }
            else
            {
                report += "- Manager Instance: NULL\n";
            }

            report += "\n";

            // UID System Status
            report += "UID SYSTEM STATUS:\n";
            var uidStats = UIDGenerator.GetStatistics();
            report += $"- Total UIDs Generated: {uidStats.TotalGenerated}\n";
            report += $"- Cache Size: {uidStats.CacheSize}\n";

            // Validation
            report += "\n";
            report += "VALIDATION:\n";
            var equipmentSaveForValidation = GameController.SaveManager?.GetSave<EquipmentSave>("Equipment");
            if (equipmentSaveForValidation != null)
            {
                var validation = EquipmentMigrationHelper.ValidateSaveData(equipmentSaveForValidation);
                report += validation.GetSummary();
            }
            else
            {
                report += "- Cannot validate: Save data not available\n";
            }

            return report;
        }

        /// <summary>
        /// Find and report any data inconsistencies
        /// </summary>
        public static List<string> FindDataInconsistencies()
        {
            var issues = new List<string>();
            
            var equipmentSave = GameController.SaveManager?.GetSave<EquipmentSave>("Equipment");
            if (equipmentSave == null)
            {
                issues.Add("EquipmentSave is null - cannot check for inconsistencies");
                return issues;
            }

            // Check for equipped items without corresponding inventory items
            for (int i = 0; i < 6; i++)
            {
                var equipped = equipmentSave.GetEquippedItem((EquipmentType)i);
                if (equipped.equipmentId != -1 && !string.IsNullOrEmpty(equipped.uid))
                {
                    var inventoryItem = equipmentSave.GetItemByUID(equipped.uid);
                    if (inventoryItem == null)
                    {
                        issues.Add($"Equipped item {equipped.equipmentType} has UID {equipped.uid} but no corresponding inventory item");
                    }
                }
            }

            // Check for duplicate UIDs
            var allUIDs = new List<string>();
            allUIDs.AddRange(equipmentSave.inventory.Where(i => !string.IsNullOrEmpty(i.uid)).Select(i => i.uid));
            allUIDs.AddRange(equipmentSave.equippedItems.Where(e => !string.IsNullOrEmpty(e.uid)).Select(e => e.uid));

            var duplicates = allUIDs.GroupBy(uid => uid).Where(g => g.Count() > 1);
            foreach (var duplicate in duplicates)
            {
                issues.Add($"Duplicate UID found: {duplicate.Key} (appears {duplicate.Count()} times)");
            }

            // Check for invalid UIDs
            foreach (var item in equipmentSave.inventory)
            {
                if (!UIDGenerator.IsValidUID(item.uid))
                {
                    issues.Add($"Invalid UID in inventory: {item.uid} for item {item.equipmentType} {item.equipmentId}");
                }
            }

            // Check for items referencing non-existent equipment
            if (EquipmentDatabase.Instance != null && EquipmentDatabase.Instance.IsDataLoaded)
            {
                foreach (var item in equipmentSave.inventory)
                {
                    var equipmentData = EquipmentDatabase.Instance.GetEquipmentByGlobalId(item.equipmentId);
                    if (equipmentData == null)
                    {
                        issues.Add($"Inventory item references non-existent equipment ID: {item.equipmentId}");
                    }
                    else if (equipmentData.EquipmentType != item.equipmentType)
                    {
                        issues.Add($"Equipment type mismatch: Item says {item.equipmentType}, database says {equipmentData.EquipmentType} for ID {item.equipmentId}");
                    }
                }
            }

            return issues;
        }

        /// <summary>
        /// Create test equipment for debugging
        /// </summary>
        public static void CreateTestEquipment()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("CreateTestEquipment can only be called in Play Mode");
                return;
            }

            if (EquipmentManager.Instance == null)
            {
                Debug.LogError("EquipmentManager not found");
                return;
            }

            if (!EquipmentDatabase.Instance.IsDataLoaded)
            {
                Debug.LogError("Equipment database not loaded");
                return;
            }

            Debug.Log("Creating test equipment...");

            // Add one item of each type and rarity
            var addedItems = new List<string>();

            for (int typeIndex = 0; typeIndex < 6; typeIndex++)
            {
                var equipmentType = (EquipmentType)typeIndex;
                var equipmentsByType = EquipmentDatabase.Instance.GetEquipmentsByType(equipmentType);

                for (int rarityIndex = 0; rarityIndex < 5; rarityIndex++)
                {
                    var rarity = (EquipmentRarity)rarityIndex;
                    var equipmentOfRarity = equipmentsByType.Where(e => e.Rarity == rarity).ToArray();

                    if (equipmentOfRarity.Length > 0)
                    {
                        var equipment = equipmentOfRarity[0]; // Take first available
                        var item = EquipmentManager.Instance.AddEquipmentToInventory(equipment.ID, 1);
                        if (item != null)
                        {
                            addedItems.Add($"{equipment.Name} (UID: {item.uid})");
                        }
                    }
                }
            }

            Debug.Log($"Created {addedItems.Count} test equipment items:");
            foreach (var item in addedItems)
            {
                Debug.Log($"- {item}");
            }
        }

        /// <summary>
        /// Clear all equipment data (for testing)
        /// </summary>
        public static void ClearAllEquipment()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("ClearAllEquipment can only be called in Play Mode");
                return;
            }

            var equipmentSave = GameController.SaveManager?.GetSave<EquipmentSave>("Equipment");
            if (equipmentSave == null)
            {
                Debug.LogError("EquipmentSave not found");
                return;
            }

            equipmentSave.Clear();
            GameController.SaveManager?.Save(false);

            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.OnInventoryChanged?.Invoke();
                for (int i = 0; i < 6; i++)
                {
                    EquipmentManager.Instance.OnEquipmentChanged?.Invoke((EquipmentType)i);
                }
            }

            Debug.Log("All equipment data cleared");
        }

        /// <summary>
        /// Export equipment data to JSON (for backup/analysis)
        /// </summary>
        public static string ExportEquipmentData()
        {
            var equipmentSave = GameController.SaveManager?.GetSave<EquipmentSave>("Equipment");
            if (equipmentSave == null)
            {
                return "Error: EquipmentSave not found";
            }

            try
            {
                var exportData = new EquipmentExportData
                {
                    ExportDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    TotalInventoryItems = equipmentSave.inventory.Count,
                    EquippedItems = equipmentSave.equippedItems.ToList(),
                    InventoryItems = equipmentSave.inventory.ToList(),
                    UIDStatistics = UIDGenerator.GetStatistics()
                };

                return JsonUtility.ToJson(exportData, true);
            }
            catch (System.Exception ex)
            {
                return $"Error exporting data: {ex.Message}";
            }
        }

        /// <summary>
        /// Log detailed inventory information
        /// </summary>
        public static void LogDetailedInventory()
        {
            var equipmentSave = GameController.SaveManager?.GetSave<EquipmentSave>("Equipment");
            if (equipmentSave == null)
            {
                Debug.LogError("EquipmentSave not found");
                return;
            }

            Debug.Log("=== DETAILED INVENTORY LOG ===");
            Debug.Log($"Total Items: {equipmentSave.inventory.Count}");

            // Group by equipment type
            var groupedItems = equipmentSave.inventory.GroupBy(i => i.equipmentType);
            
            foreach (var group in groupedItems.OrderBy(g => g.Key))
            {
                Debug.Log($"\n{group.Key} ({group.Count()} items):");
                
                foreach (var item in group.OrderBy(i => i.equipmentId).ThenBy(i => i.level))
                {
                    var equipment = EquipmentDatabase.Instance?.GetEquipmentByGlobalId(item.equipmentId);
                    var name = equipment?.Name ?? "Unknown";
                    var isEquipped = equipmentSave.IsItemEquipped(item.uid);
                    var equippedText = isEquipped ? " [EQUIPPED]" : "";
                    
                    Debug.Log($"  {name} Lv.{item.level} - UID: {item.uid} - Created: {item.createdAt:yyyy-MM-dd HH:mm}{equippedText}");
                }
            }

            Debug.Log("\n=== EQUIPPED ITEMS ===");
            for (int i = 0; i < 6; i++)
            {
                var type = (EquipmentType)i;
                var equipped = equipmentSave.GetEquippedItem(type);
                
                if (equipped.equipmentId != -1)
                {
                    var equipment = EquipmentDatabase.Instance?.GetEquipmentByGlobalId(equipped.equipmentId);
                    var name = equipment?.Name ?? "Unknown";
                    Debug.Log($"  {type}: {name} Lv.{equipped.level} - UID: {equipped.uid}");
                }
                else
                {
                    Debug.Log($"  {type}: [Empty]");
                }
            }
        }

        /// <summary>
        /// Data structure for equipment export
        /// </summary>
        [System.Serializable]
        public class EquipmentExportData
        {
            public string ExportDate;
            public int TotalInventoryItems;
            public List<EquipmentSave.EquippedItem> EquippedItems;
            public List<EquipmentSave.InventoryItem> InventoryItems;
            public UIDGenerator.UIDStatistics UIDStatistics;
        }

        /// <summary>
        /// Auto-fix common issues
        /// </summary>
        public static int AutoFixIssues()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("AutoFixIssues can only be called in Play Mode");
                return 0;
            }

            var equipmentSave = GameController.SaveManager?.GetSave<EquipmentSave>("Equipment");
            if (equipmentSave == null)
            {
                Debug.LogError("EquipmentSave not found");
                return 0;
            }

            int fixedCount = 0;

            // Fix missing UIDs
            foreach (var item in equipmentSave.inventory)
            {
                if (string.IsNullOrEmpty(item.uid))
                {
                    item.uid = UIDGenerator.GenerateInventoryItemUID();
                    fixedCount++;
                    Debug.Log($"Generated UID for inventory item: {item.equipmentType} {item.equipmentId}");
                }
            }

            // Fix equipped items without UIDs
            for (int i = 0; i < 6; i++)
            {
                var equipped = equipmentSave.equippedItems[i];
                if (equipped != null && equipped.equipmentId != -1 && string.IsNullOrEmpty(equipped.uid))
                {
                    // Try to find matching inventory item
                    var matchingItem = equipmentSave.inventory.FirstOrDefault(inv =>
                        inv.equipmentType == equipped.equipmentType &&
                        inv.equipmentId == equipped.equipmentId &&
                        inv.level == equipped.level);

                    if (matchingItem != null)
                    {
                        equipped.uid = matchingItem.uid;
                    }
                    else
                    {
                        // Create new inventory item
                        var newItem = new EquipmentSave.InventoryItem(equipped.equipmentType, equipped.equipmentId, equipped.level);
                        equipped.uid = newItem.uid;
                        // Note: Don't add to inventory as it's equipped
                    }
                    
                    fixedCount++;
                    Debug.Log($"Fixed UID for equipped item: {equipped.equipmentType}");
                }
            }

            // Clean up orphaned equipped items
            fixedCount += EquipmentMigrationHelper.CleanupOrphanedEquippedItems(equipmentSave);

            if (fixedCount > 0)
            {
                equipmentSave.ForceSync();
                GameController.SaveManager?.Save(false);
                Debug.Log($"Auto-fixed {fixedCount} issues");
            }
            else
            {
                Debug.Log("No issues found to fix");
            }

            return fixedCount;
        }

        /// <summary>
        /// Benchmark UID generation performance
        /// </summary>
        public static void BenchmarkUIDGeneration(int count = 10000)
        {
            Debug.Log($"Benchmarking UID generation ({count} UIDs)...");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < count; i++)
            {
                UIDGenerator.GenerateUID();
            }
            
            stopwatch.Stop();
            
            var avgTime = stopwatch.ElapsedMilliseconds / (float)count;
            Debug.Log($"Generated {count} UIDs in {stopwatch.ElapsedMilliseconds}ms (avg: {avgTime:F3}ms per UID)");
        }
    }
}