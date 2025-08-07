using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using OctoberStudio.Easing;
using OctoberStudio.Audio;

namespace OctoberStudio.Shop.UI
{
    public class RewardPopupBehavior : MonoBehaviour
    {
        [Header("Popup Container")]
        [SerializeField] private CanvasGroup popupCanvasGroup;
        [SerializeField] private RectTransform popupContainer;
        [SerializeField] private Image backgroundImage;

        [Header("Title")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Image titleIcon;

        [Header("Reward Items")]
        [SerializeField] private GameObject rewardItemPrefab;
        [SerializeField] private RectTransform rewardItemsParent;
        [SerializeField] private GridLayoutGroup rewardItemsGrid;

        [Header("Continue Button")]
        [SerializeField] private GameObject continuePanel;
        [SerializeField] private TMP_Text continueText;
        [SerializeField] private Button continueButton;

        [Header("Animation Settings")]
        [SerializeField] private float popupAnimationDuration = 0.3f;
        [SerializeField] private float itemRevealDelay = 0.1f;
        [SerializeField] private float itemAnimationDuration = 0.5f;
        [SerializeField] private EasingType popupEasing = EasingType.ElasticIn;
        [SerializeField] private EasingType itemEasing = EasingType.BounceOut;

        [Header("Audio")]
        [SerializeField] private string popupOpenSound = "Popup_Open";
        [SerializeField] private string itemRevealSound = "Item_Reveal";
        [SerializeField] private string epicItemSound = "Epic_Item";
        [SerializeField] private string legendaryItemSound = "Legendary_Item";

        // State
        private List<RewardItemBehavior> rewardItemBehaviors = new List<RewardItemBehavior>();
        private bool isShowing = false;
        private Coroutine showRewardsCoroutine;

        // Events
        public UnityEvent OnPopupClosed;

        private void Awake()
        {
            // Setup continue button
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            // Initially hide popup
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Show reward popup with list of rewards
        /// </summary>
        public void ShowRewards(List<RewardData> rewards, string popupTitle = "Rewards!")
        {
            if (isShowing) return;

            StartCoroutine(ShowRewardsCoroutine(rewards, popupTitle));
        }

        /// <summary>
        /// Show single reward (for simple purchases)
        /// </summary>
        public void ShowSingleReward(RewardData reward, string popupTitle = "Purchase Complete!")
        {
            ShowRewards(new List<RewardData> { reward }, popupTitle);
        }

        /// <summary>
        /// Coroutine to handle showing rewards with animation
        /// </summary>
        private IEnumerator ShowRewardsCoroutine(List<RewardData> rewards, string popupTitle)
        {
            isShowing = true;
            gameObject.SetActive(true);

            // Setup popup
            SetupPopup(popupTitle, rewards);

            // Initial state - hidden
            popupCanvasGroup.alpha = 0f;
            popupContainer.localScale = Vector3.zero;
            continuePanel.SetActive(false);

            // Play popup open sound
            PlaySound(popupOpenSound);

            // Animate popup appearance
            var popupTween = popupContainer.DoLocalScale(Vector3.one, popupAnimationDuration)
                .SetEasing(popupEasing);
            
            popupCanvasGroup.DoAlpha(1f, popupAnimationDuration);

            yield return new WaitForSecondsRealtime(popupAnimationDuration);

            // Animate rewards appearance
            yield return StartCoroutine(AnimateRewardsAppearance(rewards));

            // Show continue button
            continuePanel.SetActive(true);
            continuePanel.GetComponent<CanvasGroup>()?.DoAlpha(1f, 0.3f);

            // Enable tap anywhere to close
            EnableTapToClose();
        }

        /// <summary>
        /// Setup popup UI elements
        /// </summary>
        private void SetupPopup(string popupTitle, List<RewardData> rewards)
        {
            // Set title
            if (titleText != null)
            {
                titleText.text = popupTitle;
            }

            // Set title icon based on reward type
            if (titleIcon != null && rewards.Count > 0)
            {
                titleIcon.sprite = GetTitleIcon(rewards[0]);
            }

            // Clear existing reward items
            ClearRewardItems();

            // Create reward item UI elements
            CreateRewardItems(rewards);

            // Set continue text
            if (continueText != null)
            {
                continueText.text = rewards.Count > 1 ? "Tap to Continue" : "Tap to Continue";
            }
        }

        /// <summary>
        /// Get appropriate title icon for reward type
        /// </summary>
        private Sprite GetTitleIcon(RewardData reward)
        {
            if (DataLoadingManager.Instance == null) return null;

            string iconName = reward.Type switch
            {
                RewardType.Gold => "icon_gold",
                RewardType.Gems => "icon_gem",
                RewardType.Equipment => "icon_equipment",
                RewardType.Character => "icon_character",
                _ => "icon_gift"
            };

            return DataLoadingManager.Instance.LoadSprite("UI", iconName);
        }

        /// <summary>
        /// Clear existing reward items
        /// </summary>
        private void ClearRewardItems()
        {
            foreach (var item in rewardItemBehaviors)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            rewardItemBehaviors.Clear();
        }

        /// <summary>
        /// Create reward item UI elements
        /// </summary>
        private void CreateRewardItems(List<RewardData> rewards)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                var rewardObj = Instantiate(rewardItemPrefab, rewardItemsParent);
                var rewardBehavior = rewardObj.GetComponent<RewardItemBehavior>();
                
                if (rewardBehavior != null)
                {
                    rewardBehavior.Setup(rewards[i]);
                    rewardBehavior.gameObject.SetActive(false); // Start hidden for animation
                    rewardItemBehaviors.Add(rewardBehavior);
                }
            }

            // Adjust grid layout for better appearance
            AdjustGridLayout(rewards.Count);
        }

        /// <summary>
        /// Adjust grid layout based on number of items
        /// </summary>
        private void AdjustGridLayout(int itemCount)
        {
            if (rewardItemsGrid == null) return;

            // Adjust columns based on item count
            if (itemCount <= 3)
            {
                rewardItemsGrid.constraintCount = itemCount;
            }
            else if (itemCount <= 6)
            {
                rewardItemsGrid.constraintCount = 3;
            }
            else
            {
                rewardItemsGrid.constraintCount = 4;
            }
        }

        /// <summary>
        /// Animate rewards appearance
        /// </summary>
        private IEnumerator AnimateRewardsAppearance(List<RewardData> rewards)
        {
            for (int i = 0; i < rewardItemBehaviors.Count; i++)
            {
                var rewardBehavior = rewardItemBehaviors[i];
                var reward = rewards[i];
                
                // Show item
                rewardBehavior.gameObject.SetActive(true);
                
                // Start with scale 0
                rewardBehavior.transform.localScale = Vector3.zero;
                
                // Play appropriate sound
                string soundName = GetRewardSound(reward);
                PlaySound(soundName);
                
                // Animate scale up
                float scaleMultiplier = GetRarityScaleMultiplier(reward.Rarity);
                Vector3 targetScale = Vector3.one * scaleMultiplier;
                
                rewardBehavior.transform.DoLocalScale(targetScale, itemAnimationDuration)
                    .SetEasing(itemEasing);

                // Flash background for epic+ items
                if (reward.Rarity >= EquipmentRarity.Epic)
                {
                    rewardBehavior.FlashBackground(GetRarityColor(reward.Rarity));
                }

                // Wait before next item
                if (i < rewardItemBehaviors.Count - 1)
                {
                    yield return new WaitForSecondsRealtime(itemRevealDelay);
                }
            }
        }

        /// <summary>
        /// Get sound name for reward
        /// </summary>
        private string GetRewardSound(RewardData reward)
        {
            if (reward.Type == RewardType.Equipment)
            {
                return reward.Rarity switch
                {
                    EquipmentRarity.Epic => epicItemSound,
                    EquipmentRarity.Legendary => legendaryItemSound,
                    _ => itemRevealSound
                };
            }
            return itemRevealSound;
        }

        /// <summary>
        /// Get scale multiplier for rarity
        /// </summary>
        private float GetRarityScaleMultiplier(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Epic => 1.1f,
                EquipmentRarity.Legendary => 1.2f,
                _ => 1f
            };
        }

        /// <summary>
        /// Get color for rarity flash
        /// </summary>
        private Color GetRarityColor(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Epic => new Color(0.6f, 0.2f, 0.8f, 0.5f),
                EquipmentRarity.Legendary => new Color(1f, 0.8f, 0.2f, 0.5f),
                _ => Color.white
            };
        }

        /// <summary>
        /// Enable tap anywhere to close functionality
        /// </summary>
        private void EnableTapToClose()
        {
            if (backgroundImage != null)
            {
                var button = backgroundImage.GetComponent<Button>();
                if (button == null)
                {
                    button = backgroundImage.gameObject.AddComponent<Button>();
                }
                
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnContinueClicked);
            }
        }

        /// <summary>
        /// Handle continue button click
        /// </summary>
        private void OnContinueClicked()
        {
            if (!isShowing) return;

            StartCoroutine(HidePopupCoroutine());
        }

        /// <summary>
        /// Hide popup with animation
        /// </summary>
        private IEnumerator HidePopupCoroutine()
        {
            // Disable tap to close
            if (backgroundImage != null)
            {
                var button = backgroundImage.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }

            // Animate popup disappearance
            popupCanvasGroup.DoAlpha(0f, popupAnimationDuration);
            popupContainer.DoLocalScale(Vector3.zero, popupAnimationDuration);

            yield return new WaitForSecondsRealtime(popupAnimationDuration);

            // Clean up
            isShowing = false;
            gameObject.SetActive(false);
            
            // Trigger event
            OnPopupClosed?.Invoke();
        }

        /// <summary>
        /// Play sound with error handling
        /// </summary>
        private void PlaySound(string soundName)
        {
            if (GameController.AudioManager != null && !string.IsNullOrEmpty(soundName))
            {
                int soundHash = soundName.GetHashCode();
                GameController.AudioManager.PlaySound(soundHash);
            }
        }

        /// <summary>
        /// Force close popup (for cleanup)
        /// </summary>
        public void ForceClose()
        {
            if (showRewardsCoroutine != null)
            {
                StopCoroutine(showRewardsCoroutine);
            }

            isShowing = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Check if popup is currently showing
        /// </summary>
        public bool IsShowing()
        {
            return isShowing;
        }

        private void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
            }
        }
    }

    /// <summary>
    /// Individual reward item behavior
    /// </summary>
    public class RewardItemBehavior : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private Image backgroundFlash;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject newBadge;

        /// <summary>
        /// Setup reward item display
        /// </summary>
        public void Setup(RewardData reward)
        {
            // Set icon
            if (itemIcon != null)
            {
                itemIcon.sprite = GetRewardIcon(reward);
            }

            // Set rarity border
            if (rarityBorder != null)
            {
                rarityBorder.color = GetRarityColor(reward.Rarity);
            }

            // Set quantity
            if (quantityText != null)
            {
                if (reward.Quantity > 1 || reward.Type == RewardType.Gold || reward.Type == RewardType.Gems)
                {
                    quantityText.text = reward.Quantity.ToString();
                    quantityText.gameObject.SetActive(true);
                }
                else
                {
                    quantityText.gameObject.SetActive(false);
                }
            }

            // Set name
            if (nameText != null)
            {
                nameText.text = reward.DisplayName;
            }

            // Show new badge for equipment
            if (newBadge != null)
            {
                newBadge.SetActive(reward.Type == RewardType.Equipment);
            }

            // Setup background flash
            if (backgroundFlash != null)
            {
                backgroundFlash.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Get icon for reward
        /// </summary>
        private Sprite GetRewardIcon(RewardData reward)
        {
            if (DataLoadingManager.Instance == null) return null;

            switch (reward.Type)
            {
                case RewardType.Gold:
                    return DataLoadingManager.Instance.LoadSprite("Currency", "icon_gold");
                case RewardType.Gems:
                    return DataLoadingManager.Instance.LoadSprite("Currency", "icon_gem");
                case RewardType.Equipment:
                    return reward.EquipmentData?.GetIcon();
                case RewardType.Character:
                    return DataLoadingManager.Instance.LoadSprite("Characters", "icon_character");
                default:
                    return null;
            }
        }

        /// <summary>
        /// Get color for rarity
        /// </summary>
        private Color GetRarityColor(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => Color.white,
                EquipmentRarity.Uncommon => Color.green,
                EquipmentRarity.Rare => Color.blue,
                EquipmentRarity.Epic => Color.magenta,
                EquipmentRarity.Legendary => Color.yellow,
                _ => Color.white
            };
        }

        /// <summary>
        /// Flash background with color
        /// </summary>
        public void FlashBackground(Color flashColor)
        {
            if (backgroundFlash == null) return;

            backgroundFlash.color = flashColor;
            backgroundFlash.gameObject.SetActive(true);
            
            // Fade out flash
            backgroundFlash.DoAlpha(0f, 1f).SetOnFinish(() => {
                backgroundFlash.gameObject.SetActive(false);
            });
        }
    }
}