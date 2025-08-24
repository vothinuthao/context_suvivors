using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using OctoberStudio.Save;
using OctoberStudio.Equipment;
using OctoberStudio.Audio;
using System.Collections;

namespace OctoberStudio.Shop
{
    public class ShopManager : MonoBehaviour
    {
        private static ShopManager instance;
        public static ShopManager Instance => instance;

        [Header("Configuration")]
        [SerializeField] private GachaConfig gachaConfig;
        
        [Header("Debug Info")]
        [SerializeField, ReadOnly] private bool databaseReady = false;
        [SerializeField, ReadOnly] private int totalShopItems = 0;

        // Save data reference
        private ShopSave shopSave;

        // Events
        public UnityEvent<ShopItemModel> OnItemPurchased;
        public UnityEvent<List<RewardData>> OnRewardsReceived;
        public UnityEvent<string> OnPurchaseError;
        public UnityEvent OnShopRefreshed;

        // Audio hashes
        private static readonly int PURCHASE_SUCCESS_HASH = "Purchase_Success".GetHashCode();
        private static readonly int PURCHASE_ERROR_HASH = "Purchase_Error".GetHashCode();
        private static readonly int GACHA_START_HASH = "Gacha_Start".GetHashCode();
        private static readonly int GACHA_REVEAL_HASH = "Gacha_Reveal".GetHashCode();
        private static readonly int EPIC_ITEM_HASH = "Epic_Item".GetHashCode();
        private static readonly int LEGENDARY_ITEM_HASH = "Legendary_Item".GetHashCode();

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            shopSave = GameController.SaveManager.GetSave<ShopSave>("Shop");
            
            if (ShopDatabase.Instance != null)
            {
                ShopDatabase.Instance.OnDataLoaded += OnDatabaseLoaded;
                ShopDatabase.Instance.OnLoadingError += OnDatabaseError;
                
                if (ShopDatabase.Instance.IsDataLoaded)
                {
                    OnDatabaseLoaded();
                }
            }

            // Check for shop refreshes
            CheckAndRefreshShop();
        }

        private void OnDestroy()
        {
            if (ShopDatabase.Instance != null)
            {
                ShopDatabase.Instance.OnDataLoaded -= OnDatabaseLoaded;
                ShopDatabase.Instance.OnLoadingError -= OnDatabaseError;
            }
        }

        private void OnDatabaseLoaded()
        {
            databaseReady = true;
            totalShopItems = ShopDatabase.Instance.TotalShopItemsCount;
            Debug.Log($"[ShopManager] Shop database loaded with {totalShopItems} items");
        }

        private void OnDatabaseError(string error)
        {
            databaseReady = false;
            Debug.LogError($"[ShopManager] Shop database error: {error}");
        }

        /// <summary>
        /// Check if shop needs refresh and refresh if necessary
        /// </summary>
        public void CheckAndRefreshShop()
        {
            if (shopSave.ShouldRefreshDailyItems())
            {
                RefreshShop();
            }
        }

        /// <summary>
        /// Refresh shop (reset daily limits, etc.)
        /// </summary>
        public void RefreshShop()
        {
            if (!databaseReady) return;

            var itemsToRefresh = ShopDatabase.Instance.GetAllItems()
                .Where(item => item.ResetTimeHours > 0 && shopSave.ShouldResetPurchases(item));

            foreach (var item in itemsToRefresh)
            {
                shopSave.ResetPurchaseCount(item.ID);
            }

            shopSave.UpdateLastRefreshTime();
            OnShopRefreshed?.Invoke();

            Debug.Log("[ShopManager] Shop refreshed");
        }

        /// <summary>
        /// Attempt to purchase an item
        /// </summary>
        public bool PurchaseItem(int itemId)
        {
            if (!databaseReady)
            {
                OnPurchaseError?.Invoke("Shop not ready");
                return false;
            }

            var item = ShopDatabase.Instance.GetItemById(itemId);
            if (item == null)
            {
                OnPurchaseError?.Invoke("Item not found");
                return false;
            }

            return PurchaseItem(item);
        }

        /// <summary>
        /// Attempt to purchase an item
        /// </summary>
        public bool PurchaseItem(ShopItemModel item)
        {
            // Check if item can be purchased
            var purchaseCount = shopSave.GetPurchaseCount(item.ID);
            if (!item.CanPurchase(purchaseCount))
            {
                OnPurchaseError?.Invoke("Purchase limit reached");
                return false;
            }

            // Check if player can afford
            if (!shopSave.CanAfford(item))
            {
                OnPurchaseError?.Invoke("Insufficient funds");
                return false;
            }

            // Process purchase based on item type
            bool success = false;
            switch (item.ItemType)
            {
                case ShopItemType.Bundle:
                    success = PurchaseBundle(item);
                    break;
                case ShopItemType.Gacha:
                    success = PurchaseGacha(item);
                    break;
                case ShopItemType.Gold:
                    success = PurchaseGold(item);
                    break;
                case ShopItemType.Gem:
                    success = PurchaseGems(item);
                    break;
                case ShopItemType.Item:
                    success = PurchaseDirectItem(item);
                    break;
            }

            if (success)
            {
                // Deduct currency
                shopSave.SpendCurrency(item);
                
                // Record purchase
                shopSave.AddPurchase(item.ID);
                
                // Trigger events
                OnItemPurchased?.Invoke(item);
                
                // Play success sound
                GameController.AudioManager?.PlaySound(PURCHASE_SUCCESS_HASH);
                
                Debug.Log($"[ShopManager] Successfully purchased: {item.Name}");
            }
            else
            {
                GameController.AudioManager?.PlaySound(PURCHASE_ERROR_HASH);
            }

            return success;
        }

        /// <summary>
        /// Purchase bundle item
        /// </summary>
        private bool PurchaseBundle(ShopItemModel item)
        {
            var rewards = new List<RewardData>();

            foreach (var bundleItem in item.BundleItems)
            {
                var reward = ProcessBundleItem(bundleItem);
                if (reward != null)
                {
                    rewards.Add(reward);
                }
            }

            if (rewards.Count > 0)
            {
                OnRewardsReceived?.Invoke(rewards);
                return true;
            }

            OnPurchaseError?.Invoke("Failed to process bundle items");
            return false;
        }

        /// <summary>
        /// Process individual bundle item
        /// </summary>
        private RewardData ProcessBundleItem(BundleItem bundleItem)
        {
            switch (bundleItem.type.ToLower())
            {
                case "gold":
                    shopSave.AddGold(bundleItem.quantity);
                    return new RewardData
                    {
                        Type = RewardType.Gold,
                        Quantity = bundleItem.quantity,
                        DisplayName = $"{bundleItem.quantity} Gold"
                    };

                case "gem":
                    shopSave.AddGems(bundleItem.quantity);
                    return new RewardData
                    {
                        Type = RewardType.Gems,
                        Quantity = bundleItem.quantity,
                        DisplayName = $"{bundleItem.quantity} Gems"
                    };

                case "equipment":
                    if (bundleItem.id == -1) // Random equipment
                    {
                        var randomEquipment = GetRandomEquipment();
                        if (randomEquipment != null && EquipmentManager.Instance != null)
                        {
                            var addedItem = EquipmentManager.Instance.AddEquipmentToInventory(randomEquipment.ID, 1);
                            return new RewardData
                            {
                                Type = RewardType.Equipment,
                                EquipmentData = randomEquipment,
                                EquipmentUID = addedItem?.uid,
                                DisplayName = randomEquipment.Name,
                                Rarity = randomEquipment.Rarity
                            };
                        }
                    }
                    else
                    {
                        var equipment = EquipmentDatabase.Instance?.GetEquipmentByGlobalId(bundleItem.id);
                        if (equipment != null && EquipmentManager.Instance != null)
                        {
                            var addedItem = EquipmentManager.Instance.AddEquipmentToInventory(bundleItem.id, 1);
                            return new RewardData
                            {
                                Type = RewardType.Equipment,
                                EquipmentData = equipment,
                                EquipmentUID = addedItem?.uid,
                                DisplayName = equipment.Name,
                                Rarity = equipment.Rarity
                            };
                        }
                    }
                    break;
            }

            return null;
        }

        /// <summary>
        /// Purchase gacha item
        /// </summary>
        private bool PurchaseGacha(ShopItemModel item)
        {
            string gachaType = item.Rarity == EquipmentRarity.Epic ? "epic" : "rare";
            
            // Record gacha pull
            shopSave.AddGachaPull(gachaType, item.GachaCount);
            
            // Get gacha results
            var rewards = PerformGacha(item);
            
            if (rewards.Count > 0)
            {
                OnRewardsReceived?.Invoke(rewards);
                return true;
            }

            OnPurchaseError?.Invoke("Gacha failed to produce results");
            return false;
        }

        /// <summary>
        /// Perform gacha pull
        /// </summary>
        private List<RewardData> PerformGacha(ShopItemModel gachaItem)
        {
            var rewards = new List<RewardData>();
            string gachaType = gachaItem.Rarity == EquipmentRarity.Epic ? "epic" : "rare";
            
            for (int i = 0; i < gachaItem.GachaCount; i++)
            {
                var reward = PerformSingleGachaPull(gachaItem, gachaType, i == gachaItem.GachaCount - 1);
                if (reward != null)
                {
                    rewards.Add(reward);
                }
            }

            return rewards;
        }

        /// <summary>
        /// Perform single gacha pull
        /// </summary>
        private RewardData PerformSingleGachaPull(ShopItemModel gachaItem, string gachaType, bool isLastPull)
        {
            var currentPity = shopSave.GetCurrentPity(gachaType);
            EquipmentRarity forcedRarity = EquipmentRarity.Common;
            bool shouldForcePity = false;

            // Check pity system
            if (gachaConfig != null && gachaConfig.EnablePitySystem)
            {
                if (gachaType == "epic" && currentPity >= gachaConfig.PityCountForEpic)
                {
                    forcedRarity = EquipmentRarity.Epic;
                    shouldForcePity = true;
                }
                else if (gachaType == "rare" && currentPity >= gachaConfig.PityCountForEpic)
                {
                    forcedRarity = EquipmentRarity.Rare;
                    shouldForcePity = true;
                }
            }

            // Get equipment based on gacha pool or pity
            EquipmentModel equipment;
            if (shouldForcePity)
            {
                equipment = GetEquipmentByRarity(forcedRarity);
                shopSave.ResetGachaPity(gachaType);
            }
            else
            {
                equipment = GetGachaResult(gachaItem.GachaPool);
            }

            if (equipment != null && EquipmentManager.Instance != null)
            {
                var addedItem = EquipmentManager.Instance.AddEquipmentToInventory(equipment.ID, 1);
                
                // Reset pity if epic or legendary
                if (equipment.Rarity >= EquipmentRarity.Epic && gachaConfig.ResetPityOnEpic)
                {
                    shopSave.ResetGachaPity(gachaType);
                }

                return new RewardData
                {
                    Type = RewardType.Equipment,
                    EquipmentData = equipment,
                    EquipmentUID = addedItem?.uid,
                    DisplayName = equipment.Name,
                    Rarity = equipment.Rarity,
                    IsFromGacha = true
                };
            }

            return null;
        }

        /// <summary>
        /// Get gacha result based on pool
        /// </summary>
        private EquipmentModel GetGachaResult(List<GachaPool> gachaPool)
        {
            if (gachaPool.Count == 0) return null;

            // Calculate total weight
            int totalWeight = gachaPool.Sum(p => p.weight);
            int randomValue = Random.Range(0, totalWeight);

            // Select based on weight
            int currentWeight = 0;
            foreach (var pool in gachaPool)
            {
                currentWeight += pool.weight;
                if (randomValue < currentWeight)
                {
                    if (System.Enum.TryParse<EquipmentRarity>(pool.rarity, true, out var rarity))
                    {
                        return GetEquipmentByRarity(rarity);
                    }
                }
            }

            // Fallback to common
            return GetEquipmentByRarity(EquipmentRarity.Common);
        }

        /// <summary>
        /// Purchase gold directly
        /// </summary>
        private bool PurchaseGold(ShopItemModel item)
        {
            int goldAmount = GetGoldAmountFromItem(item);
            shopSave.AddGold(goldAmount);

            var rewards = new List<RewardData>
            {
                new RewardData
                {
                    Type = RewardType.Gold,
                    Quantity = goldAmount,
                    DisplayName = $"{goldAmount} Gold"
                }
            };

            OnRewardsReceived?.Invoke(rewards);
            return true;
        }

        /// <summary>
        /// Get gold amount from gold purchase item
        /// </summary>
        private int GetGoldAmountFromItem(ShopItemModel item)
        {
            // This should be configured in the CSV data
            // For now, we'll use some multipliers based on price
            return item.Name.ToLower() switch
            {
                var name when name.Contains("small") => 1000,
                var name when name.Contains("medium") => 5000,
                var name when name.Contains("large") => 15000,
                var name when name.Contains("huge") => 50000,
                var name when name.Contains("mega") => 150000,
                var name when name.Contains("ultimate") => 500000,
                _ => (int)(item.PriceAmount * 50) // Default multiplier
            };
        }

        /// <summary>
        /// Purchase gems directly
        /// </summary>
        private bool PurchaseGems(ShopItemModel item)
        {
            int gemAmount = GetGemAmountFromItem(item);
            shopSave.AddGems(gemAmount);

            var rewards = new List<RewardData>
            {
                new RewardData
                {
                    Type = RewardType.Gems,
                    Quantity = gemAmount,
                    DisplayName = $"{gemAmount} Gems"
                }
            };

            OnRewardsReceived?.Invoke(rewards);
            return true;
        }

        /// <summary>
        /// Get gem amount from gem purchase item
        /// </summary>
        private int GetGemAmountFromItem(ShopItemModel item)
        {
            return item.Name.ToLower() switch
            {
                var name when name.Contains("small") => 100,
                var name when name.Contains("medium") => 550,
                var name when name.Contains("large") => 1400,
                var name when name.Contains("huge") => 3800,
                var name when name.Contains("mega") => 10500,
                _ => 100 // Default
            };
        }

        /// <summary>
        /// Purchase direct item (not used in current system)
        /// </summary>
        private bool PurchaseDirectItem(ShopItemModel item)
        {
            // Not implemented for current shop system
            OnPurchaseError?.Invoke("Direct item purchase not implemented");
            return false;
        }

        /// <summary>
        /// Get random equipment
        /// </summary>
        private EquipmentModel GetRandomEquipment()
        {
            if (!EquipmentDatabase.Instance.IsDataLoaded) return null;

            var allEquipment = EquipmentDatabase.Instance.GetAllEquipment();
            if (allEquipment.Length == 0) return null;

            int randomIndex = Random.Range(0, allEquipment.Length);
            return allEquipment[randomIndex];
        }

        /// <summary>
        /// Get equipment by rarity
        /// </summary>
        private EquipmentModel GetEquipmentByRarity(EquipmentRarity rarity)
        {
            if (!EquipmentDatabase.Instance.IsDataLoaded) return null;

            var equipmentByRarity = EquipmentDatabase.Instance.GetEquipmentByRarity(rarity);
            if (equipmentByRarity.Length == 0) return null;

            int randomIndex = Random.Range(0, equipmentByRarity.Length);
            return equipmentByRarity[randomIndex];
        }

        /// <summary>
        /// Check if player can purchase item
        /// </summary>
        public bool CanPurchaseItem(int itemId)
        {
            if (!databaseReady) return false;

            var item = ShopDatabase.Instance.GetItemById(itemId);
            if (item == null) return false;

            var purchaseCount = shopSave.GetPurchaseCount(item.ID);
            return item.CanPurchase(purchaseCount) && shopSave.CanAfford(item);
        }

        /// <summary>
        /// Get purchase count for item
        /// </summary>
        public int GetPurchaseCount(int itemId)
        {
            return shopSave.GetPurchaseCount(itemId);
        }

        /// <summary>
        /// Get current pity count for gacha type
        /// </summary>
        public int GetGachaPity(string gachaType)
        {
            return shopSave.GetCurrentPity(gachaType);
        }

        /// <summary>
        /// Get player's current gems - FIXED: Now using CurrencySave
        /// </summary>
        public int GetCurrentGems()
        {
            var gemCurrency = GameController.SaveManager?.GetSave<CurrencySave>("gem");
            return gemCurrency?.Amount ?? 0;
        }

        /// <summary>
        /// Get player's current gold - FIXED: Now using CurrencySave
        /// </summary>
        public int GetCurrentGold()
        {
            var goldCurrency = GameController.SaveManager?.GetSave<CurrencySave>("gold");
            return goldCurrency?.Amount ?? 0;
        }
    }

    /// <summary>
    /// Data structure for rewards
    /// </summary>
    [System.Serializable]
    public class RewardData
    {
        public RewardType Type;
        public int Quantity;
        public string DisplayName;
        public EquipmentRarity Rarity;
        public EquipmentModel EquipmentData;
        public string EquipmentUID;
        public bool IsFromGacha;
    }

    public enum RewardType
    {
        Gold,
        Gems,
        Equipment,
        Character
    }
}