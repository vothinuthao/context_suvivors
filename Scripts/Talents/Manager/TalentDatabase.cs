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
    /// Database that auto-generates normal stats (LEFT column) and loads special skills (RIGHT column) from CSV
    /// Spawns from bottom to top for mobile-friendly scrolling
    /// </summary>
    public class TalentDatabase : MonoSingleton<TalentDatabase>
    {
        [Header("Normal Stats Generation")]
        [SerializeField] private int maxPlayerLevel = 50;
        [SerializeField] private float baseStatStartValue = 5f;
        [SerializeField] private float baseStatGrowthRate = 1.2f;
        [SerializeField] private int baseCost = 50;
        [SerializeField] private float costGrowthRate = 1.1f;
        
        [Header("Mobile Layout Settings")]
        [SerializeField] private float nodeSpacing = 450f;         // Mobile-optimized: 4 nodes visible (1800px ÷ 4)
        [SerializeField] private float positionX = 0f;             // Single column: center aligned
        [SerializeField] private float startY = 0f;               // Start from bottom
        
        [Header("Debug Info")]
        [SerializeField, ReadOnly] private int totalTalentCount = 0;
        [SerializeField, ReadOnly] private bool isDataLoaded = false;
        [SerializeField, ReadOnly] private string loadStatus = "Not Loaded";

        // Talent collections
        private Dictionary<int, TalentModel> talentsById = new Dictionary<int, TalentModel>();
        private List<TalentModel> linearTalents = new List<TalentModel>();
        private List<TalentModel> allTalents = new List<TalentModel>();
        
        // Linear progression stat rotation pattern: ATK → DEF → Speed → Heal
        private readonly string[] statRotation = { "ATK", "DEF", "Speed", "Heal" };
        private readonly string[] statIcons = { "damage_talent", "titan_talent", "speed_talent", "phoenix_talent" };
        
        // Stat formulas from CSV (base_value, multiplier, cost_base, cost_per_level)
        private Dictionary<string, StatFormula> statFormulas = new Dictionary<string, StatFormula>();

        // Events
        public event System.Action OnDataLoaded;
        public event System.Action<string> OnLoadingError;

        // Properties
        public bool IsDataLoaded => isDataLoaded;
        public int TotalTalentCount => totalTalentCount;
        public List<TalentModel> LinearTalents => linearTalents;

        protected override void Initialize()
        {
            base.Initialize();
            
            // Set mobile-optimized spacing by default
            SetMobileSpacing();
            
            LoadStatFormulas();
            LoadTalentData();
        }

        public void LoadTalentData()
        {
            StartCoroutine(LoadTalentDataCoroutine());
        }

        private IEnumerator LoadTalentDataCoroutine()
        {
            loadStatus = "Loading...";

            // Generate linear talent progression
            try
            {
                ProcessTalentData();
                
                isDataLoaded = true;
                loadStatus = $"Loaded {totalTalentCount} linear talents";
                OnDataLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                isDataLoaded = false;
                loadStatus = $"Error: {ex.Message}";
                OnLoadingError?.Invoke(ex.Message);
                Debug.LogError($"[TalentDatabase] Failed to load: {ex.Message}");
            }
            
            yield break;
        }

        /// <summary>
        /// Load stat formulas from configuration
        /// </summary>
        private void LoadStatFormulas()
        {
            // Default stat formulas matching the CSV specification
            statFormulas["ATK"] = new StatFormula { baseValue = 10f, multiplier = 2f, costBase = 100, costPerLevel = 10 };
            statFormulas["DEF"] = new StatFormula { baseValue = 5f, multiplier = 1.5f, costBase = 100, costPerLevel = 10 };
            statFormulas["Speed"] = new StatFormula { baseValue = 0.1f, multiplier = 0.05f, costBase = 100, costPerLevel = 10 };
            statFormulas["Heal"] = new StatFormula { baseValue = 2f, multiplier = 1f, costBase = 100, costPerLevel = 10 };
        }

        /// <summary>
        /// Process and organize talent data - generate linear progression system
        /// </summary>
        private void ProcessTalentData()
        {
            ClearData();

            // Generate linear progression (50 levels)
            GenerateLinearProgression();
            
            // Finalize
            allTalents.AddRange(linearTalents);
            totalTalentCount = allTalents.Count;
        }

        /// <summary>
        /// Generate linear progression: 1 stat boost per level, rotating ATK→DEF→Speed→Heal every 4 levels
        /// </summary>
        private void GenerateLinearProgression()
        {
            linearTalents.Clear();

            for (int level = 1; level <= maxPlayerLevel; level++)
            {
                // Stat rotation every 4 levels: ATK → DEF → Speed → Heal
                int statIndex = (level - 1) % 4;
                string currentStat = statRotation[statIndex];
                string iconPath = statIcons[statIndex];

                // Get stat formula
                if (!statFormulas.TryGetValue(currentStat, out var formula))
                {
                    Debug.LogError($"[TalentDatabase] No formula found for stat: {currentStat}");
                    continue;
                }

                // Linear scaling formula: base_value + (level × multiplier)
                float statValue = formula.baseValue + (level * formula.multiplier);

                // Linear cost progression: cost_base + (level × cost_per_level)
                int cost = formula.costBase + (level * formula.costPerLevel);

                // Mobile positioning: single column, 450px spacing, bottom-to-top
                float positionY = -(level - 1) * nodeSpacing; // Y = 0, -450, -900, etc.

                var talent = CreateLinearTalent(level, currentStat, statValue, cost, positionY, iconPath);
                linearTalents.Add(talent);
                talentsById[level] = talent;
            }
        }

        /// <summary>
        /// Create a linear talent for the new system
        /// </summary>
        private TalentModel CreateLinearTalent(int level, string statType, float statValue, int cost, float posY, string iconPath)
        {
            var talent = new TalentModel
            {
                ID = level,
                Name = $"{statType} Boost",
                Description = GetStatDescription(statType, level, statValue),
                StatValue = statValue,
                StatType = GetUpgradeTypeFromString(statType),
                StatTypeString = statType,
                NodeType = TalentNodeType.Normal,
                NodeTypeString = "Normal",
                PositionX = positionX,  // Single column: center aligned (X = 0)
                PositionY = posY,       // Mobile spacing: 450px apart
                Cost = cost,
                MaxLevel = 1,           // Linear system: each level = 1 talent
                RequiredPlayerLevel = level,
                IconPath = iconPath
            };

            talent.OnDataLoaded();
            return talent;
        }

        /// <summary>
        /// Get stat description for linear system
        /// </summary>
        private string GetStatDescription(string statType, int level, float statValue)
        {
            return statType switch
            {
                "ATK" => $"Increase attack damage by +{statValue:F0} (Level {level})",
                "DEF" => $"Increase defense by +{statValue:F0} (Level {level})",
                "Speed" => $"Increase movement speed by +{statValue:F2} (Level {level})",
                "Heal" => $"Increase health regeneration by +{statValue:F0} (Level {level})",
                _ => $"Unknown stat boost (Level {level})"
            };
        }

        /// <summary>
        /// Get upgrade type from stat string
        /// </summary>
        private UpgradeType GetUpgradeTypeFromString(string statType)
        {
            return statType switch
            {
                "ATK" => UpgradeType.Damage,
                "DEF" => UpgradeType.Damage, // Map defense to damage for damage reduction
                "Speed" => UpgradeType.Speed,
                "Heal" => UpgradeType.Health,
                _ => UpgradeType.Health
            };
        }

        /// <summary>
        /// Clear all data
        /// </summary>
        private void ClearData()
        {
            allTalents.Clear();
            linearTalents.Clear();
            talentsById.Clear();
            totalTalentCount = 0;
        }

        // Public API methods remain the same as before...
        
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
        /// Get talent cost for specific level (linear system)
        /// </summary>
        public int GetTalentCost(int talentId, int targetLevel = 1)
        {
            var talent = GetTalentById(talentId);
            if (talent == null) return 0;

            // Linear system: each talent has a fixed cost
            return talent.Cost;
        }

        /// <summary>
        /// Get talents that don't have dependencies (for simplified system)
        /// </summary>
        public List<int> GetTalentDependencies(int talentId)
        {
            // In this simplified system, no dependencies
            return new List<int>();
        }

        /// <summary>
        /// Get talents unlocked by this talent
        /// </summary>
        public List<int> GetTalentUnlocks(int talentId)
        {
            // In this simplified system, no unlock chains
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
        /// Get max talent level
        /// </summary>
        public int GetMaxTalentLevel(int talentId)
        {
            var talent = GetTalentById(talentId);
            return talent?.MaxLevel ?? 0;
        }

        /// <summary>
        /// Get linear talents for specific level range
        /// </summary>
        public List<TalentModel> GetLinearTalentsForLevelRange(int minLevel, int maxLevel)
        {
            return linearTalents.Where(t => t.RequiredPlayerLevel >= minLevel && t.RequiredPlayerLevel <= maxLevel).ToList();
        }

        /// <summary>
        /// Get next talent to unlock
        /// </summary>
        public TalentModel GetNextTalentToUnlock(int currentPlayerLevel)
        {
            return linearTalents.FirstOrDefault(t => t.RequiredPlayerLevel == currentPlayerLevel + 1);
        }

        /// <summary>
        /// Set custom spacing values for linear progression
        /// </summary>
        public void SetSpacing(float newNodeSpacing)
        {
            this.nodeSpacing = newNodeSpacing;
            
            // Regenerate layout if data is loaded
            if (isDataLoaded)
            {
                ProcessTalentData();
            }
        }

        /// <summary>
        /// Quick spacing presets for mobile optimization
        /// </summary>
        [ContextMenu("Mobile Spacing (1080x2160)")]
        public void SetMobileSpacing()
        {
            SetSpacing(450f); // 4 nodes visible in 1800px viewport
        }

        [ContextMenu("Compact Spacing")]
        public void SetCompactSpacing()
        {
            SetSpacing(300f); // 6 nodes visible
        }

        [ContextMenu("Normal Spacing")]
        public void SetNormalSpacing()
        {
            SetSpacing(400f); // ~4.5 nodes visible
        }

        [ContextMenu("Wide Spacing")]
        public void SetWideSpacing()
        {
            SetSpacing(600f); // 3 nodes visible
        }
        [ContextMenu("Reload Talent Data")]
        public void ReloadTalentData()
        {
            try
            {
                isDataLoaded = false;
                loadStatus = "Reloading...";
                
                LoadStatFormulas();
                ProcessTalentData();
                
                isDataLoaded = true;
                loadStatus = $"Reloaded {totalTalentCount} linear talents";
                OnDataLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                isDataLoaded = false;
                loadStatus = $"Reload Error: {ex.Message}";
                OnLoadingError?.Invoke(ex.Message);
                Debug.LogError($"[TalentDatabase] Failed to reload: {ex.Message}");
            }
        }

        /// <summary>
        /// Log talent info for debugging
        /// </summary>
        [ContextMenu("Log Talent Tree")]
        public void LogTalentTree()
        {
            if (!isDataLoaded) return;

            Debug.Log($"=== LINEAR TALENT TREE ===");
            Debug.Log($"Total: {totalTalentCount} linear talents");
            Debug.Log($"Max Player Level: {maxPlayerLevel}");
            Debug.Log($"Stat Rotation: {string.Join(" → ", statRotation)}");
            Debug.Log($"Layout: Single Column (Mobile Optimized)");
            Debug.Log($"Spawn Direction: Bottom to Top (Y: {startY} upward)");
            Debug.Log($"Node Spacing: {nodeSpacing}px");
            
            Debug.Log("\n=== LINEAR PROGRESSION ===");
            for (int i = 0; i < linearTalents.Count && i < 10; i++) // Show first 10 levels
            {
                var talent = linearTalents[i];
                Debug.Log($"Level {talent.RequiredPlayerLevel}: {talent.Name} - Value: +{talent.StatValue:F1} - Cost: {talent.Cost} - Pos: ({talent.PositionX}, {talent.PositionY:F0})");
            }
            
            if (linearTalents.Count > 10)
            {
                Debug.Log($"... and {linearTalents.Count - 10} more levels ...");
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
            Debug.Log($"Total Linear Talents: {linearTalents.Count}");
            Debug.Log($"Layout Type: Single Column (X = {positionX})");
            
            if (linearTalents.Count > 0)
            {
                var firstTalent = linearTalents[0];
                var lastTalent = linearTalents[linearTalents.Count - 1];
                var totalHeight = Mathf.Abs(lastTalent.PositionY - firstTalent.PositionY);
                var screenCount = totalHeight / 1800f;
                Debug.Log($"Total tree height: {totalHeight:F0}px");
                Debug.Log($"Tree spans {screenCount:F1} screens");
                Debug.Log($"First talent: Level {firstTalent.RequiredPlayerLevel} at Y={firstTalent.PositionY}");
                Debug.Log($"Last talent: Level {lastTalent.RequiredPlayerLevel} at Y={lastTalent.PositionY}");
            }
            
            // Show progression examples
            Debug.Log("\n=== PROGRESSION EXAMPLES ===");
            for (int i = 1; i <= 8 && i <= linearTalents.Count; i++)
            {
                var talent = linearTalents.FirstOrDefault(t => t.RequiredPlayerLevel == i);
                if (talent != null)
                {
                    Debug.Log($"Level {i}: {talent.Name} = +{talent.StatValue:F2} | Cost: {talent.Cost} gold");
                }
            }
        }
    }

    /// <summary>
    /// Stat formula for linear progression system
    /// </summary>
    [System.Serializable]
    public struct StatFormula
    {
        public float baseValue;     // Base stat value
        public float multiplier;    // Multiplier per level
        public int costBase;        // Base cost
        public int costPerLevel;    // Cost increase per level
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
