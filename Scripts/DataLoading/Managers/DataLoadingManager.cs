using System.Collections.Generic;
using TwoSleepyCats.Patterns.Singleton;
using UnityEngine;

public class DataLoadingManager : MonoSingleton<DataLoadingManager>
    {
        [System.Serializable]
        public class ResourcePath
        {
            public string name;
            public string path;
            
            public ResourcePath(string name, string path)
            {
                this.name = name;
                this.path = path;
            }
        }
        
        [Header("Resource Paths Configuration")]
        [SerializeField] private ResourcePath[] resourcePaths = new ResourcePath[]
        {
            new ResourcePath("Equipment Icons", "Icons/Equipment"),
            new ResourcePath("Ability Icons", "Icons/Abilities"),
            new ResourcePath("Currency Icons", "Icons/Currency"),
            new ResourcePath("UI Icons", "Icons/UI"),
            new ResourcePath("Character Icons", "Icons/Characters")
        };
        
        // Cache for loaded sprites
        private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private Dictionary<string, string> pathLookup = new Dictionary<string, string>();
        
        
        private void BuildPathLookup()
        {
            pathLookup.Clear();
            foreach (var resourcePath in resourcePaths)
            {
                pathLookup[resourcePath.name] = resourcePath.path;
            }
        }
        
        /// <summary>
        /// Load sprite from Resources folder with caching
        /// </summary>
        /// <param name="resourcePath">Full path in Resources folder (e.g., "Icons/Equipment/sword")</param>
        /// <returns>Loaded sprite or null if not found</returns>
        public Sprite LoadSprite(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                Debug.LogWarning("[ResourceManager] Empty resource path provided");
                return null;
            }
            
            // Check cache first
            if (spriteCache.TryGetValue(resourcePath, out var cachedSprite))
            {
                return cachedSprite;
            }
            
            var sprite = Resources.Load<Sprite>(resourcePath);
            
            if (sprite != null)
            {
                spriteCache[resourcePath] = sprite;
                Debug.Log($"[ResourceManager] Loaded and cached sprite: {resourcePath}");
            }
            return sprite;
        }
        
        public Sprite LoadSprite(string pathCategory, string fileName)
        {
            if (pathLookup.TryGetValue(pathCategory, out var basePath))
            {
                var fullPath = $"{basePath}/{fileName}";
                return LoadSprite(fullPath);
            }
            
            Debug.LogWarning($"[ResourceManager] Path category not found: {pathCategory}");
            return null;
        }
        
        /// <summary>
        /// Get the full resource path for a category
        /// </summary>
        /// <param name="pathCategory">Category name</param>
        /// <returns>Full path or empty string if not found</returns>
        public string GetPath(string pathCategory)
        {
            return pathLookup.GetValueOrDefault(pathCategory, "");
        }
        
        /// <summary>
        /// Preload all sprites in a specific folder
        /// </summary>
        /// <param name="folderPath">Folder path in Resources</param>
        public void PreloadSpritesInFolder(string folderPath)
        {
            var sprites = Resources.LoadAll<Sprite>(folderPath);
            
            foreach (var sprite in sprites)
            {
                var fullPath = $"{folderPath}/{sprite.name}";
                spriteCache.TryAdd(fullPath, sprite);
            }
        }
        
        /// <summary>
        /// Preload sprites by category
        /// </summary>
        /// <param name="pathCategory">Category to preload</param>
        public void PreloadCategory(string pathCategory)
        {
            if (pathLookup.TryGetValue(pathCategory, out var path))
            {
                PreloadSpritesInFolder(path);
            }
        }
        
        /// <summary>
        /// Check if sprite exists in cache
        /// </summary>
        public bool IsLoaded(string resourcePath)
        {
            return spriteCache.ContainsKey(resourcePath);
        }
        
        /// <summary>
        /// Clear sprite cache to free memory
        /// </summary>
        public void ClearCache()
        {
            spriteCache.Clear();
        }
        
        /// <summary>
        /// Get cache statistics
        /// </summary>
        public void LogCacheStats()
        {
            Debug.Log($"[ResourceManager] Cache contains {spriteCache.Count} sprites");
            foreach (var kvp in spriteCache)
            {
                Debug.Log($"  - {kvp.Key}");
            }
        }
        
        /// <summary>
        /// Get all available path categories
        /// </summary>
        public string[] GetAvailableCategories()
        {
            var categories = new string[pathLookup.Count];
            pathLookup.Keys.CopyTo(categories, 0);
            return categories;
        }
    }
