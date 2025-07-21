using OctoberStudio.Upgrades;
using TwoSleepyCats.CSVReader.Attributes;
using TwoSleepyCats.CSVReader.Core;
using UnityEngine;

namespace Talents.Data
{
    [System.Serializable]
    public class TalentModel : ICsvModel
    {
        [CsvColumn("id")] public int ID { get; set; }
        
        [CsvColumn("icon")] public string IconPath { get; set; }
        
        [CsvColumn("name")] public string Name { get; set; }
        
        [CsvColumn("description")] public string Description { get; set; }
        
        [CsvColumn("stat_value")] public float StatValue { get; set; }
        
        [CsvColumn("stat_type")] public string StatTypeString { get; set; }
        
        [CsvColumn("node_type")] public string NodeTypeString { get; set; }
        
        [CsvColumn("position_x")] public float PositionX { get; set; }
        
        [CsvColumn("position_y")] public float PositionY { get; set; }
        
        [CsvColumn("required_talent_id", isOptional: true)]
        public int RequiredTalentId { get; set; } = -1;
        
        [CsvColumn("cost")] public int Cost { get; set; }
        
        [CsvColumn("max_level")] public int MaxLevel { get; set; } = 1;
        
        // Parsed properties
        [CsvIgnore]
        public UpgradeType StatType { get; private set; }
        
        [CsvIgnore]
        public TalentNodeType NodeType { get; private set; }
        
        private Sprite _icon;
        
        [CsvIgnore]
        public Sprite Icon 
        { 
            get 
            {
                if (_icon == null && !string.IsNullOrEmpty(IconPath))
                {
                    LoadIcon();
                }
                return _icon;
            }
            private set 
            {
                _icon = value;
            }
        }
        
        [CsvIgnore]
        public Vector2 Position => new Vector2(PositionX, PositionY);
        
        [CsvIgnore]
        public bool HasPrerequisite => RequiredTalentId > 0;

        public string GetCsvFileName()
        {
            return "talentConfig.csv";
        }

        public void OnDataLoaded()
        {
            // Parse stat type
            if (System.Enum.TryParse<UpgradeType>(StatTypeString, true, out var parsedStatType))
            {
                StatType = parsedStatType;
            }
            else
            {
                // Try to handle some common stat types that might not be in UpgradeType
                StatType = ParseCustomStatType(StatTypeString);
            }
            
            // Parse node type
            if (System.Enum.TryParse<TalentNodeType>(NodeTypeString, true, out var parsedNodeType))
            {
                NodeType = parsedNodeType;
            }
            else
            {
                NodeType = TalentNodeType.Normal; // Default fallback
            }
            
            // Don't load icon here - will be loaded lazily on main thread
        }

        /// <summary>
        /// Parse custom stat types that might not be in UpgradeType
        /// </summary>
        private UpgradeType ParseCustomStatType(string statTypeString)
        {
            switch (statTypeString.ToLower())
            {
                case "health":
                case "hp":
                    return UpgradeType.Health;
                case "damage":
                case "dmg":
                case "attack":
                    return UpgradeType.Damage;
                case "speed":
                case "movespeed":
                case "movement":
                    return UpgradeType.Speed;
                case "xpmultiplier":
                case "xp":
                case "experience":
                    return UpgradeType.XPMultiplier;
                case "healthregen":
                case "regen":
                case "regeneration":
                    return UpgradeType.Health; // Map to health for now
                case "damagereduction":
                case "armor":
                case "defense":
                    return UpgradeType.Damage; // Map to damage for now
                case "criticalchance":
                case "crit":
                    return UpgradeType.Damage; // Map to damage for now
                case "magnetradius":
                case "magnet":
                case "pickup":
                    return UpgradeType.MagnetRadius;
                case "luck":
                case "dropchance":
                    return UpgradeType.Luck;
                case "cooldownreduction":
                case "cooldown":
                case "cdr":
                    return UpgradeType.CooldownReduction;
                case "stamina":
                case "endurance":
                    return UpgradeType.Health; // Map to health for now
                case "mana":
                case "mp":
                    return UpgradeType.Health; // Map to health for now
                case "fireresistance":
                case "iceresistance":
                case "poisonresistance":
                    return UpgradeType.Damage; // Map to damage for now
                case "attackspeed":
                case "atkspeed":
                    return UpgradeType.Speed; // Map to speed for now
                case "attackrange":
                case "range":
                    return UpgradeType.Damage; // Map to damage for now
                case "multihit":
                case "projectilecount":
                case "piercecount":
                    return UpgradeType.Damage; // Map to damage for now
                case "criticaldamage":
                case "critdamage":
                    return UpgradeType.Damage;
                case "lifesteal":
                case "vampirism":
                    return UpgradeType.Health; // Map to health for now
                case "revive":
                case "resurrection":
                    return UpgradeType.Health; // Map to health for now
                case "timeslowdown":
                case "timestop":
                    return UpgradeType.Speed; // Map to speed for now
                case "allstats":
                case "all":
                    return UpgradeType.Health; // Default to health
                default:
                    Debug.LogWarning($"[TalentModel] Unknown stat type: {statTypeString}, defaulting to Health");
                    return UpgradeType.Health;
            }
        }

        /// <summary>
        /// Load icon sprite (only on main thread)
        /// </summary>
        private void LoadIcon()
        {
            // Check if we're on main thread
            if (!UnityEngine.Application.isPlaying)
            {
                return;
            }

            if (string.IsNullOrEmpty(IconPath))
            {
                Debug.LogWarning($"[TalentModel] No icon path specified for talent {Name}");
                return;
            }

            try
            {
                // Try to load from Resources folder first
                _icon = Resources.Load<Sprite>($"Icons/Talents/{IconPath}");
                
                if (_icon == null)
                {
                    // Try alternative path
                    _icon = Resources.Load<Sprite>($"Talents/{IconPath}");
                }
                
                if (_icon == null)
                {
                    // Try direct path
                    _icon = Resources.Load<Sprite>(IconPath);
                }
                
                if (_icon == null)
                {
                    Debug.LogWarning($"[TalentModel] Could not load icon for talent {Name} at path: {IconPath}");
                    
                    // Try to load a default icon
                    _icon = Resources.Load<Sprite>("Icons/Talents/default_talent");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[TalentModel] Failed to load icon for talent {Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get formatted stat bonus text
        /// </summary>
        public string GetStatBonusText(int level = 1)
        {
            float totalBonus = StatValue * level;
            
            switch (StatType)
            {
                case UpgradeType.Health:
                    return $"+{totalBonus:F0} Health";
                case UpgradeType.Damage:
                    return $"+{totalBonus:F0} Damage";
                case UpgradeType.Speed:
                    return $"+{totalBonus * 100:F0}% Speed";
                case UpgradeType.XPMultiplier:
                    return $"+{totalBonus * 100:F0}% XP";
                case UpgradeType.MagnetRadius:
                    return $"+{totalBonus:F1} Pickup Range";
                case UpgradeType.CooldownReduction:
                    return $"-{totalBonus * 100:F0}% Cooldown";
                case UpgradeType.Luck:
                    return $"+{totalBonus * 100:F0}% Luck";
                default:
                    return $"+{totalBonus:F1} {StatType}";
            }
        }

        /// <summary>
        /// Get talent effectiveness at specific level
        /// </summary>
        public float GetEffectivenessAtLevel(int level)
        {
            return StatValue * Mathf.Clamp(level, 0, MaxLevel);
        }

        /// <summary>
        /// Get total cost to reach specific level
        /// </summary>
        public int GetTotalCostToLevel(int targetLevel)
        {
            int totalCost = 0;
            for (int level = 1; level <= targetLevel && level <= MaxLevel; level++)
            {
                totalCost += Cost * level; // Cost increases with level
            }
            return totalCost;
        }

        /// <summary>
        /// Get cost for specific level
        /// </summary>
        public int GetCostForLevel(int level)
        {
            if (level <= 0 || level > MaxLevel)
                return 0;
            
            return Cost * level;
        }

        /// <summary>
        /// Check if talent can be upgraded from current level
        /// </summary>
        public bool CanUpgrade(int currentLevel)
        {
            return currentLevel < MaxLevel;
        }

        /// <summary>
        /// Get talent rarity based on cost and type
        /// </summary>
        public TalentRarity GetRarity()
        {
            if (NodeType == TalentNodeType.Special)
                return TalentRarity.Legendary;
            
            if (Cost >= 5)
                return TalentRarity.Epic;
            else if (Cost >= 3)
                return TalentRarity.Rare;
            else if (Cost >= 2)
                return TalentRarity.Uncommon;
            else
                return TalentRarity.Common;
        }

        /// <summary>
        /// Get talent category based on stat type
        /// </summary>
        public TalentCategory GetCategory()
        {
            switch (StatType)
            {
                case UpgradeType.Health:
                    return TalentCategory.Defensive;
                case UpgradeType.Damage:
                    return TalentCategory.Offensive;
                case UpgradeType.Speed:
                    return TalentCategory.Mobility;
                case UpgradeType.XPMultiplier:
                case UpgradeType.Luck:
                    return TalentCategory.Utility;
                case UpgradeType.MagnetRadius:
                    return TalentCategory.Utility;
                case UpgradeType.CooldownReduction:
                    return TalentCategory.Offensive;
                default:
                    return TalentCategory.Utility;
            }
        }

        /// <summary>
        /// Get display color for talent based on rarity
        /// </summary>
        public Color GetDisplayColor()
        {
            switch (GetRarity())
            {
                case TalentRarity.Common:
                    return Color.white;
                case TalentRarity.Uncommon:
                    return Color.green;
                case TalentRarity.Rare:
                    return Color.blue;
                case TalentRarity.Epic:
                    return Color.magenta;
                case TalentRarity.Legendary:
                    return Color.yellow;
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// Get formatted description with stat values
        /// </summary>
        public string GetFormattedDescription(int level = 1)
        {
            var desc = Description;
            
            // Replace placeholders with actual values
            desc = desc.Replace("{value}", StatValue.ToString());
            desc = desc.Replace("{total}", GetEffectivenessAtLevel(level).ToString("F1"));
            desc = desc.Replace("{level}", level.ToString());
            desc = desc.Replace("{max_level}", MaxLevel.ToString());
            
            return desc;
        }

        /// <summary>
        /// Check if this talent conflicts with another talent
        /// </summary>
        public bool ConflictsWith(TalentModel other)
        {
            // Special talents might conflict with each other
            if (NodeType == TalentNodeType.Special && other.NodeType == TalentNodeType.Special)
            {
                // Define conflicts based on talent names or IDs
                return CheckSpecialTalentConflicts(other);
            }
            
            return false;
        }

        /// <summary>
        /// Check for special talent conflicts
        /// </summary>
        private bool CheckSpecialTalentConflicts(TalentModel other)
        {
            // Example conflicts - customize based on your game design
            var conflictGroups = new[]
            {
                new[] { "Berserker", "Guardian" }, // Offensive vs Defensive
                new[] { "Assassin", "Titan" },     // Specialized vs Generalist
                new[] { "Wizard", "Vampire" },     // Mana vs Health focus
            };

            foreach (var group in conflictGroups)
            {
                if (System.Array.Exists(group, t => t == Name) && 
                    System.Array.Exists(group, t => t == other.Name))
                {
                    return true;
                }
            }
            
            return false;
        }

        public bool ValidateData()
        {
            bool isValid = ID > 0 && 
                          !string.IsNullOrEmpty(Name) && 
                          !string.IsNullOrEmpty(Description) &&
                          Cost > 0 && 
                          MaxLevel > 0 &&
                          StatValue >= 0;
            
            if (!isValid)
            {
                Debug.LogError($"[TalentModel] Invalid data - ID: {ID}, Name: '{Name}', Cost: {Cost}, MaxLevel: {MaxLevel}");
            }
            
            return isValid;
        }

        public override string ToString()
        {
            return $"{Name} (ID:{ID}) - {GetStatBonusText()} - {NodeType} - Cost:{Cost}";
        }
    }

    /// <summary>
    /// Talent node type enumeration
    /// </summary>
    public enum TalentNodeType
    {
        Normal,
        Special
    }

    /// <summary>
    /// Talent rarity enumeration
    /// </summary>
    public enum TalentRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>
    /// Talent category enumeration
    /// </summary>
    public enum TalentCategory
    {
        Offensive,
        Defensive,
        Mobility,
        Utility
    }
}