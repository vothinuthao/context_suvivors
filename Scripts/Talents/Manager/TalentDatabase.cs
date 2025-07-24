using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OctoberStudio.Upgrades;
using TwoSleepyCats.CSVReader.Core;
using TwoSleepyCats.Patterns.Singleton;
using UnityEngine;
using Talents.Data;

namespace Talents.Manager
{
    /// <summary>
    /// Database that implements linear talent progression system
    /// Single column layout optimized for mobile (1080x2160)
    /// </summary>
    public class TalentDatabase : MonoSingleton<TalentDatabase>
    {
        [Header("Linear Progression Settings")]
        [SerializeField] private int maxLevels = 50;
        
        [Header("Mobile Layout Settings")]
        [SerializeField] private float nodeSpacing = 450f;    // 4 nodes visible on 1080x2160
        [SerializeField] private float startY = 0f;           // Start from bottom
        
        [Header("Debug Info")]
        [SerializeField, ReadOnly] private int totalTalentCount = 0;
        [SerializeField, ReadOnly] private bool isDataLoaded = false;
        [SerializeField, ReadOnly] private string loadStatus = "Not Loaded";

        // Talent collections
        private Dictionary<int, TalentModel> talentsById = new Dictionary<int, TalentModel>();
        private List<TalentModel> allTalents = new List<TalentModel>();
        
        // Configuration data
        private Dictionary<BaseStatType, TalentConfigModel> statConfigs = new Dictionary<BaseStatType, TalentConfigModel>();
        
        // Linear progression pattern - ATK → DEF → Speed → Heal
        private readonly BaseStatType[] statPattern = { 
            BaseStatType.ATK, 
            BaseStatType.DEF, 
            BaseStatType.Speed, 
            BaseStatType.Heal 
        };

        // Events
        public event System.Action OnDataLoaded;
        public event System.Action<string> OnLoadingError;

        // Properties
        public bool IsDataLoaded => isDataLoaded;
        public int TotalTalentCount => totalTalentCount;
        public List<TalentModel> AllTalents => allTalents;

        protected override void Initialize()
        {
            base.Initialize();
            LoadTalentData();
        }

        public void LoadTalentData()
        {
            StartCoroutine(LoadTalentDataCoroutine());
        }

        private IEnumerator LoadTalentDataCoroutine()
        {
            loadStatus = "Loading...";

            // Load configuration from CSV
            var configTask = CsvDataManager.Instance.LoadAsync<TalentConfigModel>();
            yield return new WaitUntil(() => configTask.IsCompleted);

            try
            {
                if (configTask.Exception != null)
                {
                    throw configTask.Exception.GetBaseException();
                }

                var configData = configTask.Result;
                
                // Process configuration and generate talents
                ProcessConfigData(configData);
                GenerateLinearProgression();
                
                isDataLoaded = true;
                loadStatus = $"Loaded {totalTalentCount} talents";
                OnDataLoaded?.Invoke();
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
        /// Process configuration data from CSV
        /// </summary>
        private void ProcessConfigData(List<TalentConfigModel> configData)
        {
            statConfigs.Clear();
            
            foreach (var config in configData)
            {
                if (Enum.TryParse<BaseStatType>(config.StatType, true, out var statType))
                {
                    statConfigs[statType] = config;
                }
                else
                {
                    Debug.LogWarning($"[TalentDatabase] Unknown stat type in config: {config.StatType}");
                }
            }
        }

        /// <summary>
        /// Generate linear progression talents - each level = 1 stat increase
        /// </summary>
        private void GenerateLinearProgression()
        {
            ClearData();
            
            int talentId = 1;
            float currentY = startY;

            for (int level = 1; level <= maxLevels; level++)
            {
                // Determine stat type using rotation pattern
                BaseStatType statType = statPattern[(level - 1) % statPattern.Length];
                
                // Get configuration for this stat type
                if (!statConfigs.TryGetValue(statType, out var config))
                {
                    Debug.LogError($"[TalentDatabase] No configuration found for stat type: {statType}");
                    continue;
                }

                // Create talent
                var talent = CreateLinearTalent(talentId, statType, level, currentY, config);
                
                allTalents.Add(talent);
                talentsById[talentId] = talent;

                talentId++;
                currentY += nodeSpacing; // Move up for next level
            }
            
            totalTalentCount = allTalents.Count;
        }

        /// <summary>
        /// Create a linear progression talent
        /// </summary>
        private TalentModel CreateLinearTalent(int id, BaseStatType statType, int level, float posY, TalentConfigModel config)
        {
            var talent = new TalentModel
            {
                ID = id,
                Name = GetStatName(statType, level),
                Description = GetStatDescription(statType, level, config),
                StatValue = CalculateStatValue(statType, level, config),
                StatType = GetUpgradeType(statType),
                StatTypeString = statType.ToString(),
                NodeType = TalentNodeType.Normal,
                NodeTypeString = "Normal",
                PositionX = 0f,  // Single column - center aligned
                PositionY = posY,
                Cost = CalculateCost(level, config),
                MaxLevel = 1, // Linear system - each talent is 1 level only
                RequiredPlayerLevel = level,
                IconPath = config.Icon
            };

            talent.OnDataLoaded();
            return talent;
        }

        /// <summary>
        /// Get stat name for display
        /// </summary>
        private string GetStatName(BaseStatType statType, int level)
        {
            string baseName = statType switch
            {
                BaseStatType.ATK => "Attack",
                BaseStatType.DEF => "Defense", 
                BaseStatType.Speed => "Speed",
                BaseStatType.Heal => "Healing",
                _ => "Unknown"
            };

            return $"{baseName} Lv.{level}";
        }

        /// <summary>
        /// Get stat description
        /// </summary>
        private string GetStatDescription(BaseStatType statType, int level, TalentConfigModel config)
        {
            float statValue = CalculateStatValue(statType, level, config);
            
            return statType switch
            {
                BaseStatType.ATK => $"Increase attack damage by {statValue:F0}",
                BaseStatType.DEF => $"Increase defense by {statValue:F1}",
                BaseStatType.Speed => $"Increase movement speed by {statValue * 100:F1}%",
                BaseStatType.Heal => $"Increase healing by {statValue:F1}",
                _ => "Unknown stat effect"
            };
        }

        /// <summary>
        /// Calculate stat value using linear formula: base_value + (level × multiplier)
        /// </summary>
        private float CalculateStatValue(BaseStatType statType, int level, TalentConfigModel config)
        {
            return config.BaseValue + (level * config.Multiplier);
        }

        /// <summary>
        /// Calculate cost using linear formula: cost_base + (level × cost_per_level)
        /// </summary>
        private int CalculateCost(int level, TalentConfigModel config)
        {
            return config.CostBase + (level * config.CostPerLevel);
        }

        /// <summary>
        /// Get upgrade type from base stat type
        /// </summary>
        private UpgradeType GetUpgradeType(BaseStatType statType)
        {
            return statType switch
            {
                BaseStatType.ATK => UpgradeType.Damage,
                BaseStatType.DEF => UpgradeType.Health, // Map to health for damage reduction
                BaseStatType.Speed => UpgradeType.Speed,
                BaseStatType.Heal => UpgradeType.Health, // Map to health for healing
                _ => UpgradeType.Health
            };
        }

        /// <summary>
        /// Clear all data
        /// </summary>
        private void ClearData()
        {
            allTalents.Clear();
            talentsById.Clear();
            totalTalentCount = 0;
        }

        // Public API methods...
        
        /// <summary>
        /// Get talent by ID
        /// </summary>
        public TalentModel GetTalentById(int id)
        {
            return talentsById.GetValueOrDefault(id);
        }

        /// <summary>
        /// Get all talents
        /// </summary>
        public TalentModel[] GetAllTalents()
        {
            return allTalents.ToArray();
        }

        /// <summary>
        /// Get talent cost (simplified for linear system)
        /// </summary>
        public int GetTalentCost(int talentId, int targetLevel = 1)
        {
            var talent = GetTalentById(talentId);
            return talent?.Cost ?? 0;
        }

        /// <summary>
        /// Get talents that don't have dependencies (simplified system)
        /// </summary>
        public List<int> GetTalentDependencies(int talentId)
        {
            // Linear system: only dependency is the previous level
            if (talentId > 1)
            {
                return new List<int> { talentId - 1 };
            }
            return new List<int>();
        }

        /// <summary>
        /// Check if talent exists
        /// </summary>
        public bool HasTalent(int talentId)
        {
            return isDataLoaded && talentsById.ContainsKey(talentId);
        }

        /// <summary>
        /// Get max talent level (always 1 in linear system)
        /// </summary>
        public int GetMaxTalentLevel(int talentId)
        {
            return 1;
        }

        /// <summary>
        /// Get talents for specific stat type
        /// </summary>
        public List<TalentModel> GetTalentsForStatType(BaseStatType statType)
        {
            return allTalents.Where(t => t.GetBaseStatType() == statType).ToList();
        }

        /// <summary>
        /// Get talents available for player level
        /// </summary>
        public List<TalentModel> GetAvailableTalents(int playerLevel)
        {
            return allTalents.Where(t => t.RequiredPlayerLevel <= playerLevel).ToList();
        }

        /// <summary>
        /// Get next talent to unlock
        /// </summary>
        public TalentModel GetNextTalent(int currentPlayerLevel)
        {
            return allTalents.FirstOrDefault(t => t.RequiredPlayerLevel == currentPlayerLevel + 1);
        }

        [ContextMenu("Reload Talent Data")]
        public void ReloadTalentData()
        {
            LoadTalentData();
        }

        /// <summary>
        /// Log talent tree for debugging
        /// </summary>
        [ContextMenu("Log Linear Talent Tree")]
        public void LogTalentTree()
        {
            if (!isDataLoaded) return;

            Debug.Log($"=== LINEAR TALENT TREE ===");
            Debug.Log($"Total: {totalTalentCount} talents");
            Debug.Log($"Max Levels: {maxLevels}");
            Debug.Log($"Pattern: {string.Join(" → ", statPattern)}");
            Debug.Log($"Layout: Single Column (Mobile Optimized)");
            Debug.Log($"Node Spacing: {nodeSpacing}px");
            
            Debug.Log("\n=== TALENT PROGRESSION ===");
            for (int i = 0; i < allTalents.Count && i < 20; i++) // Show first 20
            {
                var talent = allTalents[i];
                Debug.Log($"{i+1}. {talent.Name} - Value: {talent.StatValue:F1} - Cost: {talent.Cost} - Pos: (0, {talent.PositionY:F0})");
            }
        }

        [ContextMenu("Debug Mobile Layout")]
        public void DebugMobileLayout()
        {
            if (!isDataLoaded) return;

            Debug.Log($"=== MOBILE LAYOUT DEBUG (1080x2160) ===");
            Debug.Log($"Node Spacing: {nodeSpacing}px");
            Debug.Log($"Viewport Height: ~1800px (usable)");
            Debug.Log($"Nodes per screen: {1800f / nodeSpacing:F1}");
            Debug.Log($"Total Talents: {allTalents.Count}");
            
            if (allTalents.Count > 0)
            {
                var firstTalent = allTalents[0];
                var lastTalent = allTalents[allTalents.Count - 1];
                var totalHeight = lastTalent.PositionY - firstTalent.PositionY;
                var screenCount = totalHeight / 1800f;
                Debug.Log($"Total height: {totalHeight:F0}px ({screenCount:F1} screens)");
            }
        }
    }

    // ReadOnly attribute for inspector
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
