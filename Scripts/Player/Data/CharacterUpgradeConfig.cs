using UnityEngine;

namespace OctoberStudio
{
    [System.Serializable]
    public enum StarType
    {
        Grey = 1,
        Orange = 2,
        Gold = 3,
        Purple = 4,
        Rainbow = 5,
        Diamond = 6
    }

    [System.Serializable]
    public class MilestoneConfig
    {
        public int milestoneId;
        public StarType starType;
        public int startLevel;
        public int endLevel;
        public float hpBonus;
        public float damageBonus;
    }

    [System.Serializable]
    public class CharacterUpgradeConfig
    {
        [Header("Legacy System (Backward Compatibility)")]
        [SerializeField] public float hpPerStar = 10f;
        [SerializeField] public float damagePerStar = 2f;
        [SerializeField] public int baseCost = 100;
        [SerializeField] public int costPerStar = 50;
        [SerializeField] public int maxStars = 12; // Updated to 12 for new system

        [Header("6 Milestone System (each with 3 sub-stars)")]
        [SerializeField] public int statBonusPerTier = 10;
        [SerializeField] public string specialAbilityDescription = "Enhanced Combat Ability";

        [Header("Milestone Descriptions (6 milestones)")]
        [SerializeField, TextArea(2, 3)]
        public string[] milestoneDescriptions = new string[6]
        {
            "+20 HP & +10 DMG",          // Milestone 1 - Grey stars (Level 1-3)
            "+40 HP & +20 DMG",          // Milestone 2 - Orange stars (Level 4-6)
            "+60 HP & +30 DMG",          // Milestone 3 - Gold stars (Level 7-9)
            "+80 HP & +40 DMG",          // Milestone 4 - Purple stars (Level 10-12)
            "Special Ability Unlock",     // Milestone 5 - Future expansion
            "Ultimate Power (MAX)"        // Milestone 6 - Future expansion
        };

        [Header("Milestone Configurations")]
        [SerializeField] public MilestoneConfig[] milestoneConfigs = new MilestoneConfig[6]
        {
            new MilestoneConfig { milestoneId = 1, starType = StarType.Grey, startLevel = 1, endLevel = 3, hpBonus = 20, damageBonus = 10 },
            new MilestoneConfig { milestoneId = 2, starType = StarType.Orange, startLevel = 4, endLevel = 6, hpBonus = 40, damageBonus = 20 },
            new MilestoneConfig { milestoneId = 3, starType = StarType.Gold, startLevel = 7, endLevel = 9, hpBonus = 60, damageBonus = 30 },
            new MilestoneConfig { milestoneId = 4, starType = StarType.Purple, startLevel = 10, endLevel = 12, hpBonus = 80, damageBonus = 40 },
            new MilestoneConfig { milestoneId = 5, starType = StarType.Rainbow, startLevel = 13, endLevel = 15, hpBonus = 100, damageBonus = 50 },
            new MilestoneConfig { milestoneId = 6, starType = StarType.Diamond, startLevel = 16, endLevel = 18, hpBonus = 120, damageBonus = 60 }
        };

        [Header("Star Upgrade System Configuration")]
        [SerializeField] public StarTierConfig[] starTierConfigs = new StarTierConfig[12]
        {
            new StarTierConfig { tierLevel = 1, subStarCount = 1, piecesPerSubStar = new int[] { 1 } },                    // Level 1: 1 piece
            new StarTierConfig { tierLevel = 2, subStarCount = 1, piecesPerSubStar = new int[] { 2 } },                    // Level 2: 2 pieces
            new StarTierConfig { tierLevel = 3, subStarCount = 1, piecesPerSubStar = new int[] { 3 } },                    // Level 3: 3 pieces
            new StarTierConfig { tierLevel = 4, subStarCount = 1, piecesPerSubStar = new int[] { 5 } },                    // Level 4: 5 pieces
            new StarTierConfig { tierLevel = 5, subStarCount = 1, piecesPerSubStar = new int[] { 8 } },                    // Level 5: 8 pieces
            new StarTierConfig { tierLevel = 6, subStarCount = 1, piecesPerSubStar = new int[] { 12 } },                   // Level 6: 12 pieces
            new StarTierConfig { tierLevel = 7, subStarCount = 1, piecesPerSubStar = new int[] { 20 } },                   // Level 7: 20 pieces
            new StarTierConfig { tierLevel = 8, subStarCount = 1, piecesPerSubStar = new int[] { 30 } },                   // Level 8: 30 pieces
            new StarTierConfig { tierLevel = 9, subStarCount = 1, piecesPerSubStar = new int[] { 40 } },                   // Level 9: 40 pieces
            new StarTierConfig { tierLevel = 10, subStarCount = 1, piecesPerSubStar = new int[] { 60 } },                  // Level 10: 60 pieces
            new StarTierConfig { tierLevel = 11, subStarCount = 1, piecesPerSubStar = new int[] { 80 } },                  // Level 11: 80 pieces
            new StarTierConfig { tierLevel = 12, subStarCount = 1, piecesPerSubStar = new int[] { 100 } }                  // Level 12: 100 pieces
        };

        [Header("Level Upgrade Costs per Star Level")]
        [SerializeField] public LevelUpgradeCost[] levelUpgradeCosts = new LevelUpgradeCost[12]
        {
            new LevelUpgradeCost { maxLevel = 10, goldPerLevel = 100 },  // Level 1: max 10 levels, 100 gold each
            new LevelUpgradeCost { maxLevel = 15, goldPerLevel = 120 },  // Level 2: max 15 levels, 120 gold each
            new LevelUpgradeCost { maxLevel = 20, goldPerLevel = 150 },  // Level 3: max 20 levels, 150 gold each
            new LevelUpgradeCost { maxLevel = 25, goldPerLevel = 180 },  // Level 4: max 25 levels, 180 gold each
            new LevelUpgradeCost { maxLevel = 30, goldPerLevel = 200 },  // Level 5: max 30 levels, 200 gold each
            new LevelUpgradeCost { maxLevel = 35, goldPerLevel = 250 },  // Level 6: max 35 levels, 250 gold each
            new LevelUpgradeCost { maxLevel = 40, goldPerLevel = 300 },  // Level 7: max 40 levels, 300 gold each
            new LevelUpgradeCost { maxLevel = 45, goldPerLevel = 350 },  // Level 8: max 45 levels, 350 gold each
            new LevelUpgradeCost { maxLevel = 50, goldPerLevel = 400 },  // Level 9: max 50 levels, 400 gold each
            new LevelUpgradeCost { maxLevel = 60, goldPerLevel = 500 },  // Level 10: max 60 levels, 500 gold each
            new LevelUpgradeCost { maxLevel = 70, goldPerLevel = 600 },  // Level 11: max 70 levels, 600 gold each
            new LevelUpgradeCost { maxLevel = 80, goldPerLevel = 700 }   // Level 12: max 80 levels, 700 gold each
        };

        [Header("Legacy Material Requirements (12 entries)")]
        [SerializeField] public MaterialRequirement[] materialRequirements = new MaterialRequirement[12]
        {
            new MaterialRequirement { materialName = "Character Essence", amount = 1 },
            new MaterialRequirement { materialName = "Character Essence", amount = 2 },
            new MaterialRequirement { materialName = "Character Essence", amount = 3 },
            new MaterialRequirement { materialName = "Character Essence", amount = 5 },
            new MaterialRequirement { materialName = "Character Essence", amount = 8 },
            new MaterialRequirement { materialName = "Character Essence", amount = 12 },
            new MaterialRequirement { materialName = "Star Fragment", amount = 20 },
            new MaterialRequirement { materialName = "Star Fragment", amount = 30 },
            new MaterialRequirement { materialName = "Star Fragment", amount = 40 },
            new MaterialRequirement { materialName = "Rare Crystal", amount = 60 },
            new MaterialRequirement { materialName = "Rare Crystal", amount = 80 },
            new MaterialRequirement { materialName = "Legendary Core", amount = 100 }
        };

        // Properties
        public int MaxStars => maxStars;
        public int StatBonusPerTier => statBonusPerTier;
        public string SpecialAbilityDescription => specialAbilityDescription;
        public string[] TierDescriptions => milestoneDescriptions;
        public MaterialRequirement[] MaterialRequirements => materialRequirements;
        public StarTierConfig[] StarTierConfigs => starTierConfigs;
        public LevelUpgradeCost[] LevelUpgradeCosts => levelUpgradeCosts;

        // Helper methods for new sub-star system
        public StarTierConfig GetStarTierConfig(int starLevel)
        {
            if (starLevel <= 0 || starLevel > starTierConfigs.Length)
                return null;
            return starTierConfigs[starLevel - 1];
        }

        public int GetPiecesRequiredForSubStar(int starLevel, int subStarIndex)
        {
            var config = GetStarTierConfig(starLevel);
            if (config == null || subStarIndex < 0 || subStarIndex >= config.piecesPerSubStar.Length)
                return int.MaxValue;
            return config.piecesPerSubStar[subStarIndex];
        }

        public int GetTotalPiecesRequiredForStar(int starLevel)
        {
            var config = GetStarTierConfig(starLevel);
            if (config == null) return int.MaxValue;

            int total = 0;
            foreach (int pieces in config.piecesPerSubStar)
            {
                total += pieces;
            }
            return total;
        }

        public int GetMaxLevelForStar(int starLevel)
        {
            if (starLevel <= 0 || starLevel > levelUpgradeCosts.Length)
                return 0;
            return levelUpgradeCosts[starLevel - 1].maxLevel;
        }

        public int GetGoldCostPerLevel(int starLevel)
        {
            if (starLevel <= 0 || starLevel > levelUpgradeCosts.Length)
                return int.MaxValue;
            return levelUpgradeCosts[starLevel - 1].goldPerLevel;
        }

        // New Milestone System Methods
        public MilestoneConfig GetMilestoneForLevel(int level)
        {
            foreach (var milestone in milestoneConfigs)
            {
                if (level >= milestone.startLevel && level <= milestone.endLevel)
                {
                    return milestone;
                }
            }
            return null;
        }

        public int GetMilestoneIdForLevel(int level)
        {
            var milestone = GetMilestoneForLevel(level);
            return milestone?.milestoneId ?? 1;
        }

        public int GetStarsInMilestone(int level)
        {
            var milestone = GetMilestoneForLevel(level);
            if (milestone == null) return 1;

            // Calculate how many stars within this milestone (1-3)
            return (level - milestone.startLevel) + 1;
        }

        public StarType GetStarTypeForLevel(int level)
        {
            var milestone = GetMilestoneForLevel(level);
            return milestone?.starType ?? StarType.Grey;
        }

        public bool IsLevelCompletedMilestone(int level)
        {
            var milestone = GetMilestoneForLevel(level);
            return milestone != null && level == milestone.endLevel;
        }
    }

    [System.Serializable]
    public class MaterialRequirement
    {
        public string materialName;
        public int amount;
    }

    [System.Serializable]
    public class LevelUpgradeCost
    {
        public int maxLevel;
        public int goldPerLevel;
    }

    [System.Serializable]
    public class StarTierConfig
    {
        public int tierLevel;           // Star tier level (1-6)
        public int subStarCount;        // Number of sub-stars required to complete this tier
        public int[] piecesPerSubStar;  // Pieces required for each sub-star
    }
}