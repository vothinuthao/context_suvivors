#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using OctoberStudio.Equipment;
using OctoberStudio.Save;
using System.Collections.Generic;

namespace OctoberStudio.Equipment.Tools
{
    public class EquipmentEditorTool : EditorWindow
    {
        private EquipmentDatabase database;
        private EquipmentSave equipmentSave;
        
        // UI Fields
        private EquipmentType selectedType = EquipmentType.Hat;
        private int equipmentId = 0;
        private int level = 1;
        private int quantity = 1;
        
        // Display options
        private Vector2 scrollPosition;
        private bool showEquippedArray = true;
        private bool showInventoryArray = true;
        private bool showRawData = false;
        
        // Equipment names cache
        private Dictionary<EquipmentType, string[]> equipmentNamesCache = new Dictionary<EquipmentType, string[]>();

        [MenuItem("Tools/October Studio/Equipment Debug Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<EquipmentEditorTool>("Equipment Debug");
            window.minSize = new Vector2(500, 700);
        }

        private void OnEnable()
        {
            RefreshEquipmentSave();
            FindEquipmentDatabase();
            RefreshEquipmentNamesCache();
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
            }

            EditorGUILayout.Space();

            // Database field
            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
            database = (EquipmentDatabase)EditorGUILayout.ObjectField("Equipment Database", database, typeof(EquipmentDatabase), false);

            if (database == null)
            {
                EditorGUILayout.HelpBox("Please assign Equipment Database!", MessageType.Warning);
                return;
            }

            if (equipmentSave == null)
            {
                EditorGUILayout.HelpBox("Equipment Save not found! Make sure EquipmentManager is in scene.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();

            // Add Equipment Section
            DrawAddEquipmentSection();

            EditorGUILayout.Space();

            // Quick Actions
            DrawQuickActionsSection();

            EditorGUILayout.Space();

            // Display current data
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Show equipped items array
            DrawEquippedItemsArray();
            
            EditorGUILayout.Space();
            
            // Show inventory items array
            DrawInventoryItemsArray();

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

        private void DrawAddEquipmentSection()
        {
            EditorGUILayout.LabelField("Add Equipment", EditorStyles.boldLabel);
            
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
            
            // Level and Quantity
            level = EditorGUILayout.IntSlider("Level", level, 1, 10);
            quantity = EditorGUILayout.IntSlider("Quantity", quantity, 1, 99);

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add to Inventory", GUILayout.Height(30)))
            {
                AddEquipment(selectedType, equipmentId, level, quantity);
            }
            
            if (GUILayout.Button("Add & Equip", GUILayout.Height(30)))
            {
                AddEquipment(selectedType, equipmentId, level, 1);
                EquipItem(selectedType, equipmentId, level);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawQuickActionsSection()
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add Basic Set"))
            {
                for (int i = 0; i < 6; i++)
                {
                    AddEquipment((EquipmentType)i, 0, 1, 1);
                }
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
            
            if (GUILayout.Button("Auto Equip All"))
            {
                AutoEquipFirstItems();
            }
            
            if (GUILayout.Button("Unequip All"))
            {
                UnequipAllItems();
            }
            
            EditorGUILayout.EndHorizontal();
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
                    
                    // Show equipment name if database available
                    if (database != null && item.equipmentId != -1)
                    {
                        var equipmentData = database.GetEquipmentById(item.equipmentType, item.equipmentId);
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
                        EditorGUILayout.LabelField($"Type: {item.equipmentType}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"Equipment ID: {item.equipmentId}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"Level: {item.level}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"Quantity: {item.quantity}", EditorStyles.miniLabel);
                        
                        // Show equipment name if database available
                        if (database != null)
                        {
                            var equipmentData = database.GetEquipmentById(item.equipmentType, item.equipmentId);
                            var name = equipmentData != null ? equipmentData.Name : "Unknown Equipment";
                            EditorGUILayout.LabelField($"Name: {name}", EditorStyles.miniLabel);
                        }
                        EditorGUILayout.EndVertical();
                        
                        EditorGUILayout.BeginVertical(GUILayout.Width(80));
                        if (GUILayout.Button("Equip", GUILayout.Width(70)))
                        {
                            EquipItem(item.equipmentType, item.equipmentId, item.level);
                        }
                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            RemoveFromInventory(item.equipmentType, item.equipmentId, item.level, 1);
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
            }
        }

        private void FindEquipmentDatabase()
        {
            if (database == null)
            {
                var guids = AssetDatabase.FindAssets("t:EquipmentDatabase");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    database = AssetDatabase.LoadAssetAtPath<EquipmentDatabase>(path);
                }
            }
        }

        private void RefreshEquipmentNamesCache()
        {
            equipmentNamesCache.Clear();
            
            if (database == null) return;

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

        private void AddEquipment(EquipmentType type, int id, int level, int quantity)
        {
            if (equipmentSave == null) return;
            
            equipmentSave.AddToInventory(type, id, level, quantity);
            
            if (GameController.EquipmentManager != null)
            {
                GameController.EquipmentManager.OnInventoryChanged?.Invoke();
            }
            
            Debug.Log($"Added {quantity}x {type} ID:{id} (Lv.{level}) to inventory");
        }

        private void RemoveFromInventory(EquipmentType type, int id, int level, int quantity)
        {
            if (equipmentSave == null) return;
            
            bool removed = equipmentSave.RemoveFromInventory(type, id, level, quantity);
            
            if (removed)
            {
                if (GameController.EquipmentManager != null)
                {
                    GameController.EquipmentManager.OnInventoryChanged?.Invoke();
                }
                
                Debug.Log($"Removed {quantity}x {type} ID:{id} (Lv.{level}) from inventory");
            }
        }

        private void EquipItem(EquipmentType type, int id, int level)
        {
            if (GameController.EquipmentManager != null)
            {
                GameController.EquipmentManager.EquipItem(type, id, level);
            }
        }

        private void UnequipItem(EquipmentType type)
        {
            if (GameController.EquipmentManager != null)
            {
                GameController.EquipmentManager.UnequipItem(type);
            }
        }

        private void ClearAllEquipment()
        {
            if (equipmentSave == null) return;
            
            equipmentSave.Clear();
            
            if (GameController.EquipmentManager != null)
            {
                GameController.EquipmentManager.OnInventoryChanged?.Invoke();
                GameController.EquipmentManager.OnEquipmentChanged?.Invoke(EquipmentType.Hat);
                GameController.EquipmentManager.OnEquipmentChanged?.Invoke(EquipmentType.Armor);
                GameController.EquipmentManager.OnEquipmentChanged?.Invoke(EquipmentType.Ring);
                GameController.EquipmentManager.OnEquipmentChanged?.Invoke(EquipmentType.Necklace);
                GameController.EquipmentManager.OnEquipmentChanged?.Invoke(EquipmentType.Belt);
                GameController.EquipmentManager.OnEquipmentChanged?.Invoke(EquipmentType.Shoes);
            }
            
            Debug.Log("Cleared all equipment");
        }

        private void AutoEquipFirstItems()
        {
            if (equipmentSave == null) return;
            
            for (int i = 0; i < 6; i++)
            {
                var equipmentType = (EquipmentType)i;
                
                if (equipmentSave.inventoryItems != null && equipmentSave.inventoryItems.Length > 0)
                {
                    for (int j = 0; j < equipmentSave.inventoryItems.Length; j++)
                    {
                        var item = equipmentSave.inventoryItems[j];
                        if (item != null && item.equipmentType == equipmentType)
                        {
                            EquipItem(item.equipmentType, item.equipmentId, item.level);
                            break;
                        }
                    }
                }
            }
        }

        private void UnequipAllItems()
        {
            for (int i = 0; i < 6; i++)
            {
                UnequipItem((EquipmentType)i);
            }
        }
    }
}
#endif