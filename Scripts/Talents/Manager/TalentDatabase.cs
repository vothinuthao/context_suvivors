using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TwoSleepyCats.CSVReader.Core;
using TwoSleepyCats.Patterns.Singleton;
using UnityEngine;
using Talents.Data;

namespace Talents.Manager
{
    /// <summary>
    /// Database for managing talent data loaded from CSV
    /// </summary>
    public class TalentDatabase : MonoSingleton<TalentDatabase>
    {
        [Header("Loading Settings")]
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool preloadAllData = false;
        
        [Header("Debug Info")]
        [SerializeField, ReadOnly] private int totalTalentCount = 0;
        [SerializeField, ReadOnly] private bool isDataLoaded = false;
        [SerializeField, ReadOnly] private string loadStatus = "Not Loaded";

        // Talent data organized by type and position
        private Dictionary<int, TalentModel> talentsById = new Dictionary<int, TalentModel>();
        private List<TalentModel> normalTalents = new List<TalentModel>();
        private List<TalentModel> specialTalents = new List<TalentModel>();
        private List<TalentModel> allTalents = new List<TalentModel>();

        // Talent tree structure
        private Dictionary<int, List<int>> talentDependencies = new Dictionary<int, List<int>>();
        private Dictionary<int, List<int>> talentUnlocks = new Dictionary<int, List<int>>();

        // Events
        public event System.Action OnDataLoaded;
        public event System.Action<string> OnLoadingError;

        // Properties
        public bool IsDataLoaded => isDataLoaded;
        public int TotalTalentCount => totalTalentCount;
        public List<TalentModel> NormalTalents => normalTalents;
        public List<TalentModel> SpecialTalents => specialTalents;

        protected override void Initialize()
        {
            base.Initialize();
            
            if (loadOnStart)
            {
                LoadTalentData();
            }
        }
        public void LoadTalentData()
        {
            StartCoroutine(LoadTalentDataCoroutine());
        }


        private IEnumerator LoadTalentDataCoroutine()
        {
            loadStatus = "Loading...";
            Debug.Log("[TalentDatabase] Starting to load talent data from CSV...");

            bool loadCompleted = false;
            List<TalentModel> talentData = null;
            string errorMessage = null;

            // Start the async load
            var loadTask = CsvDataManager.Instance.LoadAsync<TalentModel>();
    
            // Wait for completion
            yield return new WaitUntil(() => loadTask.IsCompleted);

            if (loadTask.Exception != null)
            {
                errorMessage = loadTask.Exception.GetBaseException().Message;
            }
            else
            {
                talentData = loadTask.Result;
            }

            // Process on main thread
            if (string.IsNullOrEmpty(errorMessage) && talentData != null)
            {
                try
                {
                    ProcessTalentData(talentData);
            
                    isDataLoaded = true;
                    loadStatus = $"Loaded {totalTalentCount} talents";
                    OnDataLoaded?.Invoke();
            
                    Debug.Log($"[TalentDatabase] Loaded {totalTalentCount} talents successfully");
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                }
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                isDataLoaded = false;
                loadStatus = $"Error: {errorMessage}";
                OnLoadingError?.Invoke(errorMessage);
                Debug.LogError($"[TalentDatabase] Failed to load talent data: {errorMessage}");
            }
        }

        /// <summary>
        /// Process and organize loaded talent data
        /// </summary>
        private void ProcessTalentData(List<TalentModel> talentData)
        {
            // Clear existing data
            ClearData();

            // Process each talent
            foreach (var talent in talentData)
            {
                if (talent.ValidateData())
                {
                    // Add to main collections
                    allTalents.Add(talent);
                    talentsById[talent.ID] = talent;
                    
                    // Sort by node type
                    if (talent.NodeType == TalentNodeType.Normal)
                    {
                        normalTalents.Add(talent);
                    }
                    else if (talent.NodeType == TalentNodeType.Special)
                    {
                        specialTalents.Add(talent);
                    }
                }
                else
                {
                    Debug.LogWarning($"[TalentDatabase] Invalid talent data: {talent}");
                }
            }

            // Build dependency tree
            BuildDependencyTree();

            // Sort talents by position
            SortTalentsByPosition();

            totalTalentCount = allTalents.Count;
            
            Debug.Log($"[TalentDatabase] Processed {totalTalentCount} talents ({normalTalents.Count} normal, {specialTalents.Count} special)");
        }

        /// <summary>
        /// Build talent dependency relationships
        /// </summary>
        private void BuildDependencyTree()
        {
            talentDependencies.Clear();
            talentUnlocks.Clear();

            foreach (var talent in allTalents)
            {
                if (talent.RequiredTalentId > 0 && talentsById.ContainsKey(talent.RequiredTalentId))
                {
                    // This talent depends on another talent
                    if (!talentDependencies.ContainsKey(talent.ID))
                    {
                        talentDependencies[talent.ID] = new List<int>();
                    }
                    talentDependencies[talent.ID].Add(talent.RequiredTalentId);

                    // The required talent unlocks this talent
                    if (!talentUnlocks.ContainsKey(talent.RequiredTalentId))
                    {
                        talentUnlocks[talent.RequiredTalentId] = new List<int>();
                    }
                    talentUnlocks[talent.RequiredTalentId].Add(talent.ID);
                }
            }
        }

        /// <summary>
        /// Sort talents by their position for proper display order
        /// </summary>
        private void SortTalentsByPosition()
        {
            // Sort normal talents by Y position (top to bottom)
            normalTalents.Sort((a, b) => a.PositionY.CompareTo(b.PositionY));
            
            // Sort special talents by Y position (top to bottom)
            specialTalents.Sort((a, b) => a.PositionY.CompareTo(b.PositionY));
        }

        /// <summary>
        /// Clear all cached data
        /// </summary>
        private void ClearData()
        {
            allTalents.Clear();
            normalTalents.Clear();
            specialTalents.Clear();
            talentsById.Clear();
            talentDependencies.Clear();
            talentUnlocks.Clear();
            totalTalentCount = 0;
        }

        /// <summary>
        /// Get talent by ID
        /// </summary>
        public TalentModel GetTalentById(int id)
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[TalentDatabase] Data not loaded yet!");
                return null;
            }

            return talentsById.GetValueOrDefault(id);
        }

        /// <summary>
        /// Get all talents
        /// </summary>
        public TalentModel[] GetAllTalents()
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[TalentDatabase] Data not loaded yet!");
                return Array.Empty<TalentModel>();
            }

            return allTalents.ToArray();
        }

        /// <summary>
        /// Get talents that this talent depends on
        /// </summary>
        public List<int> GetTalentDependencies(int talentId)
        {
            return talentDependencies.GetValueOrDefault(talentId, new List<int>());
        }

        /// <summary>
        /// Get talents that this talent unlocks
        /// </summary>
        public List<int> GetTalentUnlocks(int talentId)
        {
            return talentUnlocks.GetValueOrDefault(talentId, new List<int>());
        }

        /// <summary>
        /// Check if a talent exists
        /// </summary>
        public bool HasTalent(int talentId)
        {
            return isDataLoaded && talentsById.ContainsKey(talentId);
        }

        /// <summary>
        /// Get talents by node type
        /// </summary>
        public List<TalentModel> GetTalentsByType(TalentNodeType nodeType)
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[TalentDatabase] Data not loaded yet!");
                return new List<TalentModel>();
            }

            return nodeType == TalentNodeType.Normal ? normalTalents : specialTalents;
        }

        /// <summary>
        /// Get max talent level for a specific talent
        /// </summary>
        public int GetMaxTalentLevel(int talentId)
        {
            var talent = GetTalentById(talentId);
            return talent?.MaxLevel ?? 0;
        }

        /// <summary>
        /// Get talent cost for specific level
        /// </summary>
        public int GetTalentCost(int talentId, int targetLevel)
        {
            var talent = GetTalentById(talentId);
            if (talent == null) return 0;

            // Cost increases with level
            return talent.Cost * targetLevel;
        }

        /// <summary>
        /// Check if talent has prerequisites
        /// </summary>
        public bool HasPrerequisites(int talentId)
        {
            return talentDependencies.ContainsKey(talentId) && talentDependencies[talentId].Count > 0;
        }

        /// <summary>
        /// Get formatted talent tree info for debugging
        /// </summary>
        public string GetTalentTreeInfo()
        {
            if (!isDataLoaded)
                return "Database not loaded";

            var info = $"=== TALENT TREE INFO ===\n";
            info += $"Total Talents: {totalTalentCount}\n";
            info += $"Normal Talents: {normalTalents.Count}\n";
            info += $"Special Talents: {specialTalents.Count}\n";
            info += $"Dependencies: {talentDependencies.Count}\n";
            info += $"Unlock Chains: {talentUnlocks.Count}\n";

            return info;
        }

        /// <summary>
        /// Force reload data from CSV
        /// </summary>
        [ContextMenu("Reload Talent Data")]
        public async void ReloadTalentData()
        {
            try
            {
                isDataLoaded = false;
                loadStatus = "Reloading...";
                
                var talentData = await CsvDataManager.Instance.ForceReloadAsync<TalentModel>();
                ProcessTalentData(talentData);
                
                isDataLoaded = true;
                loadStatus = $"Reloaded {totalTalentCount} talents";
                OnDataLoaded?.Invoke();
                
                Debug.Log($"[TalentDatabase] Reloaded {totalTalentCount} talents successfully");
            }
            catch (Exception ex)
            {
                isDataLoaded = false;
                loadStatus = $"Reload Error: {ex.Message}";
                OnLoadingError?.Invoke(ex.Message);
                Debug.LogError($"[TalentDatabase] Failed to reload talent data: {ex.Message}");
            }
        }

        /// <summary>
        /// Log talent tree structure
        /// </summary>
        [ContextMenu("Log Talent Tree")]
        public void LogTalentTree()
        {
            if (!isDataLoaded)
            {
                Debug.Log("[TalentDatabase] Database not loaded");
                return;
            }

            Debug.Log(GetTalentTreeInfo());
            
            // Log normal talents
            Debug.Log("=== NORMAL TALENTS ===");
            foreach (var talent in normalTalents)
            {
                var deps = GetTalentDependencies(talent.ID);
                var unlocks = GetTalentUnlocks(talent.ID);
                Debug.Log($"{talent.Name} (ID:{talent.ID}) - Pos:({talent.PositionX},{talent.PositionY}) - Deps:{deps.Count} - Unlocks:{unlocks.Count}");
            }

            // Log special talents
            Debug.Log("=== SPECIAL TALENTS ===");
            foreach (var talent in specialTalents)
            {
                var deps = GetTalentDependencies(talent.ID);
                var unlocks = GetTalentUnlocks(talent.ID);
                Debug.Log($"{talent.Name} (ID:{talent.ID}) - Pos:({talent.PositionX},{talent.PositionY}) - Deps:{deps.Count} - Unlocks:{unlocks.Count}");
            }
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