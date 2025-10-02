using UnityEngine;
using UnityEditor;
using OctoberStudio;

namespace OctoberStudio.Editor
{
    public static class CharacterTestHelper
    {
        [MenuItem("October/Character/Add All Characters")]
        public static void AddAllCharacters()
        {
            Debug.Log("[CharacterTestHelper] ⭐ Add All Characters called!");

            // Check if game is running first
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[CharacterTestHelper] ⚠️ Game is not running. Please start the game first and try again.");
                EditorUtility.DisplayDialog("Character Test Helper",
                    "Game is not running!\n\nPlease start the game in Play Mode and try again.", "OK");
                return;
            }

            var charactersSave = GameController.SaveManager?.GetSave<CharactersSave>("Characters");
            var charactersDatabase = FindCharactersDatabase();

            if (charactersSave == null)
            {
                Debug.LogError("[CharacterTestHelper] ❌ CharactersSave not found. SaveManager might not be initialized.");
                EditorUtility.DisplayDialog("Character Test Helper",
                    "CharactersSave not found!\n\nMake sure SaveManager is properly initialized.", "OK");
                return;
            }

            if (charactersDatabase == null)
            {
                Debug.LogError("[CharacterTestHelper] ❌ CharactersDatabase not found in project.");
                EditorUtility.DisplayDialog("Character Test Helper",
                    "CharactersDatabase not found!\n\nMake sure CharactersDatabase asset exists in the project.", "OK");
                return;
            }

            Debug.Log($"[CharacterTestHelper] 📋 Found database with {charactersDatabase.CharactersCount} characters");

            int addedCount = 0;
            for (int i = 0; i < charactersDatabase.CharactersCount; i++)
            {
                if (!charactersSave.HasCharacterBeenBought(i))
                {
                    charactersSave.AddBoughtCharacter(i);
                    addedCount++;
                    Debug.Log($"[CharacterTestHelper] ✅ Added character {i}: {charactersDatabase.GetCharacterData(i).Name}");
                }
                else
                {
                    Debug.Log($"[CharacterTestHelper] ⚪ Character {i} already owned: {charactersDatabase.GetCharacterData(i).Name}");
                }
            }

            // Force save after adding characters
            GameController.SaveManager.Save();

            Debug.Log($"[CharacterTestHelper] 🎉 Added {addedCount} characters. Total owned: {GetOwnedCharacterCount()}");
            EditorUtility.DisplayDialog("Character Test Helper",
                $"Successfully added {addedCount} characters!\n\nTotal owned: {GetOwnedCharacterCount()}", "OK");
        }

        [MenuItem("October/Character/Remove All Characters")]
        public static void RemoveAllCharacters()
        {
            Debug.Log("[CharacterTestHelper] 🗑️ Remove All Characters called!");

            // Check if game is running first
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[CharacterTestHelper] ⚠️ Game is not running. Please start the game first and try again.");
                EditorUtility.DisplayDialog("Character Test Helper",
                    "Game is not running!\n\nPlease start the game in Play Mode and try again.", "OK");
                return;
            }

            var charactersSave = GameController.SaveManager?.GetSave<CharactersSave>("Characters");

            if (charactersSave == null)
            {
                Debug.LogError("[CharacterTestHelper] ❌ CharactersSave not found. SaveManager might not be initialized.");
                EditorUtility.DisplayDialog("Character Test Helper",
                    "CharactersSave not found!\n\nMake sure SaveManager is properly initialized.", "OK");
                return;
            }

            int ownedBefore = GetOwnedCharacterCount();
            Debug.Log($"[CharacterTestHelper] 📊 Characters owned before clear: {ownedBefore}");

            charactersSave.ClearBoughtCharacters();

            // Force save after clearing characters
            GameController.SaveManager.Save();

            int ownedAfter = GetOwnedCharacterCount();
            Debug.Log($"[CharacterTestHelper] 🎯 Cleared all characters. Owned after: {ownedAfter} (should keep character 0)");
            EditorUtility.DisplayDialog("Character Test Helper",
                $"Characters cleared!\n\nOwned before: {ownedBefore}\nOwned after: {ownedAfter} (character 0 kept)", "OK");
        }

        [MenuItem("October/Character/Add Random Character")]
        public static void AddRandomCharacter()
        {
            Debug.Log("[CharacterTestHelper] 🎲 Add Random Character called!");

            var charactersSave = GameController.SaveManager?.GetSave<CharactersSave>("Characters");
            var charactersDatabase = FindCharactersDatabase();

            if (charactersSave == null)
            {
                Debug.LogError("[CharacterTestHelper] ❌ CharactersSave not found. Make sure the game is running.");
                return;
            }

            if (charactersDatabase == null)
            {
                Debug.LogError("[CharacterTestHelper] ❌ CharactersDatabase not found in project.");
                return;
            }

            // Find unowned characters
            var unownedCharacters = new System.Collections.Generic.List<int>();
            for (int i = 0; i < charactersDatabase.CharactersCount; i++)
            {
                if (!charactersSave.HasCharacterBeenBought(i))
                {
                    unownedCharacters.Add(i);
                }
            }

            Debug.Log($"[CharacterTestHelper] 📊 Found {unownedCharacters.Count} unowned characters");

            if (unownedCharacters.Count == 0)
            {
                Debug.Log("[CharacterTestHelper] ⚪ All characters are already owned.");
                return;
            }

            int randomIndex = Random.Range(0, unownedCharacters.Count);
            int characterId = unownedCharacters[randomIndex];

            charactersSave.AddBoughtCharacter(characterId);

            // Force save after adding character
            GameController.SaveManager.Save();

            Debug.Log($"[CharacterTestHelper] ✅ Added random character {characterId}: {charactersDatabase.GetCharacterData(characterId).Name}");
            Debug.Log($"[CharacterTestHelper] 📊 Total owned: {GetOwnedCharacterCount()}");
        }

        [MenuItem("October/Character/Select Random Owned Character")]
        public static void SelectRandomOwnedCharacter()
        {
            var charactersSave = GameController.SaveManager?.GetSave<CharactersSave>("Characters");
            var charactersDatabase = FindCharactersDatabase();

            if (charactersSave == null)
            {
                Debug.LogError("[CharacterTestHelper] CharactersSave not found. Make sure the game is running.");
                return;
            }

            if (charactersDatabase == null)
            {
                Debug.LogError("[CharacterTestHelper] CharactersDatabase not found in project.");
                return;
            }

            // Find owned characters
            var ownedCharacters = new System.Collections.Generic.List<int>();
            for (int i = 0; i < charactersDatabase.CharactersCount; i++)
            {
                if (charactersSave.HasCharacterBeenBought(i))
                {
                    ownedCharacters.Add(i);
                }
            }

            if (ownedCharacters.Count == 0)
            {
                Debug.Log("[CharacterTestHelper] No characters are owned yet.");
                return;
            }

            int randomIndex = Random.Range(0, ownedCharacters.Count);
            int characterId = ownedCharacters[randomIndex];

            charactersSave.SetSelectedCharacterId(characterId);
            Debug.Log($"[CharacterTestHelper] Selected character {characterId}: {charactersDatabase.GetCharacterData(characterId).Name}");
        }

        [MenuItem("October/Character/Log Character Status")]
        public static void LogCharacterStatus()
        {
            var charactersSave = GameController.SaveManager?.GetSave<CharactersSave>("Characters");
            var charactersDatabase = FindCharactersDatabase();

            if (charactersSave == null)
            {
                Debug.LogError("[CharacterTestHelper] CharactersSave not found. Make sure the game is running.");
                return;
            }

            if (charactersDatabase == null)
            {
                Debug.LogError("[CharacterTestHelper] CharactersDatabase not found in project.");
                return;
            }

            Debug.Log("=== CHARACTER STATUS ===");
            Debug.Log($"Selected Character ID: {charactersSave.SelectedCharacterId}");
            Debug.Log($"Total Characters: {charactersDatabase.CharactersCount}");
            Debug.Log($"Owned Characters: {GetOwnedCharacterCount()}");

            Debug.Log("--- Character Details ---");
            for (int i = 0; i < charactersDatabase.CharactersCount; i++)
            {
                var characterData = charactersDatabase.GetCharacterData(i);
                bool isOwned = charactersSave.HasCharacterBeenBought(i);
                bool isSelected = charactersSave.SelectedCharacterId == i;

                string status = isOwned ? "OWNED" : "LOCKED";
                if (isSelected) status += " (SELECTED)";

                Debug.Log($"{i}: {characterData.Name} - {status}");
            }
        }

        [MenuItem("October/Character/Add 1000 Gold")]
        public static void Add1000Gold()
        {
            // Check if game is running first
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[CharacterTestHelper] ⚠️ Game is not running. Please start the game first and try again.");
                EditorUtility.DisplayDialog("Character Test Helper",
                    "Game is not running!\n\nPlease start the game in Play Mode and try again.", "OK");
                return;
            }

            var goldCurrency = GameController.SaveManager?.GetSave<CurrencySave>("gold");

            if (goldCurrency == null)
            {
                Debug.LogError("[CharacterTestHelper] Gold currency save not found. SaveManager might not be initialized.");
                EditorUtility.DisplayDialog("Character Test Helper",
                    "Gold currency save not found!\n\nMake sure SaveManager is properly initialized.", "OK");
                return;
            }

            goldCurrency.Deposit(1000);
            Debug.Log($"[CharacterTestHelper] Added 1000 gold. Current gold: {goldCurrency.Amount}");
            EditorUtility.DisplayDialog("Character Test Helper",
                $"Added 1000 gold!\n\nCurrent gold: {goldCurrency.Amount}", "OK");
        }

        [MenuItem("October/Character/Check Status (Editor Safe)")]
        public static void CheckStatusEditorSafe()
        {
            Debug.Log("[CharacterTestHelper] 🔍 Check Status (Editor Safe) called!");

            var charactersDatabase = FindCharactersDatabase();

            if (charactersDatabase == null)
            {
                Debug.LogError("[CharacterTestHelper] ❌ CharactersDatabase not found in project.");
                EditorUtility.DisplayDialog("Character Test Helper",
                    "CharactersDatabase not found!\n\nMake sure CharactersDatabase asset exists in the project.", "OK");
                return;
            }

            Debug.Log($"[CharacterTestHelper] 📋 Found CharactersDatabase with {charactersDatabase.CharactersCount} characters");

            string message = $"=== CHARACTER DATABASE INFO ===\n";
            message += $"Total Characters: {charactersDatabase.CharactersCount}\n\n";
            message += "Characters in database:\n";

            for (int i = 0; i < charactersDatabase.CharactersCount; i++)
            {
                var characterData = charactersDatabase.GetCharacterData(i);
                message += $"{i}: {characterData.Name}\n";
                Debug.Log($"[CharacterTestHelper] Character {i}: {characterData.Name}");
            }

            if (Application.isPlaying && GameController.SaveManager != null)
            {
                var charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
                if (charactersSave != null)
                {
                    message += $"\n=== SAVE DATA ===\n";
                    message += $"Selected Character: {charactersSave.SelectedCharacterId}\n";
                    message += $"Owned Characters: {GetOwnedCharacterCount()}\n";
                }
                else
                {
                    message += "\n⚠️ Save data not available";
                }
            }
            else
            {
                message += "\n⚠️ Game not running - save data not available";
            }

            EditorUtility.DisplayDialog("Character Database Status", message, "OK");
        }

        // Validation methods for menu items - ALWAYS return true to allow menu access
        [MenuItem("October/Character/Add All Characters", true)]
        [MenuItem("October/Character/Remove All Characters", true)]
        [MenuItem("October/Character/Add Random Character", true)]
        [MenuItem("October/Character/Select Random Owned Character", true)]
        [MenuItem("October/Character/Log Character Status", true)]
        [MenuItem("October/Character/Add 1000 Gold", true)]
        [MenuItem("October/Character/Check Status (Editor Safe)", true)]
        public static bool ValidateCharacterActions()
        {
            // ALWAYS return true so menu items are always clickable
            // Individual methods will handle their own validation with proper error messages
            return true;
        }

        private static CharactersDatabase FindCharactersDatabase()
        {
            var guids = AssetDatabase.FindAssets("t:CharactersDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<CharactersDatabase>(path);
            }
            return null;
        }

        private static int GetOwnedCharacterCount()
        {
            var charactersSave = GameController.SaveManager?.GetSave<CharactersSave>("Characters");
            var charactersDatabase = FindCharactersDatabase();

            if (charactersSave == null || charactersDatabase == null) return 0;

            int count = 0;
            for (int i = 0; i < charactersDatabase.CharactersCount; i++)
            {
                if (charactersSave.HasCharacterBeenBought(i))
                {
                    count++;
                }
            }
            return count;
        }
    }
}