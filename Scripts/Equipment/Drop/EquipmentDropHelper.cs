using System.Collections.Generic;
using System.Linq;
using OctoberStudio.Save;
using OctoberStudio.User;
using UnityEngine;

namespace OctoberStudio.Equipment
{
    /// <summary>
    /// Static utility class for equipment drop logic
    /// </summary>
    public static class EquipmentDropHelper
    {
        /// <summary>
        /// Check if user is eligible for equipment drops based on level
        /// </summary>
        public static bool IsUserEligibleForDrops(int maxUserLevel = 5)
        {
            if (UserProfileManager.Instance?.ProfileSave == null)
                return true; // Default to true if profile system not available
                
            var userLevel = UserProfileManager.Instance.ProfileSave.UserLevel;
            return userLevel <= maxUserLevel;
        }
        
        /// <summary>
        /// Get all equipment that can drop from specified enemy type and stage level
        /// </summary>
        public static List<EquipmentModel> GetEligibleEquipment(bool isBoss, int stageLevel)
        {
            if (!EquipmentDatabase.Instance.IsDataLoaded)
            {
                Debug.LogWarning("[EquipmentDropHelper] Equipment database not loaded!");
                return new List<EquipmentModel>();
            }
            
            var allEquipment = EquipmentDatabase.Instance.GetAllEquipment();
            return allEquipment.Where(eq => eq.CanDropFromEnemy(isBoss, stageLevel)).ToList();
        }
        
        /// <summary>
        /// Select equipment to drop based on drop rates and weights
        /// </summary>
        public static EquipmentModel SelectEquipmentToDrop(List<EquipmentModel> eligibleEquipment, bool isBoss, float baseMultiplier = 1f)
        {
            if (eligibleEquipment.Count == 0)
                return null;
            
            // Calculate final multiplier
            float finalMultiplier = baseMultiplier * (isBoss ? 2f : 1f);
            
            // Create weighted list of equipment that pass drop roll
            var droppableEquipment = new List<(EquipmentModel equipment, float weight)>();
            
            foreach (var equipment in eligibleEquipment)
            {
                if (equipment.RollForDrop(finalMultiplier))
                {
                    // Weight by inverse rarity (common items drop more often when multiple items roll success)
                    float weight = GetRarityWeight(equipment.Rarity);
                    droppableEquipment.Add((equipment, weight));
                }
            }
            
            if (droppableEquipment.Count == 0)
                return null;
            
            // Select one equipment based on weight
            return SelectWeightedRandom(droppableEquipment);
        }
        
        /// <summary>
        /// Calculate equipment level for drops
        /// </summary>
        public static int CalculateDropLevel(EquipmentModel equipment, EquipmentDropConfig config = null)
        {
            int maxLevel = config?.MaxEquipmentLevel ?? 5;
            
            // Get current stage level
            var stageSave = GameController.SaveManager?.GetSave<StageSave>("Stage");
            int stageLevel = (stageSave?.SelectedStageId ?? 0) + 1;
            
            // Get user level
            int userLevel = UserProfileManager.Instance?.ProfileSave?.UserLevel ?? 1;
            
            // Equipment level = min(stage level, user level + 1, max equipment level)
            return Mathf.Min(stageLevel, userLevel + 1, maxLevel);
        }
        
        /// <summary>
        /// Get current stage level from various sources
        /// </summary>
        public static int GetCurrentStageLevel()
        {
            var stageSave = GameController.SaveManager?.GetSave<StageSave>("Stage");
            return (stageSave?.SelectedStageId ?? 0) + 1;
        }
        
        /// <summary>
        /// Get rarity weight for weighted random selection
        /// </summary>
        private static float GetRarityWeight(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => 100f,
                EquipmentRarity.Uncommon => 80f,
                EquipmentRarity.Rare => 60f,
                EquipmentRarity.Epic => 40f,
                EquipmentRarity.Legendary => 20f,
                _ => 50f
            };
        }
        
        /// <summary>
        /// Select random item based on weights
        /// </summary>
        private static EquipmentModel SelectWeightedRandom(List<(EquipmentModel equipment, float weight)> weightedList)
        {
            if (weightedList.Count == 1)
                return weightedList[0].equipment;
            
            float totalWeight = weightedList.Sum(x => x.weight);
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            
            float currentWeight = 0f;
            foreach (var (equipment, weight) in weightedList)
            {
                currentWeight += weight;
                if (randomValue <= currentWeight)
                    return equipment;
            }
            
            // Fallback to first item
            return weightedList[0].equipment;
        }
        
        /// <summary>
        /// Debug method to log eligible equipment for testing
        /// </summary>
        public static void LogEligibleEquipment(bool isBoss, int stageLevel)
        {
            var eligible = GetEligibleEquipment(isBoss, stageLevel);
            Debug.Log($"[EquipmentDropHelper] Eligible equipment for {(isBoss ? "Boss" : "Sub-Enemy")} at Stage {stageLevel}:");
            
            foreach (var equipment in eligible)
            {
                Debug.Log($"- {equipment.Name} ({equipment.Rarity}) - Drop Rate: {equipment.DropRate}%");
            }
        }
    }
}