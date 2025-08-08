using System;
using System.Collections.Generic;
using System.Linq;
using TwoSleepyCats.CSVReader.Attributes;
using TwoSleepyCats.CSVReader.Core;
using UnityEngine;

namespace OctoberStudio.Shop
{
    public enum ShopItemType
    {
        Bundle,
        Gacha,
        Gold,
        Gem,
        Item
    }

    public enum PriceType
    {
        gold,
        gem,
        usd
    }

    [System.Serializable]
    public class BundleItem
    {
        public string type;
        public int id;
        public int quantity;
    }

    [System.Serializable]
    public class GachaPool
    {
        public string rarity;
        public string type;
        public int weight;
    }

    [System.Serializable]
    public class ShopItemModel : ICsvModel
    {
        [CsvColumn("id")] public int ID { get; set; }
        [CsvColumn("name")] public string Name { get; set; }
        [CsvColumn("description")] public string Description { get; set; }
        [CsvColumn("item_type")] public string ItemTypeString { get; set; }
        [CsvColumn("price_type")] public string PriceTypeString { get; set; }
        [CsvColumn("price_amount")] public float PriceAmount { get; set; }
        [CsvColumn("purchase_limit")] public int PurchaseLimit { get; set; }
        [CsvColumn("reset_time_hours")] public int ResetTimeHours { get; set; }
        [CsvColumn("bundle_items")] public string BundleItemsJson { get; set; }
        [CsvColumn("gacha_pool")] public string GachaPoolJson { get; set; }
        [CsvColumn("gacha_count")] public int GachaCount { get; set; }
        [CsvColumn("icon_name")] public string IconName { get; set; }
        [CsvColumn("rarity")] public string RarityString { get; set; }
        [CsvColumn("is_featured")] public bool IsFeatured { get; set; }
        [CsvColumn("sort_order")] public int SortOrder { get; set; }

        // Cached parsed data to avoid repeated parsing
        private List<BundleItem> _cachedBundleItems;
        private List<GachaPool> _cachedGachaPool;
        private bool _bundleItemsParsed = false;
        private bool _gachaPoolParsed = false;

        // Parsed enum properties
        [CsvIgnore]
        public ShopItemType ItemType
        {
            get
            {
                if (System.Enum.TryParse<ShopItemType>(ItemTypeString, true, out var result))
                    return result;
                return ShopItemType.Item;
            }
        }

        [CsvIgnore]
        public PriceType PriceType
        {
            get
            {
                if (System.Enum.TryParse<PriceType>(PriceTypeString, true, out var result))
                    return result;
                return PriceType.gold;
            }
        }

        [CsvIgnore]
        public EquipmentRarity Rarity
        {
            get
            {
                if (Enum.TryParse<EquipmentRarity>(RarityString, true, out var result))
                    return result;
                return EquipmentRarity.Common;
            }
        }

        // Enhanced bundle items parsing with better error handling
        [CsvIgnore]
        public List<BundleItem> BundleItems
        {
            get
            {
                if (!_bundleItemsParsed)
                {
                    _cachedBundleItems = ParseBundleItems();
                    _bundleItemsParsed = true;
                }
                return _cachedBundleItems ?? new List<BundleItem>();
            }
        }

        // Enhanced gacha pool parsing with better error handling
        [CsvIgnore]
        public List<GachaPool> GachaPool
        {
            get
            {
                if (!_gachaPoolParsed)
                {
                    _cachedGachaPool = ParseGachaPool();
                    _gachaPoolParsed = true;
                }
                return _cachedGachaPool ?? new List<GachaPool>();
            }
        }

        private List<BundleItem> ParseBundleItems()
        {
            if (string.IsNullOrEmpty(BundleItemsJson))
            {
                Debug.Log($"[ShopItemModel] Empty bundle items JSON for {Name}");
                return new List<BundleItem>();
            }

            try
            {
                // Clean up the JSON string
                string cleanJson = CleanJsonString(BundleItemsJson);
                Debug.Log($"[ShopItemModel] Parsing bundle items for {Name}: {cleanJson}");

                // Try different JSON formats
                List<BundleItem> items = null;
                
                // Format 1: Array of objects
                if (cleanJson.Trim().StartsWith("["))
                {
                    var wrapper = JsonUtility.FromJson<BundleItemList>("{\"items\":" + cleanJson + "}");
                    items = wrapper?.items;
                }
                // Format 2: Simple comma-separated values (fallback)
                else if (cleanJson.Contains(","))
                {
                    items = ParseSimpleBundleFormat(cleanJson);
                }
                // Format 3: Single item
                else
                {
                    var singleItem = JsonUtility.FromJson<BundleItem>(cleanJson);
                    if (singleItem != null)
                        items = new List<BundleItem> { singleItem };
                }

                if (items != null && items.Count > 0)
                {
                    Debug.Log($"[ShopItemModel] Successfully parsed {items.Count} bundle items for {Name}");
                    return items;
                }
                else
                {
                    Debug.LogWarning($"[ShopItemModel] No valid bundle items found for {Name}");
                    return TryFallbackBundleParsing();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShopItemModel] Failed to parse bundle items for {Name}: {ex.Message}");
                Debug.LogError($"[ShopItemModel] Raw JSON: '{BundleItemsJson}'");
                
                // Try fallback parsing
                return TryFallbackBundleParsing();
            }
        }

        private List<GachaPool> ParseGachaPool()
        {
            if (string.IsNullOrEmpty(GachaPoolJson))
            {
                Debug.Log($"[ShopItemModel] Empty gacha pool JSON for {Name}");
                return new List<GachaPool>();
            }

            try
            {
                // Clean up the JSON string
                string cleanJson = CleanJsonString(GachaPoolJson);
                Debug.Log($"[ShopItemModel] Parsing gacha pool for {Name}: {cleanJson}");

                List<GachaPool> pools = null;

                // Format 1: Array of objects
                if (cleanJson.Trim().StartsWith("["))
                {
                    var wrapper = JsonUtility.FromJson<GachaPoolList>("{\"pools\":" + cleanJson + "}");
                    pools = wrapper?.pools;
                }
                // Format 2: Simple format (fallback)
                else if (cleanJson.Contains(","))
                {
                    pools = ParseSimpleGachaFormat(cleanJson);
                }
                // Format 3: Single pool
                else
                {
                    var singlePool = JsonUtility.FromJson<GachaPool>(cleanJson);
                    if (singlePool != null)
                        pools = new List<GachaPool> { singlePool };
                }

                if (pools != null && pools.Count > 0)
                {
                    Debug.Log($"[ShopItemModel] Successfully parsed {pools.Count} gacha pools for {Name}");
                    return pools;
                }
                else
                {
                    Debug.LogWarning($"[ShopItemModel] No valid gacha pools found for {Name}");
                    return TryFallbackGachaParsing();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShopItemModel] Failed to parse gacha pool for {Name}: {ex.Message}");
                Debug.LogError($"[ShopItemModel] Raw JSON: '{GachaPoolJson}'");
                
                // Try fallback parsing
                return TryFallbackGachaParsing();
            }
        }

        private string CleanJsonString(string json)
        {
            if (string.IsNullOrEmpty(json)) return json;

            // Remove extra quotes and whitespace
            json = json.Trim().Trim('"').Trim();
            
            // Fix escaped quotes
            json = json.Replace("\"\"", "\"");
            json = json.Replace("\\\"", "\"");
            
            return json;
        }

        private List<BundleItem> ParseSimpleBundleFormat(string input)
        {
            // Handle simple format like: "gold,1000;gem,100;equipment,5"
            var items = new List<BundleItem>();
            var parts = input.Split(';');
            
            foreach (var part in parts)
            {
                var values = part.Split(',');
                if (values.Length >= 2)
                {
                    var item = new BundleItem
                    {
                        type = values[0].Trim(),
                        quantity = int.TryParse(values[1].Trim(), out int qty) ? qty : 1,
                        id = values.Length > 2 && int.TryParse(values[2].Trim(), out int id) ? id : -1
                    };
                    items.Add(item);
                }
            }
            
            return items;
        }

        private List<GachaPool> ParseSimpleGachaFormat(string input)
        {
            // Handle simple format like: "common,70;rare,25;epic,5"
            var pools = new List<GachaPool>();
            var parts = input.Split(';');
            
            foreach (var part in parts)
            {
                var values = part.Split(',');
                if (values.Length >= 2)
                {
                    var pool = new GachaPool
                    {
                        rarity = values[0].Trim(),
                        weight = int.TryParse(values[1].Trim(), out int weight) ? weight : 1,
                        type = values.Length > 2 ? values[2].Trim() : ""
                    };
                    pools.Add(pool);
                }
            }
            
            return pools;
        }

        private List<BundleItem> TryFallbackBundleParsing()
        {
            // Create default bundle based on item type
            var fallbackItems = new List<BundleItem>();
            
            if (ItemType == ShopItemType.Bundle)
            {
                // Add some default items based on name or other properties
                if (Name.ToLower().Contains("starter"))
                {
                    fallbackItems.Add(new BundleItem { type = "gold", quantity = 1000, id = -1 });
                    fallbackItems.Add(new BundleItem { type = "gem", quantity = 100, id = -1 });
                }
                else if (Name.ToLower().Contains("premium"))
                {
                    fallbackItems.Add(new BundleItem { type = "gold", quantity = 5000, id = -1 });
                    fallbackItems.Add(new BundleItem { type = "gem", quantity = 500, id = -1 });
                    fallbackItems.Add(new BundleItem { type = "equipment", quantity = 1, id = -1 });
                }
                
                Debug.LogWarning($"[ShopItemModel] Using fallback bundle items for {Name}");
            }
            
            return fallbackItems;
        }

        private List<GachaPool> TryFallbackGachaParsing()
        {
            // Create default gacha pool based on rarity
            var fallbackPools = new List<GachaPool>();
            
            if (ItemType == ShopItemType.Gacha)
            {
                if (Rarity == EquipmentRarity.Rare)
                {
                    fallbackPools.Add(new GachaPool { rarity = "Common", weight = 60, type = "equipment" });
                    fallbackPools.Add(new GachaPool { rarity = "Uncommon", weight = 30, type = "equipment" });
                    fallbackPools.Add(new GachaPool { rarity = "Rare", weight = 10, type = "equipment" });
                }
                else if (Rarity == EquipmentRarity.Epic)
                {
                    fallbackPools.Add(new GachaPool { rarity = "Rare", weight = 70, type = "equipment" });
                    fallbackPools.Add(new GachaPool { rarity = "Epic", weight = 25, type = "equipment" });
                    fallbackPools.Add(new GachaPool { rarity = "Legendary", weight = 5, type = "equipment" });
                }
                
                Debug.LogWarning($"[ShopItemModel] Using fallback gacha pool for {Name}");
            }
            
            return fallbackPools;
        }

        // Helper classes for JSON parsing
        [System.Serializable]
        private class BundleItemList
        {
            public List<BundleItem> items;
        }

        [System.Serializable]
        private class GachaPoolList
        {
            public List<GachaPool> pools;
        }

        public string GetCsvFileName() => "shop_items.csv";

        public void OnDataLoaded()
        {
            // Reset cache flags to force re-parsing with OnDataLoaded context
            _bundleItemsParsed = false;
            _gachaPoolParsed = false;
            _cachedBundleItems = null;
            _cachedGachaPool = null;
            
            // Pre-parse data to catch errors early
            _ = BundleItems;
            _ = GachaPool;
            
            // Validation after data load
            if (!ValidateData())
            {
                Debug.LogWarning($"[ShopItemModel] Invalid shop item data: {Name} (ID: {ID})");
            }
        }

        /// <summary>
        /// Get shop item icon sprite
        /// </summary>
        public Sprite GetIcon()
        {
            if (DataLoadingManager.Instance == null || string.IsNullOrEmpty(IconName))
                return null;

            return DataLoadingManager.Instance.LoadSprite("Shop", IconName);
        }

        /// <summary>
        /// Get price display text
        /// </summary>
        public string GetPriceText()
        {
            switch (PriceType)
            {
                case PriceType.gold:
                    return $"{PriceAmount:F0}";
                case PriceType.gem:
                    return $"{PriceAmount:F0}";
                case PriceType.usd:
                    return $"${PriceAmount:F2}";
                default:
                    return PriceAmount.ToString();
            }
        }

        /// <summary>
        /// Get currency icon for price
        /// </summary>
        public Sprite GetPriceIcon()
        {
            if (DataLoadingManager.Instance == null)
                return null;

            string iconName = PriceType switch
            {
                PriceType.gold => "icon_gold",
                PriceType.gem => "icon_gem",
                PriceType.usd => "icon_usd",
                _ => "icon_gold"
            };

            return DataLoadingManager.Instance.LoadSprite("Currency", iconName);
        }

        /// <summary>
        /// Get rarity color
        /// </summary>
        public Color GetRarityColor()
        {
            return Rarity switch
            {
                EquipmentRarity.Common => new Color(0.8f, 0.8f, 0.8f, 1f),
                EquipmentRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f, 1f),
                EquipmentRarity.Rare => new Color(0.2f, 0.4f, 1f, 1f),
                EquipmentRarity.Epic => new Color(0.6f, 0.2f, 0.8f, 1f),
                EquipmentRarity.Legendary => new Color(1f, 0.8f, 0.2f, 1f),
                _ => Color.white
            };
        }

        /// <summary>
        /// Check if item is purchasable
        /// </summary>
        public bool CanPurchase(int currentPurchases = 0)
        {
            if (PurchaseLimit == 0) return true; // Unlimited
            return currentPurchases < PurchaseLimit;
        }

        /// <summary>
        /// Check if item should reset based on time
        /// </summary>
        public bool ShouldReset(DateTime lastPurchaseTime)
        {
            if (ResetTimeHours == 0) return false; // No reset
            
            var timeSinceLastPurchase = DateTime.Now - lastPurchaseTime;
            return timeSinceLastPurchase.TotalHours >= ResetTimeHours;
        }

        /// <summary>
        /// Get next reset time
        /// </summary>
        public DateTime GetNextResetTime(DateTime lastPurchaseTime)
        {
            if (ResetTimeHours == 0)
                return DateTime.MaxValue;
                
            return lastPurchaseTime.AddHours(ResetTimeHours);
        }

        /// <summary>
        /// Get time until next reset
        /// </summary>
        public TimeSpan GetTimeUntilReset(DateTime lastPurchaseTime)
        {
            if (ResetTimeHours == 0)
                return TimeSpan.Zero;
                
            var nextReset = GetNextResetTime(lastPurchaseTime);
            var timeUntilReset = nextReset - DateTime.Now;
            
            return timeUntilReset > TimeSpan.Zero ? timeUntilReset : TimeSpan.Zero;
        }

        /// <summary>
        /// Enhanced validation with better error reporting
        /// </summary>
        public bool ValidateData()
        {
            var issues = new List<string>();
            
            // Basic validation
            if (ID < 0) issues.Add("Invalid ID");
            if (string.IsNullOrEmpty(Name)) issues.Add("Missing Name");
            if (string.IsNullOrEmpty(IconName)) issues.Add("Missing IconName");
            if (PriceAmount < 0) issues.Add("Invalid PriceAmount");
            
            // Type-specific validation
            if (ItemType == ShopItemType.Bundle)
            {
                if (BundleItems.Count == 0)
                    issues.Add("Bundle has no items");
            }
            
            if (ItemType == ShopItemType.Gacha)
            {
                if (GachaPool.Count == 0)
                    issues.Add("Gacha has no pool");
                if (GachaCount <= 0)
                    issues.Add("Invalid GachaCount");
            }
            
            // Log validation issues
            if (issues.Count > 0)
            {
                Debug.LogError($"[ShopItemModel] Validation failed for {Name} (ID: {ID}):");
                foreach (var issue in issues)
                {
                    Debug.LogError($"  - {issue}");
                }
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Get bundle contents as formatted text
        /// </summary>
        public string GetBundleContentsText()
        {
            if (ItemType != ShopItemType.Bundle)
                return "";

            var contents = new List<string>();
            foreach (var item in BundleItems)
            {
                string itemText = item.type switch
                {
                    "gold" => $"{item.quantity} Gold",
                    "gem" => $"{item.quantity} Gems",
                    "equipment" => GetEquipmentName(item.id),
                    _ => $"{item.quantity} {item.type}"
                };
                contents.Add(itemText);
            }

            return string.Join("\n", contents);
        }

        private string GetEquipmentName(int equipmentId)
        {
            if (equipmentId == -1) return "Random Equipment";
            
            var equipment = OctoberStudio.Equipment.EquipmentDatabase.Instance?.GetEquipmentByGlobalId(equipmentId);
            return equipment?.Name ?? $"Equipment #{equipmentId}";
        }

        /// <summary>
        /// Get gacha rates as formatted text
        /// </summary>
        public string GetGachaRatesText()
        {
            if (ItemType != ShopItemType.Gacha)
                return "";

            var rates = new List<string>();
            int totalWeight = GachaPool.Sum(p => p.weight);
            
            foreach (var pool in GachaPool)
            {
                float percentage = (float)pool.weight / totalWeight * 100f;
                string displayName = string.IsNullOrEmpty(pool.type) ? pool.rarity : pool.type;
                rates.Add($"{displayName}: {percentage:F1}%");
            }

            return string.Join("\n", rates);
        }

        /// <summary>
        /// Get special offer text
        /// </summary>
        public string GetSpecialOfferText()
        {
            if (ItemType == ShopItemType.Bundle && BundleItems.Count > 1)
                return $"Save {CalculateBundleDiscount():F0}%!";
                
            if (ItemType == ShopItemType.Gacha && GachaCount == 10)
                return "10% Discount!";
                
            if (ResetTimeHours > 0 && ResetTimeHours <= 24)
                return "Limited Time!";
                
            return "";
        }

        private float CalculateBundleDiscount()
        {
            // Calculate potential discount compared to individual items
            // This is a placeholder calculation
            return UnityEngine.Random.Range(10f, 30f);
        }

        public override string ToString()
        {
            return $"{Name} ({ItemType}) - {GetPriceText()} {PriceType}";
        }
    }
}