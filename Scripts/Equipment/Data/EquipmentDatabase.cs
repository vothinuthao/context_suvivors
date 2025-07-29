using System;
using System.Collections.Generic;
using System.Linq;
using TwoSleepyCats.CSVReader.Core;
using TwoSleepyCats.Patterns.Singleton;
using UnityEngine;

namespace OctoberStudio.Equipment
{
    /// <summary>
    /// Equipment Database using MonoBehaviour and CSV data loading
    /// </summary>
    public class EquipmentDatabase : MonoSingleton<EquipmentDatabase>
    {
        [Header("Loading Settings")]
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool preloadAllData = false;
        
        [Header("Debug Info")]
        [SerializeField, ReadOnly] private int totalEquipmentCount = 0;
        [SerializeField, ReadOnly] private bool isDataLoaded = false;
        [SerializeField, ReadOnly] private string loadStatus = "Not Loaded";

        // Equipment data organized by type
        private Dictionary<EquipmentType, List<EquipmentModel>> equipmentByType = new Dictionary<EquipmentType, List<EquipmentModel>>();
        private Dictionary<int, EquipmentModel> equipmentById = new Dictionary<int, EquipmentModel>();
        private List<EquipmentModel> allEquipment = new List<EquipmentModel>();

        // Events
        public event System.Action OnDataLoaded;
        public event System.Action<string> OnLoadingError;

        // Properties
        public bool IsDataLoaded => isDataLoaded;
        public int TotalEquipmentCount => totalEquipmentCount;

        protected override void Initialize()
        {
            base.Initialize();
            
            foreach (EquipmentType equipmentType in Enum.GetValues(typeof(EquipmentType)))
            {
                equipmentByType[equipmentType] = new List<EquipmentModel>();
            }

            if (loadOnStart)
            {
                LoadEquipmentData();
            }
        }
        public async void LoadEquipmentData()
        {
            try
            {
                loadStatus = "Loading...";
                Debug.Log("[EquipmentDatabase] Starting to load equipment data from CSV...");
                var equipmentData = await CsvDataManager.Instance.LoadAsync<EquipmentModel>();
                
                ProcessEquipmentData(equipmentData);
                
                isDataLoaded = true;
                loadStatus = $"Loaded {totalEquipmentCount} items";
                OnDataLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                isDataLoaded = false;
                loadStatus = $"Error: {ex.Message}";
                OnLoadingError?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// Process and organize loaded equipment data
        /// </summary>
        private void ProcessEquipmentData(List<EquipmentModel> equipmentData)
        {
            // Clear existing data
            ClearData();

            // Process each equipment item
            foreach (var equipment in equipmentData)
            {
                if (equipment.ValidateData())
                {
                    // Add to main collections
                    allEquipment.Add(equipment);
                    equipmentById[equipment.ID] = equipment;
                    
                    // Add to type-specific collection
                    if (equipmentByType.ContainsKey(equipment.EquipmentType))
                    {
                        equipmentByType[equipment.EquipmentType].Add(equipment);
                    }
                }
                else
                {
                    Debug.LogWarning($"[EquipmentDatabase] Invalid equipment data: {equipment}");
                }
            }

            totalEquipmentCount = allEquipment.Count;

            // Sort equipment by ID within each type
            foreach (var kvp in equipmentByType)
            {
                kvp.Value.Sort((a, b) => a.ID.CompareTo(b.ID));
            }

            // Log statistics
            LogEquipmentStatistics();
        }

        /// <summary>
        /// Clear all cached data
        /// </summary>
        private void ClearData()
        {
            allEquipment.Clear();
            equipmentById.Clear();
            
            foreach (var kvp in equipmentByType)
            {
                kvp.Value.Clear();
            }
            
            totalEquipmentCount = 0;
        }

        /// <summary>
        /// Get equipment by type and local index (legacy compatibility)
        /// </summary>
        public EquipmentModel GetEquipmentById(EquipmentType type, int localIndex)
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[EquipmentDatabase] Data not loaded yet!");
                return null;
            }

            if (equipmentByType.TryGetValue(type, out var equipmentList))
            {
                if (localIndex >= 0 && localIndex < equipmentList.Count)
                {
                    return equipmentList[localIndex];
                }
            }

            return null;
        }

        /// <summary>
        /// Get equipment by global ID
        /// </summary>
        public EquipmentModel GetEquipmentByGlobalId(int globalId)
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[EquipmentDatabase] Data not loaded yet!");
                return null;
            }

            return equipmentById.GetValueOrDefault(globalId);
        }

        /// <summary>
        /// Get all equipment of a specific type
        /// </summary>
        public EquipmentModel[] GetEquipmentsByType(EquipmentType type)
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[EquipmentDatabase] Data not loaded yet!");
                return Array.Empty<EquipmentModel>();
            }

            if (equipmentByType.TryGetValue(type, out var equipmentList))
            {
                return equipmentList.ToArray();
            }

            return Array.Empty<EquipmentModel>();
        }

        /// <summary>
        /// Get all equipment
        /// </summary>
        public EquipmentModel[] GetAllEquipment()
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[EquipmentDatabase] Data not loaded yet!");
                return Array.Empty<EquipmentModel>();
            }

            return allEquipment.ToArray();
        }

        /// <summary>
        /// Get equipment by rarity
        /// </summary>
        public EquipmentModel[] GetEquipmentByRarity(EquipmentRarity rarity)
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[EquipmentDatabase] Data not loaded yet!");
                return Array.Empty<EquipmentModel>();
            }

            return allEquipment.Where(e => e.Rarity == rarity).ToArray();
        }

        /// <summary>
        /// Get equipment by type and rarity
        /// </summary>
        public EquipmentModel[] GetEquipmentByTypeAndRarity(EquipmentType type, EquipmentRarity rarity)
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[EquipmentDatabase] Data not loaded yet!");
                return Array.Empty<EquipmentModel>();
            }

            if (equipmentByType.TryGetValue(type, out var equipmentList))
            {
                return equipmentList.Where(e => e.Rarity == rarity).ToArray();
            }

            return Array.Empty<EquipmentModel>();
        }

        /// <summary>
        /// Search equipment by name
        /// </summary>
        public EquipmentModel[] SearchEquipmentByName(string searchTerm)
        {
            if (!isDataLoaded || string.IsNullOrEmpty(searchTerm))
            {
                return Array.Empty<EquipmentModel>();
            }

            return allEquipment.Where(e => 
                e.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                e.Description.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0
            ).ToArray();
        }

        /// <summary>
        /// Get random equipment by type
        /// </summary>
        public EquipmentModel GetRandomEquipmentByType(EquipmentType type)
        {
            var equipmentList = GetEquipmentsByType(type);
            if (equipmentList.Length == 0) return null;

            int randomIndex = UnityEngine.Random.Range(0, equipmentList.Length);
            return equipmentList[randomIndex];
        }

        /// <summary>
        /// Get random equipment by rarity (for loot drops)
        /// </summary>
        public EquipmentModel GetRandomEquipmentByRarity(EquipmentRarity rarity)
        {
            var equipmentList = GetEquipmentByRarity(rarity);
            if (equipmentList.Length == 0) return null;

            int randomIndex = UnityEngine.Random.Range(0, equipmentList.Length);
            return equipmentList[randomIndex];
        }

        /// <summary>
        /// Check if equipment exists
        /// </summary>
        public bool HasEquipment(int globalId)
        {
            return isDataLoaded && equipmentById.ContainsKey(globalId);
        }

        /// <summary>
        /// Get equipment count by type
        /// </summary>
        public int GetEquipmentCountByType(EquipmentType type)
        {
            if (!isDataLoaded) return 0;
            
            return equipmentByType.TryGetValue(type, out var list) ? list.Count : 0;
        }

        /// <summary>
        /// Force reload data from CSV
        /// </summary>
        [ContextMenu("Reload Equipment Data")]
        public async void ReloadEquipmentData()
        {
            try
            {
                isDataLoaded = false;
                loadStatus = "Reloading...";
                var equipmentData = await CsvDataManager.Instance.ForceReloadAsync<EquipmentModel>();
                ProcessEquipmentData(equipmentData);
                
                isDataLoaded = true;
                loadStatus = $"Reloaded {totalEquipmentCount} items";
                OnDataLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                isDataLoaded = false;
                loadStatus = $"Reload Error: {ex.Message}";
                OnLoadingError?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// Log equipment statistics
        /// </summary>
        private void LogEquipmentStatistics()
        {
            
            foreach (EquipmentType type in Enum.GetValues(typeof(EquipmentType)))
            {
                int count = GetEquipmentCountByType(type);
            }
            foreach (EquipmentRarity rarity in Enum.GetValues(typeof(EquipmentRarity)))
            {
                var count = allEquipment.Count(e => e.Rarity == rarity);
            }
        }

        /// <summary>
        /// Get database info for debugging
        /// </summary>
        [ContextMenu("Log Database Info")]
        public void LogDatabaseInfo()
        {
            if (!isDataLoaded)
            {
                Debug.Log("[EquipmentDatabase] Database not loaded");
                return;
            }

            LogEquipmentStatistics();
            
            // Show cache info
            var cacheInfo = CsvDataManager.Instance.GetCacheInfo();
            Debug.Log($"[EquipmentDatabase] CSV Cache Info:\n{cacheInfo}");
        }
    }

    /// <summary>
    /// ReadOnly attribute for inspector display
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            UnityEditor.EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
#endif
}