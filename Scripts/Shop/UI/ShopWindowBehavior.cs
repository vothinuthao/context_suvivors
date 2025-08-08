using System.Collections.Generic;
using System.Linq;
using OctoberStudio.Audio;
using OctoberStudio.Easing;
using OctoberStudio.Input;
using OctoberStudio.Extensions;
using OctoberStudio.Shop;
using OctoberStudio.Shop.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    public class ShopWindowBehavior : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private ScrollRect scrollView;

        [Header("Currency Display")]
        [SerializeField] private ScalingLabelBehavior coinsLabel;
        [SerializeField] private ScalingLabelBehavior gemsLabel;

        [Header("Shop Sections")]
        [SerializeField] private Transform bundlesSection;
        [SerializeField] private Transform gachaSection;
        [SerializeField] private Transform characterGachaSection;
        [SerializeField] private Transform goldSection;

        [Header("Shop Item Prefabs")]
        [SerializeField] private GameObject bundleItemPrefab;
        [SerializeField] private GameObject gachaItemPrefab;
        [SerializeField] private GameObject goldItemPrefab;
        [SerializeField] private GameObject characterGachaItemPrefab;

        [Header("Gacha Layout")] 
        [SerializeField] private Transform rareGachaContainer;
        [SerializeField] private Transform epicGachaContainer;

        [Header("Gold Layout")]
        [SerializeField] private GridLayoutGroup goldGrid;

        [Header("Popups")]
        [SerializeField] private RewardPopupBehavior rewardPopup;
        [SerializeField] private GachaAnimationBehavior gachaAnimation;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // State
        private List<ShopItemBehavior> allShopItems = new List<ShopItemBehavior>();
        private bool isShopReady = false;

        // Events
        public event UnityAction OnBackPressed;

        private void Awake()
        {
            // backButton.onClick.AddListener(OnBackButtonClicked);
        }

        private void Start()
        {
            // Subscribe to shop manager events
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnItemPurchased.AddListener(OnItemPurchased);
                ShopManager.Instance.OnRewardsReceived.AddListener(OnRewardsReceived);
                ShopManager.Instance.OnPurchaseError.AddListener(OnPurchaseError);
                ShopManager.Instance.OnShopRefreshed.AddListener(OnShopRefreshed);
            }

            // Subscribe to gacha animation events  
            if (gachaAnimation != null)
            {
                gachaAnimation.OnAnimationComplete.AddListener(OnGachaAnimationComplete);
                gachaAnimation.OnAnimationSkipped.AddListener(OnGachaAnimationSkipped);
            }

            // Subscribe to reward popup events
            if (rewardPopup != null)
            {
                rewardPopup.OnPopupClosed.AddListener(OnRewardPopupClosed);
            }
        }

        public void Init( )
        {
        }

        public void Open()
        {
            gameObject.SetActive(true);

            // Initialize shop if database is ready
            if (ShopDatabase.Instance != null && ShopDatabase.Instance.IsDataLoaded)
            {
                InitializeShop();
            }
            else
            {
                // Wait for database to load
                if (ShopDatabase.Instance != null)
                {
                    ShopDatabase.Instance.OnDataLoaded += OnShopDatabaseLoaded;
                }
            }

            // Update currency display
            UpdateCurrencyDisplay();

            // Setup input handling
            EasingManager.DoNextFrame(() => {
                GameController.InputManager.InputAsset.UI.Back.performed += OnBackInputClicked;
            });

            GameController.InputManager.onInputChanged += OnInputChanged;

            LogDebug("Shop window opened");
        }

        public void Close()
        {
            gameObject.SetActive(false);

            // Cleanup input handling
            GameController.InputManager.InputAsset.UI.Back.performed -= OnBackInputClicked;
            GameController.InputManager.onInputChanged -= OnInputChanged;

            // Unsubscribe from database events
            if (ShopDatabase.Instance != null)
            {
                ShopDatabase.Instance.OnDataLoaded -= OnShopDatabaseLoaded;
            }

            LogDebug("Shop window closed");
        }

        /// <summary>
        /// Initialize shop layout and items
        /// </summary>
        private void InitializeShop()
        {
            if (isShopReady) return;

            LogDebug("Initializing shop...");

            // Clear existing items
            ClearAllShopItems();

            // Create shop sections in order
            CreateBundleItems();
            CreateGachaItems();
            CreateCharacterGachaItems();
            // CreateGoldItems();

            isShopReady = true;
            LogDebug($"Shop initialized with {allShopItems.Count} items");
        }

        /// <summary>
        /// Create bundle items (Row 1)
        /// </summary>
        private void CreateBundleItems()
        {
            var bundleItems = ShopDatabase.Instance.GetBundleItems();
            LogDebug($"Creating {bundleItems.Length} bundle items");

            foreach (var item in bundleItems)
            {
                CreateShopItem(item, bundleItemPrefab, bundlesSection);
            }
        }

        /// <summary>
        /// Create gacha items (Row 2 - 2 columns)
        /// </summary>
        private void CreateGachaItems()
        {
            var gachaItems = ShopDatabase.Instance.GetGachaItems()
                .Where(g => g.ItemType == ShopItemType.Gacha && 
                           (g.Rarity == EquipmentRarity.Rare || 
                            g.Rarity == EquipmentRarity.Epic))
                .ToArray();

            LogDebug($"Creating {gachaItems.Length} gacha items");

            // Separate rare and epic gacha
            var rareGacha = gachaItems.Where(g => g.Rarity == EquipmentRarity.Rare).ToArray();
            var epicGacha = gachaItems.Where(g => g.Rarity == EquipmentRarity.Epic).ToArray();

            // Create rare gacha items (left column)
            foreach (var item in rareGacha)
            {
                CreateShopItem(item, gachaItemPrefab, rareGachaContainer);
            }

            // Create epic gacha items (right column)
            foreach (var item in epicGacha)
            {
                CreateShopItem(item, gachaItemPrefab, epicGachaContainer);
            }
        }

        /// <summary>
        /// Create character gacha items (Row 3)
        /// </summary>
        private void CreateCharacterGachaItems()
        {
            var characterGachaItems = ShopDatabase.Instance.GetGachaItems()
                .Where(g => g.Name.ToLower().Contains("character"))
                .ToArray();

            LogDebug($"Creating {characterGachaItems.Length} character gacha items");

            foreach (var item in characterGachaItems)
            {
                CreateShopItem(item, characterGachaItemPrefab, characterGachaSection);
            }
        }

        /// <summary>
        /// Create gold purchase items (Row 4 - 3x2 grid)
        /// </summary>
        private void CreateGoldItems()
        {
            var goldItems = ShopDatabase.Instance.GetGoldItems();
            LogDebug($"Creating {goldItems.Length} gold items");

            // Ensure grid is set to 3 columns
            if (goldGrid != null)
            {
                goldGrid.constraintCount = 3;
            }

            foreach (var item in goldItems)
            {
                CreateShopItem(item, goldItemPrefab, goldSection);
            }
        }

        /// <summary>
        /// Create individual shop item
        /// </summary>
        private ShopItemBehavior CreateShopItem(ShopItemModel itemData, GameObject prefab, Transform parent)
        {
            var itemObject = prefab.SpawnWithSetup<ShopItemBehavior>(
                parent,
                behavior => {
                    behavior.Init(itemData);
                    behavior.OnItemClicked.AddListener(OnShopItemClicked);
                }
            );

            allShopItems.Add(itemObject);
            return itemObject;
        }

        /// <summary>
        /// Clear all shop items
        /// </summary>
        private void ClearAllShopItems()
        {
            foreach (var item in allShopItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            allShopItems.Clear();
        }

        /// <summary>
        /// Handle shop item click
        /// </summary>
        private void OnShopItemClicked(ShopItemModel item)
        {
            LogDebug($"Shop item clicked: {item.Name}");

            // Check if this is a gacha item
            if (item.ItemType == ShopItemType.Gacha)
            {
                HandleGachaPurchase(item);
            }
            else
            {
                HandleRegularPurchase(item);
            }
        }

        /// <summary>
        /// Handle gacha purchase with animation
        /// </summary>
        private void HandleGachaPurchase(ShopItemModel item)
        {
            if (ShopManager.Instance == null) return;

            // Attempt purchase
            bool success = ShopManager.Instance.PurchaseItem(item);
            
            if (!success)
            {
                LogDebug($"Gacha purchase failed for: {item.Name}");
                return;
            }

            LogDebug($"Gacha purchase successful: {item.Name}");

            // Purchase successful, but don't show rewards yet
            // The rewards will be shown after gacha animation completes
        }

        /// <summary>
        /// Handle regular (non-gacha) purchase
        /// </summary>
        private void HandleRegularPurchase(ShopItemModel item)
        {
            if (ShopManager.Instance == null) return;

            bool success = ShopManager.Instance.PurchaseItem(item);
            
            if (success)
            {
                LogDebug($"Regular purchase successful: {item.Name}");
                // Rewards will be shown via OnRewardsReceived callback
            }
            else
            {
                LogDebug($"Regular purchase failed: {item.Name}");
            }
        }

        /// <summary>
        /// Update currency display
        /// </summary>
        private void UpdateCurrencyDisplay()
        {
            if (coinsLabel != null)
            {
                int currentCoins = ShopManager.Instance?.GetCurrentGold() ?? 0;
                coinsLabel.SetAmount(currentCoins);
            }

            if (gemsLabel != null)
            {
                int currentGems = ShopManager.Instance?.GetCurrentGems() ?? 0;
                gemsLabel.SetAmount(currentGems);
            }
        }

        /// <summary>
        /// Refresh all shop items
        /// </summary>
        private void RefreshAllShopItems()
        {
            foreach (var item in allShopItems)
            {
                if (item != null)
                {
                    item.Refresh();
                }
            }

            UpdateCurrencyDisplay();
        }

        // ===== EVENT HANDLERS =====

        private void OnShopDatabaseLoaded()
        {
            LogDebug("Shop database loaded, initializing shop");
            InitializeShop();
        }

        private void OnItemPurchased(ShopItemModel item)
        {
            LogDebug($"Item purchased: {item.Name}");
            
            // Update currency display
            UpdateCurrencyDisplay();
            
            // Refresh shop items to update availability
            RefreshAllShopItems();
        }

        private void OnRewardsReceived(List<RewardData> rewards)
        {
            LogDebug($"Rewards received: {rewards.Count} items");

            // Check if any reward is from gacha
            bool hasGachaReward = rewards.Any(r => r.IsFromGacha);

            if (hasGachaReward && gachaAnimation != null)
            {
                // Show gacha animation
                bool isMultiPull = rewards.Count > 1;
                gachaAnimation.StartGachaAnimation(rewards, isMultiPull);
            }
            else if (rewardPopup != null)
            {
                // Show regular reward popup
                string title = rewards.Count > 1 ? "Items Received!" : "Purchase Complete!";
                rewardPopup.ShowRewards(rewards, title);
            }
        }

        private void OnPurchaseError(string error)
        {
            LogDebug($"Purchase error: {error}");
            
            // TODO: Show error popup or notification
            Debug.LogWarning($"[ShopWindow] Purchase error: {error}");
        }

        private void OnShopRefreshed()
        {
            LogDebug("Shop refreshed");
            RefreshAllShopItems();
        }

        private void OnGachaAnimationComplete(List<RewardData> rewards)
        {
            LogDebug("Gacha animation completed");
            
            // Show reward popup after gacha animation
            if (rewardPopup != null)
            {
                string title = rewards.Count > 1 ? "Gacha Results!" : "Item Obtained!";
                rewardPopup.ShowRewards(rewards, title);
            }
        }

        private void OnGachaAnimationSkipped()
        {
            LogDebug("Gacha animation skipped");
        }

        private void OnRewardPopupClosed()
        {
            LogDebug("Reward popup closed");
            
            // Update currency display after popup closes
            UpdateCurrencyDisplay();
        }

        private void OnBackButtonClicked()
        {
            GameController.AudioManager?.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            OnBackPressed?.Invoke();
        }

        private void OnBackInputClicked(InputAction.CallbackContext context)
        {
            OnBackButtonClicked();
        }

        private void OnInputChanged(InputType prevInput, InputType inputType)
        {
            if (prevInput == InputType.UIJoystick)
            {
            }
        }

        // ===== UTILITY METHODS =====

        /// <summary>
        /// Get shop item behavior by ID
        /// </summary>
        public ShopItemBehavior GetShopItemById(int itemId)
        {
            return allShopItems.FirstOrDefault(item => 
                item.GetItemData()?.ID == itemId);
        }

        /// <summary>
        /// Refresh specific shop item
        /// </summary>
        public void RefreshShopItem(int itemId)
        {
            var shopItem = GetShopItemById(itemId);
            shopItem?.Refresh();
        }

        /// <summary>
        /// Get all shop items of specific type
        /// </summary>
        public List<ShopItemBehavior> GetShopItemsByType(ShopItemType itemType)
        {
            return allShopItems.Where(item => 
                item.GetItemData()?.ItemType == itemType).ToList();
        }

        /// <summary>
        /// Force refresh shop (reload from database)
        /// </summary>
        [ContextMenu("Force Refresh Shop")]
        public void ForceRefreshShop()
        {
            isShopReady = false;
            
            if (ShopDatabase.Instance != null)
            {
                ShopDatabase.Instance.ReloadShopData();
            }
        }

        /// <summary>
        /// Debug logging
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[ShopWindow] {message}");
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from all events
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnItemPurchased.RemoveListener(OnItemPurchased);
                ShopManager.Instance.OnRewardsReceived.RemoveListener(OnRewardsReceived);
                ShopManager.Instance.OnPurchaseError.RemoveListener(OnPurchaseError);
                ShopManager.Instance.OnShopRefreshed.RemoveListener(OnShopRefreshed);
            }

            if (gachaAnimation != null)
            {
                gachaAnimation.OnAnimationComplete.RemoveListener(OnGachaAnimationComplete);
                gachaAnimation.OnAnimationSkipped.RemoveListener(OnGachaAnimationSkipped);
            }

            if (rewardPopup != null)
            {
                rewardPopup.OnPopupClosed.RemoveListener(OnRewardPopupClosed);
            }

            if (ShopDatabase.Instance != null)
            {
                ShopDatabase.Instance.OnDataLoaded -= OnShopDatabaseLoaded;
            }

            if (GameController.InputManager != null)
            {
                GameController.InputManager.InputAsset.UI.Back.performed -= OnBackInputClicked;
                GameController.InputManager.onInputChanged -= OnInputChanged;
            }

            // Clean up shop items
            foreach (var item in allShopItems)
            {
                if (item != null && item.OnItemClicked != null)
                {
                    item.OnItemClicked.RemoveAllListeners();
                }
            }
        }

        // ===== PUBLIC API =====

        /// <summary>
        /// Check if shop is ready to use
        /// </summary>
        public bool IsShopReady => isShopReady;

        /// <summary>
        /// Get current number of shop items
        /// </summary>
        public int GetShopItemCount() => allShopItems.Count;

        /// <summary>
        /// Manually trigger currency update
        /// </summary>
        public void UpdateCurrency() => UpdateCurrencyDisplay();
    }
}