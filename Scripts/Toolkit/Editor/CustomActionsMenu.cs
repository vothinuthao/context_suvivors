using UnityEngine;
using UnityEditor;
using OctoberStudio.Abilities;
using OctoberStudio.User;
using User.Manager;

namespace OctoberStudio.Save
{
    public static class SaveActionsMenu
    {
        [MenuItem("Tools/October/Delete Save File", priority = 3)]
        private static void DeleteSaveFile()
        {
            PlayerPrefs.DeleteAll();
            SaveManager.DeleteSaveFile();
        }

        [MenuItem("Tools/October/Delete Save File", true)]
        private static bool DeleteSaveFileValidation()
        {
            return !Application.isPlaying;
        }

        [MenuItem("Tools/October/Open All Stages", priority = 2)]
        private static void OpenAllStages()
        {
            var stageSave = GameController.SaveManager.GetSave<StageSave>("Stage");

            string[] guiID = AssetDatabase.FindAssets("t:StagesDatabase");

            if (guiID != null)
            {
                var database = AssetDatabase.LoadAssetAtPath<StagesDatabase>(AssetDatabase.GUIDToAssetPath(guiID[0]));

                if(database != null)
                {
                    stageSave.SetMaxReachedStageId(database.StagesCount - 1);

                    EditorApplication.isPlaying = false;
                }
            }
        }

        [MenuItem("Tools/October/Open All Stages", true)]
        private static bool OpenAllStagesValidation()
        {
            return Application.isPlaying;
        }

        [MenuItem("Tools/October/Get 1K Gold", priority = 1)]
        private static void GetGold()
        {
            var gold = GameController.SaveManager.GetSave<CurrencySave>("gold");

            gold.Deposit(1000);
        }
        [MenuItem("Tools/October/Get 2 Orc", priority = 1)]
        private static void GetOrc()
        {
            var orc = GameController.SaveManager.GetSave<CurrencySave>("orc");

            orc.Deposit(2);
        }
        [MenuItem("Tools/October/Get 1K Gem", priority = 1)]
        private static void GetGem()
        {
            var gem = GameController.SaveManager.GetSave<CurrencySave>("gem");

            gem.Deposit(1000);
        }

        [MenuItem("Tools/October/Get 1K Gold", true)]
        private static bool GetGoldValidation()
        {
            return Application.isPlaying;
        }
        
        [MenuItem("Tools/October/User Profile/Add 100 XP", priority = 20)]
        private static void AddUserXP()
        {
            if (UserProfileManager.Instance != null)
            {
                UserProfileManager.Instance.DebugAddXP(100);
            }
        }

        [MenuItem("Tools/October/User Profile/Add 100 XP", true)]
        private static bool AddUserXPValidation()
        {
            return Application.isPlaying && UserProfileManager.Instance != null;
        }

        [MenuItem("Tools/October/User Profile/Complete Stage (5min)", priority = 21)]
        private static void CompleteStage()
        {
            if (UserProfileManager.Instance != null)
            {
                UserProfileManager.Instance.DebugCompleteStage(5f, 1);
            }
        }

        [MenuItem("Tools/October/User Profile/Complete Stage (5min)", true)]
        private static bool CompleteStageValidation()
        {
            return Application.isPlaying && UserProfileManager.Instance != null;
        }

        [MenuItem("Tools/October/User Profile/Set Level 10", priority = 22)]
        private static void SetUserLevel10()
        {
            if (UserProfileManager.Instance != null)
            {
                UserProfileManager.Instance.DebugSetLevel(10);
            }
        }

        [MenuItem("Tools/October/User Profile/Set Level 10", true)]
        private static bool SetUserLevel10Validation()
        {
            return Application.isPlaying && UserProfileManager.Instance != null;
        }
        [MenuItem("Tools/October/Level Database/Load Level Data", priority = 40)]
        private static void LoadLevelDatabase()
        {
            if (UserLevelDatabase.Instance != null)
            {
                UserLevelDatabase.Instance.LoadLevelData();
            }
        }

        [MenuItem("Tools/October/Level Database/Load Level Data", true)]
        private static bool LoadLevelDatabaseValidation()
        {
            return Application.isPlaying && UserLevelDatabase.Instance != null;
        }

        [MenuItem("Tools/October/Level Database/Show Level Progression", priority = 41)]
        private static void ShowLevelProgression()
        {
            if (UserLevelDatabase.Instance != null)
            {
                UserLevelDatabase.Instance.LogLevelProgression();
            }
        }

        [MenuItem("Tools/October/Level Database/Show Level Progression", true)]
        private static bool ShowLevelProgressionValidation()
        {
            return Application.isPlaying && UserLevelDatabase.Instance != null;
        }

        [MenuItem("Tools/October/Level Database/Simulate Progression", priority = 42)]
        private static void SimulateProgression()
        {
            if (UserLevelDatabase.Instance != null)
            {
                UserLevelDatabase.Instance.SimulateLevelProgression();
            }
        }

        [MenuItem("Tools/October/Level Database/Simulate Progression", true)]
        private static bool SimulateProgressionValidation()
        {
            return Application.isPlaying && UserLevelDatabase.Instance != null;
        }

        [MenuItem("Tools/October/User Profile/Show Profile Info", priority = 23)]
        private static void ShowProfileInfo()
        {
            if (UserProfileManager.Instance?.ProfileSave != null)
            {
                var save = UserProfileManager.Instance.ProfileSave;
                Debug.Log($"=== USER PROFILE ===");
                Debug.Log($"Level: {save.UserLevel}");
                Debug.Log($"Total XP: {save.TotalXP:N0}");
                Debug.Log($"XP to next level: {save.GetXPRequiredForNextLevel():N0}");
                Debug.Log($"Level progress: {save.GetLevelProgress():P}");
                Debug.Log($"Games played: {save.TotalGamesPlayed}");
                Debug.Log($"Best survival time: {save.BestSurvivalTime:F1} minutes");
                Debug.Log($"Stages completed: {save.TotalStagesCompleted}");
            }
        }

        [MenuItem("Tools/October/User Profile/Show Profile Info", true)]
        private static bool ShowProfileInfoValidation()
        {
            return Application.isPlaying && UserProfileManager.Instance != null;
        }
        
    }
}