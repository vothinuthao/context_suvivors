using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using OctoberStudio.Audio;

namespace OctoberStudio.Shop.UI
{
    public class ShopItemBehavior : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemDescriptionText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Image priceIcon;
        [SerializeField] private Button purchaseButton;
        
        [Header("Bundle/Gacha Specific")]
        [SerializeField] private TMP_Text specialOfferText;
        [SerializeField] private TMP_Text contentsText;
        [SerializeField] private GameObject limitIndicator;
        [SerializeField] private TMP_Text limitText;
        [SerializeField] private GameObject soldOutOverlay;
        
        [Header("Featured Item")]
        [SerializeField] private GameObject featuredBadge;
        [SerializeField] private GameObject discountBadge;
        [SerializeField] private TMP_Text discountText;
        
        [Header("Timer (for limited items)")]
        [SerializeField] private GameObject timerPanel;
        [SerializeField] private TMP_Text timerText;

        // Data
        private ShopItemModel itemData;
        private int currentPurchases;
        
        // Events
        public UnityEvent<ShopItemModel> OnItemClicked;

        private void Awake()
        {
            if (purchaseButton != null)
            {
                purchaseButton.onClick.AddListener(OnPurchaseClicked);
            }
        }

        /// <summary>
        /// Initialize shop item display
        /// </summary>
        public void Init(ShopItemModel item)
        {
            itemData = item;
            UpdateDisplay();
        }

        /// <summary>
        /// Update the display with current data
        /// </summary>
        public void UpdateDisplay()
        {
            if (itemData == null) return;

            // Get current purchase count
            currentPurchases = ShopManager.Instance?.GetPurchaseCount(itemData.ID) ?? 0;

            // Update basic info
            SetupBasicInfo();
            
            // Update price info
            SetupPriceInfo();
            
            // Update special elements
            SetupSpecialElements();
            
            // Update availability
            UpdateAvailability();
        }

        /// <summary>
        /// Setup basic item information
        /// </summary>
        private void SetupBasicInfo()
        {
            // Item name
            if (itemNameText != null)
            {
                itemNameText.text = itemData.Name;
            }

            // Item description
            if (itemDescriptionText != null)
            {
                itemDescriptionText.text = itemData.Description;
            }

            // Item icon
            if (itemIcon != null)
            {
                itemIcon.sprite = itemData.GetIcon();
            }

            // Rarity border
            if (rarityBorder != null)
            {
                rarityBorder.color = itemData.GetRarityColor();
            }

            // Featured badge
            if (featuredBadge != null)
            {
                featuredBadge.SetActive(itemData.IsFeatured);
            }
        }

        /// <summary>
        /// Setup price information
        /// </summary>
        private void SetupPriceInfo()
        {
            // Price text
            if (priceText != null)
            {
                priceText.text = itemData.GetPriceText();
            }

            // Price icon
            if (priceIcon != null)
            {
                priceIcon.sprite = itemData.GetPriceIcon();
            }
        }

        /// <summary>
        /// Setup special elements based on item type
        /// </summary>
        private void SetupSpecialElements()
        {
            // Special offer text
            if (specialOfferText != null)
            {
                string offerText = itemData.GetSpecialOfferText();
                specialOfferText.text = offerText;
                specialOfferText.gameObject.SetActive(!string.IsNullOrEmpty(offerText));
            }

            // Contents text (for bundles and gacha)
            if (contentsText != null)
            {
                string contents = "";
                switch (itemData.ItemType)
                {
                    case ShopItemType.Bundle:
                        contents = itemData.GetBundleContentsText();
                        break;
                    case ShopItemType.Gacha:
                        contents = itemData.GetGachaRatesText();
                        break;
                }
                
                contentsText.text = contents;
                contentsText.gameObject.SetActive(!string.IsNullOrEmpty(contents));
            }

            // Discount badge
            SetupDiscountBadge();

            // Purchase limit indicator
            SetupLimitIndicator();

            // Timer for time-limited items
            SetupTimer();
        }

        /// <summary>
        /// Setup discount badge
        /// </summary>
        private void SetupDiscountBadge()
        {
            if (discountBadge == null) return;

            bool hasDiscount = false;
            string discountString = "";

            // Check for gacha 10x discount
            if (itemData.ItemType == ShopItemType.Gacha && itemData.GachaCount == 10)
            {
                hasDiscount = true;
                discountString = "10% OFF";
            }
            // Check for bundle discount
            else if (itemData.ItemType == ShopItemType.Bundle)
            {
                hasDiscount = !string.IsNullOrEmpty(itemData.GetSpecialOfferText());
                discountString = itemData.GetSpecialOfferText();
            }

            discountBadge.SetActive(hasDiscount);
            if (discountText != null && hasDiscount)
            {
                discountText.text = discountString;
            }
        }

        /// <summary>
        /// Setup purchase limit indicator
        /// </summary>
        private void SetupLimitIndicator()
        {
            if (limitIndicator == null) return;

            bool hasLimit = itemData.PurchaseLimit > 0;
            limitIndicator.SetActive(hasLimit);

            if (hasLimit && limitText != null)
            {
                int remaining = itemData.PurchaseLimit - currentPurchases;
                limitText.text = $"{remaining}/{itemData.PurchaseLimit}";
            }
        }

        /// <summary>
        /// Setup timer for time-limited items
        /// </summary>
        private void SetupTimer()
        {
            if (timerPanel == null) return;

            bool hasTimer = itemData.ResetTimeHours > 0 && currentPurchases >= itemData.PurchaseLimit;
            
            if (hasTimer && ShopManager.Instance != null)
            {
                var shopSave = GameController.SaveManager?.GetSave<ShopSave>("Shop");
                if (shopSave != null)
                {
                    var lastPurchaseTime = shopSave.GetLastPurchaseTime(itemData.ID);
                    if (lastPurchaseTime != System.DateTime.MinValue)
                    {
                        var timeUntilReset = itemData.GetTimeUntilReset(lastPurchaseTime);
                        if (timeUntilReset > System.TimeSpan.Zero)
                        {
                            timerPanel.SetActive(true);
                            UpdateTimerDisplay(timeUntilReset);
                        }
                        else
                        {
                            timerPanel.SetActive(false);
                        }
                    }
                    else
                    {
                        timerPanel.SetActive(false);
                    }
                }
            }
            else
            {
                timerPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Update timer display
        /// </summary>
        private void UpdateTimerDisplay(System.TimeSpan timeRemaining)
        {
            if (timerText == null) return;

            if (timeRemaining.TotalDays >= 1)
            {
                timerText.text = $"{timeRemaining.Days}d {timeRemaining.Hours}h";
            }
            else if (timeRemaining.TotalHours >= 1)
            {
                timerText.text = $"{timeRemaining.Hours}h {timeRemaining.Minutes}m";
            }
            else
            {
                timerText.text = $"{timeRemaining.Minutes}m {timeRemaining.Seconds}s";
            }
        }

        /// <summary>
        /// Update availability and purchase button state
        /// </summary>
        private void UpdateAvailability()
        {
            bool canPurchase = CanPurchase();
            
            // Update purchase button
            if (purchaseButton != null)
            {
                purchaseButton.interactable = canPurchase;
            }

            // Update sold out overlay
            if (soldOutOverlay != null)
            {
                bool isSoldOut = !itemData.CanPurchase(currentPurchases);
                soldOutOverlay.SetActive(isSoldOut);
            }
        }

        /// <summary>
        /// Check if item can be purchased
        /// </summary>
        private bool CanPurchase()
        {
            if (ShopManager.Instance == null) return false;
            return ShopManager.Instance.CanPurchaseItem(itemData.ID);
        }

        /// <summary>
        /// Handle purchase button click
        /// </summary>
        private void OnPurchaseClicked()
        {
            if (itemData == null || ShopManager.Instance == null) return;

            // Play button click sound
            GameController.AudioManager?.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            // Trigger purchase event
            OnItemClicked?.Invoke(itemData);
        }

        /// <summary>
        /// Update display (called periodically for timers)
        /// </summary>
        private void Update()
        {
            // Update timer if active
            if (timerPanel != null && timerPanel.activeInHierarchy)
            {
                SetupTimer();
            }
        }

        /// <summary>
        /// Get item data
        /// </summary>
        public ShopItemModel GetItemData()
        {
            return itemData;
        }

        /// <summary>
        /// Refresh the item display
        /// </summary>
        public void Refresh()
        {
            UpdateDisplay();
        }

        /// <summary>
        /// Set highlighted state for UI navigation
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            // Optional: Add highlight visual feedback
            if (purchaseButton != null)
            {
                var colors = purchaseButton.colors;
                colors.normalColor = highlighted ? Color.yellow : Color.white;
                purchaseButton.colors = colors;
            }
        }

        private void OnDestroy()
        {
            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveAllListeners();
            }
        }
    }
}