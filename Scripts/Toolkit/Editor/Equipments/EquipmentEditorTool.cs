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
        private EquipmentType selectedType = EquipmentType.Hat;
        private int equipmentId = 0;
        private int directEquipmentId = 0; // For direct ID input
        private int level = 1;
        private int count = 1; // How many individual items to create
        
        // Display options
        private Vector2 scrollPosition;
        private bool showEquippedArray = true;
        private bool showInventoryArray = true;
        private bool showRawData = false;
        private bool showCSVItems = false;
        
        // Equipment names cache
        private Dictionary<EquipmentType, string[]> equipmentNamesCache = new Dictionary<EquipmentType, string[]>();
        private Dictionary<int, EquipmentModel> equipmentByIdCache = new Dictionary<int, EquipmentModel>();
        
        // Search functionality
        private string searchFilter = "";
        private EquipmentRarity rarityFilter = EquipmentRarity.Common;
        private bool useRarityFilter = false;

        [MenuItem("Tools/Astral Frontier/Equipment Debug Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<EquipmentEditorTool>("Equipment Debug");
            window.minSize = new Vector2(600, 800);
        }

        private void OnEnable()
        {
            RefreshEquipmentSave();
            FindEquipmentDatabase();
            RefreshEquipmentNamesCache();
            RefreshEquipmentByIdCache();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Equipment Debug Tool", EditorStyles.largeLabel);
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

            // Refresh data
            if (GUILayout.Button("Refresh Data"))
            {
                RefreshEquipmentSave();
                FindEquipmentDatabase();
                RefreshEquipmentNamesCache();
                RefreshEquipmentByIdCache();
            }

            EditorGUILayout.Space();

            // Database field and system status
            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
            database = (EquipmentDatabase)EditorGUILayout.ObjectField("Equipment Database", database, typeof(EquipmentDatabase), false);

            // Show System Manager status if available
            if (EquipmentSystemManager.Instance != null)
            {
                var systemStatus = EquipmentSystemManager.Instance.GetSystemStatus();
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("System Manager Status:", EditorStyles.boldLabel);
                
                var statusColor = systemStatus.IsHealthy ? Color.green : (systemStatus.IsInitialized ? Color.yellow : Color.red);
                var originalColor = GUI.color;
                GUI.color = statusColor;
                
                EditorGUILayout.LabelField($"Overall Status: {(systemStatus.IsHealthy ? "HEALTHY" : (systemStatus.IsInitialized ? "ISSUES" : "NOT READY"))}");
                GUI.color = originalColor;
                
                EditorGUILayout.LabelField($"Initialized: {systemStatus.IsInitialized}");
                EditorGUILayout.LabelField($"Database Ready: {systemStatus.DatabaseReady}");
                EditorGUILayout.LabelField($"Manager Ready: {systemStatus.ManagerReady}");
                EditorGUILayout.LabelField($"Save Data Ready: {systemStatus.SaveDataReady}");
                EditorGUILayout.LabelField($"Validation Errors: {systemStatus.ValidationErrors}");
                EditorGUILayout.LabelField($"Equipment in DB: {systemStatus.TotalEquipmentInDB}");
                EditorGUILayout.LabelField($"Inventory Items: {systemStatus.TotalInventoryItems}");
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Reinitialize System"))
                {
                    EquipmentSystemManager.Instance.ReinitializeSystem();
                }
                if (GUILayout.Button("Emergency Repair"))
                {
                    EquipmentSystemManager.Instance.EmergencyRepair();
                }
                EditorGUILayout.EndHorizontal();
                
                if (GUILayout.Button("Generate Full System Report"))
                {
                    EquipmentSystemManager.Instance.GenerateSystemReport();
                }
                
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("EquipmentSystemManager not found in scene. Consider adding one for better system management.", MessageType.Info);
                
                if (GUILayout.Button("Create Equipment System Manager"))
                {
                    CreateEquipmentSystemManager();
                }
            }

            // Debug info about database status
            if (database == null)
            {
                EditorGUILayout.HelpBox("Equipment Database not found! Searching...", MessageType.Warning);
                
                // Try to create or find database
                if (GUILayout.Button("Create/Find Equipment Database"))
                {
                    CreateOrFindEquipmentDatabase();
                }
                return;
            }
            else
            {
                // Show database status
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Database Status:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Instance Found: {database != null}");
                EditorGUILayout.LabelField($"Data Loaded: {database.IsDataLoaded}");
                EditorGUILayout.LabelField($"Total Equipment: {database.TotalEquipmentCount}");
                
                if (!database.IsDataLoaded)
                {
                    EditorGUILayout.HelpBox("Database found but data not loaded. Try loading equipment data.", MessageType.Warning);
                    
                    if (GUILayout.Button("Force Load Equipment Data"))
                    {
                        database.LoadEquipmentData();
                    }
                    
                    if (GUILayout.Button("Check CSV File"))
                    {
                        CheckCSVFile();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Database ready!", MessageType.Info);
                }
                EditorGUILayout.EndVertical();
            }

            if (equipmentSave == null)
            {
                EditorGUILayout.HelpBox("Equipment Save not found! Make sure EquipmentManager is in scene.", MessageType.Warning);
                
                if (GUILayout.Button("Find Equipment Manager"))
                {
                    FindEquipmentManager();
                }
                return;
            }

            EditorGUILayout.Space();

            // Add Equipment Section (by Type) - only show if database has data
            if (database != null && database.IsDataLoaded)
            {
                DrawAddEquipmentByTypeSection();

                EditorGUILayout.Space();

                // Add Equipment Section (by Direct ID)
                DrawAddEquipmentByIdSection();

                EditorGUILayout.Space();

                // Bulk Actions Section
                DrawBulkActionsSection();

                EditorGUILayout.Space();

                // Quick Actions
                DrawQuickActionsSection();
            }
            else
            {
                EditorGUILayout.HelpBox("Equipment Database must be loaded before you can add equipment.", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Display current data
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Show equipped items array
            DrawEquippedItemsArray();
            
            EditorGUILayout.Space();
            
            // Show inventory items array
            DrawInventoryItemsArray();

            EditorGUILayout.Space();

            // Show CSV items browser
            if (database != null && database.IsDataLoaded)
            {
                DrawCSVItemsBrowser();
            }

            EditorGUILayout.Space();

            // Show raw data
            if (showRawData)
            {
                DrawRawDataSection();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // Toggle for raw data
            showRawData = EditorGUILayout.Toggle("Show Raw JSON Data", showRawData);
        }

        private void DrawAddEquipmentByTypeSection()
        {
            EditorGUILayout.LabelField("Add Equipment by Type", EditorStyles.boldLabel);
            
            // Equipment Type dropdown
            selectedType = (EquipmentType)EditorGUILayout.EnumPopup("Equipment Type", selectedType);
            
            // Equipment ID dropdown with names
            if (equipmentNamesCache.ContainsKey(selectedType))
            {
                var names = equipmentNamesCache[selectedType];
                if (names.Length > 0)
                {
                    equipmentId = EditorGUILayout.Popup("Equipment", equipmentId, names);
                }
                else
                {
                    EditorGUILayout.LabelField("Equipment", "No equipment found for this type");
                    equipmentId = 0;
                }
            }
            else
            {
                equipmentId = EditorGUILayout.IntField("Equipment ID", equipmentId);
            }
            
            // Level and Count
            level = EditorGUILayout.IntSlider("Level", level, 1, 10);
            count = EditorGUILayout.IntSlider("Count (Individual Items)", count, 1, 20);

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add to Inventory", GUILayout.Height(30)))
            {
                AddEquipmentByType(selectedType, equipmentId, level, count);
            }
            
            if (GUILayout.Button("Add & Equip First", GUILayout.Height(30)))
            {
                var addedItems = AddEquipmentByType(selectedType, equipmentId, level, 1);
                if (addedItems != null && addedItems.Count > 0)
                {
                    EquipItemByUID(addedItems[0].uid);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAddEquipmentByIdSection()
        {
            EditorGUILayout.LabelField("Add Equipment by Global ID", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            directEquipmentId = EditorGUILayout.IntField("Global Equipment ID", directEquipmentId);
            
            // Show equipment info if ID is valid
            if (equipmentByIdCache.ContainsKey(directEquipmentId))
            {
                var equipment = equipmentByIdCache[directEquipmentId];
                EditorGUILayout.LabelField($"({equipment.EquipmentType}: {equipment.Name})", EditorStyles.miniLabel);
            }
            else if (directEquipmentId > 0)
            {
                EditorGUILayout.LabelField("(Invalid ID)", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
            
            // Level and Count for direct ID
            EditorGUILayout.BeginHorizontal();
            level = EditorGUILayout.IntSlider("Level", level, 1, 10);
            count = EditorGUILayout.IntSlider("Count", count, 1, 20);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add by Global ID", GUILayout.Height(30)))
            {
                AddEquipmentByGlobalId(directEquipmentId, level, count);
            }
            
            if (GUILayout.Button("Add & Equip by ID", GUILayout.Height(30)))
            {
                var addedItems = AddEquipmentByGlobalId(directEquipmentId, level, 1);
                if (addedItems != null && addedItems.Count > 0)
                {
                    EquipItemByUID(addedItems[0].uid);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBulkActionsSection()
        {
            EditorGUILayout.LabelField("Bulk Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add All Items from CSV", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Add All Equipment", 
                    "This will add ALL equipment from the CSV to your inventory. Continue?", 
                    "Yes", "Cancel"))
                {
                    AddAllItemsFromCSV();
                }
            }
            
            if (GUILayout.Button("Add All Common Items", GUILayout.Height(30)))
            {
                AddAllItemsByRarity(EquipmentRarity.Common);
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add All Uncommon Items", GUILayout.Height(30)))
            {
                AddAllItemsByRarity(EquipmentRarity.Uncommon);
            }
            
            if (GUILayout.Button("Add All Rare Items", GUILayout.Height(30)))
            {
                AddAllItemsByRarity(EquipmentRarity.Rare);
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add All Epic Items", GUILayout.Height(30)))
            {
                AddAllItemsByRarity(EquipmentRarity.Epic);
            }
            
            if (GUILayout.Button("Add All Legendary Items", GUILayout.Height(30)))
            {
                AddAllItemsByRarity(EquipmentRarity.Legendary);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawQuickActionsSection()
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add Basic Set"))
            {
                AddBasicEquipmentSet();
            }
            
            if (GUILayout.Button("Clear All"))
            {
                if (EditorUtility.DisplayDialog("Clear All Equipment", 
                    "This will remove all equipped and inventory items. Continue?", 
                    "Yes", "Cancel"))
                {
                    ClearAllEquipment();
                }
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Auto Equip Best"))
            {
                AutoEquipBestItems();
            }
            
            if (GUILayout.Button("Unequip All"))
            {
                UnequipAllItems();
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Save Data"))
            {
                SaveEquipmentData();
            }
            
            if (GUILayout.Button("Clear Inventory Only"))
            {
                if (EditorUtility.DisplayDialog("Clear Inventory", 
                    "This will remove all inventory items (equipped items will remain). Continue?", 
                    "Yes", "Cancel"))
                {
                    ClearInventoryOnly();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCSVItemsBrowser()
        {
            showCSVItems = EditorGUILayout.Foldout(showCSVItems, $"CSV Items Browser [{database.TotalEquipmentCount}]", true, EditorStyles.foldoutHeader);
            if (!showCSVItems) return;

            EditorGUI.indentLevel++;
            
            // Search and filter controls
            EditorGUILayout.BeginHorizontal();
            searchFilter = EditorGUILayout.TextField("Search", searchFilter);
            useRarityFilter = EditorGUILayout.Toggle("Filter Rarity", useRarityFilter, GUILayout.Width(100));
            if (useRarityFilter)
            {
                rarityFilter = (EquipmentRarity)EditorGUILayout.EnumPopup(rarityFilter, GUILayout.Width(100));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Get all equipment and apply filters
            var allEquipment = database.GetAllEquipment();
            var filteredEquipment = allEquipment.AsEnumerable();

            if (!string.IsNullOrEmpty(searchFilter))
            {
                filteredEquipment = filteredEquipment.Where(e => 
                    e.Name.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    e.Description.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (useRarityFilter)
            {
                filteredEquipment = filteredEquipment.Where(e => e.Rarity == rarityFilter);
            }

            var filteredList = filteredEquipment.ToArray();

            EditorGUILayout.LabelField($"Showing {filteredList.Length} items", EditorStyles.miniLabel);

            // Display filtered items
            foreach (var equipment in filteredList.Take(20)) // Limit to 20 items for performance
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"ID: {equipment.ID} - {equipment.Name}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Type: {equipment.EquipmentType} | Rarity: {equipment.Rarity}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Stats: {equipment.GetStatsText()}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.BeginVertical(GUILayout.Width(120));
                if (GUILayout.Button("Add to Inventory", GUILayout.Width(110)))
                {
                    AddEquipmentByGlobalId(equipment.ID, 1, 1);
                }
                if (GUILayout.Button("Add & Equip", GUILayout.Width(110)))
                {
                    var addedItems = AddEquipmentByGlobalId(equipment.ID, 1, 1);
                    if (addedItems != null && addedItems.Count > 0)
                    {
                        EquipItemByUID(addedItems[0].uid);
                    }
                }
                if (GUILayout.Button("Add 5x", GUILayout.Width(110)))
                {
                    AddEquipmentByGlobalId(equipment.ID, 1, 5);
                }
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            if (filteredList.Length > 20)
            {
                EditorGUILayout.LabelField($"... and {filteredList.Length - 20} more items (refine your search)", EditorStyles.miniLabel);
            }
            
            EditorGUI.indentLevel--;
        }

        private void DrawEquippedItemsArray()
        {
            showEquippedArray = EditorGUILayout.Foldout(showEquippedArray, $"Equipped Items Array [6]", true, EditorStyles.foldoutHeader);
            if (!showEquippedArray) return;

            EditorGUI.indentLevel++;
            
            if (equipmentSave.equippedItems == null)
            {
                EditorGUILayout.LabelField("equippedItems array is NULL!");
                EditorGUI.indentLevel--;
                return;
            }

            EditorGUILayout.LabelField($"Array Length: {equipmentSave.equippedItems.Length}", EditorStyles.miniLabel);
            
            for (int i = 0; i < equipmentSave.equippedItems.Length; i++)
            {
                var item = equipmentSave.equippedItems[i];
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField($"Index [{i}] - {(EquipmentType)i}", EditorStyles.boldLabel);
                
                if (item == null)
                {
                    EditorGUILayout.LabelField("Item is NULL!", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField($"Type: {item.equipmentType}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"Equipment ID: {item.equipmentId}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"Level: {item.level}", EditorStyles.miniLabel);
                    
                    if (!string.IsNullOrEmpty(item.uid))
                    {
                        EditorGUILayout.LabelField($"UID: {item.uid}", EditorStyles.miniLabel);
                    }
                    
                    // Show equipment name if database available
                    if (database != null && item.equipmentId != -1)
                    {
                        var equipmentData = database.GetEquipmentByGlobalId(item.equipmentId);
                        var name = equipmentData != null ? equipmentData.Name : "Unknown Equipment";
                        EditorGUILayout.LabelField($"Name: {name}", EditorStyles.miniLabel);
                    }
                    else if (item.equipmentId == -1)
                    {
                        EditorGUILayout.LabelField("Status: EMPTY SLOT", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.BeginVertical(GUILayout.Width(80));
                    if (item.equipmentId != -1)
                    {
                        if (GUILayout.Button("Unequip", GUILayout.Width(70)))
                        {
                            UnequipItem(item.equipmentType);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUI.indentLevel--;
        }

        private void DrawInventoryItemsArray()
        {
            showInventoryArray = EditorGUILayout.Foldout(showInventoryArray, $"Inventory Items Array [{(equipmentSave.inventoryItems?.Length ?? 0)}]", true, EditorStyles.foldoutHeader);
            if (!showInventoryArray) return;

            EditorGUI.indentLevel++;
            
            if (equipmentSave.inventoryItems == null)
            {
                EditorGUILayout.LabelField("inventoryItems array is NULL!");
                EditorGUI.indentLevel--;
                return;
            }

            EditorGUILayout.LabelField($"Array Length: {equipmentSave.inventoryItems.Length}", EditorStyles.miniLabel);
            
            if (equipmentSave.inventoryItems.Length == 0)
            {
                EditorGUILayout.LabelField("Inventory is empty", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < equipmentSave.inventoryItems.Length; i++)
                {
                    var item = equipmentSave.inventoryItems[i];
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.LabelField($"Index [{i}]", EditorStyles.boldLabel);
                    
                    if (item == null)
                    {
                        EditorGUILayout.LabelField("Item is NULL!", EditorStyles.miniLabel);
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField($"UID: {item.uid}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"Type: {item.equipmentType}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"Equipment ID: {item.equipmentId}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"Level: {item.level}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"Created: {item.createdAt:yyyy-MM-dd HH:mm}", EditorStyles.miniLabel);
                        
                        // Show equipment name if database available
                        if (database != null)
                        {
                            var equipmentData = database.GetEquipmentByGlobalId(item.equipmentId);
                            var name = equipmentData != null ? equipmentData.Name : "Unknown Equipment";
                            EditorGUILayout.LabelField($"Name: {name}", EditorStyles.miniLabel);
                            
                            // Show if item is currently equipped
                            bool isEquipped = equipmentSave.IsItemEquipped(item.uid);
                            if (isEquipped)
                            {
                                EditorGUILayout.LabelField("Status: EQUIPPED", EditorStyles.miniLabel);
                            }
                        }
                        EditorGUILayout.EndVertical();
                        
                        EditorGUILayout.BeginVertical(GUILayout.Width(80));
                        
                        // Show different buttons based on equipped status
                        bool isItemEquipped = equipmentSave.IsItemEquipped(item.uid);
                        
                        if (!isItemEquipped)
                        {
                            if (GUILayout.Button("Equip", GUILayout.Width(70)))
                            {
                                EquipItemByUID(item.uid);
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Unequip", GUILayout.Width(70)))
                            {
                                UnequipItem(item.equipmentType);
                            }
                        }
                        
                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            if (EditorUtility.DisplayDialog("Remove Item", 
                                $"Remove this item?\nUID: {item.uid}", 
                                "Yes", "Cancel"))
                            {
                                RemoveFromInventory(item.uid);
                            }
                        }
                        EditorGUILayout.EndVertical();
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUILayout.EndVertical();
                }
            }
            
            EditorGUI.indentLevel--;
        }

        private void DrawRawDataSection()
        {
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
                Debug.Log("Equipment save JSON copied to clipboard");
            }
            
            EditorGUILayout.EndVertical();
        }

        private void RefreshEquipmentSave()
        {
            if (Application.isPlaying && GameController.SaveManager != null)
            {
                equipmentSave = GameController.SaveManager.GetSave<EquipmentSave>("Equipment");
        
                if (equipmentSave != null)
                {
                    equipmentSave.Init(); 
                }
            }
        }

        private void FindEquipmentDatabase()
        {
            if (database == null)
            {
                // Try to get from Instance first
                database = EquipmentDatabase.Instance;
                
                // If still null, try to find in scene
                if (database == null)
                {
                    database = FindObjectOfType<EquipmentDatabase>();
                }
                
                // If still null, try to find asset in project
                if (database == null)
                {
                    var guids = AssetDatabase.FindAssets("t:EquipmentDatabase");
                    if (guids.Length > 0)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        var databaseAsset = AssetDatabase.LoadAssetAtPath<EquipmentDatabase>(path);
                        if (databaseAsset != null)
                        {
                            Debug.LogWarning("Found EquipmentDatabase asset but no instance in scene. Database needs to be in scene to work properly.");
                        }
                    }
                }
            }
        }

        private void RefreshEquipmentNamesCache()
        {
            equipmentNamesCache.Clear();
            
            if (database == null || !database.IsDataLoaded) return;

            for (int i = 0; i < 6; i++)
            {
                var equipmentType = (EquipmentType)i;
                var equipments = database.GetEquipmentsByType(equipmentType);
                var names = new string[equipments.Length];
                
                for (int j = 0; j < equipments.Length; j++)
                {
                    names[j] = $"{j}: {equipments[j].Name}";
                }
                
                equipmentNamesCache[equipmentType] = names;
            }
        }

        private void RefreshEquipmentByIdCache()
        {
            equipmentByIdCache.Clear();
            
            if (database == null || !database.IsDataLoaded) return;

            var allEquipment = database.GetAllEquipment();
            foreach (var equipment in allEquipment)
            {
                equipmentByIdCache[equipment.ID] = equipment;
            }
        }

        // ===================== ACTION METHODS =====================

        private List<EquipmentSave.InventoryItem> AddEquipmentByType(EquipmentType type, int localIndex, int level, int count)
        {
            if (database == null || !database.IsDataLoaded)
            {
                Debug.LogError("Database not ready!");
                return null;
            }
            
            var equipments = database.GetEquipmentsByType(type);
            if (localIndex >= 0 && localIndex < equipments.Length)
            {
                var equipment = equipments[localIndex];
                return AddEquipmentByGlobalId(equipment.ID, level, count);
            }
            return null;
        }

        private List<EquipmentSave.InventoryItem> AddEquipmentByGlobalId(int globalId, int level, int count)
        {
            if (database == null || !database.IsDataLoaded)
            {
                Debug.LogError("Database not ready!");
                return null;
            }
            
            var equipment = database.GetEquipmentByGlobalId(globalId);
            if (equipment == null)
            {
                Debug.LogError($"Equipment with ID {globalId} not found in database!");
                return null;
            }

            if (Application.isPlaying && GameController.EquipmentManager != null)
            {
                var addedItems = GameController.EquipmentManager.AddMultipleEquipmentToInventory(globalId, level, count);
                GameController.SaveManager?.Save(false);
                Debug.Log($"Added {count}x {equipment.Name} (ID:{globalId}, Lv.{level}) to inventory");
                return addedItems;
            }
            else if (equipmentSave != null)
            {
                var addedItems = equipmentSave.AddMultipleToInventory(equipment.EquipmentType, globalId, level, count);
                GameController.SaveManager?.Save(false);
                Debug.Log($"Added {count}x {equipment.Name} (ID:{globalId}, Lv.{level}) to inventory (direct save)");
                return addedItems;
            }
            return null;
        }

        private void AddAllItemsFromCSV()
        {
            if (database == null || !database.IsDataLoaded)
            {
                Debug.LogError("Database not ready!");
                return;
            }

            var allEquipment = database.GetAllEquipment();
            int addedCount = 0;

            foreach (var equipment in allEquipment)
            {
                var addedItems = AddEquipmentByGlobalId(equipment.ID, 1, 1);
                if (addedItems != null && addedItems.Count > 0)
                {
                    addedCount++;
                }
            }

            Debug.Log($"Added {addedCount} unique items from CSV to inventory");
        }

        private void AddAllItemsByRarity(EquipmentRarity rarity)
        {
            if (database == null || !database.IsDataLoaded)
            {
                Debug.LogError("Database not ready!");
                return;
            }

            var equipmentByRarity = database.GetEquipmentByRarity(rarity);
            int addedCount = 0;

            foreach (var equipment in equipmentByRarity)
            {
                var addedItems = AddEquipmentByGlobalId(equipment.ID, 1, 1);
                if (addedItems != null && addedItems.Count > 0)
                {
                    addedCount++;
                }
            }

            Debug.Log($"Added {addedCount} {rarity} items to inventory");
        }

        private void AddBasicEquipmentSet()
        {
            if (database == null || !database.IsDataLoaded)
            {
                Debug.LogError("Database not ready!");
                return;
            }

            // Add one item of each type (first available item)
            for (int i = 0; i < 6; i++)
            {
                var equipmentType = (EquipmentType)i;
                var equipments = database.GetEquipmentsByType(equipmentType);
                if (equipments.Length > 0)
                {
                    AddEquipmentByGlobalId(equipments[0].ID, 1, 1);
                }
            }

            Debug.Log("Added basic equipment set to inventory");
        }

        private void EquipItemByUID(string itemUID)
        {
            if (GameController.EquipmentManager != null)
            {
                bool success = GameController.EquipmentManager.EquipItemByUID(itemUID);
                if (success)
                {
                    var item = GameController.EquipmentManager.GetItemByUID(itemUID);
                    var equipment = database.GetEquipmentByGlobalId(item.equipmentId);
                    Debug.Log($"Equipped {equipment?.Name} (UID: {itemUID})");
                }
                else
                {
                    Debug.LogError($"Failed to equip item with UID: {itemUID}");
                }
            }
        }

        private void EquipItemByType(EquipmentType type, int localIndex, int level)
        {
            var equipments = database.GetEquipmentsByType(type);
            if (localIndex >= 0 && localIndex < equipments.Length)
            {
                var equipment = equipments[localIndex];
                EquipItemByGlobalId(equipment.ID, level);
            }
        }

        private void EquipItemByGlobalId(int globalId, int level)
        {
            var equipment = database.GetEquipmentByGlobalId(globalId);
            if (equipment == null)
            {
                Debug.LogError($"Equipment with ID {globalId} not found!");
                return;
            }

            if (GameController.EquipmentManager != null)
            {
                bool success = GameController.EquipmentManager.EquipItem(equipment.EquipmentType, globalId, level);
                if (success)
                {
                    Debug.Log($"Equipped {equipment.Name} (ID:{globalId})");
                }
                else
                {
                    Debug.LogError($"Failed to equip {equipment.Name}");
                }
            }
        }

        private void UnequipItem(EquipmentType type)
        {
            if (GameController.EquipmentManager != null)
            {
                GameController.EquipmentManager.UnequipItem(type);
            }
        }

        private void RemoveFromInventory(string itemUID)
        {
            if (equipmentSave == null) return;
            
            var item = equipmentSave.GetItemByUID(itemUID);
            if (item == null)
            {
                Debug.LogError($"Item with UID {itemUID} not found!");
                return;
            }
            
            bool removed = equipmentSave.RemoveFromInventory(itemUID);
            
            if (removed)
            {
                if (GameController.EquipmentManager != null)
                {
                    GameController.EquipmentManager.OnInventoryChanged?.Invoke();
                }
                
                var equipment = database.GetEquipmentByGlobalId(item.equipmentId);
                var name = equipment != null ? equipment.Name : $"ID:{item.equipmentId}";
                Debug.Log($"Removed {name} (UID: {itemUID}) from inventory");
                
                SaveEquipmentData();
            }
        }

        private void RemoveFromInventory(EquipmentType type, int globalId, int level)
        {
            if (equipmentSave == null) return;
            
            bool removed = equipmentSave.RemoveFromInventory(type, globalId, level);
            
            if (removed)
            {
                if (GameController.EquipmentManager != null)
                {
                    GameController.EquipmentManager.OnInventoryChanged?.Invoke();
                }
                
                var equipment = database.GetEquipmentByGlobalId(globalId);
                var name = equipment != null ? equipment.Name : $"ID:{globalId}";
                Debug.Log($"Removed {name} (Lv.{level}) from inventory");
                
                SaveEquipmentData();
            }
        }

        private void ClearAllEquipment()
        {
            if (equipmentSave == null) return;
            
            equipmentSave.Clear();
            
            if (GameController.EquipmentManager != null)
            {
                GameController.EquipmentManager.OnInventoryChanged?.Invoke();
                for (int i = 0; i < 6; i++)
                {
                    GameController.EquipmentManager.OnEquipmentChanged?.Invoke((EquipmentType)i);
                }
            }
            
            SaveEquipmentData();
            Debug.Log("Cleared all equipment");
        }

        private void ClearInventoryOnly()
        {
            if (equipmentSave == null) return;
            
            // Clear only inventory, keep equipped items
            equipmentSave.inventory.Clear();
            equipmentSave.ForceSync();
            
            if (GameController.EquipmentManager != null)
            {
                GameController.EquipmentManager.OnInventoryChanged?.Invoke();
            }
            
            SaveEquipmentData();
            Debug.Log("Cleared inventory only");
        }

        private void AutoEquipBestItems()
        {
            if (equipmentSave == null || database == null) return;
            
            // For each equipment type, find the best item in inventory and equip it
            for (int i = 0; i < 6; i++)
            {
                var equipmentType = (EquipmentType)i;
                
                // Find all items of this type in inventory
                var itemsOfType = equipmentSave.inventory
                    .Where(item => item.equipmentType == equipmentType)
                    .ToList();
                
                if (itemsOfType.Count > 0)
                {
                    // Find the best item (highest rarity, then highest level)
                    var bestItem = itemsOfType
                        .Select(item => new { 
                            Item = item, 
                            Equipment = database.GetEquipmentByGlobalId(item.equipmentId) 
                        })
                        .Where(x => x.Equipment != null)
                        .OrderByDescending(x => (int)x.Equipment.Rarity)
                        .ThenByDescending(x => x.Item.level)
                        .FirstOrDefault();
                    
                    if (bestItem != null)
                    {
                        EquipItemByGlobalId(bestItem.Item.equipmentId, bestItem.Item.level);
                    }
                }
            }
            
            Debug.Log("Auto-equipped best available items");
        }

        private void UnequipAllItems()
        {
            for (int i = 0; i < 6; i++)
            {
                UnequipItem((EquipmentType)i);
            }
            Debug.Log("Unequipped all items");
        }

        private void SaveEquipmentData()
        {
            if (GameController.SaveManager != null)
            {
                GameController.SaveManager.Save(false);
                Debug.Log("Equipment data saved");
            }
        }

        // ===================== HELPER METHODS =====================
        
        private void CreateEquipmentSystemManager()
        {
            var existingSystemManager = FindObjectOfType<EquipmentSystemManager>();
            if (existingSystemManager != null)
            {
                Debug.Log("EquipmentSystemManager already exists in scene");
                Selection.activeGameObject = existingSystemManager.gameObject;
                return;
            }
            
            if (EditorUtility.DisplayDialog("Create Equipment System Manager", 
                "Create a new EquipmentSystemManager? This will help manage the entire equipment system.", 
                "Yes", "Cancel"))
            {
                GameObject systemManagerGO = new GameObject("EquipmentSystemManager");
                var systemManager = systemManagerGO.AddComponent<EquipmentSystemManager>();
                
                // Mark the object as dirty so it gets saved
                EditorUtility.SetDirty(systemManagerGO);
                Selection.activeGameObject = systemManagerGO;
                
                Debug.Log("Created new EquipmentSystemManager in scene");
            }
        }
        
        private void CreateOrFindEquipmentDatabase()
        {
            // First, try to find existing EquipmentDatabase in scene
            var existingDatabase = FindObjectOfType<EquipmentDatabase>();
            if (existingDatabase != null)
            {
                database = existingDatabase;
                Debug.Log("Found existing EquipmentDatabase in scene");
                return;
            }
            
            // Try to get singleton instance
            if (EquipmentDatabase.Instance != null)
            {
                database = EquipmentDatabase.Instance;
                Debug.Log("Found EquipmentDatabase singleton instance");
                return;
            }
            
            // If not found, create a new GameObject with EquipmentDatabase
            if (EditorUtility.DisplayDialog("Create Equipment Database", 
                "No EquipmentDatabase found in scene. Create a new one?", 
                "Yes", "Cancel"))
            {
                GameObject dbGO = new GameObject("EquipmentDatabase");
                database = dbGO.AddComponent<EquipmentDatabase>();
                
                // Mark the object as dirty so it gets saved
                EditorUtility.SetDirty(dbGO);
                Selection.activeGameObject = dbGO;
                
                Debug.Log("Created new EquipmentDatabase in scene");
            }
        }
        
        private void FindEquipmentManager()
        {
            var equipmentManager = FindObjectOfType<EquipmentManager>();
            if (equipmentManager != null)
            {
                Debug.Log("Found EquipmentManager in scene");
                RefreshEquipmentSave();
            }
            else
            {
                if (EditorUtility.DisplayDialog("Create Equipment Manager", 
                    "No EquipmentManager found in scene. Create a new one?", 
                    "Yes", "Cancel"))
                {
                    GameObject emGO = new GameObject("EquipmentManager");
                    emGO.AddComponent<EquipmentManager>();
                    
                    EditorUtility.SetDirty(emGO);
                    Selection.activeGameObject = emGO;
                    
                    Debug.Log("Created new EquipmentManager in scene");
                }
            }
        }
        
        private void CheckCSVFile()
        {
            // Check if equipment.csv exists in Resources/CSV/
            var csvResource = Resources.Load<TextAsset>("CSV/equipment");
            if (csvResource != null)
            {
                Debug.Log($"Found equipment.csv in Resources/CSV/ - {csvResource.text.Length} characters");
                
                // Try to count lines
                var lines = csvResource.text.Split('\n');
                Debug.Log($"CSV has {lines.Length} lines");
                
                if (lines.Length > 1)
                {
                    Debug.Log($"Header: {lines[0]}");
                    if (lines.Length > 2)
                    {
                        Debug.Log($"First data row: {lines[1]}");
                    }
                }
            }
            else
            {
                Debug.LogError("equipment.csv not found in Resources/CSV/! Make sure the file exists at Assets/Resources/CSV/equipment.csv");
                
                // Try to find CSV files in project
                var csvGuids = AssetDatabase.FindAssets("equipment t:TextAsset");
                if (csvGuids.Length > 0)
                {
                    Debug.Log("Found equipment CSV files in project:");
                    foreach (var guid in csvGuids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        Debug.Log($"  - {path}");
                    }
                }
            }
        }
    }
}
#endif