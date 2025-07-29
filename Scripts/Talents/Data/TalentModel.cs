using System.Globalization;
using OctoberStudio.Upgrades;
using TwoSleepyCats.CSVReader.Attributes;
using TwoSleepyCats.CSVReader.Core;
using UnityEngine;

namespace Talents.Data
{
    [System.Serializable]
    public class TalentModel : ICsvModel
    {
        [CsvColumn("id")] 
        public int ID { get; set; }
        
        [CsvColumn("icon")] 
        public string IconPath { get; set; }
        
        [CsvColumn("name")] 
        public string Name { get; set; }
        
        [CsvColumn("description")] 
        public string Description { get; set; }
        
        [CsvColumn("stat_value")] 
        public float StatValue { get; set; }
        
        [CsvColumn("stat_type")] 
        public string StatTypeString { get; set; }
        
        [CsvColumn("node_type")] 
        public string NodeTypeString { get; set; }
        
        [CsvColumn("position_x")] 
        public float PositionX { get; set; }
        
        [CsvColumn("position_y")] 
        public float PositionY { get; set; }
        
        [CsvColumn("cost")] 
        public int Cost { get; set; }
        
        [CsvColumn("max_level")] 
        public int MaxLevel { get; set; } = 1;
        
        [CsvColumn("required_player_level")] 
        public int RequiredPlayerLevel { get; set; } = 1;
        
        // Parsed properties
        [CsvIgnore]
        public UpgradeType StatType { get; set; }
        
        [CsvIgnore]
        public TalentNodeType NodeType { get; set; }
        
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
        public bool IsNormalNode => NodeType == TalentNodeType.Normal;
        
        [CsvIgnore]
        public bool IsSpecialNode => NodeType == TalentNodeType.Special;

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
        }

        /// <summary>
        /// Parse custom stat types for zone-based system
        /// </summary>
        private UpgradeType ParseCustomStatType(string statTypeString)
        {
            switch (statTypeString.ToLower())
            {
                case "attack":
                case "atk":
                case "damage":
                    return UpgradeType.Damage;
                    
                case "defense":
                case "def":
                case "armor":
                    return UpgradeType.Health; // Defense maps to health system
                    
                case "speed":
                case "spd":
                case "movement":
                    return UpgradeType.Speed;
                    
                case "healing":
                case "heal":
                case "regeneration":
                    return UpgradeType.Health;
                    
                case "health":
                case "hp":
                    return UpgradeType.Health;
                    
                default:
                    Debug.LogWarning($"[TalentModel] Unknown stat type: {statTypeString}, defaulting to Health");
                    return UpgradeType.Health;
            }
        }

        /// <summary>
        /// Load icon sprite (main thread only)
        /// </summary>
        private void LoadIcon()
        {
            if (!UnityEngine.Application.isPlaying)
                return;

            if (string.IsNullOrEmpty(IconPath))
            {
                Debug.LogWarning($"[TalentModel] No icon path specified for talent {Name}");
                return;
            }

            try
            {
                _icon = Resources.Load<Sprite>($"Icons/Talents/{IconPath}");
        
                if (_icon == null)
                {
                    // Try fallback icon based on node type
                    string fallbackPath = NodeType == TalentNodeType.Normal ? 
                        GetNormalStatFallbackIcon() : "special_default";
                    
                    _icon = Resources.Load<Sprite>($"Icons/Talents/{fallbackPath}");
                    
                    if (_icon == null)
                    {
                        _icon = Resources.Load<Sprite>("Icons/Talents/default_talent");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TalentModel] Failed to load icon for talent {Name}: {ex.Message}");
                _icon = Resources.Load<Sprite>("Icons/Talents/default_talent");
            }
        }

        /// <summary>
        /// Get fallback icon for normal stats
        /// </summary>
        private string GetNormalStatFallbackIcon()
        {
            if (Name.Contains("Attack")) return "atk_icon";
            if (Name.Contains("Defense")) return "def_icon";
            if (Name.Contains("Speed")) return "speed_icon";
            if (Name.Contains("Healing")) return "heal_icon";
            return "default_talent";
        }

        /// <summary>
        /// Get stat bonus text for UI display
        /// </summary>
        public string GetStatBonusText()
        {
            if (NodeType == TalentNodeType.Special)
                return "Special Ability";
                
            return StatType switch
            {
                UpgradeType.Damage => $"+{StatValue:F0} ATK",
                UpgradeType.Health => GetHealthStatText(),
                UpgradeType.Speed => $"+{StatValue:F0} SPD",
                _ => $"+{StatValue:F1} {StatType}"
            };
        }

        /// <summary>
        /// Get health stat text (could be DEF or HEAL)
        /// </summary>
        private string GetHealthStatText()
        {
            if (Name.Contains("Defense"))
                return $"+{StatValue:F0} DEF";
            if (Name.Contains("Healing"))
                return $"+{StatValue:F0} HEAL";
            return $"+{StatValue:F0} HP";
        }

        /// <summary>
        /// Get currency type for this talent
        /// </summary>
        public string GetCurrencyType()
        {
            return NodeType == TalentNodeType.Normal ? "Gold" : "Orc";
        }

        /// <summary>
        /// Get talent zone level (same as required player level)
        /// </summary>
        public int GetZoneLevel()
        {
            return RequiredPlayerLevel;
        }

        /// <summary>
        /// Get stat type for normal nodes
        /// </summary>
        public NormalStatType? GetNormalStatType()
        {
            if (NodeType != TalentNodeType.Normal) return null;
            
            if (Name.Contains("Attack")) return NormalStatType.ATK;
            if (Name.Contains("Defense")) return NormalStatType.DEF;
            if (Name.Contains("Speed")) return NormalStatType.SPEED;
            if (Name.Contains("Healing")) return NormalStatType.HEAL;
            
            return null;
        }

        /// <summary>
        /// Get display color based on node type and stat
        /// </summary>
        public Color GetDisplayColor()
        {
            if (NodeType == TalentNodeType.Special)
                return new Color(1f, 0.8f, 0f); // Gold for special
                
            // Colors for normal stats
            if (Name.Contains("Attack")) return new Color(1f, 0.3f, 0.3f); // Red
            if (Name.Contains("Defense")) return new Color(0.3f, 0.3f, 1f); // Blue
            if (Name.Contains("Speed")) return new Color(0.3f, 1f, 0.3f); // Green
            if (Name.Contains("Healing")) return new Color(1f, 1f, 0.3f); // Yellow
            
            return Color.white;
        }

        /// <summary>
        /// Get formatted description with zone info
        /// </summary>
        public string GetFormattedDescription()
        {
            string desc = Description;
            
            if (NodeType == TalentNodeType.Normal)
            {
                desc += $"\n\nZone Level: {RequiredPlayerLevel}";
                desc += $"\nStat Bonus: {GetStatBonusText()}";
                desc += $"\nCost: {Cost} Gold";
            }
            else
            {
                desc += $"\n\nRequired Player Level: {RequiredPlayerLevel}";
                desc += $"\nCost: {Cost} Orc";
                desc += $"\nType: Special Ability";
            }
            
            return desc;
        }

        /// <summary>
        /// Validate talent data
        /// </summary>
        public bool ValidateData()
        {
            bool isValid = ID > 0 && 
                          !string.IsNullOrEmpty(Name) && 
                          !string.IsNullOrEmpty(Description) &&
                          Cost > 0 && 
                          MaxLevel > 0 &&
                          StatValue >= 0 &&
                          RequiredPlayerLevel > 0;
            
            if (!isValid)
            {
                Debug.LogError($"[TalentModel] Invalid data for talent {Name} (ID: {ID})");
            }
            
            return isValid;
        }

        /// <summary>
        /// Get talent effectiveness (for balancing)
        /// </summary>
        public float GetEffectiveness()
        {
            if (NodeType == TalentNodeType.Special)
                return Cost; // Special talents effectiveness based on cost
                
            return StatValue / Cost; // Normal talents: stat value per cost
        }

        /// <summary>
        /// Check if talent is in specified zone level
        /// </summary>
        public bool IsInZoneLevel(int zoneLevel)
        {
            return RequiredPlayerLevel == zoneLevel;
        }

        /// <summary>
        /// Get short display name for UI
        /// </summary>
        public string GetShortDisplayName()
        {
            if (NodeType == TalentNodeType.Normal)
            {
                if (Name.Contains("Attack")) return "ATK";
                if (Name.Contains("Defense")) return "DEF";
                if (Name.Contains("Speed")) return "SPD";
                if (Name.Contains("Healing")) return "HEAL";
            }
            
            return Name.Length > 8 ? Name.Substring(0, 8) + "..." : Name;
        }

        /// <summary>
        /// Compare talents for sorting
        /// </summary>
        public int CompareTo(TalentModel other)
        {
            if (other == null) return 1;
            
            // Sort by zone level first
            int zoneCompare = RequiredPlayerLevel.CompareTo(other.RequiredPlayerLevel);
            if (zoneCompare != 0) return zoneCompare;
            
            // Then by node type (Normal before Special)
            int typeCompare = NodeType.CompareTo(other.NodeType);
            if (typeCompare != 0) return typeCompare;
            
            // Finally by position Y
            return PositionY.CompareTo(other.PositionY);
        }

        public override string ToString()
        {
            return $"{Name} (ID:{ID}, Zone:{RequiredPlayerLevel}) - {GetStatBonusText()} - Cost:{Cost} {GetCurrencyType()}";
        }
    }

    /// <summary>
    /// Talent node type enumeration
    /// </summary>
    public enum TalentNodeType
    {
        Normal,   // ATK, DEF, SPEED, HEAL nodes
        Special   // Special ability nodes
    }

    /// <summary>
    /// Normal stat type enumeration for zone system
    /// </summary>
    public enum NormalStatType
    {
        ATK,    // Attack damage
        DEF,    // Defense/Armor
        SPEED,  // Movement speed
        HEAL    // Healing rate
    }
}