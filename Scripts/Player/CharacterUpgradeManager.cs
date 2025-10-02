using System;
using UnityEngine;
using UnityEngine.Events;
using OctoberStudio.Currency;

namespace OctoberStudio
{
    public class CharacterUpgradeManager : MonoBehaviour
    {
        [SerializeField] private CharactersDatabase charactersDatabase;

        private CharactersSave charactersSave;
        private CurrencySave goldCurrency;

        public UnityAction<int> onCharacterUpgraded;

        private void Awake()
        {
            charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
            goldCurrency = GameController.SaveManager.GetSave<CurrencySave>("gold");
        }

        private void Start()
        {
            if (charactersSave != null)
            {
                charactersSave.onCharacterUpgraded += OnCharacterDataChanged;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from character upgrade events
            if (charactersSave != null)
            {
                charactersSave.onCharacterUpgraded -= OnCharacterDataChanged;
            }
        }

        private void OnCharacterDataChanged(int characterId)
        {
            // When character data changes (including pieces), check for potential auto star upgrades
            CheckAndAutoUpgradeCharacterStar(characterId);
        }

        /// <summary>
        /// Check and automatically upgrade character star if enough pieces available
        /// </summary>
        public void CheckAndAutoUpgradeCharacterStar(int characterId)
        {
            if (charactersDatabase == null || characterId < 0 || characterId >= charactersDatabase.CharactersCount)
                return;

            var characterData = charactersDatabase.GetCharacterData(characterId);
            if (characterData?.UpgradeConfig == null) return;

            int currentStarLevel = charactersSave.GetCharacterStarLevel(characterId);
            int maxStarLevel = 12;

            // Keep upgrading until we can't anymore
            while (currentStarLevel < maxStarLevel && CanUpgradeCharacterStar(characterId))
            {
                if (TryUpgradeCharacterStar(characterId))
                {
                    currentStarLevel = charactersSave.GetCharacterStarLevel(characterId);
                    Debug.Log($"[CharacterUpgradeManager] Auto upgraded character {characterId} to star level {currentStarLevel}");
                }
                else
                {
                    break;
                }
            }
        }

        // Check if character star can be upgraded (uses character-specific pieces with sub-star system)
        public bool CanUpgradeCharacterStar(int characterId)
        {
            if (charactersDatabase == null || characterId < 0 || characterId >= charactersDatabase.CharactersCount)
                return false;

            var characterData = charactersDatabase.GetCharacterData(characterId);
            if (characterData.UpgradeConfig == null) return false;

            int currentStarLevel = charactersSave.GetCharacterStarLevel(characterId);
            int currentSubStarProgress = charactersSave.GetCharacterSubStarProgress(characterId);

            // Check if character can be upgraded (not at max stars)
            if (currentStarLevel >= characterData.UpgradeConfig.MaxStars)
                return false;

            // Get current star tier config
            var currentTierConfig = characterData.UpgradeConfig.GetStarTierConfig(currentStarLevel);
            if (currentTierConfig == null) return false;

            // Check if we can upgrade within current tier or move to next tier
            if (currentSubStarProgress < currentTierConfig.subStarCount)
            {
                // Can upgrade sub-star within current tier
                int piecesRequired = characterData.UpgradeConfig.GetPiecesRequiredForSubStar(currentStarLevel, currentSubStarProgress);
                return charactersSave.CanAffordCharacterPieces(characterId, piecesRequired);
            }
            else
            {
                // Current tier is complete, check if we can move to next tier
                if (currentStarLevel + 1 <= characterData.UpgradeConfig.MaxStars)
                {
                    var nextTierConfig = characterData.UpgradeConfig.GetStarTierConfig(currentStarLevel + 1);
                    if (nextTierConfig != null)
                    {
                        int piecesRequired = characterData.UpgradeConfig.GetPiecesRequiredForSubStar(currentStarLevel + 1, 0);
                        return charactersSave.CanAffordCharacterPieces(characterId, piecesRequired);
                    }
                }
            }

            return false;
        }

        // Check if character level can be upgraded (uses gold)
        public bool CanUpgradeCharacterLevel(int characterId)
        {
            if (charactersDatabase == null || characterId < 0 || characterId >= charactersDatabase.CharactersCount)
                return false;

            var characterData = charactersDatabase.GetCharacterData(characterId);
            if (characterData.UpgradeConfig == null) return false;

            int currentStarLevel = charactersSave.GetCharacterStarLevel(characterId);
            int currentLevel = charactersSave.GetCharacterLevel(characterId);
            int maxLevelForStar = characterData.UpgradeConfig.GetMaxLevelForStar(currentStarLevel);

            // Check if character level can be upgraded (not at max level for current star)
            if (!charactersSave.CanUpgradeCharacterLevel(characterId, maxLevelForStar))
                return false;

            // Check if player has enough gold
            int goldCost = characterData.UpgradeConfig.GetGoldCostPerLevel(currentStarLevel);
            return goldCurrency.CanAfford(goldCost);
        }

        // Legacy method for backward compatibility - defaults to star upgrade
        public bool CanUpgradeCharacter(int characterId)
        {
            return CanUpgradeCharacterStar(characterId);
        }

        // Try to upgrade character star (uses character-specific pieces with sub-star system)
        public bool TryUpgradeCharacterStar(int characterId)
        {
            if (!CanUpgradeCharacterStar(characterId))
                return false;

            var characterData = charactersDatabase.GetCharacterData(characterId);
            int currentStarLevel = charactersSave.GetCharacterStarLevel(characterId);
            int currentSubStarProgress = charactersSave.GetCharacterSubStarProgress(characterId);

            // Get current star tier config
            var currentTierConfig = characterData.UpgradeConfig.GetStarTierConfig(currentStarLevel);
            if (currentTierConfig == null) return false;

            int piecesRequired;
            bool isUpgradingToNextTier = false;

            // Determine what we're upgrading
            if (currentSubStarProgress < currentTierConfig.subStarCount)
            {
                // Upgrading sub-star within current tier
                piecesRequired = characterData.UpgradeConfig.GetPiecesRequiredForSubStar(currentStarLevel, currentSubStarProgress);
            }
            else
            {
                // Moving to next tier
                piecesRequired = characterData.UpgradeConfig.GetPiecesRequiredForSubStar(currentStarLevel + 1, 0);
                isUpgradingToNextTier = true;
            }

            // Deduct character-specific pieces and upgrade
            if (charactersSave.TrySpendCharacterPieces(characterId, piecesRequired))
            {
                if (isUpgradingToNextTier)
                {
                    // Move to next star tier and reset sub-star progress
                    charactersSave.UpgradeCharacterStar(characterId);
                    charactersSave.SetCharacterSubStarProgress(characterId, 1); // First sub-star of new tier
                }
                else
                {
                    // Increment sub-star progress within current tier
                    charactersSave.IncrementCharacterSubStarProgress(characterId);
                }

                onCharacterUpgraded?.Invoke(characterId);
                return true;
            }

            return false;
        }

        // Try to upgrade character level (uses gold)
        public bool TryUpgradeCharacterLevel(int characterId)
        {
            if (!CanUpgradeCharacterLevel(characterId))
                return false;

            var characterData = charactersDatabase.GetCharacterData(characterId);
            int currentStarLevel = charactersSave.GetCharacterStarLevel(characterId);
            int goldCost = characterData.UpgradeConfig.GetGoldCostPerLevel(currentStarLevel);

            // Deduct gold and upgrade character level
            if (goldCurrency.TryWithdraw(goldCost))
            {
                charactersSave.UpgradeCharacterLevel(characterId);
                onCharacterUpgraded?.Invoke(characterId);
                return true;
            }

            return false;
        }

        // Legacy method for backward compatibility - defaults to star upgrade
        public bool TryUpgradeCharacter(int characterId)
        {
            return TryUpgradeCharacterStar(characterId);
        }

        // Get character-specific pieces required for next star upgrade (sub-star system)
        public int GetStarUpgradeCost(int characterId)
        {
            if (charactersDatabase == null || characterId < 0 || characterId >= charactersDatabase.CharactersCount)
                return 0;

            var characterData = charactersDatabase.GetCharacterData(characterId);
            if (characterData.UpgradeConfig == null) return 0;

            int currentStarLevel = charactersSave.GetCharacterStarLevel(characterId);
            int currentSubStarProgress = charactersSave.GetCharacterSubStarProgress(characterId);

            // Get current star tier config
            var currentTierConfig = characterData.UpgradeConfig.GetStarTierConfig(currentStarLevel);
            if (currentTierConfig == null) return 0;

            // Determine cost for next upgrade
            if (currentSubStarProgress < currentTierConfig.subStarCount)
            {
                // Cost for next sub-star within current tier
                return characterData.UpgradeConfig.GetPiecesRequiredForSubStar(currentStarLevel, currentSubStarProgress);
            }
            else if (currentStarLevel + 1 <= characterData.UpgradeConfig.MaxStars)
            {
                // Cost for first sub-star of next tier
                return characterData.UpgradeConfig.GetPiecesRequiredForSubStar(currentStarLevel + 1, 0);
            }

            return 0; // Max level reached
        }

        // Get current character-specific pieces
        public int GetCharacterPieces(int characterId)
        {
            return charactersSave.GetCharacterPieces(characterId);
        }

        // Add character-specific pieces
        public void AddCharacterPieces(int characterId, int amount)
        {
            charactersSave.AddCharacterPieces(characterId, amount);
        }

        // Get gold required for level upgrade
        public int GetLevelUpgradeCost(int characterId)
        {
            if (charactersDatabase == null || characterId < 0 || characterId >= charactersDatabase.CharactersCount)
                return 0;

            var characterData = charactersDatabase.GetCharacterData(characterId);
            if (characterData.UpgradeConfig == null) return 0;

            int currentStarLevel = charactersSave.GetCharacterStarLevel(characterId);
            return characterData.UpgradeConfig.GetGoldCostPerLevel(currentStarLevel);
        }

        // Legacy method for backward compatibility - returns star upgrade cost
        public int GetUpgradeCost(int characterId)
        {
            return GetStarUpgradeCost(characterId);
        }

        public int GetCharacterStarLevel(int characterId)
        {
            return charactersSave.GetCharacterStarLevel(characterId);
        }

        public int GetCharacterLevel(int characterId)
        {
            return charactersSave.GetCharacterLevel(characterId);
        }

        public int GetMaxLevelForCurrentStar(int characterId)
        {
            if (charactersDatabase == null || characterId < 0 || characterId >= charactersDatabase.CharactersCount)
                return 0;

            var characterData = charactersDatabase.GetCharacterData(characterId);
            if (characterData.UpgradeConfig == null) return 0;

            int currentStarLevel = charactersSave.GetCharacterStarLevel(characterId);
            return characterData.UpgradeConfig.GetMaxLevelForStar(currentStarLevel);
        }

        /// <summary>
        /// Manually trigger auto star upgrade check for a specific character
        /// (useful when opening character tab or when pieces are added)
        /// </summary>
        public void TriggerAutoStarUpgradeCheck(int characterId)
        {
            CheckAndAutoUpgradeCharacterStar(characterId);
        }

        /// <summary>
        /// Manually trigger auto star upgrade check for all owned characters
        /// </summary>
        public void TriggerAutoStarUpgradeCheckForAllCharacters()
        {
            if (charactersSave != null && charactersDatabase != null)
            {
                for (int i = 0; i < charactersDatabase.CharactersCount; i++)
                {
                    if (charactersSave.HasCharacterBeenBought(i))
                    {
                        CheckAndAutoUpgradeCharacterStar(i);
                    }
                }
            }
        }

        public float GetCharacterHP(int characterId)
        {
            if (charactersDatabase == null || characterId < 0 || characterId >= charactersDatabase.CharactersCount)
                return 0;

            var characterData = charactersDatabase.GetCharacterData(characterId);
            int starLevel = charactersSave.GetCharacterStarLevel(characterId);
            return characterData.GetHPAtStarLevel(starLevel);
        }

        public float GetCharacterDamage(int characterId)
        {
            if (charactersDatabase == null || characterId < 0 || characterId >= charactersDatabase.CharactersCount)
                return 0;

            var characterData = charactersDatabase.GetCharacterData(characterId);
            int starLevel = charactersSave.GetCharacterStarLevel(characterId);
            return characterData.GetDamageAtStarLevel(starLevel);
        }
    }
}