using System.Collections.Generic;
using System.Linq;
using OctoberStudio;
using OctoberStudio.Save;
using OctoberStudio.Upgrades;
using TwoSleepyCats.Patterns.Singleton;
using UnityEngine;
using UnityEngine.Events;
using Talents.Data;
using UnityEngine.Serialization;

namespace Talents.Manager
{
    /// <summary>
    /// Manager for talent system - handles learning, upgrading, and calculating bonuses
    /// </summary>
    public class TalentManager : MonoSingleton<TalentManager>
    {
        [FormerlySerializedAs("currentTalentPoints")]
        [Header("Currency System")]
        [SerializeField] private int currentGoldCoins = 0;
        [SerializeField] private int currentOrc = 0;
        [SerializeField] private bool unlimitedCurrency = false; 

        [Header("Events")]
        public UnityEvent OnGoldCoinsChanged;
        public UnityEvent<TalentModel> OnTalentLearned;
        public UnityEvent<TalentModel> OnTalentUpgraded;
        public UnityEvent<string> OnTalentError;

        // Save data
        private TalentSave talentSave;

        // Properties
        public int CurrentGoldCoins => currentGoldCoins;
        public int CurrentOrc => currentOrc;
        public bool IsInitialized { get; private set; }

        protected override void Initialize()
        {
            base.Initialize();
            
            if (GameController.SaveManager != null)
            {
                InitializeTalentSystem();
            }
            else
            {
                StartCoroutine(WaitForSaveManager());
            }
        }

        private System.Collections.IEnumerator WaitForSaveManager()
        {
            while (GameController.SaveManager == null)
            {
                yield return null;
            }
            
            InitializeTalentSystem();
        }

        private void InitializeTalentSystem()
        {
            talentSave = GameController.SaveManager.GetSave<TalentSave>("Talents");
            
            if (talentSave != null)
            {
                talentSave.Init();
                IsInitialized = true;
                LoadTalentPoints();
            }
            else
            {
                Debug.LogError("[TalentManager] Failed to initialize talent save");
            }
        }
        


        /// <summary>
        /// Get current level of a talent
        /// </summary>
        public int GetTalentLevel(int talentId)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[TalentManager] Manager not initialized");
                return 0;
            }

            return talentSave.GetTalentLevel(talentId);
        }

        /// <summary>
        /// Check if talent is learned (level > 0)
        /// </summary>
        public bool IsTalentLearned(int talentId)
        {
            return GetTalentLevel(talentId) > 0;
        }

        /// <summary>
        /// Check if talent can be learned/upgraded
        /// </summary>
        public bool CanLearnTalent(int talentId)
        {
            if (!IsInitialized)
                return false;

            var talent = TalentDatabase.Instance.GetTalentById(talentId);
            if (talent == null)
                return false;

            // Check if talent is already at max level
            var currentLevel = GetTalentLevel(talentId);
            if (currentLevel >= talent.MaxLevel)
                return false;

            // Kiểm tra Player Level requirement (thay vì prerequisites)
            if (!IsPlayerLevelSufficient(talentId))
                return false;

            // Kiểm tra tiền tệ theo loại talent
            var cost = TalentDatabase.Instance.GetTalentCost(talentId, currentLevel + 1);
    
            if (IsBaseStat(talent))
            {
                // Base Stats dùng Gold Coins
                if (!unlimitedCurrency && currentGoldCoins < cost)
                    return false;
            }
            else if (IsSpecialSkill(talent))
            {
                // Special Skills dùng Orc (Gold DNA)
                if (!unlimitedCurrency && currentOrc < cost)
                    return false;
            }

            return true;
        }
        private bool IsBaseStat(TalentModel talent)
        {
            return talent.StatType == UpgradeType.Damage ||
                   talent.StatType == UpgradeType.Health ||
                   talent.NodeType == TalentNodeType.Normal; 
        }
        private bool IsSpecialSkill(TalentModel talent)
        {
            return talent.NodeType == TalentNodeType.Special;
        }
        
        
        /// <summary>
        /// Check if all prerequisites for a talent are met
        /// </summary>
        public bool ArePrerequisitesMet(int talentId)
        {
            if (!IsInitialized)
                return false;

            var dependencies = TalentDatabase.Instance.GetTalentDependencies(talentId);
            
            foreach (var depId in dependencies)
            {
                if (!IsTalentLearned(depId))
                {
                    return false;
                }
            }

            return true;
        }
        private bool IsPlayerLevelSufficient(int talentId)
        {
            var talent = TalentDatabase.Instance.GetTalentById(talentId);
            if (talent == null)
                return false;

            // Lấy Player Level từ hệ thống game
            int playerLevel = GetPlayerLevel(); 
    
            if (IsBaseStat(talent))
            {
                // Base Stats: có thể nâng cấp từ level 1
                return playerLevel >= 1;
            }
            else if (IsSpecialSkill(talent))
            {
                // Special Skills: yêu cầu level cụ thể
                // Ví dụ: Lucky Dog level 3, Rogue level 5, Athlete level 7
                return playerLevel >= talent.RequiredPlayerLevel; // Cần thêm field này vào TalentModel
            }

            return false;
        }
        public bool LearnTalent(int talentId)
        {
            if (!CanLearnTalent(talentId))
            {
                OnTalentError?.Invoke($"Cannot learn talent {talentId}");
                return false;
            }

            var talent = TalentDatabase.Instance.GetTalentById(talentId);
            if (talent == null)
            {
                OnTalentError?.Invoke($"Talent {talentId} not found");
                return false;
            }

            var currentLevel = GetTalentLevel(talentId);
            var newLevel = currentLevel + 1;
            var cost = TalentDatabase.Instance.GetTalentCost(talentId, newLevel);

            // Trừ tiền tệ đúng loại
            if (!unlimitedCurrency)
            {
                if (IsBaseStat(talent))
                {
                    currentGoldCoins -= cost;
                    var goldSave = GameController.SaveManager.GetSave<CurrencySave>("GoldCoins");
                    goldSave?.Withdraw(cost);
                    OnGoldCoinsChanged?.Invoke();
                }
                else if (IsSpecialSkill(talent))
                {
                    // Trừ Orc cho Special Skills
                    currentOrc -= cost;
                    var orcSave = GameController.SaveManager.GetSave<CurrencySave>("Orc");
                    orcSave?.Withdraw(cost);
                    // Cần thêm event OnOrcChanged
                }
            }

            // Update talent level
            talentSave.SetTalentLevel(talentId, newLevel);

            // Trigger events
            if (currentLevel == 0)
            {
                OnTalentLearned?.Invoke(talent);
                Debug.Log($"[TalentManager] Learned talent: {talent.Name} (Level {newLevel})");
            }
            else
            {
                OnTalentUpgraded?.Invoke(talent);
                Debug.Log($"[TalentManager] Upgraded talent: {talent.Name} to Level {newLevel}");
            }

            // Update player stats
            UpdatePlayerStats();

            return true;
        }

        // 6. Thêm hàm lấy Player Level (cần implement)
        private int GetPlayerLevel()
        {
            // TODO: Implement logic lấy player level từ PlayerBehavior hoặc GameController
            // Tạm thời return level cao để test
            return  10;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void LoadTalentPoints()
        {
            var goldSave = GameController.SaveManager.GetSave<CurrencySave>("GoldCoins");
            var orcSave = GameController.SaveManager.GetSave<CurrencySave>("Orc");
            
            currentGoldCoins = goldSave?.Amount ?? 0;
            currentOrc = orcSave?.Amount ?? 0;
            
            Debug.Log($"[TalentManager] Loaded currencies - Gold: {currentGoldCoins}, Orc: {currentOrc}");
        }
        
        /// <summary>
        /// Reset all talents
        /// </summary>
        public void ResetAllTalents()
        {
            if (!IsInitialized)
                return;

            var allTalents = talentSave.GetAllTalents();
            int totalRefund = 0;

            foreach (var kvp in allTalents)
            {
                var talentId = kvp.Key;
                var level = kvp.Value;

                // Calculate refund for this talent
                for (int l = 1; l <= level; l++)
                {
                    totalRefund += TalentDatabase.Instance.GetTalentCost(talentId, l);
                }
            }

            // Clear all talents
            talentSave.Clear();

            // Update player stats
            UpdatePlayerStats();

            Debug.Log($"[TalentManager] Reset all talents (Refunded {totalRefund} points)");
        }

        /// <summary>
        /// Calculate total stat bonuses from all learned talents
        /// </summary>
        public Dictionary<UpgradeType, float> GetTotalTalentBonuses()
        {
            var bonuses = new Dictionary<UpgradeType, float>();

            if (!IsInitialized)
                return bonuses;

            var allTalents = talentSave.GetAllTalents();

            foreach (var kvp in allTalents)
            {
                var talentId = kvp.Key;
                var level = kvp.Value;

                if (level <= 0)
                    continue;

                var talent = TalentDatabase.Instance.GetTalentById(talentId);
                if (talent == null)
                    continue;

                // Calculate bonus for this talent
                var bonus = talent.StatValue * level;

                if (bonuses.ContainsKey(talent.StatType))
                {
                    bonuses[talent.StatType] += bonus;
                }
                else
                {
                    bonuses[talent.StatType] = bonus;
                }
            }

            return bonuses;
        }

        /// <summary>
        /// Get bonus for specific stat type
        /// </summary>
        public float GetTalentBonus(UpgradeType statType)
        {
            var bonuses = GetTotalTalentBonuses();
            return bonuses.GetValueOrDefault(statType, 0f);
        }

        /// <summary>
        /// Update player stats based on talent bonuses
        /// </summary>
        private void UpdatePlayerStats()
        {
            if (PlayerBehavior.Player != null)
            {
                // Apply talent bonuses to player
                var bonuses = GetTotalTalentBonuses();
                
                // This would need to be implemented in PlayerBehavior
                // PlayerBehavior.Player.ApplyTalentBonuses(bonuses);
                
                Debug.Log($"[TalentManager] Updated player stats with {bonuses.Count} talent bonuses");
            }
        }

        /// <summary>
        /// Get talent unlock status for UI
        /// </summary>
        public TalentUnlockStatus GetTalentUnlockStatus(int talentId)
        {
            if (!IsInitialized)
                return TalentUnlockStatus.Locked;

            var talent = TalentDatabase.Instance.GetTalentById(talentId);
            if (talent == null)
                return TalentUnlockStatus.Locked;

            var currentLevel = GetTalentLevel(talentId);
            
            // Check if at max level
            if (currentLevel >= talent.MaxLevel)
                return TalentUnlockStatus.MaxLevel;

            // Check if can be learned
            if (CanLearnTalent(talentId))
                return TalentUnlockStatus.Available;

            // Check if learned but can't upgrade
            if (currentLevel > 0)
                return TalentUnlockStatus.Learned;

            if (!ArePrerequisitesMet(talentId))
                return TalentUnlockStatus.Locked;

            // Not enough points
            return TalentUnlockStatus.InsufficientPoints;
        }

        /// <summary>
        /// Get talent progress info for UI
        /// </summary>
        public TalentProgressInfo GetTalentProgressInfo(int talentId)
        {
            var talent = TalentDatabase.Instance.GetTalentById(talentId);
            if (talent == null)
                return new TalentProgressInfo();

            var currentLevel = GetTalentLevel(talentId);
            var cost = currentLevel < talent.MaxLevel ? TalentDatabase.Instance.GetTalentCost(talentId, currentLevel + 1) : 0;

            return new TalentProgressInfo
            {
                TalentId = talentId,
                CurrentLevel = currentLevel,
                MaxLevel = talent.MaxLevel,
                NextLevelCost = cost,
                UnlockStatus = GetTalentUnlockStatus(talentId),
                CurrentBonus = talent.StatValue * currentLevel,
                NextLevelBonus = talent.StatValue * (currentLevel + 1)
            };
        }

        /// <summary>
        /// Get formatted talent info for tooltips
        /// </summary>
        public string GetTalentTooltip(int talentId)
        {
            var talent = TalentDatabase.Instance.GetTalentById(talentId);
            if (talent == null)
                return "Unknown Talent";

            var progressInfo = GetTalentProgressInfo(talentId);
            var tooltip = $"<b>{talent.Name}</b>\n";
            tooltip += $"{talent.Description}\n\n";
            tooltip += $"Level: {progressInfo.CurrentLevel}/{progressInfo.MaxLevel}\n";
            tooltip += $"Stat: {talent.StatType} +{talent.StatValue}\n";
            
            if (progressInfo.CurrentLevel > 0)
            {
                tooltip += $"Current Bonus: +{progressInfo.CurrentBonus}\n";
            }
            
            if (progressInfo.CurrentLevel < progressInfo.MaxLevel)
            {
                tooltip += $"Next Level: +{progressInfo.NextLevelBonus}\n";
                tooltip += $"Cost: {progressInfo.NextLevelCost} points\n";
            }

            return tooltip;
        }

        [ContextMenu("Toggle Unlimited Points")]
        public void ToggleUnlimitedPoints()
        {
            unlimitedCurrency = !unlimitedCurrency;
            Debug.Log($"[TalentManager] Unlimited points: {unlimitedCurrency}");
        }

        [ContextMenu("Log Talent Bonuses")]
        public void LogTalentBonuses()
        {
            var bonuses = GetTotalTalentBonuses();
            Debug.Log("=== TALENT BONUSES ===");
            
            foreach (var kvp in bonuses)
            {
                Debug.Log($"{kvp.Key}: +{kvp.Value}");
            }
        }
    }

    /// <summary>
    /// Talent unlock status enumeration
    /// </summary>
    public enum TalentUnlockStatus
    {
        Locked,              // Prerequisites not met
        Available,           // Can be learned
        Learned,             // Already learned but can be upgraded
        InsufficientPoints,  // Not enough talent points
        MaxLevel             // Already at maximum level
    }

    /// <summary>
    /// Talent progress information structure
    /// </summary>
    [System.Serializable]
    public struct TalentProgressInfo
    {
        public int TalentId;
        public int CurrentLevel;
        public int MaxLevel;
        public int NextLevelCost;
        public TalentUnlockStatus UnlockStatus;
        public float CurrentBonus;
        public float NextLevelBonus;
    }
}