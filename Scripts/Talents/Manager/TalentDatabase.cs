using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OctoberStudio.Upgrades;
using TwoSleepyCats.CSVReader.Core;
using TwoSleepyCats.Patterns.Singleton;
using UnityEngine;
using Talents.Data;
using Talents.Config;

namespace Talents.Manager
{
    /// <summary>
    /// Auto-generation talent database
    /// CSV contains: 4 base stats + special nodes only
    /// Normal nodes auto-generated for all levels with linear scaling
    /// </summary>
    public class TalentDatabase : MonoSingleton<TalentDatabase>
    {
        [Header("Configuration")]
        [SerializeField] private TalentLayoutConfig layoutConfig;
        
        [Header("Debug Info")]
        [SerializeField, ReadOnly] private int totalTalentCount = 0;
        [SerializeField, ReadOnly] private bool isDataLoaded = false;
        [SerializeField, ReadOnly] private string loadStatus = "Not Loaded";
        [SerializeField, ReadOnly] private int normalTalentCount = 0;
        [SerializeField, ReadOnly] private int specialTalentCount = 0;
        [SerializeField, ReadOnly] private int maxPlayerLevel = 0;

        // Collections
        private Dictionary<int, TalentModel> talentsById = new Dictionary<int, TalentModel>();
        private List<TalentModel> normalTalents = new List<TalentModel>();
        private List<TalentModel> specialTalents = new List<TalentModel>();
        private List<TalentModel> allTalents = new List<TalentModel>();
        
        // Base stats from CSV (4 base stats)
        private Dictionary<string, TalentModel> baseStats = new Dictionary<string, TalentModel>();
        
        // Zone tracking
        private Dictionary<int, List<TalentModel>> talentsByZone = new Dictionary<int, List<TalentModel>>();

        // Events
        public event System.Action OnDataLoaded;
        public event System.Action<string> OnLoadingError;

        // Properties
        public bool IsDataLoaded => isDataLoaded;
        public int TotalTalentCount => totalTalentCount;
        public TalentLayoutConfig LayoutConfig => layoutConfig;
        public int MaxPlayerLevel => maxPlayerLevel;

        protected override void Initialize()
        {
            base.Initialize();
            
            if (layoutConfig == null)
            {
                CreateDefaultLayoutConfig();
            }
            
            LoadTalentData();
        }

        private void CreateDefaultLayoutConfig()
        {
            layoutConfig = ScriptableObject.CreateInstance<TalentLayoutConfig>();
            layoutConfig.ResetToDefaults();
            Debug.LogWarning("[TalentDatabase] No layout config assigned, using default settings");
        }

        public void LoadTalentData()
        {
            StartCoroutine(LoadTalentDataCoroutine());
        }

        private IEnumerator LoadTalentDataCoroutine()
        {
            loadStatus = "Loading CSV data...";

            var loadTask = CsvDataManager.Instance.LoadAsync<TalentModel>();
            yield return new WaitUntil(() => loadTask.IsCompleted);

            try
            {
                if (loadTask.Exception != null)
                    throw loadTask.Exception.GetBaseException();

                var csvTalents = loadTask.Result;
                ProcessTalentData(csvTalents);
                
                isDataLoaded = true;
                loadStatus = $"Generated {totalTalentCount} talents ({normalTalentCount} normal, {specialTalentCount} special) for {maxPlayerLevel} levels";
                OnDataLoaded?.Invoke();
                
                Debug.Log($"[TalentDatabase] {loadStatus}");
            }
            catch (Exception ex)
            {
                isDataLoaded = false;
                loadStatus = $"Error: {ex.Message}";
                OnLoadingError?.Invoke(ex.Message);
                Debug.LogError($"[TalentDatabase] Failed to load: {ex.Message}");
            }
        }

        /// <summary>
        /// Process talent data: Extract base stats + special nodes, then auto-generate
        /// </summary>
        private void ProcessTalentData(List<TalentModel> csvTalents)
        {
            ClearData();
            
            // Step 1: Extract base stats and special nodes from CSV
            ExtractBaseStatsAndSpecials(csvTalents);
            
            // Step 2: Auto-generate normal nodes for all levels
            AutoGenerateNormalNodes();
            
            // Step 3: Apply positioning
            ApplyUniformPositioning();
            
            // Step 4: Organize by zones
            OrganizeTalentsByZones();
            
            // Step 5: Update statistics
            UpdateStatistics();
        }

        /// <summary>
        /// Extract base stats (4 types) and special nodes from CSV
        /// </summary>
        private void ExtractBaseStatsAndSpecials(List<TalentModel> csvTalents)
        {
            baseStats.Clear();
            
            foreach (var talent in csvTalents)
            {
                if (!talent.ValidateData()) continue;
                
                talent.OnDataLoaded();
                FixTalentIconPath(talent);
                
                if (talent.NodeType == TalentNodeType.Normal)
                {
                    // Store as base stat template
                    string statKey = GetStatKey(talent);
                    if (!baseStats.ContainsKey(statKey))
                    {
                        baseStats[statKey] = talent;
                        Debug.Log($"[TalentDatabase] Base stat loaded: {statKey} = {talent.StatValue}");
                    }
                }
                else if (talent.NodeType == TalentNodeType.Special)
                {
                    // Add special nodes directly
                    specialTalents.Add(talent);
                    talentsById[talent.ID] = talent;
                    Debug.Log($"[TalentDatabase] Special talent loaded: {talent.Name} (Level {talent.RequiredPlayerLevel})");
                }
            }
        }

        /// <summary>
        /// Auto-generate normal nodes for all levels based on base stats
        /// </summary>
        private void AutoGenerateNormalNodes()
        {
            if (!layoutConfig.AutoGenerateNormalNodes) return;
            
            int currentId = 10000; // Start normal IDs from 10000
            
            for (int level = 1; level <= layoutConfig.MaxPlayerLevel; level++)
            {
                foreach (var baseStatPair in baseStats)
                {
                    var baseStat = baseStatPair.Value;
                    var generatedTalent = CreateNormalNodeForLevel(currentId, baseStat, level);
                    
                    normalTalents.Add(generatedTalent);
                    talentsById[currentId] = generatedTalent;
                    allTalents.Add(generatedTalent);
                    
                    currentId++;
                }
            }
            
            // Add special talents to all talents list
            allTalents.AddRange(specialTalents);
        }

        /// <summary>
        /// Create normal node for specific level based on base stat
        /// </summary>
        private TalentModel CreateNormalNodeForLevel(int id, TalentModel baseStat, int level)
        {
            var talent = new TalentModel
            {
                ID = id,
                Name = GetLeveledStatName(baseStat, level),
                Description = GetLeveledStatDescription(baseStat, level),
                StatValue = CalculateStatValueForLevel(baseStat.StatValue, level),
                StatType = baseStat.StatType,
                StatTypeString = baseStat.StatTypeString,
                NodeType = TalentNodeType.Normal,
                NodeTypeString = "Normal",
                Cost = CalculateCostForLevel(baseStat.Cost, level),
                MaxLevel = 1,
                RequiredPlayerLevel = level,
                IconPath = baseStat.IconPath,
                PositionX = 0, // Will be set in positioning
                PositionY = 0  // Will be set in positioning
            };

            talent.OnDataLoaded();
            return talent;
        }

        /// <summary>
        /// Get stat key for base stat identification
        /// </summary>
        private string GetStatKey(TalentModel talent)
        {
            if (talent.Name.Contains("Attack") || talent.StatType == UpgradeType.Damage)
                return "ATK";
            if (talent.Name.Contains("Defense") || talent.Name.Contains("Armor"))
                return "DEF";
            if (talent.Name.Contains("Speed") || talent.StatType == UpgradeType.Speed)
                return "SPEED";
            if (talent.Name.Contains("Heal") || talent.Name.Contains("Health"))
                return "HEAL";
            
            return "UNKNOWN";
        }

        /// <summary>
        /// Get leveled stat name
        /// </summary>
        private string GetLeveledStatName(TalentModel baseStat, int level)
        {
            string statKey = GetStatKey(baseStat);
            return level == 1 ? statKey : $"{statKey} {ToRoman(level)}";
        }

        /// <summary>
        /// Get leveled stat description
        /// </summary>
        private string GetLeveledStatDescription(TalentModel baseStat, int level)
        {
            string statKey = GetStatKey(baseStat);
            return $"Level {level} {statKey.ToLower()} enhancement. Base power increased by tier progression.";
        }

        /// <summary>
        /// Calculate stat value for specific level using growth formula
        /// </summary>
        private float CalculateStatValueForLevel(float baseValue, int level)
        {
            // Linear growth with slight exponential curve
            // Formula: baseValue * (1 + (level-1) * growthRate)
            return baseValue * (1f + (level - 1) * (layoutConfig.StatGrowthRate - 1f));
        }

        /// <summary>
        /// Calculate cost for specific level
        /// </summary>
        private int CalculateCostForLevel(int baseCost, int level)
        {
            // Exponential cost growth
            return Mathf.RoundToInt(baseCost * Mathf.Pow(layoutConfig.CostGrowthRate, level - 1));
        }

        /// <summary>
        /// Apply uniform positioning to all talents
        /// </summary>
        private void ApplyUniformPositioning()
        {
            float currentY = layoutConfig.StartY;
            int nodeCount = 0;
            
            // Sort all talents by level, then by type (normal first)
            var sortedTalents = allTalents
                .OrderBy(t => t.RequiredPlayerLevel)
                .ThenBy(t => t.NodeType) // Normal before Special
                .ThenBy(t => GetStatKey(t)) // ATK, DEF, SPEED, HEAL order
                .ToList();

            foreach (var talent in sortedTalents)
            {
                // Set X position based on type
                talent.PositionX = talent.NodeType == TalentNodeType.Normal ? 
                    layoutConfig.NormalColumnX : layoutConfig.SpecialColumnX;
                
                // Set Y position with uniform spacing
                talent.PositionY = currentY;
                
                // Increment Y for next node
                currentY += layoutConfig.NodeSpacing;
                nodeCount++;
            }
            
            Debug.Log($"[TalentDatabase] Positioned {nodeCount} talents with uniform spacing");
        }

        /// <summary>
        /// Organize talents by zones
        /// </summary>
        private void OrganizeTalentsByZones()
        {
            talentsByZone.Clear();
            
            foreach (var talent in allTalents)
            {
                int zone = talent.RequiredPlayerLevel;
                
                if (!talentsByZone.ContainsKey(zone))
                    talentsByZone[zone] = new List<TalentModel>();
                
                talentsByZone[zone].Add(talent);
            }
        }

        /// <summary>
        /// Update statistics
        /// </summary>
        private void UpdateStatistics()
        {
            totalTalentCount = allTalents.Count;
            normalTalentCount = normalTalents.Count;
            specialTalentCount = specialTalents.Count;
            maxPlayerLevel = layoutConfig.MaxPlayerLevel;
        }

        /// <summary>
        /// Clear all data
        /// </summary>
        private void ClearData()
        {
            allTalents.Clear();
            normalTalents.Clear();
            specialTalents.Clear();
            talentsById.Clear();
            talentsByZone.Clear();
            baseStats.Clear();
            totalTalentCount = 0;
            normalTalentCount = 0;
            specialTalentCount = 0;
            maxPlayerLevel = 0;
        }

        /// <summary>
        /// Fix talent icon path
        /// </summary>
        private void FixTalentIconPath(TalentModel talent)
        {
            if (string.IsNullOrEmpty(talent.IconPath))
            {
                talent.IconPath = talent.NodeType == TalentNodeType.Normal ? 
                    GetDefaultNormalIcon(talent) : layoutConfig.DefaultSpecialIcon;
            }
            
            if (talent.IconPath.StartsWith(layoutConfig.IconBasePath))
            {
                talent.IconPath = talent.IconPath.Substring(layoutConfig.IconBasePath.Length);
            }
        }

        /// <summary>
        /// Get default icon for normal talent
        /// </summary>
        private string GetDefaultNormalIcon(TalentModel talent)
        {
            string statKey = GetStatKey(talent);
            return statKey.ToLower() + "_icon";
        }

        /// <summary>
        /// Convert number to Roman numerals
        /// </summary>
        private string ToRoman(int number)
        {
            if (number <= 1) return "";
            
            return number switch
            {
                2 => "II", 3 => "III", 4 => "IV", 5 => "V",
                6 => "VI", 7 => "VII", 8 => "VIII", 9 => "IX", 10 => "X",
                _ => number.ToString()
            };
        }

        // Public API methods

        public TalentModel GetTalentById(int id)
        {
            return talentsById.GetValueOrDefault(id);
        }

        public TalentModel[] GetAllTalents()
        {
            return allTalents.ToArray();
        }

        public List<TalentModel> GetTalentsInZone(int zoneLevel)
        {
            return talentsByZone.GetValueOrDefault(zoneLevel, new List<TalentModel>());
        }

        public List<TalentModel> GetNormalTalentsInZone(int zoneLevel)
        {
            var zoneTalents = GetTalentsInZone(zoneLevel);
            return zoneTalents.Where(t => t.NodeType == TalentNodeType.Normal).ToList();
        }

        public TalentModel GetSpecialTalentInZone(int zoneLevel)
        {
            var zoneTalents = GetTalentsInZone(zoneLevel);
            return zoneTalents.FirstOrDefault(t => t.NodeType == TalentNodeType.Special);
        }

        public TalentModel GetPreviousTalent(int talentId)
        {
            var talent = GetTalentById(talentId);
            if (talent == null) return null;
            
            if (talent.NodeType == TalentNodeType.Normal)
            {
                return GetPreviousNormalTalent(talent);
            }
            else if (talent.NodeType == TalentNodeType.Special)
            {
                return GetPreviousSpecialTalent(talent);
            }
            
            return null;
        }

        private TalentModel GetPreviousNormalTalent(TalentModel talent)
        {
            var statKey = GetStatKey(talent);
            var previousLevel = talent.RequiredPlayerLevel - 1;
            
            if (previousLevel < 1) return null;
            
            var previousZoneNormals = GetNormalTalentsInZone(previousLevel);
            return previousZoneNormals.FirstOrDefault(t => GetStatKey(t) == statKey);
        }

        private TalentModel GetPreviousSpecialTalent(TalentModel talent)
        {
            var currentLevel = talent.RequiredPlayerLevel;
            
            for (int level = currentLevel - 1; level >= 1; level--)
            {
                var previousSpecial = GetSpecialTalentInZone(level);
                if (previousSpecial != null) return previousSpecial;
            }
            
            return null;
        }

        public List<int> GetTalentDependencies(int talentId)
        {
            var dependencies = new List<int>();
            var previousTalent = GetPreviousTalent(talentId);
            
            if (previousTalent != null)
                dependencies.Add(previousTalent.ID);
            
            return dependencies;
        }

        public int GetTalentCost(int talentId, int targetLevel = 1)
        {
            var talent = GetTalentById(talentId);
            return talent?.Cost ?? 0;
        }

        public bool HasTalent(int talentId)
        {
            return isDataLoaded && talentsById.ContainsKey(talentId);
        }

        public List<int> GetActiveZones()
        {
            return talentsByZone.Keys.OrderBy(k => k).ToList();
        }

        /// <summary>
        /// Calculate exact content height needed
        /// </summary>
        public float CalculateRequiredContentHeight()
        {
            if (!isDataLoaded || allTalents.Count == 0) 
                return layoutConfig.StartY + 500f; // Default height

            // Get the highest positioned talent
            float maxY = allTalents.Max(t => t.PositionY);
            
            // Add padding for the last node size and some extra space
            float nodeHeight = layoutConfig.NormalNodeSize.y;
            float padding = 100f; // Minimal padding
            
            return maxY + nodeHeight + padding;
        }

        [ContextMenu("Reload CSV Data")]
        public void ReloadCSVData()
        {
            if (Application.isPlaying)
            {
                LoadTalentData();
            }
        }

        [ContextMenu("Log Generation Stats")]
        public void LogGenerationStats()
        {
            if (!isDataLoaded) return;

            Debug.Log($"=== AUTO-GENERATION STATS ===");
            Debug.Log($"Base Stats: {baseStats.Count}");
            foreach (var baseStat in baseStats)
            {
                Debug.Log($"  {baseStat.Key}: {baseStat.Value.StatValue} (Cost: {baseStat.Value.Cost})");
            }
            
            Debug.Log($"Generated Normal Talents: {normalTalentCount}");
            Debug.Log($"Special Talents: {specialTalentCount}");
            Debug.Log($"Total Levels: {maxPlayerLevel}");
            Debug.Log($"Required Content Height: {CalculateRequiredContentHeight():F0}px");
            
            var sampleLevel5 = GetNormalTalentsInZone(5);
            if (sampleLevel5.Count > 0)
            {
                Debug.Log($"Level 5 Sample:");
                foreach (var talent in sampleLevel5)
                {
                    Debug.Log($"  {talent.Name}: {talent.StatValue:F1} (Cost: {talent.Cost})");
                }
            }
        }
    }

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