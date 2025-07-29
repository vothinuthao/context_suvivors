#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using OctoberStudio.Equipment;
using OctoberStudio.Save;
using System.Collections.Generic;
using System.Linq;

namespace OctoberStudio.Equipment.Tools
{
    public class EquipmentEditorTool : EditorWindow
    {
        private EquipmentDatabase database;
        private EquipmentSave equipmentSave;
        
        // UI Fields
        private int directEquipmentId = 0;
        private int level = 1;
        private int count = 1;
        
        // Display options
        private Vector2 scrollPosition;
        private bool showRawJSON = false;
        private bool showDetailedLog = true;
        
        // Status
        private string lastActionResult = "";

        [MenuItem("Tools/Astral Frontier/Equipment Debug Tool (Simple)")]
        public static void ShowWindow()
        {
            var window = GetWindow<EquipmentEditorTool>("Equipment Debug (Simple)");
            window.minSize = new Vector2(500, 600);
        }

        private void OnEnable()
        {
            RefreshData();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Equipment Debug Tool (Simplified)", EditorStyles.largeLabel);
            EditorGUILayout.Space();

            // Check if game is running
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Tool requires Play Mode to access save data.", MessageType.Info);
                if (GUILayout.Button("Enter Play Mode"))
                {
                    EditorApplication.isPlaying = true;
                }
                return;
            }

            // Refresh button
            if (GUILayout.Button("Refresh Data", GUILayout.Height(30)))
            {
                RefreshData();
            }

            EditorGUILayout.Space();

            // Status Section
            DrawStatusSection();

            EditorGUILayout.Space();

            // Main Actions
            if (database != null && database.IsDataLoaded && equipmentSave != null)
            {
                DrawMainActionsSection();
            }
            else
            {
                EditorGUILayout.HelpBox("Database or Save not ready. Check status above.", MessageType.Warning);
            }

            EditorGUILayout.Space();

            // Show result of last action
            if (!string.IsNullOrEmpty(lastActionResult))
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Last Action Result:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(lastActionResult, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            // Display current data
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawCurrentDataSection();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // Debug options
            showDetailedLog = EditorGUILayout.Toggle("Show Detailed Logs", showDetailedLog);
            showRawJSON = EditorGUILayout.Toggle("Show Raw JSON", showRawJSON);
        }

        private void DrawStatusSection()
        {
            EditorGUILayout.LabelField("System Status", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Game state
            EditorGUILayout.LabelField($"Game Playing: {Application.isPlaying}");
            
            // SaveManager status
            bool saveManagerReady = GameController.SaveManager != null;
            EditorGUILayout.LabelField($"SaveManager: {(saveManagerReady ? "Ready" : "Not Found")}", 
                saveManagerReady ? EditorStyles.label : EditorStyles.centeredGreyMiniLabel);
            
            // Database status
            bool databaseReady = database != null && database.IsDataLoaded;
            EditorGUILayout.LabelField($"Database: {(databaseReady ? $"Ready ({database.TotalEquipmentCount} items)" : "Not Ready")}", 
                databaseReady ? EditorStyles.label : EditorStyles.centeredGreyMiniLabel);
            
            // Save data status
            bool saveDataReady = equipmentSave != null;
            EditorGUILayout.LabelField($"Save Data: {(saveDataReady ? $"Ready ({equipmentSave.inventory.Count} items)" : "Not Found")}", 
                saveDataReady ? EditorStyles.label : EditorStyles.centeredGreyMiniLabel);
            
            // EquipmentManager status
            bool managerReady = EquipmentManager.Instance != null;
            EditorGUILayout.LabelField($"Equipment Manager: {(managerReady ? "Ready" : "Not Found")}", 
                managerReady ? EditorStyles.label : EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawMainActionsSection()
        {
            EditorGUILayout.LabelField("Main Actions", EditorStyles.boldLabel);
            
            // Add Item by ID Section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Add Equipment by ID", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            directEquipmentId = EditorGUILayout.IntField("Equipment ID", directEquipmentId);
            
            // Show equipment info if ID is valid
            if (directEquipmentId > 0 && database != null)
            {
                var equipment = database.GetEquipmentByGlobalId(directEquipmentId);
                if (equipment != null)
                {
                    EditorGUILayout.LabelField($"({equipment.EquipmentType}: {equipment.Name})", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("(Invalid ID)", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            level = EditorGUILayout.IntSlider("Level", level, 1, 10);
            count = EditorGUILayout.IntSlider("Count", count, 1, 10);
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Add Equipment by ID", GUILayout.Height(30)))
            {
                AddEquipmentById(directEquipmentId, level, count);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Bulk Actions
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Bulk Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add All Equipment from CSV", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Add All Equipment", 
                    "This will add ALL equipment from the CSV to your inventory. Continue?", 
                    "Yes", "Cancel"))
                {
                    AddAllEquipment();
                }
            }
            
            if (GUILayout.Button("Clear All Equipment", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Clear All Equipment", 
                    "This will remove all equipped and inventory items. Continue?", 
                    "Yes", "Cancel"))
                {
                    ClearAllEquipment();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Save Actions
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Save Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Force Save Data", GUILayout.Height(30)))
            {
                ForceSaveData();
            }
            
            if (GUILayout.Button("Validate & Fix Data", GUILayout.Height(30)))
            {
                ValidateAndFixData();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawCurrentDataSection()
        {
            EditorGUILayout.LabelField("Current Equipment Data", EditorStyles.boldLabel);
            
            if (equipmentSave == null)
            {
                EditorGUILayout.LabelField("No save data available");
                return;
            }
            
            // Summary
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total Inventory Items: {equipmentSave.inventory.Count}");
            
            int equippedCount = 0;
            for (int i = 0; i < 6; i++)
            {
                var equipped = equipmentSave.GetEquippedItem((EquipmentType)i);
                if (equipped.equipmentId != -1) equippedCount++;
            }
            EditorGUILayout.LabelField($"Equipped Items: {equippedCount}/6");
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Equipped Items
            EditorGUILayout.LabelField("Equipped Items", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            for (int i = 0; i < 6; i++)
            {
                var type = (EquipmentType)i;
                var equipped = equipmentSave.GetEquippedItem(type);
                
                if (equipped.equipmentId != -1)
                {
                    var equipment = database?.GetEquipmentByGlobalId(equipped.equipmentId);
                    var name = equipment?.Name ?? "Unknown";
                    EditorGUILayout.LabelField($"{type}: {name} (ID:{equipped.equipmentId}, Lv.{equipped.level}, UID:{equipped.uid})");
                }
                else
                {
                    EditorGUILayout.LabelField($"{type}: [Empty]");
                }
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Inventory Items Summary
            EditorGUILayout.LabelField("Inventory Items", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (equipmentSave.inventory.Count == 0)
            {
                EditorGUILayout.LabelField("Inventory is empty");
            }
            else
            {
                // Group by equipment type
                var groupedItems = equipmentSave.inventory.GroupBy(i => i.equipmentType);
                
                foreach (var group in groupedItems.OrderBy(g => g.Key))
                {
                    EditorGUILayout.LabelField($"{group.Key}: {group.Count()} items");
                    
                    foreach (var item in group.Take(5)) // Show first 5 items
                    {
                        var equipment = database?.GetEquipmentByGlobalId(item.equipmentId);
                        var name = equipment?.Name ?? "Unknown";
                        var isEquipped = equipmentSave.IsItemEquipped(item.uid) ? " [EQUIPPED]" : "";
                        EditorGUILayout.LabelField($"  • {name} Lv.{item.level} (UID:{item.uid?.Substring(0, 8)}...){isEquipped}", EditorStyles.miniLabel);
                    }
                    
                    if (group.Count() > 5)
                    {
                        EditorGUILayout.LabelField($"  ... and {group.Count() - 5} more", EditorStyles.miniLabel);
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
            
            // Raw JSON Data
            if (showRawJSON)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Raw JSON Data", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                var json = JsonUtility.ToJson(equipmentSave, true);
                
                var textStyle = new GUIStyle(EditorStyles.label);
                textStyle.wordWrap = true;
                textStyle.fontSize = 10;
                textStyle.richText = false;
                
                EditorGUILayout.LabelField(json, textStyle);
                
                if (GUILayout.Button("Copy JSON to Clipboard"))
                {
                    EditorGUIUtility.systemCopyBuffer = json;
                    LogAction("JSON copied to clipboard");
                }
                
                EditorGUILayout.EndVertical();
            }
        }

        // ===================== ACTION METHODS =====================

        private void RefreshData()
        {
            LogAction("Refreshing data...");
            
            // Find Database
            database = EquipmentDatabase.Instance;
            if (database == null)
            {
                database = FindObjectOfType<EquipmentDatabase>();
            }
            
            if (database == null)
            {
                LogAction("Database not found! Creating one...");
                GameObject dbGO = new GameObject("EquipmentDatabase (Auto-Created)");
                database = dbGO.AddComponent<EquipmentDatabase>();
            }
            
            // Load database if not loaded
            if (database != null && !database.IsDataLoaded)
            {
                LogAction("Loading database data...");
                database.LoadEquipmentData();
            }
            
            // Get Save Data
            if (Application.isPlaying && GameController.SaveManager != null)
            {
                equipmentSave = GameController.SaveManager.GetSave<EquipmentSave>("Equipment");
                
                if (equipmentSave != null)
                {
                    equipmentSave.Init();
                    LogAction($"Save data loaded. Inventory: {equipmentSave.inventory.Count} items");
                }
                else
                {
                    LogAction("Failed to get save data!");
                }
            }
            else
            {
                LogAction("SaveManager not available!");
            }
        }

        private void AddEquipmentById(int globalId, int itemLevel, int itemCount)
        {
            if (database == null || !database.IsDataLoaded)
            {
                LogAction("ERROR: Database not ready!");
                return;
            }
            
            if (equipmentSave == null)
            {
                LogAction("ERROR: Save data not ready!");
                return;
            }
            
            var equipment = database.GetEquipmentByGlobalId(globalId);
            if (equipment == null)
            {
                LogAction($"ERROR: Equipment with ID {globalId} not found in database!");
                return;
            }
            
            LogAction($"Adding {itemCount}x {equipment.Name} (ID:{globalId}, Lv.{itemLevel})...");
            
            try
            {
                // Add items directly to save data
                var addedItems = new List<EquipmentSave.InventoryItem>();
                
                for (int i = 0; i < itemCount; i++)
                {
                    var newItem = equipmentSave.AddToInventory(equipment.EquipmentType, globalId, itemLevel);
                    addedItems.Add(newItem);
                    LogAction($"  Added item {i+1}: UID={newItem.uid}");
                }
                
                // Force sync to array
                equipmentSave.ForceSync();
                
                // Save data
                ForceSaveData();
                
                // Notify manager if available
                if (EquipmentManager.Instance != null)
                {
                    EquipmentManager.Instance.OnInventoryChanged?.Invoke();
                }
                
                LogAction($"SUCCESS: Added {itemCount}x {equipment.Name}. Total inventory: {equipmentSave.inventory.Count}");
            }
            catch (System.Exception ex)
            {
                LogAction($"ERROR adding equipment: {ex.Message}");
            }
        }

        private void AddAllEquipment()
        {
            if (database == null || !database.IsDataLoaded)
            {
                LogAction("ERROR: Database not ready!");
                return;
            }
            
            LogAction("Adding all equipment from CSV...");
            
            var allEquipment = database.GetAllEquipment();
            int addedCount = 0;
            int failedCount = 0;
            
            foreach (var equipment in allEquipment)
            {
                try
                {
                    var newItem = equipmentSave.AddToInventory(equipment.EquipmentType, equipment.ID, 1);
                    addedCount++;
                    
                    if (showDetailedLog)
                    {
                        LogAction($"  Added: {equipment.Name} (UID: {newItem.uid})");
                    }
                }
                catch (System.Exception ex)
                {
                    failedCount++;
                    LogAction($"  FAILED to add {equipment.Name}: {ex.Message}");
                }
            }
            
            // Force sync and save
            equipmentSave.ForceSync();
            ForceSaveData();
            
            // Notify manager
            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.OnInventoryChanged?.Invoke();
            }
            
            LogAction($"COMPLETED: Added {addedCount} items, {failedCount} failed. Total inventory: {equipmentSave.inventory.Count}");
        }

        private void ClearAllEquipment()
        {
            if (equipmentSave == null)
            {
                LogAction("ERROR: Save data not ready!");
                return;
            }
            
            LogAction("Clearing all equipment...");
            
            try
            {
                equipmentSave.Clear();
                equipmentSave.ForceSync();
                ForceSaveData();
                
                // Notify manager
                if (EquipmentManager.Instance != null)
                {
                    EquipmentManager.Instance.OnInventoryChanged?.Invoke();
                    for (int i = 0; i < 6; i++)
                    {
                        EquipmentManager.Instance.OnEquipmentChanged?.Invoke((EquipmentType)i);
                    }
                }
                
                LogAction("SUCCESS: All equipment cleared");
            }
            catch (System.Exception ex)
            {
                LogAction($"ERROR clearing equipment: {ex.Message}");
            }
        }

        private void ForceSaveData()
        {
            try
            {
                if (equipmentSave != null)
                {
                    equipmentSave.Flush(); // Ensure data is prepared for save
                }
                
                if (GameController.SaveManager != null)
                {
                    GameController.SaveManager.Save(false);
                    LogAction("Data saved successfully");
                }
                else
                {
                    LogAction("WARNING: SaveManager not available - data may not be persisted");
                }
            }
            catch (System.Exception ex)
            {
                LogAction($"ERROR saving data: {ex.Message}");
            }
        }

        private void ValidateAndFixData()
        {
            if (equipmentSave == null)
            {
                LogAction("ERROR: Save data not ready!");
                return;
            }
            
            LogAction("Validating and fixing data...");
            
            try
            {
                int fixedCount = 0;
                
                // Fix missing UIDs in inventory
                foreach (var item in equipmentSave.inventory)
                {
                    if (string.IsNullOrEmpty(item.uid))
                    {
                        item.uid = UIDGenerator.GenerateInventoryItemUID();
                        fixedCount++;
                        LogAction($"  Fixed missing UID for {item.equipmentType} {item.equipmentId}");
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
                            equipped.uid = UIDGenerator.GenerateInventoryItemUID();
                        }
                        
                        fixedCount++;
                        LogAction($"  Fixed equipped item UID for {equipped.equipmentType}");
                    }
                }
                
                if (fixedCount > 0)
                {
                    equipmentSave.ForceSync();
                    ForceSaveData();
                    LogAction($"SUCCESS: Fixed {fixedCount} issues");
                }
                else
                {
                    LogAction("No issues found to fix");
                }
            }
            catch (System.Exception ex)
            {
                LogAction($"ERROR during validation: {ex.Message}");
            }
        }

        private void LogAction(string message)
        {
            lastActionResult = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
            
            if (showDetailedLog)
            {
                Debug.Log($"[EquipmentEditorTool] {message}");
            }
        }
    }
}
#endif