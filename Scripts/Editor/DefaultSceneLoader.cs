using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OctoberStudio
{
    [InitializeOnLoad]
    public static class DefaultSceneLoader
    {
        static DefaultSceneLoader()
        {
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;

            RewriteStartScene();
        }

        public static void OnProjectSettingsChanged()
        {
            ActivateGameScene();
        }

        private static void RewriteStartScene()
        {
            ActivateGameScene();
        }

        private static void ActivateGameScene()
        {
            var mainMenuSceneName = GetMainMenuSceneName();

            if(mainMenuSceneName != null)
            {
                SceneAsset gameScene = GetAsset<SceneAsset>(mainMenuSceneName);
                if (gameScene != null)
                {
                    EditorSceneManager.playModeStartScene = gameScene;
                }
            } else
            {
                EditorSceneManager.playModeStartScene = null;
            }
        }

        private static T GetAsset<T>(string name = "") where T : Object
        {
            string[] assets = AssetDatabase.FindAssets((string.IsNullOrEmpty(name) ? "" : name + " ") + "t:" + typeof(T).Name);
            if (assets.Length > 0)
            {
                return (T)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assets[0]), typeof(T));
            }

            return null;
        }

        private static void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            RewriteStartScene();
        }

        private static string GetMainMenuSceneName()
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(ProjectSettings).Name}");
            ProjectSettings projectSettings = null;

            foreach (string guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ProjectSettings>(path);
                if (asset != null)
                {
                    projectSettings = asset;
                }
            }

            if (projectSettings != null && SceneExistsInAssets(projectSettings.MainMenuSceneName))
            {
                return projectSettings.MainMenuSceneName;
            }
            else if (SceneExistsInAssets("Main Menu"))
            {
                return "Main Menu";
            }
            else
            {
                return null;
            }
        }

        private static bool SceneExistsInAssets(string sceneName)
        {
            string[] guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
            return guids.Any(guid =>
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                return System.IO.Path.GetFileNameWithoutExtension(path) == sceneName;
            });
        }
    }
}