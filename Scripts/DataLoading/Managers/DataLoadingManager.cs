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
        new ResourcePath("Equipment", "Icons"),
        new ResourcePath("Accessories", "Icons"),
        new ResourcePath("Gems", "Icons"),
        new ResourcePath("Currency", "Icons"),
        new ResourcePath("Special", "Icons"),
        new ResourcePath("UI", "Icons"),
        new ResourcePath("Abilities", "Icons"),
        new ResourcePath("Characters", "Icons")
    };
    
    // Cache for loaded sprites
    private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private Dictionary<string, string> pathLookup = new Dictionary<string, string>();
    
    /// <summary>
    /// Initialize the DataLoadingManager - called automatically by MonoSingleton
    /// </summary>
    protected override void Initialize()
    {
        base.Initialize();
        BuildPathLookup();
        Debug.Log("[DataLoadingManager] Initialized with path lookup dictionary");
    }
    
    /// <summary>
    /// Build pathLookup dictionary from resourcePaths array
    /// </summary>
    private void BuildPathLookup()
    {
        pathLookup.Clear();
        
        foreach (var resourcePath in resourcePaths)
        {
            if (!string.IsNullOrEmpty(resourcePath.name))
            {
                pathLookup[resourcePath.name] = resourcePath.path;
                Debug.Log($"[DataLoadingManager] Registered path: {resourcePath.name} -> {resourcePath.path}");
            }
        }
        
        Debug.Log($"[DataLoadingManager] Built path lookup with {pathLookup.Count} entries");
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
            Debug.LogWarning("[DataLoadingManager] Empty resource path provided");
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
            Debug.Log($"[DataLoadingManager] Loaded and cached sprite: {resourcePath}");
        }
        else
        {
            Debug.LogWarning($"[DataLoadingManager] Sprite not found at path: {resourcePath}");
        }
        
        return sprite;
    }
    
    /// <summary>
    /// Load sprite by category and filename
    /// </summary>
    /// <param name="pathCategory">Category name from resourcePaths</param>
    /// <param name="fileName">File name without extension</param>
    /// <returns>Loaded sprite or null if not found</returns>
    public Sprite LoadSprite(string pathCategory, string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }
        
        if (pathLookup.Count == 0)
        {
            BuildPathLookup();
        }

        string basePath = "";
        if (pathLookup.TryGetValue(pathCategory, out var foundPath))
        {
            basePath = foundPath;
        }
        else
        {
            Debug.LogWarning($"[DataLoadingManager] Path category '{pathCategory}' not found in lookup. Available categories: {string.Join(", ", pathLookup.Keys)}");
        }
        
        // Build full path
        var fullPath = string.IsNullOrEmpty(basePath) ? fileName : $"{basePath}/{fileName}";
        
        return LoadSprite(fullPath);
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
        
        Debug.Log($"[DataLoadingManager] Preloaded {sprites.Length} sprites from {folderPath}");
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
        else
        {
            Debug.LogWarning($"[DataLoadingManager] Cannot preload category '{pathCategory}' - not found in path lookup");
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
        Debug.Log("[DataLoadingManager] Sprite cache cleared");
    }
    
    /// <summary>
    /// Get cache statistics
    /// </summary>
    [ContextMenu("Log Cache Stats")]
    public void LogCacheStats()
    {
        Debug.Log($"[DataLoadingManager] Cache contains {spriteCache.Count} sprites:");
        foreach (var kvp in spriteCache)
        {
            Debug.Log($"  - {kvp.Key}");
        }
    }
    
    /// <summary>
    /// Log path lookup information
    /// </summary>
    [ContextMenu("Log Path Lookup")]
    public void LogPathLookup()
    {
        Debug.Log($"[DataLoadingManager] Path lookup contains {pathLookup.Count} entries:");
        foreach (var kvp in pathLookup)
        {
            Debug.Log($"  - {kvp.Key} -> {kvp.Value}");
        }
    }
    
    /// <summary>
    /// Get all available path categories
    /// </summary>
    public string[] GetAvailableCategories()
    {
        if (pathLookup.Count == 0)
        {
            BuildPathLookup();
        }
        
        var categories = new string[pathLookup.Count];
        pathLookup.Keys.CopyTo(categories, 0);
        return categories;
    }
    
    /// <summary>
    /// Add new resource path at runtime
    /// </summary>
    public void AddResourcePath(string categoryName, string resourcePath)
    {
        pathLookup[categoryName] = resourcePath;
        Debug.Log($"[DataLoadingManager] Added resource path: {categoryName} -> {resourcePath}");
    }
    
    /// <summary>
    /// Check if category exists in path lookup
    /// </summary>
    public bool HasCategory(string pathCategory)
    {
        return pathLookup.ContainsKey(pathCategory);
    }
    
    /// <summary>
    /// Rebuild path lookup from resourcePaths array (useful for runtime changes)
    /// </summary>
    [ContextMenu("Rebuild Path Lookup")]
    public void RebuildPathLookup()
    {
        BuildPathLookup();
    }
    
    /// <summary>
    /// Validate that all configured paths exist in Resources
    /// </summary>
    [ContextMenu("Validate Resource Paths")]
    public void ValidateResourcePaths()
    {
        Debug.Log("[DataLoadingManager] Validating resource paths...");
        
        foreach (var kvp in pathLookup)
        {
            var testSprites = Resources.LoadAll<Sprite>(kvp.Value);
            if (testSprites.Length > 0)
            {
                Debug.Log($"✅ {kvp.Key} ({kvp.Value}): Found {testSprites.Length} sprites");
            }
            else
            {
                Debug.LogWarning($"❌ {kvp.Key} ({kvp.Value}): No sprites found");
            }
        }
    }
}