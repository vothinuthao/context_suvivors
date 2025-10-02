using UnityEngine;
using System.Collections.Generic;
using OctoberStudio.Equipment;
using TwoSleepyCats.Patterns.Singleton;

namespace OctoberStudio.Harvest
{
    /// <summary>
    /// Central manager for harvest reward system
    /// Handles configuration, reward generation, and persistence
    /// </summary>
    public class HarvestManager : MonoSingleton<HarvestManager>
    {
        [Header("Configuration")]
        [SerializeField] private HarvestRewardConfig defaultConfig;
        [SerializeField] private bool autoLoadConfig = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool simulateOfflineTime = false;
        [SerializeField] private float simulatedOfflineHours = 24f;

        // Current configuration
        private HarvestRewardConfig currentConfig;

        // Events
        public System.Action<List<HarvestRewardData>> OnRewardsGenerated;
        public System.Action<RewardHarvestType, int> OnRewardApplied;

        protected override void Initialize()
        {
            base.Initialize();

            if (autoLoadConfig)
            {
                LoadConfiguration();
            }
        }

        /// <summary>
        /// Load harvest configuration
        /// </summary>
        public void LoadConfiguration()
        {
            if (defaultConfig != null)
            {
                currentConfig = defaultConfig;
                if (enableDebugLogs)
                    Debug.Log("[HarvestManager] Configuration loaded");
            }
            else
            {
                Debug.LogError("[HarvestManager] No default configuration assigned!");
            }
        }

        /// <summary>
        /// Set custom configuration
        /// </summary>
        public void SetConfiguration(HarvestRewardConfig config)
        {
            currentConfig = config;
            if (enableDebugLogs)
                Debug.Log("[HarvestManager] Custom configuration set");
        }

        /// <summary>
        /// Generate rewards for current world and time offline
        /// </summary>
        public List<HarvestRewardData> GenerateRewards(int world, float hoursOffline)
        {
            if (currentConfig == null)
            {
                Debug.LogError("[HarvestManager] No configuration available!");
                return new List<HarvestRewardData>();
            }

            // Use simulated time if enabled
            if (simulateOfflineTime)
            {
                hoursOffline = simulatedOfflineHours;
                if (enableDebugLogs)
                    Debug.Log($"[HarvestManager] Using simulated offline time: {hoursOffline:F1} hours");
            }

            var rewards = currentConfig.GenerateRewards(world, hoursOffline);
            OnRewardsGenerated?.Invoke(rewards);

            if (enableDebugLogs)
                Debug.Log($"[HarvestManager] Generated {rewards.Count} rewards for World {world}, {hoursOffline:F1}h offline");

            return rewards;
        }

        /// <summary>
        /// Generate rewards based on current game state
        /// </summary>
        public List<HarvestRewardData> GenerateCurrentRewards()
        {
            int currentWorld = GetCurrentWorld();
            float hoursOffline = GetHoursOffline();

            return GenerateRewards(currentWorld, hoursOffline);
        }

        /// <summary>
        /// Apply rewards to player inventory and currencies
        /// </summary>
        public void ApplyRewards(List<HarvestRewardData> rewards)
        {
            if (rewards == null || rewards.Count == 0) return;

            foreach (var reward in rewards)
            {
                ApplyReward(reward);
            }

            if (enableDebugLogs)
                Debug.Log($"[HarvestManager] Applied {rewards.Count} rewards");
        }

        /// <summary>
        /// Apply single reward
        /// </summary>
        private void ApplyReward(HarvestRewardData reward)
        {
            if (reward.amount <= 0) return;

            switch (reward.rewardHarvestType)
            {
                case RewardHarvestType.Gold:
                    GameController.CurrenciesManager?.Add("gold", reward.amount);
                    break;

                case RewardHarvestType.Gem:
                    GameController.CurrenciesManager?.Add("gem", reward.amount);
                    break;


                case RewardHarvestType.Exp:
                    // Add experience if you have experience manager
                    // GameController.ExperienceManager?.AddExp(reward.amount);
                    break;

                case RewardHarvestType.Equipment:
                    ApplyEquipmentReward(reward);
                    break;

                case RewardHarvestType.CharacterPieces:
                    ApplyCharacterPiecesReward(reward);
                    break;
            }

            OnRewardApplied?.Invoke(reward.rewardHarvestType, reward.amount);

            if (enableDebugLogs)
                Debug.Log($"[HarvestManager] Applied reward: {reward.rewardHarvestType} x{reward.amount}");
        }

        /// <summary>
        /// Apply equipment reward
        /// </summary>
        private void ApplyEquipmentReward(HarvestRewardData reward)
        {
            if (EquipmentDatabase.Instance == null) return;

            var equipment = EquipmentDatabase.Instance.GetRandomEquipmentByRarity(reward.equipmentRarity);
            if (equipment != null)
            {
                GameController.SaveManager
                    .GetSave<EquipmentSave>("Equipment")
                    .AddToInventory(equipment.EquipmentType, equipment.ID, reward.amount);

                if (enableDebugLogs)
                    Debug.Log($"[HarvestManager] Added equipment: {equipment.GetDisplayName()} ({reward.equipmentRarity})");
            }
        }

        /// <summary>
        /// Apply character pieces reward
        /// </summary>
        private void ApplyCharacterPiecesReward(HarvestRewardData reward)
        {
            if (GameController.SaveManager == null) return;

            var charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
            if (charactersSave != null)
            {
                // For now, give pieces to the currently selected character
                int selectedCharacterId = charactersSave.SelectedCharacterId;
                charactersSave.AddCharacterPieces(selectedCharacterId, reward.amount);

                if (enableDebugLogs)
                    Debug.Log($"[HarvestManager] Added {reward.amount} character pieces to character {selectedCharacterId}");
            }
        }

        /// <summary>
        /// Get current world from save system
        /// </summary>
        private int GetCurrentWorld()
        {
            if (GameController.SaveManager != null)
            {
                var stageSave = GameController.SaveManager.GetSave<StageSave>("Stage");
                return stageSave.MaxReachedStageId + 1;
            }
            return 1;
        }

        /// <summary>
        /// Calculate hours offline from harvest save
        /// </summary>
        private float GetHoursOffline()
        {
            if (GameController.SaveManager == null) return 0f;

            var harvestSave = GameController.SaveManager.GetSave<HarvestSave>("Harvest");
            if (harvestSave == null) return 0f;

            // Calculate based on remaining seconds vs max time
            float maxHours = 24f; // This should match HarvestWindowBehavior.maxOfflineHours
            float remainingSeconds = harvestSave.RemainingSeconds;
            float maxSeconds = maxHours * 3600f;

            float hoursOffline = (maxSeconds - remainingSeconds) / 3600f;
            return Mathf.Max(hoursOffline, 0.1f); // Minimum 0.1 hour
        }

        /// <summary>
        /// Get reward preview for specific time
        /// </summary>
        public List<HarvestRewardData> GetRewardPreview(int world, float hoursOffline)
        {
            if (currentConfig == null) return new List<HarvestRewardData>();

            return currentConfig.GenerateRewards(world, hoursOffline);
        }

        /// <summary>
        /// Get probability info for UI
        /// </summary>
        public string GetProbabilityInfo(float hoursOffline)
        {
            if (currentConfig == null) return "No configuration available";

            string info = "Reward Probabilities:\n";
            info += "• Gold: 100% (guaranteed)\n";
            info += "• EXP: 100% (guaranteed)\n";

            // Check other reward probabilities
            bool gemsAvailable = currentConfig.ShouldGiveReward(RewardHarvestType.Gem, hoursOffline);
            bool equipmentAvailable = currentConfig.ShouldGiveReward(RewardHarvestType.Equipment, hoursOffline);

            if (gemsAvailable)
                info += "• Gems: Available\n";
            else
                info += "• Gems: Not available\n";

            if (equipmentAvailable)
                info += "• Equipment: Available\n";
            else
                info += "• Equipment: Not available\n";

            return info;
        }

        /// <summary>
        /// Get time bonus info for UI
        /// </summary>
        public string GetTimeBonusInfo()
        {
            if (currentConfig == null) return "";
            return currentConfig.GetTimeBonusInfo();
        }

        /// <summary>
        /// Check if rewards are ready to be collected
        /// </summary>
        public bool CanCollectRewards()
        {
            if (GameController.SaveManager == null) return false;

            var harvestSave = GameController.SaveManager.GetSave<HarvestSave>("Harvest");
            return harvestSave != null && harvestSave.CanHarvest;
        }

        /// <summary>
        /// Get formatted time remaining until next harvest
        /// </summary>
        public string GetTimeRemainingFormatted()
        {
            if (GameController.SaveManager == null) return "N/A";

            var harvestSave = GameController.SaveManager.GetSave<HarvestSave>("Harvest");
            if (harvestSave == null) return "N/A";

            float remainingSeconds = harvestSave.RemainingSeconds;
            System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(remainingSeconds);

            return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        /// <summary>
        /// Debug method to test reward generation
        /// </summary>
        [ContextMenu("Test Reward Generation")]
        public void TestRewardGeneration()
        {
            var rewards = GenerateCurrentRewards();
            Debug.Log($"[HarvestManager] Test generated {rewards.Count} rewards:");

            foreach (var reward in rewards)
            {
                string rewardInfo = $"  - {reward.rewardHarvestType}: {reward.amount}";
                if (reward.rewardHarvestType == RewardHarvestType.Equipment)
                    rewardInfo += $" ({reward.equipmentRarity})";
                if (!string.IsNullOrEmpty(reward.name))
                    rewardInfo += $" [{reward.name}]";

                Debug.Log(rewardInfo);
            }
        }

        /// <summary>
        /// Debug method to simulate harvest collection
        /// </summary>
        [ContextMenu("Test Collect Rewards")]
        public void TestCollectRewards()
        {
            var rewards = GenerateCurrentRewards();
            ApplyRewards(rewards);
            Debug.Log("[HarvestManager] Test rewards applied!");
        }

        /// <summary>
        /// Toggle simulation mode
        /// </summary>
        [ContextMenu("Toggle Simulation Mode")]
        public void ToggleSimulationMode()
        {
            simulateOfflineTime = !simulateOfflineTime;
            Debug.Log($"[HarvestManager] Simulation mode: {simulateOfflineTime}");
        }

        /// <summary>
        /// Test time-based reward generation at different time intervals
        /// </summary>
        [ContextMenu("Test Time-Based Rewards")]
        public void TestTimeBasedRewards()
        {
            if (currentConfig == null)
            {
                Debug.LogError("[HarvestManager] No configuration available for testing!");
                return;
            }

            float[] testTimes = { 0.5f, 2f, 6f, 12f, 24f };
            int currentWorld = GetCurrentWorld();

            Debug.Log($"[HarvestManager] Testing time-based rewards for World {currentWorld}:");

            foreach (float hours in testTimes)
            {
                var rewards = currentConfig.GenerateRewards(currentWorld, hours);
                Debug.Log($"\n--- {hours}h offline rewards ({rewards.Count} total) ---");

                foreach (var reward in rewards)
                {
                    string rewardInfo = $"  • {reward.rewardHarvestType}: {reward.amount}";
                    if (reward.rewardHarvestType == RewardHarvestType.Equipment)
                        rewardInfo += $" ({reward.equipmentRarity})";

                    Debug.Log(rewardInfo);
                }
            }
        }
    }
}