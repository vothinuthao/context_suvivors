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
        
        private List<TalentModel> baseStatsTalents = new List<TalentModel>();
        private List<TalentModel> specialSkillsTalents = new List<TalentModel>();

        // Events
        public event System.Action OnDataLoaded;
        public event System.Action<string> OnLoadingError;
        
        public List<TalentModel> BaseStatsTalents => baseStatsTalents;
        public List<TalentModel> SpecialSkillsTalents => specialSkillsTalents;

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
            
                    // Phân loại theo logic mới
                    if (talent.IsBaseStat)
                    {
                        baseStatsTalents.Add(talent);
                        normalTalents.Add(talent); // Giữ cho backward compatibility
                    }
                    else if (talent.IsSpecialSkill)
                    {
                        specialSkillsTalents.Add(talent);
                        specialTalents.Add(talent); // Giữ cho backward compatibility
                    }
                }
                else
                {
                    Debug.LogWarning($"[TalentDatabase] Invalid talent data: {talent}");
                }
            }

            // Build dependency tree (giữ nguyên cho special skills nếu cần)
            BuildDependencyTree();

            // Sort talents
            SortTalentsByType();

            totalTalentCount = allTalents.Count;
    
            Debug.Log($"[TalentDatabase] Processed {totalTalentCount} talents " +
                      $"({baseStatsTalents.Count} base stats, {specialSkillsTalents.Count} special skills)");
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
        private void SortTalentsByType()
        {
            // Sort Base Stats theo thứ tự: ATK, HP, Armor, Healing
            baseStatsTalents.Sort((a, b) => {
                var orderA = GetBaseStatOrder(a);
                var orderB = GetBaseStatOrder(b);
                return orderA.CompareTo(orderB);
            });
    
            specialSkillsTalents.Sort((a, b) => a.RequiredPlayerLevel.CompareTo(b.RequiredPlayerLevel));
        }
        private int GetBaseStatOrder(TalentModel talent)
        {
            var baseStatType = talent.GetBaseStatType();
            if (!baseStatType.HasValue) return 999;
    
            switch (baseStatType.Value)
            {
                case BaseStatType.ATK: return 0;
                case BaseStatType.HP: return 1;
                case BaseStatType.Armor: return 2;
                case BaseStatType.Healing: return 3;
                default: return 999;
            }
        }

        /// <summary>
        /// Clear all cached data
        /// </summary>
        private void ClearData()
        {
            allTalents.Clear();
            normalTalents.Clear();
            specialTalents.Clear();
            baseStatsTalents.Clear();      // Thêm
            specialSkillsTalents.Clear();  // Thêm
            talentsById.Clear();
            talentDependencies.Clear();
            talentUnlocks.Clear();
            totalTalentCount = 0;
        }
        /// <summary>
        /// Get Base Stat talent by type
        /// </summary>
        public TalentModel GetBaseStatTalent(BaseStatType statType)
        {
            return baseStatsTalents.FirstOrDefault(t => t.GetBaseStatType() == statType);
        }

        /// <summary>
        /// Get all Base Stats talents ordered by type
        /// </summary>
        public List<TalentModel> GetOrderedBaseStats()
        {
            return baseStatsTalents.OrderBy(GetBaseStatOrder).ToList();
        }

        /// <summary>
        /// Get Special Skills available for player level
        /// </summary>
        public List<TalentModel> GetAvailableSpecialSkills(int playerLevel)
        {
            return specialSkillsTalents.Where(t => t.RequiredPlayerLevel <= playerLevel).ToList();
        }

        /// <summary>
        /// Get next Special Skill to unlock
        /// </summary>
        public TalentModel GetNextSpecialSkill(int playerLevel)
        {
            return specialSkillsTalents
                .Where(t => t.RequiredPlayerLevel > playerLevel)
                .OrderBy(t => t.RequiredPlayerLevel)
                .FirstOrDefault();
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

            if (talent.IsBaseStat)
            {
                return talent.Cost * targetLevel;
            }
            else if (talent.IsSpecialSkill)
            {
                return talent.Cost;
            }

            return talent.Cost * targetLevel;
        }
        /// <summary>
        /// Validate talent tree structure
        /// </summary>
        public bool ValidateTalentTree()
        {
            bool isValid = true;
    
            // Check Base Stats
            var expectedBaseStats = new[] { 
                BaseStatType.ATK, 
                BaseStatType.HP, 
                BaseStatType.Armor, 
                BaseStatType.Healing 
            };
    
            foreach (var expectedStat in expectedBaseStats)
            {
                var talent = GetBaseStatTalent(expectedStat);
                if (talent == null)
                {
                    Debug.LogError($"[TalentDatabase] Missing Base Stat: {expectedStat}");
                    isValid = false;
                }
            }
    
            // Check Special Skills có level requirements hợp lý
            foreach (var skill in specialSkillsTalents)
            {
                if (skill.RequiredPlayerLevel < 1 || skill.RequiredPlayerLevel > 100)
                {
                    Debug.LogWarning($"[TalentDatabase] Unusual player level requirement for {skill.Name}: {skill.RequiredPlayerLevel}");
                }
        
                if (skill.MaxLevel > 1)
                {
                    Debug.LogWarning($"[TalentDatabase] Special Skill {skill.Name} has MaxLevel > 1: {skill.MaxLevel}");
                }
            }
    
            return isValid;
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
            info += $"Base Stats: {baseStatsTalents.Count}\n";
            info += $"Special Skills: {specialSkillsTalents.Count}\n";
            info += $"Dependencies: {talentDependencies.Count}\n";
            info += $"Unlock Chains: {talentUnlocks.Count}\n";
            info += $"Structure Valid: {ValidateTalentTree()}\n";

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