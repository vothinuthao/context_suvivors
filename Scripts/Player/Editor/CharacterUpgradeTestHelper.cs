using UnityEngine;
using UnityEditor;

namespace OctoberStudio.Editor
{
    public static class CharacterUpgradeTestHelper
    {
        [MenuItem("October Studio/Character Upgrade/Add Gold")]
        public static void AddGold()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("This can only be used in play mode!");
                return;
            }

            var goldSave = GameController.SaveManager.GetSave<CurrencySave>("gold");
            goldSave.Deposit(1000);
            Debug.Log("Added 1000 gold for testing character upgrades");
        }

        [MenuItem("October Studio/Character Upgrade/Reset Character Stars")]
        public static void ResetCharacterStars()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("This can only be used in play mode!");
                return;
            }

            var charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");

            // Clear all star levels (you could implement a Reset method in CharactersSave)
            Debug.Log("Character star levels reset (implement reset functionality if needed)");
        }

        [MenuItem("October Studio/Character Upgrade/Print Character Stats")]
        public static void PrintCharacterStats()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("This can only be used in play mode!");
                return;
            }

            var upgradeManager = Object.FindObjectOfType<CharacterUpgradeManager>();
            var charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");

            if (upgradeManager == null)
            {
                Debug.LogError("CharacterUpgradeManager not found in scene!");
                return;
            }

            int selectedId = charactersSave.SelectedCharacterId;
            int starLevel = upgradeManager.GetCharacterStarLevel(selectedId);
            float hp = upgradeManager.GetCharacterHP(selectedId);
            float damage = upgradeManager.GetCharacterDamage(selectedId);
            int upgradeCost = upgradeManager.GetUpgradeCost(selectedId);
            bool canUpgrade = upgradeManager.CanUpgradeCharacter(selectedId);

            Debug.Log($"Character {selectedId}: Star Level {starLevel}, HP {hp}, Damage {damage}, Upgrade Cost {upgradeCost}, Can Upgrade: {canUpgrade}");
        }
    }
}