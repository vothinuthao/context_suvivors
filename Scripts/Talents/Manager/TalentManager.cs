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
        /// Check if normal node can be learned - FIXED: Now checks same stat type in previous level
        /// </summary>
        private bool CanLearnNormalNode(int talentId)
        {
            var talent = TalentDatabase.Instance.GetTalentById(talentId);
            if (talent.RequiredPlayerLevel == 1) return true; // Level 1 nodes are always available

            // Find node with same stat type in previous level
            var previousLevelNodes = TalentDatabase.Instance.GetNormalTalentsInZone(talent.RequiredPlayerLevel - 1);
            var previousSameStatNode = previousLevelNodes.FirstOrDefault(n => GetStatType(n) == GetStatType(talent));

            return previousSameStatNode != null && IsTalentLearned(previousSameStatNode.ID);
        }

        /// <summary>
        /// Get stat type from talent for dependency checking
        /// </summary>
        private string GetStatType(TalentModel talent)
        {
            if (talent.Name.Contains("ATK") || talent.StatType == UpgradeType.Damage)
                return "ATK";
            if (talent.Name.Contains("DEF") || talent.Name.Contains("Defense") || talent.Name.Contains("Armor"))
                return "DEF";
            if (talent.Name.Contains("SPEED") || talent.StatType == UpgradeType.Speed)
                return "SPEED";
            if (talent.Name.Contains("HEAL") || talent.Name.Contains("Health"))
                return "HEAL";

            return "UNKNOWN";
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
                var goldSave = GameController.SaveManager.GetSave<CurrencySave>("gold");
                return goldSave != null && goldSave.Amount >= talent.Cost;
            }
            else if (talent.NodeType == TalentNodeType.Special)
            {
                // Special nodes use Orc
                var orcSave = GameController.SaveManager.GetSave<CurrencySave>("orc");
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
                var goldSave = GameController.SaveManager.GetSave<CurrencySave>("gold");
                if (goldSave != null && goldSave.CanAfford(talent.Cost))
                {
                    goldSave.Withdraw(talent.Cost);
                    return true;
                }
            }
            else if (talent.NodeType == TalentNodeType.Special)
            {
                var orcSave = GameController.SaveManager.GetSave<CurrencySave>("orc");
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
            if (bypassLevelRequirement) return 999;

            // Get from UserProfileManager (actual player system)
            if (OctoberStudio.User.UserProfileManager.Instance != null &&
                OctoberStudio.User.UserProfileManager.Instance.IsDataReady)
            {
                return OctoberStudio.User.UserProfileManager.Instance.ProfileSave.UserLevel;
            }

            // Fallback to level 1 if user profile not ready
            return 1;
        }

        /// <summary>
        /// Get current currencies
        /// </summary>
        public int GetGoldCoins()
        {
            var goldSave = GameController.SaveManager.GetSave<CurrencySave>("gold");
            return goldSave?.Amount ?? 0;
        }

        public int GetOrc()
        {
            var orcSave = GameController.SaveManager.GetSave<CurrencySave>("orc");
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
                var goldSave = GameController.SaveManager.GetSave<CurrencySave>("gold");
                goldSave?.Deposit(totalGoldRefund);
            }

            if (totalOrcRefund > 0)
            {
                var orcSave = GameController.SaveManager.GetSave<CurrencySave>("orc");
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

        [ContextMenu("Debug Player Level Info")]
        public void DebugPlayerLevelInfo()
        {
            int currentLevel = GetCurrentPlayerLevel();
            Debug.Log($"[TalentManager] Current player level: {currentLevel}");
            Debug.Log($"[TalentManager] Bypass level requirement: {bypassLevelRequirement}");

            if (OctoberStudio.User.UserProfileManager.Instance != null)
            {
                Debug.Log($"[TalentManager] UserProfileManager exists: {OctoberStudio.User.UserProfileManager.Instance.IsDataReady}");
                if (OctoberStudio.User.UserProfileManager.Instance.IsDataReady)
                {
                    Debug.Log($"[TalentManager] UserProfileManager level: {OctoberStudio.User.UserProfileManager.Instance.ProfileSave.UserLevel}");
                }
            }
            else
            {
                Debug.Log("[TalentManager] UserProfileManager is null");
            }
        }

        [ContextMenu("Debug Currency Info")]
        public void DebugCurrencyInfo()
        {
            Debug.Log("=== TALENT CURRENCY DEBUG ===");

            // Test Gold currency access
            var goldSave = GameController.SaveManager.GetSave<CurrencySave>("gold");
            var goldAmount = GetGoldCoins();
            Debug.Log($"Gold - Save exists: {goldSave != null}, Amount via TalentManager: {goldAmount}, Amount via Save: {goldSave?.Amount ?? -1}");

            // Test Orc currency access
            var orcSave = GameController.SaveManager.GetSave<CurrencySave>("orc");
            var orcAmount = GetOrc();
            Debug.Log($"Orc - Save exists: {orcSave != null}, Amount via TalentManager: {orcAmount}, Amount via Save: {orcSave?.Amount ?? -1}");

            // Test CurrenciesManager integration
            if (OctoberStudio.Currency.CurrenciesManager.Instance != null)
            {
                var cmGold = OctoberStudio.Currency.CurrenciesManager.Instance.GoldAmount;
                var cmOrc = OctoberStudio.Currency.CurrenciesManager.Instance.OrcAmount;
                Debug.Log($"CurrenciesManager - Gold: {cmGold}, Orc: {cmOrc}");
            }
            else
            {
                Debug.Log("CurrenciesManager.Instance is null");
            }
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