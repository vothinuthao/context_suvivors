using System;
using System.Collections.Generic;
using System.Linq;
using TwoSleepyCats.CSVReader.Core;
using TwoSleepyCats.Patterns.Singleton;
using UnityEngine;

namespace OctoberStudio.Shop
{
    /// <summary>
    /// Shop Database using MonoBehaviour and CSV data loading
    /// </summary>
    public class ShopDatabase : MonoSingleton<ShopDatabase>
    {
        [Header("Loading Settings")]
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool preloadAllData = false;
        
        [Header("Debug Info")]
        [SerializeField, ReadOnly] private int totalShopItemsCount = 0;
        [SerializeField, ReadOnly] private bool isDataLoaded = false;
        [SerializeField, ReadOnly] private string loadStatus = "Not Loaded";

        // Shop data organized by type
        private Dictionary<ShopItemType, List<ShopItemModel>> itemsByType = new Dictionary<ShopItemType, List<ShopItemModel>>();
        private Dictionary<int, ShopItemModel> itemsById = new Dictionary<int, ShopItemModel>();
        private List<ShopItemModel> allItems = new List<ShopItemModel>();

        // Cached collections for performance
        private List<ShopItemModel> bundleItems;
        private List<ShopItemModel> gachaItems;
        private List<ShopItemModel> goldItems;
        private List<ShopItemModel> gemItems;
        private List<ShopItemModel> featuredItems;

        // Events
        public event System.Action OnDataLoaded;
        public event System.Action<string> OnLoadingError;

        // Properties
        public bool IsDataLoaded => isDataLoaded;
        public int TotalShopItemsCount => totalShopItemsCount;

        protected override void Initialize()
        {
            base.Initialize();
            
            // Initialize type collections
            foreach (ShopItemType itemType in Enum.GetValues(typeof(ShopItemType)))
            {
                itemsByType[itemType] = new List<ShopItemModel>();
            }

            if (loadOnStart)
            {
                LoadShopData();
            }
        }

        public async void LoadShopData()
        {
            try
            {
                loadStatus = "Loading...";
                Debug.Log("[ShopDatabase] Starting to load shop data from CSV...");
                
                var shopData = await CsvDataManager.Instance.LoadAsync<ShopItemModel>();
                ProcessShopData(shopData);
                
                isDataLoaded = true;
                loadStatus = $"Loaded {totalShopItemsCount} items";
                OnDataLoaded?.Invoke();
                
                Debug.Log($"[ShopDatabase] Successfully loaded {totalShopItemsCount} shop items");
            }
            catch (Exception ex)
            {
                isDataLoaded = false;
                loadStatus = $"Error: {ex.Message}";
                OnLoadingError?.Invoke(ex.Message);
                Debug.LogError($"[ShopDatabase] Failed to load shop data: {ex.Message}");
            }
        }

        /// <summary>
        /// Process and organize loaded shop data
        /// </summary>
        private void ProcessShopData(List<ShopItemModel> shopData)
        {
            // Clear existing data
            ClearData();

            // Process each shop item
            foreach (var item in shopData)
            {
                if (item.ValidateData())
                {
                    // Add to main collections
                    allItems.Add(item);
                    itemsById[item.ID] = item;
                    
                    // Add to type-specific collection
                    if (itemsByType.ContainsKey(item.ItemType))
                    {
                        itemsByType[item.ItemType].Add(item);
                    }
                }
                else
                {
                    Debug.LogWarning($"[ShopDatabase] Invalid shop item data: {item}");
                }
            }

            totalShopItemsCount = allItems.Count;

            // Sort items by sort order within each type
            foreach (var kvp in itemsByType)
            {
                kvp.Value.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            }

            // Sort all items by sort order
            allItems.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));

            // Update cached collections
            UpdateCachedCollections();

            // Log statistics
            LogShopStatistics();
        }

        /// <summary>
        /// Clear all cached data
        /// </summary>
        private void ClearData()
        {
            allItems.Clear();
            itemsById.Clear();
            
            foreach (var kvp in itemsByType)
            {
                kvp.Value.Clear();
            }
            
            totalShopItemsCount = 0;

            // Clear cached collections
            bundleItems = null;
            gachaItems = null;
            goldItems = null;
            gemItems = null;
            featuredItems = null;
        }

        /// <summary>
        /// Update cached collections for quick access
        /// </summary>
        private void UpdateCachedCollections()
        {
            bundleItems = GetItemsByType(ShopItemType.Bundle).ToList();
            gachaItems = GetItemsByType(ShopItemType.Gacha).ToList();
            goldItems = GetItemsByType(ShopItemType.Gold).ToList();
            gemItems = GetItemsByType(ShopItemType.Gem).ToList();
            featuredItems = allItems.Where(i => i.IsFeatured).ToList();
        }

        /// <summary>
        /// Get shop item by ID
        /// </summary>
        public ShopItemModel GetItemById(int id)
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[ShopDatabase] Data not loaded yet!");
                return null;
            }

            return itemsById.GetValueOrDefault(id);
        }

        /// <summary>
        /// Get all shop items of a specific type
        /// </summary>
        public ShopItemModel[] GetItemsByType(ShopItemType type)
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[ShopDatabase] Data not loaded yet!");
                return Array.Empty<ShopItemModel>();
            }

            if (itemsByType.TryGetValue(type, out var itemList))
            {
                return itemList.ToArray();
            }

            return Array.Empty<ShopItemModel>();
        }

        /// <summary>
        /// Get all shop items
        /// </summary>
        public ShopItemModel[] GetAllItems()
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[ShopDatabase] Data not loaded yet!");
                return Array.Empty<ShopItemModel>();
            }

            return allItems.ToArray();
        }

        /// <summary>
        /// Get featured items
        /// </summary>
        public ShopItemModel[] GetFeaturedItems()
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[ShopDatabase] Data not loaded yet!");
                return Array.Empty<ShopItemModel>();
            }

            return featuredItems?.ToArray() ?? Array.Empty<ShopItemModel>();
        }

        /// <summary>
        /// Get bundle items
        /// </summary>
        public ShopItemModel[] GetBundleItems()
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[ShopDatabase] Data not loaded yet!");
                return Array.Empty<ShopItemModel>();
            }

            return bundleItems?.ToArray() ?? Array.Empty<ShopItemModel>();
        }

        /// <summary>
        /// Get gacha items
        /// </summary>
        public ShopItemModel[] GetGachaItems()
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[ShopDatabase] Data not loaded yet!");
                return Array.Empty<ShopItemModel>();
            }

            return gachaItems?.ToArray() ?? Array.Empty<ShopItemModel>();
        }

        /// <summary>
        /// Get gold purchase items
        /// </summary>
        public ShopItemModel[] GetGoldItems()
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[ShopDatabase] Data not loaded yet!");
                return Array.Empty<ShopItemModel>();
            }

            return goldItems?.ToArray() ?? Array.Empty<ShopItemModel>();
        }

        /// <summary>
        /// Get gem purchase items
        /// </summary>
        public ShopItemModel[] GetGemItems()
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[ShopDatabase] Data not loaded yet!");
                return Array.Empty<ShopItemModel>();
            }

            return gemItems?.ToArray() ?? Array.Empty<ShopItemModel>();
        }

        /// <summary>
        /// Get gacha items by rarity focus
        /// </summary>
        public ShopItemModel[] GetGachaItemsByRarity(EquipmentRarity focusRarity)
        {
            if (!isDataLoaded)
                return Array.Empty<ShopItemModel>();

            return gachaItems?.Where(g => g.Rarity == focusRarity).ToArray() ?? Array.Empty<ShopItemModel>();
        }

        /// <summary>
        /// Search items by name
        /// </summary>
        public ShopItemModel[] SearchItemsByName(string searchTerm)
        {
            if (!isDataLoaded || string.IsNullOrEmpty(searchTerm))
            {
                return Array.Empty<ShopItemModel>();
            }

            return allItems.Where(i => 
                i.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                i.Description.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0
            ).ToArray();
        }

        /// <summary>
        /// Get items by price range
        /// </summary>
        public ShopItemModel[] GetItemsByPriceRange(PriceType priceType, float minPrice, float maxPrice)
        {
            if (!isDataLoaded)
                return Array.Empty<ShopItemModel>();

            return allItems.Where(i => 
                i.PriceType == priceType && 
                i.PriceAmount >= minPrice && 
                i.PriceAmount <= maxPrice
            ).ToArray();
        }

        /// <summary>
        /// Check if item exists
        /// </summary>
        public bool HasItem(int id)
        {
            return isDataLoaded && itemsById.ContainsKey(id);
        }

        /// <summary>
        /// Get item count by type
        /// </summary>
        public int GetItemCountByType(ShopItemType type)
        {
            if (!isDataLoaded) return 0;
            
            return itemsByType.TryGetValue(type, out var list) ? list.Count : 0;
        }

        /// <summary>
        /// Force reload data from CSV
        /// </summary>
        [ContextMenu("Reload Shop Data")]
        public async void ReloadShopData()
        {
            try
            {
                isDataLoaded = false;
                loadStatus = "Reloading...";
                
                var shopData = await CsvDataManager.Instance.ForceReloadAsync<ShopItemModel>();
                ProcessShopData(shopData);
                
                isDataLoaded = true;
                loadStatus = $"Reloaded {totalShopItemsCount} items";
                OnDataLoaded?.Invoke();
                
                Debug.Log($"[ShopDatabase] Successfully reloaded {totalShopItemsCount} shop items");
            }
            catch (Exception ex)
            {
                isDataLoaded = false;
                loadStatus = $"Reload Error: {ex.Message}";
                OnLoadingError?.Invoke(ex.Message);
                Debug.LogError($"[ShopDatabase] Failed to reload shop data: {ex.Message}");
            }
        }

        /// <summary>
        /// Log shop statistics
        /// </summary>
        private void LogShopStatistics()
        {
            Debug.Log($"[ShopDatabase] Loaded {totalShopItemsCount} shop items:");
            
            foreach (ShopItemType type in Enum.GetValues(typeof(ShopItemType)))
            {
                int count = GetItemCountByType(type);
                if (count > 0)
                {
                    Debug.Log($"[ShopDatabase] - {type}: {count} items");
                }
            }

            var featuredCount = featuredItems?.Count ?? 0;
            if (featuredCount > 0)
            {
                Debug.Log($"[ShopDatabase] - Featured items: {featuredCount}");
            }
        }

        /// <summary>
        /// Get database info for debugging
        /// </summary>
        [ContextMenu("Log Database Info")]
        public void LogDatabaseInfo()
        {
            if (!isDataLoaded)
            {
                Debug.Log("[ShopDatabase] Database not loaded");
                return;
            }

            LogShopStatistics();
            
            // Show cache info
            var cacheInfo = CsvDataManager.Instance.GetCacheInfo();
            Debug.Log($"[ShopDatabase] CSV Cache Info:\n{cacheInfo}");

            // Show featured items
            if (featuredItems != null && featuredItems.Count > 0)
            {
                Debug.Log("[ShopDatabase] Featured Items:");
                foreach (var item in featuredItems)
                {
                    Debug.Log($"[ShopDatabase] - {item.Name} ({item.ItemType}) - {item.GetPriceText()} {item.PriceType}");
                }
            }
        }

        /// <summary>
        /// Validate all shop items
        /// </summary>
        [ContextMenu("Validate Shop Items")]
        public void ValidateAllItems()
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[ShopDatabase] Cannot validate - data not loaded");
                return;
            }

            int validCount = 0;
            int invalidCount = 0;

            foreach (var item in allItems)
            {
                if (item.ValidateData())
                {
                    validCount++;
                }
                else
                {
                    invalidCount++;
                    Debug.LogError($"[ShopDatabase] Invalid item: {item.Name} (ID: {item.ID})");
                }
            }

            Debug.Log($"[ShopDatabase] Validation complete: {validCount} valid, {invalidCount} invalid items");
        }
    }

    /// <summary>
    /// ReadOnly attribute for inspector display
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            UnityEditor.EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
#endif
}