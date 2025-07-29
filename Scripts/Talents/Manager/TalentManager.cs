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
    /// Zone-based talent manager with independent column progression
    /// </summary>
    public class TalentManager : MonoSingleton<TalentManager>
    {
        [Header("Debug Settings")]
        [SerializeField] private bool unlimitedCurrency = false;
        [SerializeField] private bool bypassLevelRequirement = false;

        [Header("Events")]
        public UnityEvent OnCurrencyChanged;
        public UnityEvent<TalentModel> OnTalentLearned;
        public UnityEvent<TalentModel> OnTalentUpgraded;
        public UnityEvent<string> OnTalentError;

        // Save data
        private TalentSave talentSave;

        // Properties
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
            if (!IsInitialized) return 0;
            return talentSave.GetTalentLevel(talentId);
        }

        /// <summary>
        /// Check if talent is learned
        /// </summary>
        public bool IsTalentLearned(int talentId)
        {
            return GetTalentLevel(talentId) > 0;
        }

        /// <summary>
        /// Check if talent can be learned - ZONE-BASED LOGIC
        /// </summary>
        public bool CanLearnTalent(int talentId)
        {
            if (!IsInitialized) return false;

            var talent = TalentDatabase.Instance.GetTalentById(talentId);
            if (talent == null) return false;

            // Check if already learned
            if (IsTalentLearned(talentId)) return false;

            // Check player level requirement
            if (!bypassLevelRequirement && GetCurrentPlayerLevel() < talent.RequiredPlayerLevel)
                return false;

            // Check currency requirement
            if (!unlimitedCurrency && !HasSufficientCurrency(talent))
                return false;

            // Check dependencies based on node type
            if (talent.NodeType == TalentNodeType.Normal)
            {
                return CanLearnNormalNode(talentId);
            }
            else if (talent.NodeType == TalentNodeType.Special)
            {
                return CanLearnSpecialNode(talentId);
            }

            return false;
        }

        /// <summary>
        /// Check if normal node can be learned
        /// </summary>
        private bool CanLearnNormalNode(int talentId)
        {
            var talent = TalentDatabase.Instance.GetTalentById(talentId);
            if (talent.RequiredPlayerLevel == 1) return true; // First zone nodes are always available

            // Must have learned previous node in same column
            var previousNode = TalentDatabase.Instance.GetPreviousTalent(talentId);
            if (previousNode == null) return true; // No previous node

            return IsTalentLearned(previousNode.ID);
        }

        /// <summary>
        /// Check if special node can be learned
        /// </summary>
        private bool CanLearnSpecialNode(int talentId)
        {
            // Must have learned previous special node
            var previousSpecial = TalentDatabase.Instance.GetPreviousTalent(talentId);
            if (previousSpecial == null) return true; // First special node

            return IsTalentLearned(previousSpecial.ID);
        }

        /// <summary>
        /// Check if has sufficient currency
        /// </summary>
        private bool HasSufficientCurrency(TalentModel talent)
        {
            if (talent.NodeType == TalentNodeType.Normal)
            {
                // Normal nodes use Gold
                var goldSave = GameController.SaveManager.GetSave<CurrencySave>("GoldCoins");
                return goldSave != null && goldSave.Amount >= talent.Cost;
            }
            else if (talent.NodeType == TalentNodeType.Special)
            {
                // Special nodes use Orc
                var orcSave = GameController.SaveManager.GetSave<CurrencySave>("Orc");
                return orcSave != null && orcSave.Amount >= talent.Cost;
            }

            return false;
        }

        /// <summary>
        /// Learn talent with zone-based logic
        /// </summary>
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

            // Deduct currency
            if (!unlimitedCurrency)
            {
                if (!DeductCurrency(talent))
                {
                    OnTalentError?.Invoke($"Failed to deduct currency for talent {talentId}");
                    return false;
                }
            }

            // Learn talent
            talentSave.SetTalentLevel(talentId, 1);

            // Trigger events
            OnTalentLearned?.Invoke(talent);
            OnCurrencyChanged?.Invoke();
            
            Debug.Log($"[TalentManager] Learned talent: {talent.Name} (Zone {talent.RequiredPlayerLevel})");

            return true;
        }

        /// <summary>
        /// Deduct appropriate currency
        /// </summary>
        private bool DeductCurrency(TalentModel talent)
        {
            if (talent.NodeType == TalentNodeType.Normal)
            {
                var goldSave = GameController.SaveManager.GetSave<CurrencySave>("GoldCoins");
                if (goldSave != null && goldSave.CanAfford(talent.Cost))
                {
                    goldSave.Withdraw(talent.Cost);
                    return true;
                }
            }
            else if (talent.NodeType == TalentNodeType.Special)
            {
                var orcSave = GameController.SaveManager.GetSave<CurrencySave>("Orc");
                if (orcSave != null && orcSave.CanAfford(talent.Cost))
                {
                    orcSave.Withdraw(talent.Cost);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get current player level from game system
        /// </summary>
        private int GetCurrentPlayerLevel()
        {
            // TODO: Get from actual player system
            // For now, return a test value
            return bypassLevelRequirement ? 999 : 10;
        }

        /// <summary>
        /// Get current currencies
        /// </summary>
        public int GetGoldCoins()
        {
            var goldSave = GameController.SaveManager.GetSave<CurrencySave>("GoldCoins");
            return goldSave?.Amount ?? 0;
        }

        public int GetOrc()
        {
            var orcSave = GameController.SaveManager.GetSave<CurrencySave>("Orc");
            return orcSave?.Amount ?? 0;
        }

        /// <summary>
        /// Reset all talents
        /// </summary>
        public void ResetAllTalents()
        {
            if (!IsInitialized) return;

            var allTalents = talentSave.GetAllTalents();
            int totalGoldRefund = 0;
            int totalOrcRefund = 0;

            foreach (var kvp in allTalents)
            {
                var talentId = kvp.Key;
                var level = kvp.Value;

                if (level > 0)
                {
                    var talent = TalentDatabase.Instance.GetTalentById(talentId);
                    if (talent != null)
                    {
                        if (talent.NodeType == TalentNodeType.Normal)
                            totalGoldRefund += talent.Cost;
                        else if (talent.NodeType == TalentNodeType.Special)
                            totalOrcRefund += talent.Cost;
                    }
                }
            }

            // Clear all talents
            talentSave.Clear();

            // Refund currencies
            if (totalGoldRefund > 0)
            {
                var goldSave = GameController.SaveManager.GetSave<CurrencySave>("GoldCoins");
                goldSave?.Deposit(totalGoldRefund);
            }

            if (totalOrcRefund > 0)
            {
                var orcSave = GameController.SaveManager.GetSave<CurrencySave>("Orc");
                orcSave?.Deposit(totalOrcRefund);
            }

            OnCurrencyChanged?.Invoke();
            Debug.Log($"[TalentManager] Reset all talents (Refunded {totalGoldRefund} Gold, {totalOrcRefund} Orc)");
        }

        /// <summary>
        /// Get talent unlock status for UI
        /// </summary>
        public TalentUnlockStatus GetTalentUnlockStatus(int talentId)
        {
            if (!IsInitialized) return TalentUnlockStatus.Locked;

            var talent = TalentDatabase.Instance.GetTalentById(talentId);
            if (talent == null) return TalentUnlockStatus.Locked;

            // Check if already learned
            if (IsTalentLearned(talentId)) return TalentUnlockStatus.Learned;

            // Check player level
            if (!bypassLevelRequirement && GetCurrentPlayerLevel() < talent.RequiredPlayerLevel)
                return TalentUnlockStatus.Locked;

            // Check currency
            if (!unlimitedCurrency && !HasSufficientCurrency(talent))
                return TalentUnlockStatus.InsufficientPoints;

            // Check dependencies
            if (talent.NodeType == TalentNodeType.Normal)
            {
                if (!CanLearnNormalNode(talentId)) return TalentUnlockStatus.Locked;
            }
            else if (talent.NodeType == TalentNodeType.Special)
            {
                if (!CanLearnSpecialNode(talentId)) return TalentUnlockStatus.Locked;
            }

            return TalentUnlockStatus.Available;
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

            return new TalentProgressInfo
            {
                TalentId = talentId,
                CurrentLevel = currentLevel,
                MaxLevel = talent.MaxLevel,
                NextLevelCost = currentLevel < talent.MaxLevel ? talent.Cost : 0,
                UnlockStatus = GetTalentUnlockStatus(talentId),
                CurrentBonus = talent.StatValue * currentLevel,
                NextLevelBonus = talent.StatValue * (currentLevel + 1)
            };
        }

        /// <summary>
        /// Get next available talents (for current progression visualization)
        /// </summary>
        public List<TalentModel> GetNextAvailableTalents()
        {
            var availableTalents = new List<TalentModel>();
            var allTalents = TalentDatabase.Instance.GetAllTalents();

            foreach (var talent in allTalents)
            {
                if (GetTalentUnlockStatus(talent.ID) == TalentUnlockStatus.Available)
                {
                    availableTalents.Add(talent);
                }
            }

            return availableTalents;
        }

        /// <summary>
        /// Get current progression position (for auto-scroll)
        /// </summary>
        public TalentModel GetCurrentProgressionNode()
        {
            var allTalents = TalentDatabase.Instance.GetAllTalents();
            TalentModel lastLearnedNode = null;

            // Find the last learned node (highest position Y)
            foreach (var talent in allTalents)
            {
                if (IsTalentLearned(talent.ID))
                {
                    if (lastLearnedNode == null || talent.PositionY > lastLearnedNode.PositionY)
                        lastLearnedNode = talent;
                }
            }

            return lastLearnedNode;
        }

        /// <summary>
        /// Calculate total stat bonuses from all learned talents
        /// </summary>
        public Dictionary<UpgradeType, float> GetTotalTalentBonuses()
        {
            var bonuses = new Dictionary<UpgradeType, float>();

            if (!IsInitialized) return bonuses;

            var allTalents = talentSave.GetAllTalents();

            foreach (var kvp in allTalents)
            {
                var talentId = kvp.Key;
                var level = kvp.Value;

                if (level <= 0) continue;

                var talent = TalentDatabase.Instance.GetTalentById(talentId);
                if (talent == null) continue;

                var bonus = talent.StatValue * level;

                if (bonuses.ContainsKey(talent.StatType))
                    bonuses[talent.StatType] += bonus;
                else
                    bonuses[talent.StatType] = bonus;
            }

            return bonuses;
        }

        /// <summary>
        /// Get formatted talent tooltip
        /// </summary>
        public string GetTalentTooltip(int talentId)
        {
            var talent = TalentDatabase.Instance.GetTalentById(talentId);
            if (talent == null) return "Unknown Talent";

            var progressInfo = GetTalentProgressInfo(talentId);
            var tooltip = $"<b>{talent.Name}</b>\n";
            tooltip += $"{talent.Description}\n\n";
            
            if (talent.NodeType == TalentNodeType.Normal)
            {
                tooltip += $"Bonus: +{talent.StatValue:F1} {talent.StatType}\n";
                tooltip += $"Cost: {talent.Cost} Gold\n";
            }
            else
            {
                tooltip += $"Special Ability\n";
                tooltip += $"Cost: {talent.Cost} Orc\n";
            }
            
            tooltip += $"Required Level: {talent.RequiredPlayerLevel}\n";
            
            if (progressInfo.UnlockStatus == TalentUnlockStatus.Learned)
            {
                tooltip += $"<color=green>LEARNED</color>";
            }
            else if (progressInfo.UnlockStatus == TalentUnlockStatus.Available)
            {
                tooltip += $"<color=yellow>AVAILABLE</color>";
            }
            else
            {
                tooltip += $"<color=red>LOCKED</color>";
            }

            return tooltip;
        }

        [ContextMenu("Toggle Unlimited Currency")]
        public void ToggleUnlimitedCurrency()
        {
            unlimitedCurrency = !unlimitedCurrency;
            Debug.Log($"[TalentManager] Unlimited currency: {unlimitedCurrency}");
        }

        [ContextMenu("Toggle Bypass Level Requirement")]
        public void ToggleBypassLevel()
        {
            bypassLevelRequirement = !bypassLevelRequirement;
            Debug.Log($"[TalentManager] Bypass level requirement: {bypassLevelRequirement}");
        }
    }

    /// <summary>
    /// Talent unlock status enumeration
    /// </summary>
    public enum TalentUnlockStatus
    {
        Locked,              // Dependencies not met or level too low
        Available,           // Can be learned
        Learned,             // Already learned
        InsufficientPoints,  // Not enough currency
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