#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DataLoadingManager))]
public class DataLoadingManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            var resourceManager = (DataLoadingManager)target;
            
            GUILayout.Space(10);
            GUILayout.Label("Tools", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Preload All Categories"))
            {
                var categories = resourceManager.GetAvailableCategories();
                foreach (var category in categories)
                {
                    resourceManager.PreloadCategory(category);
                }
            }
            
            if (GUILayout.Button("Clear Cache"))
            {
                resourceManager.ClearCache();
            }
            
            if (GUILayout.Button("Log Cache Stats"))
            {
                resourceManager.LogCacheStats();
            }
        }
    }
#endif