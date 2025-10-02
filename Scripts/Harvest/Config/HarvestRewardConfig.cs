using UnityEngine;
using OctoberStudio.Equipment;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace OctoberStudio.Harvest
{
    /// <summary>
    /// ScriptableObject configuration for harvest reward system
    /// Manages time-based probability and reward formulas
    /// </summary>
    [CreateAssetMenu(fileName = "HarvestRewardConfig", menuName = "Harvest/Reward Config")]
    public class HarvestRewardConfig : ScriptableObject
    {
        [Header("Time-Based Multipliers")]
        [SerializeField] private AnimationCurve timeMultiplierCurve = AnimationCurve.Linear(0f, 1f, 24f, 2f);
        [SerializeField] private float maxTimeMultiplier = 3f;
        [SerializeField] private float minTimeMultiplier = 0.5f;

        [Header("Time-Based Reward Thresholds")]
        [SerializeField] private List<TimeBasedRewardTier> rewardTiers = new List<TimeBasedRewardTier>();

        [Header("Reward Icons")]
        [SerializeField] private Sprite goldIcon;
        [SerializeField] private Sprite expIcon;
        [SerializeField] private Sprite gemIcon;
        [SerializeField] private Sprite characterPiecesIcon;

        [Header("Base Rewards Per Hour")]
        [SerializeField] private BaseRewardData baseGoldPerHour = new BaseRewardData(200, 100);
        [SerializeField] private BaseRewardData baseExpPerHour = new BaseRewardData(100, 50);
        [SerializeField] private BaseRewardData baseGemPerHour = new BaseRewardData(10, 5);
        [SerializeField] private BaseRewardData baseCharacterPiecesPerHour = new BaseRewardData(20, 10);

        [Header("Probability Settings")]
        [SerializeField] private List<RewardProbabilityData> rewardProbabilities = new List<RewardProbabilityData>();

        [Header("Equipment Reward Settings")]
        [SerializeField] private List<EquipmentRarityChance> equipmentRarityChances = new List<EquipmentRarityChance>();
        [SerializeField] private int maxEquipmentRewards = 3;
        [SerializeField] private int minEquipmentRewards = 1;

        [Header("Time Bonus Settings")]
        [SerializeField] private List<TimeBonusData> timeBonuses = new List<TimeBonusData>();

        [Header("World/Stage Scaling")]
        [SerializeField] private float worldScalingFactor = 1.2f;
        [SerializeField] private int maxWorldForScaling = 50;

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool guaranteeEquipment = false;

        /// <summary>
        /// Calculate gold reward based on world and time
        /// </summary>
        public int CalculateGoldReward(int world, float hoursOffline)
        {
            float baseAmount = baseGoldPerHour.CalculateForWorld(world, worldScalingFactor, maxWorldForScaling);
            float timeMultiplier = GetTimeMultiplier(hoursOffline);
            float timeBonusMultiplier = GetTimeBonusMultiplier(hoursOffline);

            int finalAmount = Mathf.RoundToInt(baseAmount * hoursOffline * timeMultiplier * timeBonusMultiplier);

            if (enableDebugLogs)
                Debug.Log($"[HarvestReward] Gold: Base={baseAmount:F0}, Hours={hoursOffline:F1}, TimeMulti={timeMultiplier:F2}, BonusMulti={timeBonusMultiplier:F2}, Final={finalAmount}");

            return finalAmount;
        }

        /// <summary>
        /// Calculate experience reward based on world and time
        /// </summary>
        public int CalculateExpReward(int world, float hoursOffline)
        {
            float baseAmount = baseExpPerHour.CalculateForWorld(world, worldScalingFactor, maxWorldForScaling);
            float timeMultiplier = GetTimeMultiplier(hoursOffline);
            float timeBonusMultiplier = GetTimeBonusMultiplier(hoursOffline);

            int finalAmount = Mathf.RoundToInt(baseAmount * hoursOffline * timeMultiplier * timeBonusMultiplier);

            if (enableDebugLogs)
                Debug.Log($"[HarvestReward] EXP: Base={baseAmount:F0}, Hours={hoursOffline:F1}, TimeMulti={timeMultiplier:F2}, BonusMulti={timeBonusMultiplier:F2}, Final={finalAmount}");

            return finalAmount;
        }

        /// <summary>
        /// Calculate gem reward based on world and time
        /// </summary>
        public int CalculateGemReward(int world, float hoursOffline)
        {
            float baseAmount = baseGemPerHour.CalculateForWorld(world, worldScalingFactor, maxWorldForScaling);
            float timeMultiplier = GetTimeMultiplier(hoursOffline);

            // Gems are more rare, so lower multiplier
            int finalAmount = Mathf.RoundToInt(baseAmount * hoursOffline * (timeMultiplier * 0.5f));

            if (enableDebugLogs)
                Debug.Log($"[HarvestReward] Gems: Base={baseAmount:F0}, Hours={hoursOffline:F1}, TimeMulti={timeMultiplier:F2}, Final={finalAmount}");

            return finalAmount;
        }

        /// <summary>
        /// Calculate character pieces reward based on world and time
        /// </summary>
        public int CalculateCharacterPiecesReward(int world, float hoursOffline)
        {
            float baseAmount = baseCharacterPiecesPerHour.CalculateForWorld(world, worldScalingFactor, maxWorldForScaling);
            float timeMultiplier = GetTimeMultiplier(hoursOffline);
            float timeBonusMultiplier = GetTimeBonusMultiplier(hoursOffline);

            int finalAmount = Mathf.RoundToInt(baseAmount * hoursOffline * timeMultiplier * timeBonusMultiplier);

            if (enableDebugLogs)
                Debug.Log($"[HarvestReward] Character Pieces: Base={baseAmount:F0}, Hours={hoursOffline:F1}, TimeMulti={timeMultiplier:F2}, BonusMulti={timeBonusMultiplier:F2}, Final={finalAmount}");

            return finalAmount;
        }

        /// <summary>
        /// Get time multiplier based on hours offline
        /// </summary>
        public float GetTimeMultiplier(float hoursOffline)
        {
            float multiplier = timeMultiplierCurve.Evaluate(hoursOffline);
            return Mathf.Clamp(multiplier, minTimeMultiplier, maxTimeMultiplier);
        }

        /// <summary>
        /// Get time bonus multiplier for special time thresholds
        /// </summary>
        public float GetTimeBonusMultiplier(float hoursOffline)
        {
            float bonusMultiplier = 1f;

            foreach (var bonus in timeBonuses)
            {
                if (hoursOffline >= bonus.requiredHours)
                {
                    bonusMultiplier = Mathf.Max(bonusMultiplier, bonus.bonusMultiplier);
                }
            }

            return bonusMultiplier;
        }

        /// <summary>
        /// Check if reward type should be given based on probability
        /// </summary>
        public bool ShouldGiveReward(RewardHarvestType rewardHarvestType, float hoursOffline)
        {
            var probData = rewardProbabilities.Find(p => p.rewardHarvestType == rewardHarvestType);
            if (probData == null) return false;

            float probability = probData.CalculateProbability(hoursOffline);
            return Random.Range(0f, 1f) <= probability;
        }

        /// <summary>
        /// Get equipment rarity based on chance and time
        /// </summary>
        public EquipmentRarity GetEquipmentRarity(float hoursOffline)
        {
            // Higher offline time increases chance of better rarity
            float timeBonus = Mathf.Min(hoursOffline / 24f, 1f); // Max bonus at 24 hours

            float randomValue = Random.Range(0f, 1f);
            float cumulativeChance = 0f;

            foreach (var rarityChance in equipmentRarityChances)
            {
                float adjustedChance = rarityChance.baseChance + (rarityChance.timeBonusChance * timeBonus);
                cumulativeChance += adjustedChance;

                if (randomValue <= cumulativeChance)
                {
                    if (enableDebugLogs)
                        Debug.Log($"[HarvestReward] Equipment Rarity: {rarityChance.rarity} (chance: {adjustedChance:F3}, timeBonus: {timeBonus:F2})");

                    return rarityChance.rarity;
                }
            }

            // Fallback to common
            return EquipmentRarity.Common;
        }

        /// <summary>
        /// Get number of equipment rewards to give
        /// </summary>
        public int GetEquipmentRewardCount(float hoursOffline)
        {
            if (guaranteeEquipment) return maxEquipmentRewards;

            // More equipment for longer offline time
            float timeRatio = hoursOffline / 24f;
            int count = Mathf.RoundToInt(Mathf.Lerp(minEquipmentRewards, maxEquipmentRewards, timeRatio));

            return Mathf.Clamp(count, minEquipmentRewards, maxEquipmentRewards);
        }

        /// <summary>
        /// Generate complete reward list based on time-based configuration
        /// Requires minimum 3 minutes (0.05 hours) to generate any rewards
        /// </summary>
        public List<HarvestRewardData> GenerateRewards(int world, float hoursOffline)
        {
            var rewards = new List<HarvestRewardData>();

            // Minimum time requirement: 3 minutes = 0.05 hours
            const float minimumHours = 0.05f;

            if (hoursOffline < minimumHours)
            {
                if (enableDebugLogs)
                    Debug.Log($"[HarvestReward] No rewards - insufficient time. Need {minimumHours:F2} hours, got {hoursOffline:F3} hours");
                return rewards; // Return empty list
            }

            if (enableDebugLogs)
                Debug.Log($"[HarvestReward] Generating time-based rewards for World {world}, {hoursOffline:F3} hours offline");

            // Always give gold and exp (when minimum time is met)
            rewards.Add(new HarvestRewardData
            {
                rewardHarvestType = RewardHarvestType.Gold,
                amount = CalculateGoldReward(world, hoursOffline),
                icon = goldIcon,
                name = "Gold"
            });

            rewards.Add(new HarvestRewardData
            {
                rewardHarvestType = RewardHarvestType.Exp,
                amount = CalculateExpReward(world, hoursOffline),
                icon = expIcon,
                name = "EXP"
            });

            // Always give character pieces
            rewards.Add(new HarvestRewardData
            {
                rewardHarvestType = RewardHarvestType.CharacterPieces,
                amount = CalculateCharacterPiecesReward(world, hoursOffline),
                icon = characterPiecesIcon,
                name = "Character Pieces"
            });

            // Time-based rewards (gems and equipment based on time thresholds)
            var timeBasedRewards = GetTimeBasedRewards(world, hoursOffline);
            rewards.AddRange(timeBasedRewards);

            if (enableDebugLogs)
                Debug.Log($"[HarvestReward] Generated {rewards.Count} time-based rewards");

            return rewards;
        }

        /// <summary>
        /// Get time-based rewards (gems and equipment) based on time thresholds
        /// </summary>
        private List<HarvestRewardData> GetTimeBasedRewards(int world, float hoursOffline)
        {
            var rewards = new List<HarvestRewardData>();

            foreach (var tier in rewardTiers)
            {
                if (hoursOffline >= tier.requiredHours)
                {
                    // Add gems if tier provides them
                    if (tier.provideGems && tier.gemAmount > 0)
                    {
                        rewards.Add(new HarvestRewardData
                        {
                            rewardHarvestType = RewardHarvestType.Gem,
                            amount = Mathf.RoundToInt(tier.gemAmount * (world * 0.1f + 1f)), // Scale with world
                            icon = gemIcon,
                            name = "Gems"
                        });
                    }

                    // Add equipment if tier provides them
                    if (tier.provideEquipment && tier.equipmentCount > 0)
                    {
                        for (int i = 0; i < tier.equipmentCount; i++)
                        {
                            rewards.Add(new HarvestRewardData
                            {
                                rewardHarvestType = RewardHarvestType.Equipment,
                                amount = 1,
                                equipmentRarity = tier.equipmentRarity,
                                name = $"{tier.equipmentRarity} Equipment"
                            });
                        }
                    }
                }
            }

            return rewards;
        }

        /// <summary>
        /// Get formatted time bonus information for UI
        /// </summary>
        public string GetTimeBonusInfo()
        {
            string info = "Time Bonuses:\n";
            foreach (var bonus in timeBonuses)
            {
                info += $"• {bonus.requiredHours}h: +{(bonus.bonusMultiplier - 1f) * 100:F0}% bonus\n";
            }
            return info;
        }

        /// <summary>
        /// Validate configuration in editor
        /// </summary>
        private void OnValidate()
        {
            // Ensure probabilities don't exceed 1.0
            foreach (var prob in rewardProbabilities)
            {
                prob.baseProbability = Mathf.Clamp01(prob.baseProbability);
                prob.maxProbability = Mathf.Clamp01(prob.maxProbability);
            }

            // Ensure equipment rarity chances are reasonable
            float totalChance = 0f;
            foreach (var rarity in equipmentRarityChances)
            {
                totalChance += rarity.baseChance + rarity.timeBonusChance;
            }

            if (totalChance > 1.1f) // Allow some tolerance
            {
                Debug.LogWarning($"[HarvestRewardConfig] Total equipment rarity chances exceed 100%: {totalChance * 100:F1}%");
            }
        }

        /// <summary>
        /// Reset to default values
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            // Reset time multiplier curve
            timeMultiplierCurve = AnimationCurve.Linear(0f, 1f, 24f, 2f);
            maxTimeMultiplier = 3f;
            minTimeMultiplier = 0.5f;

            // Reset base rewards (increased for better Quick Harvest rewards)
            baseGoldPerHour = new BaseRewardData(200, 100);
            baseExpPerHour = new BaseRewardData(100, 50);
            baseGemPerHour = new BaseRewardData(10, 5);
            baseCharacterPiecesPerHour = new BaseRewardData(20, 10);

            // Reset probabilities
            rewardProbabilities.Clear();
            rewardProbabilities.Add(new RewardProbabilityData(RewardHarvestType.Gold, 1f, 1f, 0f));
            rewardProbabilities.Add(new RewardProbabilityData(RewardHarvestType.Exp, 1f, 1f, 0f));
            rewardProbabilities.Add(new RewardProbabilityData(RewardHarvestType.CharacterPieces, 1f, 1f, 0f));
            rewardProbabilities.Add(new RewardProbabilityData(RewardHarvestType.Gem, 0.3f, 0.8f, 12f));
            rewardProbabilities.Add(new RewardProbabilityData(RewardHarvestType.Equipment, 0.5f, 0.9f, 6f));

            // Reset time-based reward tiers (minimum 3 minutes required)
            rewardTiers.Clear();
            rewardTiers.Add(new TimeBasedRewardTier(0.05f, true, 5, false, 0, EquipmentRarity.Common)); // 3 minutes = 0.05 hours (minimum)
            rewardTiers.Add(new TimeBasedRewardTier(0.5f, true, 15, true, 1, EquipmentRarity.Common));  // 30 minutes
            rewardTiers.Add(new TimeBasedRewardTier(2f, true, 30, true, 1, EquipmentRarity.Rare));      // 2 hours
            rewardTiers.Add(new TimeBasedRewardTier(6f, true, 50, true, 2, EquipmentRarity.Rare));      // 6 hours
            rewardTiers.Add(new TimeBasedRewardTier(12f, true, 75, true, 2, EquipmentRarity.Epic));     // 12 hours
            rewardTiers.Add(new TimeBasedRewardTier(24f, true, 100, true, 3, EquipmentRarity.Epic));    // 24 hours

            // Reset equipment rarities
            equipmentRarityChances.Clear();
            equipmentRarityChances.Add(new EquipmentRarityChance(EquipmentRarity.Common, 0.6f, 0f));
            equipmentRarityChances.Add(new EquipmentRarityChance(EquipmentRarity.Rare, 0.3f, 0.1f));
            equipmentRarityChances.Add(new EquipmentRarityChance(EquipmentRarity.Epic, 0.08f, 0.05f));
            equipmentRarityChances.Add(new EquipmentRarityChance(EquipmentRarity.Legendary, 0.02f, 0.03f));

            // Reset time bonuses
            timeBonuses.Clear();
            timeBonuses.Add(new TimeBonusData(6f, 1.2f));
            timeBonuses.Add(new TimeBonusData(12f, 1.5f));
            timeBonuses.Add(new TimeBonusData(24f, 2f));

            Debug.Log("[HarvestRewardConfig] Reset to default values");
        }
    }

    [System.Serializable]
    public class BaseRewardData
    {
        public int baseAmount;
        public int incrementPerWorld;

        public BaseRewardData(int baseAmount, int incrementPerWorld)
        {
            this.baseAmount = baseAmount;
            this.incrementPerWorld = incrementPerWorld;
        }

        public float CalculateForWorld(int world, float scalingFactor, int maxWorld)
        {
            int clampedWorld = Mathf.Clamp(world, 1, maxWorld);
            float linearAmount = baseAmount + (incrementPerWorld * (clampedWorld - 1));

            // Apply exponential scaling for higher worlds
            if (clampedWorld > 10)
            {
                float extraScaling = Mathf.Pow(scalingFactor, clampedWorld - 10);
                linearAmount *= extraScaling;
            }

            return linearAmount;
        }
    }

    [System.Serializable]
    public class RewardProbabilityData
    {
        [FormerlySerializedAs("rewardType")] public RewardHarvestType rewardHarvestType;
        [Range(0f, 1f)] public float baseProbability;
        [Range(0f, 1f)] public float maxProbability;
        public float maxProbabilityHours; // Hours to reach max probability

        public RewardProbabilityData(RewardHarvestType harvestType, float baseProb, float maxProb, float maxHours)
        {
            rewardHarvestType = harvestType;
            baseProbability = baseProb;
            maxProbability = maxProb;
            maxProbabilityHours = maxHours;
        }

        public float CalculateProbability(float hoursOffline)
        {
            if (maxProbabilityHours <= 0f) return baseProbability;

            float timeRatio = Mathf.Clamp01(hoursOffline / maxProbabilityHours);
            return Mathf.Lerp(baseProbability, maxProbability, timeRatio);
        }
    }

    [System.Serializable]
    public class EquipmentRarityChance
    {
        public EquipmentRarity rarity;
        [Range(0f, 1f)] public float baseChance;
        [Range(0f, 0.2f)] public float timeBonusChance; // Extra chance based on time

        public EquipmentRarityChance(EquipmentRarity rarity, float baseChance, float timeBonusChance)
        {
            this.rarity = rarity;
            this.baseChance = baseChance;
            this.timeBonusChance = timeBonusChance;
        }
    }

    [System.Serializable]
    public class TimeBonusData
    {
        public float requiredHours;
        public float bonusMultiplier;

        public TimeBonusData(float requiredHours, float bonusMultiplier)
        {
            this.requiredHours = requiredHours;
            this.bonusMultiplier = bonusMultiplier;
        }
    }

    [System.Serializable]
    public class HarvestRewardData
    {
        public RewardHarvestType rewardHarvestType;
        public int amount;
        public EquipmentRarity equipmentRarity;

        // Additional fields for UI display
        public Sprite icon;
        public string name;
        public EquipmentModel equipmentData;
    }

    [System.Serializable]
    public class TimeBasedRewardTier
    {
        [Header("Time Requirement")]
        public float requiredHours;

        [Header("Gem Rewards")]
        public bool provideGems;
        public int gemAmount;

        [Header("Equipment Rewards")]
        public bool provideEquipment;
        public int equipmentCount;
        public EquipmentRarity equipmentRarity;

        public TimeBasedRewardTier(float hours, bool gems, int gemAmt, bool equipment, int equipCount, EquipmentRarity rarity)
        {
            requiredHours = hours;
            provideGems = gems;
            gemAmount = gemAmt;
            provideEquipment = equipment;
            equipmentCount = equipCount;
            equipmentRarity = rarity;
        }
    }

    public enum RewardHarvestType
    {
        Gold,
        Exp,
        Gem,
        Equipment,
        CharacterPieces
    }
}