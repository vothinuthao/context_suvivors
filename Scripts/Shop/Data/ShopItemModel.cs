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

        // Parsed bundle items
        [CsvIgnore]
        public List<BundleItem> BundleItems
        {
            get
            {
                if (string.IsNullOrEmpty(BundleItemsJson))
                    return new List<BundleItem>();

                try
                {
                    return JsonUtility.FromJson<BundleItemList>("{\"items\":" + BundleItemsJson + "}").items ?? new List<BundleItem>();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ShopItemModel] Failed to parse bundle items for {Name}: {ex.Message}");
                    return new List<BundleItem>();
                }
            }
        }

        // Parsed gacha pool
        [CsvIgnore]
        public List<GachaPool> GachaPool
        {
            get
            {
                if (string.IsNullOrEmpty(GachaPoolJson))
                    return new List<GachaPool>();

                try
                {
                    return JsonUtility.FromJson<GachaPoolList>("{\"pools\":" + GachaPoolJson + "}").pools ?? new List<GachaPool>();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ShopItemModel] Failed to parse gacha pool for {Name}: {ex.Message}");
                    return new List<GachaPool>();
                }
            }
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
        /// Validate shop item data
        /// </summary>
        public bool ValidateData()
        {
            bool isValid = ID >= 0 && 
                          !string.IsNullOrEmpty(Name) && 
                          !string.IsNullOrEmpty(IconName) &&
                          PriceAmount >= 0;

            // Validate bundle items if bundle type
            if (ItemType == ShopItemType.Bundle)
            {
                isValid &= BundleItems.Count > 0;
            }

            // Validate gacha pool if gacha type
            if (ItemType == ShopItemType.Gacha)
            {
                isValid &= GachaPool.Count > 0 && GachaCount > 0;
            }

            return isValid;
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