#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using OctoberStudio.Save;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;

namespace OctoberStudio.Tools
{
    public class SaveManagerDebugTool : EditorWindow
    {
        private SaveManager saveManager;
        private SaveDatabase saveDatabase;
        private SaveCell[] saveCells;
        
        // UI State
        private Vector2 scrollPosition;
        private bool showRawJSON = false;
        private bool autoRefresh = true;
        private float lastRefreshTime;
        private const float REFRESH_INTERVAL = 1f;
        
        // Selected save details
        private int selectedSaveIndex = -1;
        private string selectedSaveJSON = "";
        private ISave selectedSaveObject = null;
        
        // Filters
        private string searchFilter = "";
        private bool showEmptySaves = true;

        [MenuItem("Tools/October Studio/Save Manager Debug")]
        public static void ShowWindow()
        {
            var window = GetWindow<SaveManagerDebugTool>("Save Manager Debug");
            window.minSize = new Vector2(600, 500);
        }

        private void OnEnable()
        {
            RefreshSaveData();
        }

        private void Update()
        {
            if (autoRefresh && Application.isPlaying && Time.time - lastRefreshTime > REFRESH_INTERVAL)
            {
                RefreshSaveData();
                lastRefreshTime = Time.time;
                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Save Manager Debug Tool", EditorStyles.largeLabel);
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

            DrawToolbar();
            EditorGUILayout.Space();

            if (saveManager == null)
            {
                EditorGUILayout.HelpBox("SaveManager not found! Make sure it exists in the scene.", MessageType.Warning);
                return;
            }

            if (saveCells == null)
            {
                EditorGUILayout.HelpBox("No save data found.", MessageType.Info);
                return;
            }

            DrawSavesList();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshSaveData();
            }
            
            autoRefresh = GUILayout.Toggle(autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Save All", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                SaveAllData();
            }
            
            if (GUILayout.Button("Clear All", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                if (EditorUtility.DisplayDialog("Clear All Save Data", 
                    "This will delete ALL save data permanently. Continue?", 
                    "Yes, Clear All", "Cancel"))
                {
                    ClearAllSaveData();
                }
            }
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(150));
            
            showEmptySaves = GUILayout.Toggle(showEmptySaves, "Show Empty", EditorStyles.toolbarButton);
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSavesList()
        {
            var filteredSaves = FilterSaves();
            
            EditorGUILayout.LabelField($"Save Data ({filteredSaves.Count} items)", EditorStyles.boldLabel);
            
            if (filteredSaves.Count == 0)
            {
                EditorGUILayout.LabelField("No save data matches current filter.");
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            for (int i = 0; i < filteredSaves.Count; i++)
            {
                var saveInfo = filteredSaves[i];
                DrawSaveItem(saveInfo, i);
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawSaveItem(SaveInfo saveInfo, int index)
        {
            var isSelected = selectedSaveIndex == saveInfo.originalIndex;
            var backgroundColor = isSelected ? Color.cyan * 0.3f : (index % 2 == 0 ? Color.white * 0.1f : Color.clear);
            
            var rect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(rect, backgroundColor);
            
            EditorGUILayout.BeginHorizontal();
            
            // Save type and name
            EditorGUILayout.BeginVertical();
            
            var displayName = saveInfo.friendlyName;
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = $"Hash: {saveInfo.hash}";
            }
            
            var style = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
            EditorGUILayout.LabelField(displayName, style);
            
            EditorGUILayout.LabelField($"Type: {saveInfo.saveType}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Hash: {saveInfo.hash}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
            
            // Status indicators
            EditorGUILayout.BeginVertical(GUILayout.Width(100));
            
            var statusColor = saveInfo.isReassembled ? Color.green : Color.yellow;
            var statusText = saveInfo.isReassembled ? "Loaded" : "Serialized";
            
            var oldColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(statusText, EditorStyles.miniLabel);
            GUI.color = oldColor;
            
            if (saveInfo.hasData)
            {
                EditorGUILayout.LabelField($"Size: {saveInfo.jsonSize} chars", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("No Data", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
            
            // Action buttons
            EditorGUILayout.BeginVertical(GUILayout.Width(120));
            
            if (GUILayout.Button("Inspect", GUILayout.Height(20)))
            {
                SelectSave(saveInfo);
            }
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Clear", GUILayout.Width(55), GUILayout.Height(18)))
            {
                if (EditorUtility.DisplayDialog("Clear Save Data", 
                    $"Clear save data for '{displayName}'?", 
                    "Yes", "Cancel"))
                {
                    ClearSaveData(saveInfo);
                }
            }
            
            if (GUILayout.Button("Copy", GUILayout.Width(55), GUILayout.Height(18)))
            {
                CopySaveDataToClipboard(saveInfo);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            // Show detailed info if selected
            if (isSelected)
            {
                DrawSelectedSaveDetails();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawSelectedSaveDetails()
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Details:", EditorStyles.boldLabel);
            
            if (selectedSaveObject != null)
            {
                // Draw object properties using reflection
                DrawObjectProperties(selectedSaveObject);
            }
            
            // JSON Section
            EditorGUILayout.Space();
            showRawJSON = EditorGUILayout.Foldout(showRawJSON, "Raw JSON Data");
            
            if (showRawJSON && !string.IsNullOrEmpty(selectedSaveJSON))
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                var textStyle = new GUIStyle(EditorStyles.label);
                textStyle.wordWrap = true;
                textStyle.fontSize = 10;
                
                EditorGUILayout.LabelField(selectedSaveJSON, textStyle);
                
                if (GUILayout.Button("Copy JSON to Clipboard"))
                {
                    EditorGUIUtility.systemCopyBuffer = selectedSaveJSON;
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUI.indentLevel--;
        }

        private void DrawObjectProperties(ISave saveObject)
        {
            var type = saveObject.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<System.NonSerializedAttribute>() != null)
                    continue;
                    
                var value = field.GetValue(saveObject);
                var valueString = value?.ToString() ?? "null";
                
                // Limit string length for display
                if (valueString.Length > 100)
                {
                    valueString = valueString.Substring(0, 100) + "...";
                }
                
                EditorGUILayout.LabelField($"{field.Name}:", valueString, EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void RefreshSaveData()
        {
            // Find SaveManager instance
            saveManager = FindObjectOfType<SaveManager>();
            
            if (saveManager == null) return;
            
            // Use reflection to access private SaveDatabase
            var saveManagerType = typeof(SaveManager);
            var saveDatabaseField = saveManagerType.GetProperty("SaveDatabase", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (saveDatabaseField != null)
            {
                saveDatabase = saveDatabaseField.GetValue(saveManager) as SaveDatabase;
            }
            
            if (saveDatabase == null) return;
            
            // Get save cells using reflection
            var saveDatabaseType = typeof(SaveDatabase);
            var saveCellsField = saveDatabaseType.GetField("saveCells", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (saveCellsField != null)
            {
                saveCells = saveCellsField.GetValue(saveDatabase) as SaveCell[];
            }
        }

        private List<SaveInfo> FilterSaves()
        {
            var result = new List<SaveInfo>();
            
            if (saveCells == null) return result;
            
            for (int i = 0; i < saveCells.Length; i++)
            {
                var cell = saveCells[i];
                if (cell == null) continue;
                
                var saveInfo = CreateSaveInfo(cell, i);
                
                // Apply filters
                if (!showEmptySaves && !saveInfo.hasData) continue;
                
                if (!string.IsNullOrEmpty(searchFilter))
                {
                    var searchLower = searchFilter.ToLower();
                    if (!saveInfo.friendlyName.ToLower().Contains(searchLower) && 
                        !saveInfo.saveType.ToLower().Contains(searchLower) &&
                        !saveInfo.hash.ToString().Contains(searchFilter))
                    {
                        continue;
                    }
                }
                
                result.Add(saveInfo);
            }
            
            return result;
        }

        private SaveInfo CreateSaveInfo(SaveCell cell, int index)
        {
            var saveInfo = new SaveInfo();
            saveInfo.originalIndex = index;
            
            // Use reflection to get SaveCell properties
            var cellType = typeof(SaveCell);
            
            var hashProperty = cellType.GetProperty("Hash");
            if (hashProperty != null)
            {
                saveInfo.hash = (int)hashProperty.GetValue(cell);
            }
            
            var isReassembledProperty = cellType.GetProperty("IsReassembled");
            if (isReassembledProperty != null)
            {
                saveInfo.isReassembled = (bool)isReassembledProperty.GetValue(cell);
            }
            
            var saveProperty = cellType.GetProperty("Save");
            if (saveProperty != null)
            {
                var saveObject = saveProperty.GetValue(cell) as ISave;
                if (saveObject != null)
                {
                    saveInfo.saveType = saveObject.GetType().Name;
                    saveInfo.saveObject = saveObject;
                }
            }
            
            // Get JSON data
            var jsonField = cellType.GetField("json", BindingFlags.NonPublic | BindingFlags.Instance);
            if (jsonField != null)
            {
                var json = jsonField.GetValue(cell) as string;
                if (!string.IsNullOrEmpty(json))
                {
                    saveInfo.json = json;
                    saveInfo.jsonSize = json.Length;
                    saveInfo.hasData = true;
                }
            }
            
            // Try to get friendly name from known save types
            saveInfo.friendlyName = GetFriendlyName(saveInfo.hash, saveInfo.saveType);
            
            return saveInfo;
        }

        private string GetFriendlyName(int hash, string saveType)
        {
            // Common save names and their hashes
            var knownSaves = new Dictionary<string, string>
            {
                {"Characters", "Characters"},
                {"Equipment", "Equipment"},
                {"Abilities", "Abilities"},
                {"Upgrades", "Upgrades"},
                {"Audio", "Audio"},
                {"Vibration", "Vibration"},
                {"gold", "Gold Currency"},
                {"tempGold", "Temp Gold Currency"}
            };
            
            foreach (var kvp in knownSaves)
            {
                if (kvp.Key.GetHashCode() == hash)
                {
                    return kvp.Value;
                }
            }
            
            return saveType;
        }

        private void SelectSave(SaveInfo saveInfo)
        {
            selectedSaveIndex = saveInfo.originalIndex;
            selectedSaveJSON = saveInfo.json;
            selectedSaveObject = saveInfo.saveObject;
        }

        private void SaveAllData()
        {
            if (saveManager != null)
            {
                saveManager.Save();
                Debug.Log("All save data has been saved.");
            }
        }

        private void ClearAllSaveData()
        {
            if (saveDatabase != null)
            {
                // Clear all save data using reflection
                var saveDatabaseType = typeof(SaveDatabase);
                var saveCellsListField = saveDatabaseType.GetField("saveCellsList", BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (saveCellsListField != null)
                {
                    var saveCellsList = saveCellsListField.GetValue(saveDatabase) as List<SaveCell>;
                    if (saveCellsList != null)
                    {
                        saveCellsList.Clear();
                        RefreshSaveData();
                        Debug.Log("All save data cleared.");
                    }
                }
            }
        }

        private void ClearSaveData(SaveInfo saveInfo)
        {
            if (saveDatabase != null)
            {
                var saveDatabaseType = typeof(SaveDatabase);
                var saveCellsListField = saveDatabaseType.GetField("saveCellsList", BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (saveCellsListField != null)
                {
                    var saveCellsList = saveCellsListField.GetValue(saveDatabase) as List<SaveCell>;
                    if (saveCellsList != null && saveInfo.originalIndex < saveCellsList.Count)
                    {
                        saveCellsList.RemoveAt(saveInfo.originalIndex);
                        RefreshSaveData();
                        selectedSaveIndex = -1;
                        Debug.Log($"Cleared save data: {saveInfo.friendlyName}");
                    }
                }
            }
        }

        private void CopySaveDataToClipboard(SaveInfo saveInfo)
        {
            var data = $"Save: {saveInfo.friendlyName}\n";
            data += $"Type: {saveInfo.saveType}\n";
            data += $"Hash: {saveInfo.hash}\n";
            data += $"JSON: {saveInfo.json}";
            
            EditorGUIUtility.systemCopyBuffer = data;
            Debug.Log($"Copied save data to clipboard: {saveInfo.friendlyName}");
        }

        private class SaveInfo
        {
            public int originalIndex;
            public int hash;
            public string friendlyName;
            public string saveType = "Unknown";
            public bool isReassembled;
            public bool hasData;
            public string json = "";
            public int jsonSize;
            public ISave saveObject;
        }
    }
}
#endif