using UnityEngine;
using OctoberStudio.Equipment;

namespace OctoberStudio.Harvest
{
    /// <summary>
    /// Utility methods for harvest reward system
    /// Provides helper functions for reward creation and conversion
    /// </summary>
    public static class HarvestRewardUtilities
    {
        /// <summary>
        /// Create HarvestRewardData with basic properties
        /// </summary>
        public static HarvestRewardData CreateReward(RewardHarvestType type, int amount, Sprite icon = null, string name = null)
        {
            var reward = new HarvestRewardData
            {
                rewardHarvestType = type,
                amount = amount,
                icon = icon,
                name = name ?? type.ToString()
            };

            return reward;
        }

        /// <summary>
        /// Create equipment reward with rarity
        /// </summary>
        public static HarvestRewardData CreateEquipmentReward(EquipmentModel equipment, int amount = 1)
        {
            if (equipment == null) return null;

            var reward = new HarvestRewardData
            {
                rewardHarvestType = RewardHarvestType.Equipment,
                amount = amount,
                icon = equipment.GetIcon(),
                name = equipment.GetDisplayName(),
                equipmentData = equipment,
                equipmentRarity = equipment.Rarity
            };

            return reward;
        }

        /// <summary>
        /// Create equipment reward with specific rarity (random equipment)
        /// </summary>
        public static HarvestRewardData CreateRandomEquipmentReward(EquipmentRarity rarity, int amount = 1)
        {
            if (EquipmentDatabase.Instance == null) return null;

            var equipment = EquipmentDatabase.Instance.GetRandomEquipmentByRarity(rarity);
            return CreateEquipmentReward(equipment, amount);
        }

        /// <summary>
        /// Get display name for reward type
        /// </summary>
        public static string GetRewardTypeName(RewardHarvestType rewardType)
        {
            return rewardType switch
            {
                RewardHarvestType.Gold => "Gold Coins",
                RewardHarvestType.Gem => "Gems",
                RewardHarvestType.Exp => "Experience",
                RewardHarvestType.Equipment => "Equipment",
                _ => rewardType.ToString()
            };
        }

        /// <summary>
        /// Get color associated with reward type
        /// </summary>
        public static Color GetRewardTypeColor(RewardHarvestType rewardType)
        {
            return rewardType switch
            {
                RewardHarvestType.Gold => Color.yellow,
                RewardHarvestType.Gem => Color.magenta,
                RewardHarvestType.Exp => Color.cyan,
                RewardHarvestType.Equipment => Color.white,
                _ => Color.gray
            };
        }

        /// <summary>
        /// Get color for equipment rarity
        /// </summary>
        public static Color GetRarityColor(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => Color.white,
                EquipmentRarity.Rare => Color.blue,
                EquipmentRarity.Epic => Color.magenta,
                EquipmentRarity.Legendary => Color.yellow,
                _ => Color.white
            };
        }

        /// <summary>
        /// Check if reward type is currency-based
        /// </summary>
        public static bool IsCurrencyReward(RewardHarvestType rewardType)
        {
            return rewardType == RewardHarvestType.Gold ||
                   rewardType == RewardHarvestType.Gem;
        }

        /// <summary>
        /// Check if reward type is resource-based
        /// </summary>
        public static bool IsResourceReward(RewardHarvestType rewardType)
        {
            return rewardType == RewardHarvestType.Exp;
        }

        /// <summary>
        /// Check if reward type is item-based
        /// </summary>
        public static bool IsItemReward(RewardHarvestType rewardType)
        {
            return rewardType == RewardHarvestType.Equipment;
        }

        /// <summary>
        /// Validate reward data
        /// </summary>
        public static bool IsValidReward(HarvestRewardData reward)
        {
            if (reward == null) return false;
            if (reward.amount <= 0) return false;

            // Equipment rewards should have valid equipment data
            if (reward.rewardHarvestType == RewardHarvestType.Equipment)
            {
                return reward.equipmentData != null;
            }

            return true;
        }

        /// <summary>
        /// Create reward list from simple data
        /// </summary>
        public static HarvestRewardData[] CreateRewardList(params (RewardHarvestType type, int amount)[] rewardData)
        {
            var rewards = new HarvestRewardData[rewardData.Length];

            for (int i = 0; i < rewardData.Length; i++)
            {
                rewards[i] = CreateReward(rewardData[i].type, rewardData[i].amount);
            }

            return rewards;
        }

        /// <summary>
        /// Calculate total value of rewards (for comparison purposes)
        /// </summary>
        public static float CalculateRewardValue(HarvestRewardData reward)
        {
            if (reward == null) return 0f;

            float baseValue = reward.rewardHarvestType switch
            {
                RewardHarvestType.Gold => reward.amount * 1f,
                RewardHarvestType.Gem => reward.amount * 10f,
                RewardHarvestType.Exp => reward.amount * 0.5f,
                RewardHarvestType.Equipment => GetEquipmentValue(reward.equipmentRarity) * reward.amount,
                _ => 0f
            };

            return baseValue;
        }

        /// <summary>
        /// Get relative value of equipment by rarity
        /// </summary>
        private static float GetEquipmentValue(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => 50f,
                EquipmentRarity.Rare => 200f,
                EquipmentRarity.Epic => 500f,
                EquipmentRarity.Legendary => 1000f,
                _ => 50f
            };
        }

        /// <summary>
        /// Format reward text for UI display
        /// </summary>
        public static string FormatRewardText(HarvestRewardData reward, bool includeType = true)
        {
            if (reward == null) return "Unknown Reward";

            string text = "";

            if (includeType)
            {
                text += GetRewardTypeName(reward.rewardHarvestType);
            }
            else if (!string.IsNullOrEmpty(reward.name))
            {
                text += reward.name;
            }
            else
            {
                text += reward.rewardHarvestType.ToString();
            }

            if (reward.amount > 1 || !IsItemReward(reward.rewardHarvestType))
            {
                text += $" x{reward.amount}";
            }

            if (reward.rewardHarvestType == RewardHarvestType.Equipment)
            {
                text += $" ({reward.equipmentRarity})";
            }

            return text;
        }
    }
}