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
        
        [Header("Layout Settings")]
        [SerializeField] private float leftColumnX = -200f;   // Normal stats column
        [SerializeField] private float rightColumnX = 200f;   // Special skills column  
        [SerializeField] private float normalNodeSpacing = 450f;    // Mobile optimized: 4 nodes visible
        [SerializeField] private float specialNodeSpacing = 450f;  // Consistent spacing
        [SerializeField] private float tierSpacing = 0f;           // No tier spacing for linear progression
        [SerializeField] private float startY = 50f;              // Start from bottom with small padding
        
        [Header("Debug Info")]
        [SerializeField, ReadOnly] private int totalTalentCount = 0;
        [SerializeField, ReadOnly] private bool isDataLoaded = false;
        [SerializeField, ReadOnly] private string loadStatus = "Not Loaded";

        // Talent collections
        private Dictionary<int, TalentModel> talentsById = new Dictionary<int, TalentModel>();
        private List<TalentModel> normalTalents = new List<TalentModel>();
        private List<TalentModel> specialTalents = new List<TalentModel>();
        private List<TalentModel> allTalents = new List<TalentModel>();
        
        // Linear progression pattern: ATK → DEF → Speed → Heal
        private readonly BaseStatType[] statPattern = { 
            BaseStatType.ATK, 
            BaseStatType.Armor, 
            BaseStatType.Speed, 
            BaseStatType.Healing 
        };

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
            
            // Set mobile-optimized spacing by default
            SetMobileSpacing();
            
            LoadTalentData();
        }

        public void LoadTalentData()
        {
            StartCoroutine(LoadTalentDataCoroutine());
        }

        private IEnumerator LoadTalentDataCoroutine()
        {
            loadStatus = "Loading...";

            // Load special skills from CSV - yield OUTSIDE of try/catch
            var loadTask = CsvDataManager.Instance.LoadAsync<TalentModel>();
            yield return new WaitUntil(() => loadTask.IsCompleted);

            // Handle the result in try/catch (no yield statements here)
            try
            {
                if (loadTask.Exception != null)
                {
                    throw loadTask.Exception.GetBaseException();
                }

                var specialSkillsData = loadTask.Result;
                
                // Process all talent data
                ProcessTalentData(specialSkillsData);
                
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
        /// Process and organize talent data - try to load from CSV first, fallback to auto-generation
        /// </summary>
        private void ProcessTalentData(List<TalentModel> csvData)
        {
            ClearData();

            // Check if we have valid CSV data for normal stats
            var normalStatsFromCsv = csvData?.Where(t => t.NodeTypeString?.ToLower() == "normal").ToList();
            
            if (normalStatsFromCsv != null && normalStatsFromCsv.Any())
            {
                // Use CSV data for normal stats (linear progression)
                ProcessNormalStatsFromCsv(normalStatsFromCsv);
            }
            else
            {
                // Fallback: Auto-generate normal stats with linear progression
                GenerateLinearNormalStats();
            }

            // Process special skills from CSV
            ProcessSpecialSkills(csvData);

            // Combine and finalize
            CombineAllTalents();
            
            totalTalentCount = allTalents.Count;
        }

        /// <summary>
        /// Process normal stats from CSV data (linear progression)
        /// </summary>
        private void ProcessNormalStatsFromCsv(List<TalentModel> normalStatsData)
        {
            // Sort by required player level to ensure proper order
            var sortedStats = normalStatsData.OrderBy(t => t.RequiredPlayerLevel).ToList();
            
            foreach (var talent in sortedStats)
            {
                // Ensure proper position for mobile layout
                talent.PositionX = leftColumnX;
                talent.NodeType = TalentNodeType.Normal;
                talent.OnDataLoaded();

                normalTalents.Add(talent);
                talentsById[talent.ID] = talent;
            }
        }

        /// <summary>
        /// Auto-generate linear progression normal stats (fallback)
        /// </summary>
        private void GenerateLinearNormalStats()
        {
            int normalStatId = 1;
            float currentY = startY;

            // Generate exactly one stat per level following the rotation pattern
            for (int level = 1; level <= maxPlayerLevel; level++)
            {
                // Determine stat type based on rotation pattern
                var statType = statPattern[(level - 1) % statPattern.Length];
                
                var normalStat = CreateLinearNormalStat(normalStatId, statType, level, currentY);
                normalTalents.Add(normalStat);
                talentsById[normalStatId] = normalStat;

                normalStatId++;
                currentY += normalNodeSpacing; // 450px spacing for mobile optimization
            }
        }

        /// <summary>
        /// Create a linear progression normal stat
        /// </summary>
        private TalentModel CreateLinearNormalStat(int id, BaseStatType statType, int level, float posY)
        {
            var talent = new TalentModel
            {
                ID = id,
                Name = GetLinearStatName(statType, level),
                Description = GetLinearStatDescription(statType, level),
                StatValue = CalculateLinearStatValue(statType, level),
                StatType = GetUpgradeType(statType),
                StatTypeString = GetUpgradeType(statType).ToString(),
                NodeType = TalentNodeType.Normal,
                NodeTypeString = "Normal",
                PositionX = leftColumnX,
                PositionY = posY,
                Cost = CalculateLinearCost(level),
                MaxLevel = 10, // All normal stats can be upgraded 10 times
                RequiredPlayerLevel = level,
                IconPath = GetStatIconPath(statType)
            };

            talent.OnDataLoaded();
            return talent;
        }

        /// <summary>
        /// Get linear stat name
        /// </summary>
        private string GetLinearStatName(BaseStatType statType, int level)
        {
            string baseName = statType switch
            {
                BaseStatType.ATK => "Attack",
                BaseStatType.Armor => "Defense", 
                BaseStatType.Speed => "Speed",
                BaseStatType.Healing => "Healing",
                _ => "Unknown"
            };

            // Determine tier from level for display
            int tier = ((level - 1) / statPattern.Length) + 1;
            return tier > 1 ? $"{baseName} {ToRoman(tier)}" : baseName;
        }

        /// <summary>
        /// Get linear stat description
        /// </summary>
        private string GetLinearStatDescription(BaseStatType statType, int level)
        {
            int tier = ((level - 1) / statPattern.Length) + 1;
            return statType switch
            {
                BaseStatType.ATK => $"Increase base attack damage (Level {level})",
                BaseStatType.Armor => $"Reduce incoming damage (Level {level})",
                BaseStatType.Speed => $"Increase movement speed (Level {level})",
                BaseStatType.Healing => $"Increase health regeneration (Level {level})",
                _ => "Unknown stat"
            };
        }

        /// <summary>
        /// Calculate linear stat value with proper scaling
        /// </summary>
        private float CalculateLinearStatValue(BaseStatType statType, int level)
        {
            // Tier progression for scaling
            int tier = ((level - 1) / statPattern.Length) + 1;
            
            float baseValue = statType switch
            {
                BaseStatType.ATK => 2f + (tier - 1) * 1f,        // 2, 3, 4, 5... per tier
                BaseStatType.Armor => 1.5f + (tier - 1) * 0.5f,  // 1.5, 2, 2.5, 3... per tier
                BaseStatType.Speed => 0.05f + (tier - 1) * 0.02f, // 0.05, 0.07, 0.09... per tier
                BaseStatType.Healing => 1f + (tier - 1) * 0.5f,   // 1, 1.5, 2, 2.5... per tier
                _ => baseStatStartValue
            };

            return baseValue;
        }

        /// <summary>
        /// Calculate linear cost progression
        /// </summary>
        private int CalculateLinearCost(int level)
        {
            // Linear cost progression: 100 + (level * 10)
            return 100 + (level * 10);
        }

        private UpgradeType GetUpgradeType(BaseStatType statType)
        {
            return statType switch
            {
                BaseStatType.ATK => UpgradeType.Damage,
                BaseStatType.Armor => UpgradeType.Damage, // Map to damage for damage reduction
                BaseStatType.Speed => UpgradeType.Speed,
                BaseStatType.Healing => UpgradeType.Health, // Map to health for healing
                _ => UpgradeType.Health
            };
        }

        private string GetStatIconPath(BaseStatType statType)
        {
            return statType switch
            {
                BaseStatType.ATK => "atk_icon",
                BaseStatType.Armor => "armor_icon", 
                BaseStatType.Speed => "speed_icon",
                BaseStatType.Healing => "healing_icon",
                _ => "default_icon"
            };
        }

        /// <summary>
        /// Process special skills from CSV data
        /// </summary>
        private void ProcessSpecialSkills(List<TalentModel> specialSkillsData)
        {
            float currentY = startY; // Start from bottom
            
            // Sort special skills by required player level
            var sortedSpecialSkills = specialSkillsData
                .Where(t => t.NodeTypeString?.ToLower() == "special")
                .OrderBy(t => t.RequiredPlayerLevel)
                .ToList();

            foreach (var skill in sortedSpecialSkills)
            {
                // Set position - special skills on the RIGHT
                skill.PositionX = rightColumnX;
                skill.PositionY = currentY;
                
                // Ensure it's marked as special
                skill.NodeType = TalentNodeType.Special;
                skill.OnDataLoaded();

                specialTalents.Add(skill);
                talentsById[skill.ID] = skill;

                currentY += specialNodeSpacing; // Use specific spacing for special skills
            }
        }

        /// <summary>
        /// Combine all talents and sort
        /// </summary>
        private void CombineAllTalents()
        {
            allTalents.Clear();
            allTalents.AddRange(normalTalents);
            allTalents.AddRange(specialTalents);
            
            // Sort by position Y (bottom to top)
            allTalents.Sort((a, b) => a.PositionY.CompareTo(b.PositionY));
        }

        /// <summary>
        /// Convert number to Roman numerals
        /// </summary>
        private string ToRoman(int number)
        {
            if (number <= 1) return "";
            
            return number switch
            {
                2 => "II",
                3 => "III", 
                4 => "IV",
                5 => "V",
                6 => "VI",
                7 => "VII",
                8 => "VIII",
                9 => "IX",
                10 => "X",
                _ => number.ToString()
            };
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
        /// Get talent cost for specific level
        /// </summary>
        public int GetTalentCost(int talentId, int targetLevel)
        {
            var talent = GetTalentById(talentId);
            if (talent == null) return 0;

            if (talent.NodeType == TalentNodeType.Normal)
            {
                // Normal stats: cost increases with each upgrade level
                return talent.Cost * targetLevel;
            }
            else
            {
                // Special skills: fixed cost
                return talent.Cost;
            }
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
        /// Get normal stats for specific tier/cycle
        /// </summary>
        public List<TalentModel> GetNormalStatsForTier(int tier)
        {
            return normalTalents.Where(t => t.Name.Contains(ToRoman(tier)) || (tier == 1 && !t.Name.Contains("II"))).ToList();
        }

        /// <summary>
        /// Get special skills available for player level
        /// </summary>
        public List<TalentModel> GetAvailableSpecialSkills(int playerLevel)
        {
            return specialTalents.Where(t => t.RequiredPlayerLevel <= playerLevel).ToList();
        }

        /// <summary>
        /// Get next tier of stats to unlock
        /// </summary>
        public List<TalentModel> GetNextTierStats(int currentPlayerLevel)
        {
            return normalTalents.Where(t => t.RequiredPlayerLevel == currentPlayerLevel + 1).ToList();
        }

        /// <summary>
        /// Set custom spacing values
        /// </summary>
        public void SetSpacing(float normalSpacing, float specialSpacing, float tierSpacing = 0f)
        {
            this.normalNodeSpacing = normalSpacing;
            this.specialNodeSpacing = specialSpacing;
            this.tierSpacing = tierSpacing;
            
            // Regenerate layout if data is loaded
            if (isDataLoaded)
            {
                var specialData = specialTalents.ToList();
                ProcessTalentData(specialData);
            }
        }

        /// <summary>
        /// Quick spacing presets
        /// </summary>
        [ContextMenu("Mobile Spacing (1080x2160)")]
        public void SetMobileSpacing()
        {
            SetSpacing(450f, 450f, 0f); // 4 nodes visible, no tier spacing
        }

        [ContextMenu("Compact Spacing")]
        public void SetCompactSpacing()
        {
            SetSpacing(300f, 350f, 0f); // 5-6 nodes visible
        }

        [ContextMenu("Normal Spacing")]
        public void SetNormalSpacing()
        {
            SetSpacing(450f, 450f, 0f); // 4 nodes visible (default)
        }

        [ContextMenu("Wide Spacing")]
        public void SetWideSpacing()
        {
            SetSpacing(600f, 600f, 0f); // 3 nodes visible
        }
        [ContextMenu("Reload Talent Data")]
        public async void ReloadTalentData()
        {
            try
            {
                isDataLoaded = false;
                loadStatus = "Reloading...";
                
                var specialSkillsData = await CsvDataManager.Instance.ForceReloadAsync<TalentModel>();
                ProcessTalentData(specialSkillsData);
                
                isDataLoaded = true;
                loadStatus = $"Reloaded {totalTalentCount} talents";
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

            Debug.Log($"=== AUTO-GENERATED TALENT TREE ===");
            Debug.Log($"Total: {totalTalentCount} ({normalTalents.Count} normal, {specialTalents.Count} special)");
            Debug.Log($"Max Player Level: {maxPlayerLevel}");
            Debug.Log($"Stat Pattern: {string.Join(" → ", statPattern)}");
            Debug.Log($"Layout: Normal Stats (LEFT), Special Skills (RIGHT)");
            Debug.Log($"Spawn Direction: Bottom to Top (Y: {startY} upward)");
            
            Debug.Log("\n=== NORMAL STATS (LEFT COLUMN) ===");
            for (int i = 0; i < normalTalents.Count; i++)
            {
                var talent = normalTalents[i];
                Debug.Log($"{i+1}. {talent.Name} (Lv.{talent.RequiredPlayerLevel}) - Pos: ({talent.PositionX}, {talent.PositionY:F0}) - Value: {talent.StatValue:F1} - Cost: {talent.Cost}");
                
                if ((i + 1) % 4 == 0) Debug.Log("--- End of Cycle ---");
            }

            Debug.Log("\n=== SPECIAL SKILLS (RIGHT COLUMN) ===");
            foreach (var skill in specialTalents)
            {
                Debug.Log($"{skill.Name} (Lv.{skill.RequiredPlayerLevel}) - Pos: ({skill.PositionX}, {skill.PositionY:F0}) - Cost: {skill.Cost}");
            }
        }

        [ContextMenu("Debug Mobile Layout")]
        public void DebugMobileLayout()
        {
            if (!isDataLoaded) return;

            Debug.Log($"=== MOBILE LAYOUT DEBUG (1080x2160) ===");
            Debug.Log($"Normal Node Spacing: {normalNodeSpacing}px");
            Debug.Log($"Special Node Spacing: {specialNodeSpacing}px");
            Debug.Log($"Tier Spacing: {tierSpacing}px");
            Debug.Log($"Viewport Height: ~1800px (usable)");
            Debug.Log($"Nodes per screen: {1800f / normalNodeSpacing:F1}");
            Debug.Log($"Total Normal Talents: {normalTalents.Count}");
            Debug.Log($"Total Special Talents: {specialTalents.Count}");
            
            if (normalTalents.Count > 0)
            {
                var firstNormal = normalTalents[0];
                var lastNormal = normalTalents[normalTalents.Count - 1];
                var totalHeight = lastNormal.PositionY - firstNormal.PositionY;
                var screenCount = totalHeight / 1800f;
                Debug.Log($"Normal column spans {screenCount:F1} screens");
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
