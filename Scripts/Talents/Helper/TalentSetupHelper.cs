using UnityEngine;
using Talents.Config;
using Talents.Manager;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Talents.Tools
{
    /// <summary>
    /// Helper tool for auto-setup talent system and fix common issues
    /// </summary>
    public class TalentSetupHelper : MonoBehaviour
    {
        [Header("Setup Options")]
        [SerializeField] private bool autoCreateFolders = true;
        [SerializeField] private bool autoCreateLayoutConfig = true;
        [SerializeField] private bool autoCreateIcons = true;
        [SerializeField] private bool validateSetup = true;

        [Header("Current Setup Status")]
        [SerializeField, ReadOnly] private bool hasLayoutConfig = false;
        [SerializeField, ReadOnly] private bool hasIconFolder = false;
        [SerializeField, ReadOnly] private bool hasRequiredIcons = false;
        [SerializeField, ReadOnly] private string setupStatus = "Not Checked";

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private void Start()
        {
            if (validateSetup)
            {
                ValidateCurrentSetup();

                // Disable auto-fix to prevent loops - user can manually run if needed
                // if (setupStatus.Contains("❌"))
                // {
                //     Log("Setup incomplete detected. Auto-fixing...");
                //     AutoSetupTalentSystem();
                // }
            }
        }

        /// <summary>
        /// Auto-setup entire talent system
        /// </summary>
        [ContextMenu("Auto Setup Talent System")]
        public void AutoSetupTalentSystem()
        {
            Log("Starting auto-setup talent system...");

            if (autoCreateFolders)
                CreateRequiredFolders();

            if (autoCreateLayoutConfig)
                CreateLayoutConfig();

            if (autoCreateIcons)
                CreateDefaultIcons();

            ValidateCurrentSetup();
            FixCommonIssues();

            Log("Auto-setup completed!");
        }

        /// <summary>
        /// Create required folder structure
        /// </summary>
        [ContextMenu("Create Required Folders")]
        public void CreateRequiredFolders()
        {
            Log("Creating required folders...");

#if UNITY_EDITOR
            // Create folder structure
            CreateFolderIfNotExists("Assets/Resources");
            CreateFolderIfNotExists("Assets/Resources/Icons");
            CreateFolderIfNotExists("Assets/Resources/Icons/Talents");
            CreateFolderIfNotExists("Assets/Resources/Icons/UI");
            CreateFolderIfNotExists("Assets/ScriptableObjects");
            CreateFolderIfNotExists("Assets/ScriptableObjects/Talents");
            CreateFolderIfNotExists("Assets/Prefabs");
            CreateFolderIfNotExists("Assets/Prefabs/UI");
            CreateFolderIfNotExists("Assets/Prefabs/UI/Talents");

            AssetDatabase.Refresh();
            Log("Folders created successfully!");
#else
            Log("Folder creation only available in Editor mode");
#endif
        }

        /// <summary>
        /// Create layout config if not exists
        /// </summary>
        [ContextMenu("Create Layout Config")]
        public void CreateLayoutConfig()
        {
            Log("Creating layout config...");

#if UNITY_EDITOR
            // Check if config already exists
            var existingConfig = Resources.Load<TalentLayoutConfig>("TalentLayoutConfig");
            if (existingConfig != null)
            {
                Log("Layout config already exists!");
                return;
            }

            // Create new config
            var config = ScriptableObject.CreateInstance<TalentLayoutConfig>();
            config.ResetToDefaults();

            // Save as asset
            string path = "Assets/ScriptableObjects/Talents/TalentLayoutConfig.asset";
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Auto-assign to TalentDatabase if found
            var talentDatabase = FindObjectOfType<TalentDatabase>();
            if (talentDatabase != null)
            {
                var serializedObject = new SerializedObject(talentDatabase);
                var layoutConfigProperty = serializedObject.FindProperty("layoutConfig");
                layoutConfigProperty.objectReferenceValue = config;
                serializedObject.ApplyModifiedProperties();
                Log("Layout config assigned to TalentDatabase!");
            }

            Log($"Layout config created at: {path}");
#else
            Log("Layout config creation only available in Editor mode");
#endif
        }

        /// <summary>
        /// Create default icons
        /// </summary>
        [ContextMenu("Create Default Icons")]
        public void CreateDefaultIcons()
        {
            Log("Creating default icons...");

#if UNITY_EDITOR
            // Create folders first
            CreateFolderIfNotExists("Assets/Resources");
            CreateFolderIfNotExists("Assets/Resources/Icons");
            CreateFolderIfNotExists("Assets/Resources/Icons/Talents");
            CreateFolderIfNotExists("Assets/Resources/Icons/UI");
#endif

            // Create default icon textures (only if they don't exist)
            CreateDefaultIcon("Icons/Talents/default_normal", Color.white);
            CreateDefaultIcon("Icons/Talents/default_special", new Color(1f, 0.8f, 0f)); // Gold
            CreateDefaultIcon("Icons/UI/gold_icon", new Color(1f, 0.8f, 0f)); // Gold
            CreateDefaultIcon("Icons/UI/orc_icon", new Color(0.5f, 0.3f, 0.1f)); // Brown

            Log("Default icons created!");
        }

        /// <summary>
        /// Create a default icon texture
        /// </summary>
        private void CreateDefaultIcon(string resourcePath, Color color)
        {
#if UNITY_EDITOR
            // Check if icon already exists (as Sprite or Texture2D)
            var existingSprite = Resources.Load<Sprite>(resourcePath);
            var existingTexture = Resources.Load<Texture2D>(resourcePath);
            if (existingSprite != null || existingTexture != null)
            {
                Log($"Icon already exists: {resourcePath}");
                return;
            }

            // Create texture
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color[64 * 64];
            
            // Create simple colored square with border
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    bool isBorder = x < 2 || x > 61 || y < 2 || y > 61;
                    pixels[y * 64 + x] = isBorder ? Color.black : color;
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();

            // Save as PNG
            string fullPath = $"Assets/Resources/{resourcePath}.png";
            string directory = Path.GetDirectoryName(fullPath);
            
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(fullPath, pngData);

            // Import settings
            AssetDatabase.ImportAsset(fullPath);
            var importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.maxTextureSize = 128;
                AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);
            }

            DestroyImmediate(texture);
            Log($"Created icon: {resourcePath}");
#endif
        }

        /// <summary>
        /// Validate current setup
        /// </summary>
        [ContextMenu("Validate Setup")]
        public void ValidateCurrentSetup()
        {
            Log("Validating current setup...");

            // Check layout config - use AssetDatabase for non-Resources files
            TalentLayoutConfig layoutConfig = null;

#if UNITY_EDITOR
            // In Editor, search for the asset
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TalentLayoutConfig");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                layoutConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<TalentLayoutConfig>(path);
                Log($"✅ Found TalentLayoutConfig at: {path}");
            }
#else
            // In build, try Resources paths
            layoutConfig = Resources.Load<TalentLayoutConfig>("TalentLayoutConfig");
            if (layoutConfig == null) layoutConfig = Resources.Load<TalentLayoutConfig>("Talent/TalentLayoutConfig");
            if (layoutConfig == null) layoutConfig = Resources.Load<TalentLayoutConfig>("Talents/TalentLayoutConfig");
#endif

            hasLayoutConfig = layoutConfig != null;

            if (!hasLayoutConfig)
            {
                Log($"❌ TalentLayoutConfig not found");
            }

            // Check icon folder
            hasIconFolder = Resources.Load<Sprite>("Icons/Talents/atk_icon") != null;

            // Check required icons
            hasRequiredIcons = CheckRequiredIcons();

            // Update status
            if (hasLayoutConfig && hasIconFolder && hasRequiredIcons)
            {
                setupStatus = "✅ Setup Complete";
            }
            else
            {
                setupStatus = "❌ Setup Incomplete";
            }

            Log($"Validation complete: {setupStatus}");
            LogValidationDetails();
        }

        /// <summary>
        /// Check if required icons exist
        /// </summary>
        private bool CheckRequiredIcons()
        {
            string[] requiredIcons = {
                "Icons/Talents/atk_icon",
                "Icons/Talents/def_icon", 
                "Icons/Talents/speed_icon",
                "Icons/Talents/heal_icon",
                "Icons/Talents/default_normal",
                "Icons/Talents/default_special",
                "Icons/UI/gold_icon",
                "Icons/UI/orc_icon"
            };

            foreach (string iconPath in requiredIcons)
            {
                // Try loading as Sprite first, then as Texture2D (for .psd files)
                var sprite = Resources.Load<Sprite>(iconPath);
                var texture = Resources.Load<Texture2D>(iconPath);

                if (sprite == null && texture == null)
                {
                    Log($"❌ Missing icon: {iconPath}");
                    return false;
                }
                else if (sprite != null)
                {
                    Log($"✅ Found icon (Sprite): {iconPath}");
                }
                else if (texture != null)
                {
                    Log($"✅ Found icon (Texture2D): {iconPath}");
                }
            }

            return true;
        }

        /// <summary>
        /// Fix common setup issues
        /// </summary>
        [ContextMenu("Fix Common Issues")]
        public void FixCommonIssues()
        {
            Log("Fixing common issues...");

            // Fix 1: Assign layout config to TalentDatabase
            var talentDatabase = FindObjectOfType<TalentDatabase>();
            if (talentDatabase != null)
            {
                var layoutConfig = Resources.Load<TalentLayoutConfig>("TalentLayoutConfig");
                if (layoutConfig != null)
                {
#if UNITY_EDITOR
                    var serializedObject = new SerializedObject(talentDatabase);
                    var layoutConfigProperty = serializedObject.FindProperty("layoutConfig");
                    if (layoutConfigProperty.objectReferenceValue == null)
                    {
                        layoutConfigProperty.objectReferenceValue = layoutConfig;
                        serializedObject.ApplyModifiedProperties();
                        Log("Fixed: Assigned layout config to TalentDatabase");
                    }
#endif
                }
            }

            // Fix 2: Set mobile optimization
            var layoutConfigAsset = Resources.Load<TalentLayoutConfig>("TalentLayoutConfig");
            if (layoutConfigAsset != null)
            {
                // Apply mobile optimization for current screen
                Vector2 screenSize = new Vector2(Screen.width, Screen.height);
                layoutConfigAsset.ApplyMobileOptimization(screenSize);
                Log("Fixed: Applied mobile optimization");
            }

            // Fix 3: Validate CSV connection
            if (talentDatabase != null && talentDatabase.IsDataLoaded)
            {
                Log("CSV data loaded successfully");
            }
            else
            {
                Log("Warning: CSV data not loaded. Check talentConfig.csv file");
            }

            Log("Common issues fixed!");
        }

        /// <summary>
        /// Log validation details
        /// </summary>
        private void LogValidationDetails()
        {
            Log("=== SETUP VALIDATION DETAILS ===");
            Log($"Layout Config: {(hasLayoutConfig ? "✅" : "❌")}");
            Log($"Icon Folder: {(hasIconFolder ? "✅" : "❌")}");
            Log($"Required Icons: {(hasRequiredIcons ? "✅" : "❌")}");

            var talentDatabase = FindObjectOfType<TalentDatabase>();
            Log($"TalentDatabase Found: {(talentDatabase != null ? "✅" : "❌")}");

            if (talentDatabase != null)
            {
                Log($"CSV Data Loaded: {(talentDatabase.IsDataLoaded ? "✅" : "❌")}");

                if (!talentDatabase.IsDataLoaded)
                {
                    Log("⚠️ Attempting to load CSV data...");
                    try
                    {
                        talentDatabase.LoadTalentData();
                        Log($"✅ Manual load attempt - Data loaded: {talentDatabase.IsDataLoaded}");
                    }
                    catch (System.Exception e)
                    {
                        Log($"❌ Failed to load CSV data: {e.Message}");
                    }
                }

                Log($"Total Talents: {talentDatabase.TotalTalentCount}");
                Log($"Max Zone Level: {talentDatabase.MaxPlayerLevel}");
            }

            var layoutConfig = Resources.Load<TalentLayoutConfig>("TalentLayoutConfig");
            if (layoutConfig != null)
            {
                Log($"Node Spacing: {layoutConfig.NodeSpacing}");
                Log($"Mobile Optimized: {(layoutConfig.GetOptimalContentSize() != Vector2.zero ? "✅" : "❌")}");
            }
        }

        /// <summary>
        /// Create folder if not exists (Editor only)
        /// </summary>
        private void CreateFolderIfNotExists(string folderPath)
        {
#if UNITY_EDITOR
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentFolder = Path.GetDirectoryName(folderPath);
                string folderName = Path.GetFileName(folderPath);
                
                if (!string.IsNullOrEmpty(parentFolder) && !AssetDatabase.IsValidFolder(parentFolder))
                {
                    CreateFolderIfNotExists(parentFolder);
                }
                
                AssetDatabase.CreateFolder(parentFolder, folderName);
                Log($"Created folder: {folderPath}");
            }
#endif
        }

        /// <summary>
        /// Helper logging method
        /// </summary>
        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[TalentSetupHelper] {message}");
            }
        }

        /// <summary>
        /// Quick setup for new projects
        /// </summary>
        [ContextMenu("Quick Setup (New Project)")]
        public void QuickSetupNewProject()
        {
            Log("Starting quick setup for new project...");
            
            autoCreateFolders = true;
            autoCreateLayoutConfig = true;
            autoCreateIcons = true;
            validateSetup = true;
            
            AutoSetupTalentSystem();
            
            Log("Quick setup completed! Ready to use talent system.");
        }

        /// <summary>
        /// Reset all settings
        /// </summary>
        [ContextMenu("Reset All Settings")]
        public void ResetAllSettings()
        {
            Log("Resetting all settings...");
            
            var layoutConfig = Resources.Load<TalentLayoutConfig>("TalentLayoutConfig");
            if (layoutConfig != null)
            {
                layoutConfig.ResetToDefaults();
                Log("Layout config reset to defaults");
            }
            
            var talentDatabase = FindObjectOfType<TalentDatabase>();
            if (talentDatabase != null)
            {
                talentDatabase.ReloadCSVData();
                Log("TalentDatabase reloaded");
            }
            
            Log("All settings reset!");
        }
    }

    // ReadOnly attribute for inspector display
    public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
#endif
}