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
        [SerializeField] private float normalNodeSpacing = 400f;    // 4 nodes visible
        [SerializeField] private float specialNodeSpacing = 450f;  // Spacing for special skills
        [SerializeField] private float tierSpacing = 100f;         // Extra spacing between tiers
        [SerializeField] private float startY = 0f;                // Start from center (no offset)
        
        [Header("Debug Info")]
        [SerializeField, ReadOnly] private int totalTalentCount = 0;
        [SerializeField, ReadOnly] private bool isDataLoaded = false;
        [SerializeField, ReadOnly] private string loadStatus = "Not Loaded";

        // Talent collections
        private Dictionary<int, TalentModel> talentsById = new Dictionary<int, TalentModel>();
        private List<TalentModel> normalTalents = new List<TalentModel>();
        private List<TalentModel> specialTalents = new List<TalentModel>();
        private List<TalentModel> allTalents = new List<TalentModel>();
        
        // Auto-generated normal stats pattern
        private readonly BaseStatType[] statPattern = { 
            BaseStatType.ATK, 
            BaseStatType.HP, 
            BaseStatType.Armor, 
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
        /// Process and organize talent data - auto-generate normal stats + load special skills
        /// </summary>
        private void ProcessTalentData(List<TalentModel> specialSkillsData)
        {
            ClearData();

            // 1. Auto-generate normal stats
            GenerateNormalStats();

            // 2. Process special skills from CSV
            ProcessSpecialSkills(specialSkillsData);

            // 3. Combine and finalize
            CombineAllTalents();
            
            totalTalentCount = allTalents.Count;
        }

        /// <summary>
        /// Auto-generate normal stats based on pattern and player levels
        /// </summary>
        private void GenerateNormalStats()
        {
            int normalStatId = 1; // Start from ID 1
            float currentY = startY; // Start from bottom

            // Generate stats for each "cycle" based on max player level
            int cycles = Mathf.CeilToInt((float)maxPlayerLevel / statPattern.Length);
            
            for (int cycle = 0; cycle < cycles; cycle++)
            {
                for (int i = 0; i < statPattern.Length; i++)
                {
                    var statType = statPattern[i];
                    int level = cycle + 1;
                    
                    // Stop if we exceed max player level
                    int requiredPlayerLevel = (cycle * statPattern.Length) + i + 1;
                    if (requiredPlayerLevel > maxPlayerLevel) break;

                    var normalStat = CreateNormalStat(normalStatId, statType, level, currentY, requiredPlayerLevel);
                    normalTalents.Add(normalStat);
                    talentsById[normalStatId] = normalStat;

                    normalStatId++;
                    currentY += normalNodeSpacing; // Use specific spacing for normal stats
                    
                    // Add extra spacing between complete cycles (tiers)
                    if (i == statPattern.Length - 1 && cycle < cycles - 1)
                    {
                        currentY += tierSpacing;
                    }
                }
            }
        }

        /// <summary>
        /// Create a normal stat talent
        /// </summary>
        private TalentModel CreateNormalStat(int id, BaseStatType statType, int tier, float posY, int requiredLevel)
        {
            var talent = new TalentModel
            {
                ID = id,
                Name = GetStatName(statType, tier),
                Description = GetStatDescription(statType, tier),
                StatValue = CalculateStatValue(statType, tier),
                StatType = GetUpgradeType(statType),
                StatTypeString = GetUpgradeType(statType).ToString(),
                NodeType = TalentNodeType.Normal,
                NodeTypeString = "Normal",
                PositionX = leftColumnX,  // Normal stats on LEFT column
                PositionY = posY,
                Cost = CalculateCost(tier),
                MaxLevel = 10, // All normal stats can be upgraded 10 times
                RequiredPlayerLevel = requiredLevel,
                IconPath = GetStatIconPath(statType)
            };

            talent.OnDataLoaded();
            return talent;
        }

        /// <summary>
        /// Get stat name with tier
        /// </summary>
        private string GetStatName(BaseStatType statType, int tier)
        {
            string baseName = statType switch
            {
                BaseStatType.ATK => "Attack",
                BaseStatType.HP => "Health", 
                BaseStatType.Armor => "Armor",
                BaseStatType.Healing => "Healing",
                _ => "Unknown"
            };

            return tier > 1 ? $"{baseName} {ToRoman(tier)}" : baseName;
        }

        /// <summary>
        /// Get stat description
        /// </summary>
        private string GetStatDescription(BaseStatType statType, int tier)
        {
            return statType switch
            {
                BaseStatType.ATK => $"Increase base attack damage (Tier {tier})",
                BaseStatType.HP => $"Increase maximum health (Tier {tier})",
                BaseStatType.Armor => $"Reduce incoming damage (Tier {tier})",
                BaseStatType.Healing => $"Increase health regeneration (Tier {tier})",
                _ => "Unknown stat"
            };
        }

        /// <summary>
        /// Calculate stat value based on tier
        /// </summary>
        private float CalculateStatValue(BaseStatType statType, int tier)
        {
            float baseValue = statType switch
            {
                BaseStatType.ATK => baseStatStartValue,
                BaseStatType.HP => baseStatStartValue * 4f, // HP has higher base value
                BaseStatType.Armor => baseStatStartValue * 0.5f, // Armor has lower base value
                BaseStatType.Healing => baseStatStartValue * 0.3f, // Healing has lowest base value
                _ => baseStatStartValue
            };

            // Apply tier growth
            return baseValue * Mathf.Pow(baseStatGrowthRate, tier - 1);
        }

        /// <summary>
        /// Calculate cost based on tier
        /// </summary>
        private int CalculateCost(int tier)
        {
            return Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowthRate, tier - 1));
        }

        /// <summary>
        /// Get upgrade type from base stat type
        /// </summary>
        private UpgradeType GetUpgradeType(BaseStatType statType)
        {
            return statType switch
            {
                BaseStatType.ATK => UpgradeType.Damage,
                BaseStatType.HP => UpgradeType.Health,
                BaseStatType.Armor => UpgradeType.Damage, // Map to damage for damage reduction
                BaseStatType.Healing => UpgradeType.Health, // Map to health for healing
                _ => UpgradeType.Health
            };
        }

        /// <summary>
        /// Get icon path for stat type
        /// </summary>
        private string GetStatIconPath(BaseStatType statType)
        {
            return statType switch
            {
                BaseStatType.ATK => "atk_icon",
                BaseStatType.HP => "hp_icon", 
                BaseStatType.Armor => "armor_icon",
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
            SetSpacing(400f, 450f, 100f); // 4 nodes visible
        }

        [ContextMenu("Compact Spacing")]
        public void SetCompactSpacing()
        {
            SetSpacing(300f, 350f, 50f); // 5-6 nodes visible
        }

        [ContextMenu("Normal Spacing")]
        public void SetNormalSpacing()
        {
            SetSpacing(400f, 450f, 100f); // 4 nodes visible (default)
        }

        [ContextMenu("Wide Spacing")]
        public void SetWideSpacing()
        {
            SetSpacing(500f, 550f, 150f); // 3 nodes visible
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
