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
    public class GachaAnimationBehavior : MonoBehaviour
    {
        [Header("Animation Container")]
        [SerializeField] private CanvasGroup animationCanvasGroup;
        [SerializeField] private RectTransform animationContainer;
        [SerializeField] private Image backgroundImage;

        [Header("Gacha Machine")]
        [SerializeField] private GameObject gachaMachine;
        [SerializeField] private Animator gachaMachineAnimator;
        [SerializeField] private ParticleSystem gachaParticles;
        [SerializeField] private TMP_Text gachaTitleText;

        [Header("Single Pull Animation")]
        [SerializeField] private GameObject singlePullContainer;
        [SerializeField] private Image singleItemIcon;
        [SerializeField] private Image singleItemBorder;
        [SerializeField] private ParticleSystem singleItemParticles;
        [SerializeField] private TMP_Text singleItemNameText;

        [Header("Multi Pull Animation")]
        [SerializeField] private GameObject multiPullContainer;
        [SerializeField] private Transform multiItemsParent;
        [SerializeField] private GameObject multiItemPrefab;
        [SerializeField] private GridLayoutGroup multiItemsGrid;

        [Header("Skip Button")]
        [SerializeField] private Button skipButton;
        [SerializeField] private GameObject skipButtonObject;

        [Header("Animation Settings")]
        [SerializeField] private float machineAnimationDuration = 2f;
        [SerializeField] private float singleRevealDuration = 1.5f;
        [SerializeField] private float multiRevealDelay = 0.15f;
        [SerializeField] private float multiItemDuration = 0.8f;
        [SerializeField] private EasingType revealEasing = EasingType.BounceOut;
        [SerializeField] private EasingType itemAppearEasing = EasingType.SineOut;

        [Header("Audio")]
        [SerializeField] private string gachaStartSound = "Gacha_Start";
        [SerializeField] private string gachaSpinSound = "Gacha_Spin";
        [SerializeField] private string itemRevealSound = "Gacha_Reveal";
        [SerializeField] private string epicRevealSound = "Epic_Item";
        [SerializeField] private string legendaryRevealSound = "Legendary_Item";

        // Animation State
        private bool isAnimating = false;
        private bool canSkip = false;
        private Coroutine currentAnimation;
        private List<GameObject> multiItemObjects = new List<GameObject>();

        // Events
        public UnityEvent<List<RewardData>> OnAnimationComplete;
        public UnityEvent OnAnimationSkipped;

        // Animation Triggers (if using Animator)
        private static readonly int SPIN_TRIGGER = Animator.StringToHash("Spin");
        private static readonly int REVEAL_TRIGGER = Animator.StringToHash("Reveal");

        private void Awake()
        {
            // Setup skip button
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkipClicked);
            }

            // Initially hide
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Start gacha animation
        /// </summary>
        public void StartGachaAnimation(List<RewardData> rewards, bool isMultiPull = false)
        {
            if (isAnimating) return;

            if (isMultiPull)
            {
                StartCoroutine(MultiPullAnimationCoroutine(rewards));
            }
            else
            {
                StartCoroutine(SinglePullAnimationCoroutine(rewards[0]));
            }
        }

        /// <summary>
        /// Single pull animation coroutine
        /// </summary>
        private IEnumerator SinglePullAnimationCoroutine(RewardData reward)
        {
            isAnimating = true;
            canSkip = false;
            
            // Setup UI
            SetupSinglePullUI(reward);
            
            // Show animation container
            gameObject.SetActive(true);
            animationCanvasGroup.alpha = 0f;
            animationCanvasGroup.DoAlpha(1f, 0.3f);

            // Phase 1: Machine animation
            yield return StartCoroutine(PlayMachineAnimation());

            // Phase 2: Item reveal
            yield return StartCoroutine(RevealSingleItem(reward));

            // Phase 3: Wait and finish
            yield return new WaitForSecondsRealtime(1f);

            FinishAnimation(new List<RewardData> { reward });
        }

        /// <summary>
        /// Multi pull animation coroutine
        /// </summary>
        private IEnumerator MultiPullAnimationCoroutine(List<RewardData> rewards)
        {
            isAnimating = true;
            canSkip = false;
            
            // Setup UI
            SetupMultiPullUI(rewards);
            
            // Show animation container
            gameObject.SetActive(true);
            animationCanvasGroup.alpha = 0f;
            animationCanvasGroup.DoAlpha(1f, 0.3f);

            // Phase 1: Machine animation
            yield return StartCoroutine(PlayMachineAnimation());

            // Enable skip after machine animation
            canSkip = true;
            ShowSkipButton(true);

            // Phase 2: Reveal items one by one
            yield return StartCoroutine(RevealMultipleItems(rewards));

            // Phase 3: Final display
            yield return new WaitForSecondsRealtime(1f);

            FinishAnimation(rewards);
        }

        /// <summary>
        /// Setup UI for single pull
        /// </summary>
        private void SetupSinglePullUI(RewardData reward)
        {
            // Hide multi pull UI
            if (multiPullContainer != null)
                multiPullContainer.SetActive(false);

            // Show single pull UI
            if (singlePullContainer != null)
                singlePullContainer.SetActive(true);

            // Set title
            if (gachaTitleText != null)
            {
                gachaTitleText.text = "Equipment Gacha";
            }

            // Hide single item initially
            if (singleItemIcon != null)
            {
                singleItemIcon.gameObject.SetActive(false);
            }

            if (singleItemNameText != null)
            {
                singleItemNameText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Setup UI for multi pull
        /// </summary>
        private void SetupMultiPullUI(List<RewardData> rewards)
        {
            // Hide single pull UI
            if (singlePullContainer != null)
                singlePullContainer.SetActive(false);

            // Show multi pull UI
            if (multiPullContainer != null)
                multiPullContainer.SetActive(true);

            // Set title
            if (gachaTitleText != null)
            {
                gachaTitleText.text = $"Equipment Gacha x{rewards.Count}";
            }

            // Clear existing items
            ClearMultiItems();

            // Create item placeholders
            CreateMultiItemPlaceholders(rewards);
        }

        /// <summary>
        /// Play gacha machine animation
        /// </summary>
        private IEnumerator PlayMachineAnimation()
        {
            // Play start sound
            PlaySound(gachaStartSound);

            // Start machine animation
            if (gachaMachineAnimator != null)
            {
                gachaMachineAnimator.SetTrigger(SPIN_TRIGGER);
            }

            // Start particles
            if (gachaParticles != null)
            {
                gachaParticles.Play();
            }

            // Play spinning sound
            PlaySound(gachaSpinSound);

            // Wait for machine animation
            yield return new WaitForSecondsRealtime(machineAnimationDuration);

            // Stop particles
            if (gachaParticles != null)
            {
                gachaParticles.Stop();
            }
        }

        /// <summary>
        /// Reveal single item
        /// </summary>
        private IEnumerator RevealSingleItem(RewardData reward)
        {
            // Setup item data
            if (singleItemIcon != null)
            {
                singleItemIcon.sprite = GetRewardIcon(reward);
                singleItemIcon.gameObject.SetActive(true);
                singleItemIcon.transform.localScale = Vector3.zero;
            }

            if (singleItemBorder != null)
            {
                singleItemBorder.color = GetRarityColor(reward.Rarity);
            }

            if (singleItemNameText != null)
            {
                singleItemNameText.text = reward.DisplayName;
                singleItemNameText.gameObject.SetActive(true);
                singleItemNameText.GetComponent<CanvasGroup>()?.DoAlpha(0f, 0f);
            }

            // Play reveal sound
            string revealSound = GetRevealSound(reward);
            PlaySound(revealSound);

            // Animate item appearance
            float scaleMultiplier = GetRarityScaleMultiplier(reward.Rarity);
            if (singleItemIcon != null)
            {
                singleItemIcon.transform.DoLocalScale(Vector3.one * scaleMultiplier, singleRevealDuration)
                    .SetEasing(revealEasing);
            }

            // Start item particles
            if (singleItemParticles != null)
            {
                var main = singleItemParticles.main;
                main.startColor = GetRarityColor(reward.Rarity);
                singleItemParticles.Play();
            }

            // Fade in name text
            yield return new WaitForSecondsRealtime(singleRevealDuration * 0.5f);
            
            if (singleItemNameText != null)
            {
                singleItemNameText.GetComponent<CanvasGroup>()?.DoAlpha(1f, 0.5f);
            }

            yield return new WaitForSecondsRealtime(singleRevealDuration * 0.5f);
        }

        /// <summary>
        /// Reveal multiple items
        /// </summary>
        private IEnumerator RevealMultipleItems(List<RewardData> rewards)
        {
            for (int i = 0; i < multiItemObjects.Count && i < rewards.Count; i++)
            {
                if (!canSkip) break; // Animation was skipped

                var itemObject = multiItemObjects[i];
                var reward = rewards[i];
                var itemBehavior = itemObject.GetComponent<GachaItemBehavior>();

                if (itemBehavior != null)
                {
                    // Setup item
                    itemBehavior.Setup(reward);

                    // Play reveal sound
                    string revealSound = GetRevealSound(reward);
                    PlaySound(revealSound);

                    // Animate item
                    yield return StartCoroutine(itemBehavior.PlayRevealAnimation(multiItemDuration, itemAppearEasing));
                }

                // Wait before next item
                if (i < multiItemObjects.Count - 1)
                {
                    yield return new WaitForSecondsRealtime(multiRevealDelay);
                }
            }

            // Hide skip button
            ShowSkipButton(false);
        }

        /// <summary>
        /// Create placeholders for multi pull items
        /// </summary>
        private void CreateMultiItemPlaceholders(List<RewardData> rewards)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                var itemObject = Instantiate(multiItemPrefab, multiItemsParent);
                multiItemObjects.Add(itemObject);
                
                // Initially hide
                itemObject.SetActive(true);
                itemObject.transform.localScale = Vector3.zero;
            }

            // Adjust grid layout
            AdjustMultiItemGrid(rewards.Count);
        }

        /// <summary>
        /// Adjust grid layout for multi items
        /// </summary>
        private void AdjustMultiItemGrid(int itemCount)
        {
            if (multiItemsGrid == null) return;

            if (itemCount <= 5)
            {
                multiItemsGrid.constraintCount = 5;
            }
            else
            {
                multiItemsGrid.constraintCount = 5; // 2 rows of 5
            }
        }

        /// <summary>
        /// Clear multi item objects
        /// </summary>
        private void ClearMultiItems()
        {
            foreach (var item in multiItemObjects)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            multiItemObjects.Clear();
        }

        /// <summary>
        /// Show/hide skip button
        /// </summary>
        private void ShowSkipButton(bool show)
        {
            if (skipButtonObject != null)
            {
                skipButtonObject.SetActive(show);
            }
        }

        /// <summary>
        /// Handle skip button click
        /// </summary>
        private void OnSkipClicked()
        {
            if (!canSkip) return;

            canSkip = false;
            ShowSkipButton(false);

            // Skip to end of animation
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            StartCoroutine(SkipToEnd());
        }

        /// <summary>
        /// Skip to end of animation
        /// </summary>
        private IEnumerator SkipToEnd()
        {
            // Instantly reveal all remaining items
            for (int i = 0; i < multiItemObjects.Count; i++)
            {
                var itemObject = multiItemObjects[i];
                var itemBehavior = itemObject.GetComponent<GachaItemBehavior>();
                
                if (itemBehavior != null)
                {
                    itemBehavior.InstantReveal();
                }
            }

            OnAnimationSkipped?.Invoke();

            yield return new WaitForSecondsRealtime(0.5f);

            // Get rewards from multi item behaviors
            var rewards = new List<RewardData>();
            foreach (var itemObject in multiItemObjects)
            {
                var itemBehavior = itemObject.GetComponent<GachaItemBehavior>();
                if (itemBehavior != null)
                {
                    rewards.Add(itemBehavior.GetReward());
                }
            }

            FinishAnimation(rewards);
        }

        /// <summary>
        /// Finish animation and clean up
        /// </summary>
        private void FinishAnimation(List<RewardData> rewards)
        {
            StartCoroutine(FinishAnimationCoroutine(rewards));
        }

        /// <summary>
        /// Finish animation coroutine
        /// </summary>
        private IEnumerator FinishAnimationCoroutine(List<RewardData> rewards)
        {
            // Hide animation container
            animationCanvasGroup.DoAlpha(0f, 0.3f);

            yield return new WaitForSecondsRealtime(0.3f);

            // Clean up
            isAnimating = false;
            canSkip = false;
            gameObject.SetActive(false);
            ClearMultiItems();

            // Trigger completion event
            OnAnimationComplete?.Invoke(rewards);
        }

        /// <summary>
        /// Get reward icon
        /// </summary>
        private Sprite GetRewardIcon(RewardData reward)
        {
            if (reward.Type == RewardType.Equipment && reward.EquipmentData != null)
            {
                return reward.EquipmentData.GetIcon();
            }

            // Fallback icons
            if (DataLoadingManager.Instance != null)
            {
                string iconName = reward.Type switch
                {
                    RewardType.Gold => "icon_gold",
                    RewardType.Gems => "icon_gem",
                    RewardType.Character => "icon_character",
                    _ => "icon_equipment"
                };
                return DataLoadingManager.Instance.LoadSprite("Currency", iconName);
            }

            return null;
        }

        /// <summary>
        /// Get rarity color
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
        /// Get appropriate reveal sound for reward
        /// </summary>
        private string GetRevealSound(RewardData reward)
        {
            if (reward.Type == RewardType.Equipment)
            {
                return reward.Rarity switch
                {
                 EquipmentRarity.Epic => epicRevealSound,
                 EquipmentRarity.Legendary => legendaryRevealSound,
                    _ => itemRevealSound
                };
            }
            return itemRevealSound;
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
        /// Force stop animation
        /// </summary>
        public void ForceStop()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            isAnimating = false;
            canSkip = false;
            gameObject.SetActive(false);
            ClearMultiItems();
        }

        /// <summary>
        /// Check if animation is playing
        /// </summary>
        public bool IsAnimating()
        {
            return isAnimating;
        }

        private void OnDestroy()
        {
            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
            }
        }
    }

    /// <summary>
    /// Individual gacha item behavior for multi pull
    /// </summary>
    public class GachaItemBehavior : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private Image backgroundFlash;
        [SerializeField] private ParticleSystem itemParticles;
        [SerializeField] private TMP_Text itemNameText;

        private RewardData rewardData;

        /// <summary>
        /// Setup gacha item
        /// </summary>
        public void Setup(RewardData reward)
        {
            rewardData = reward;

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

            // Set name
            if (itemNameText != null)
            {
                itemNameText.text = reward.DisplayName;
                itemNameText.gameObject.SetActive(false); // Start hidden
            }

            // Setup particles
            if (itemParticles != null)
            {
                var main = itemParticles.main;
                main.startColor = GetRarityColor(reward.Rarity);
            }
        }

        /// <summary>
        /// Play reveal animation
        /// </summary>
        public IEnumerator PlayRevealAnimation(float duration, EasingType easing)
        {
            // Scale up
            float scaleMultiplier = GetRarityScaleMultiplier(rewardData.Rarity);
            transform.DoLocalScale(Vector3.one * scaleMultiplier, duration).SetEasing(easing);

            // Flash background for epic+ items
            if (rewardData.Rarity >= EquipmentRarity.Epic && backgroundFlash != null)
            {
                backgroundFlash.color = GetRarityColor(rewardData.Rarity);
                backgroundFlash.gameObject.SetActive(true);
                backgroundFlash.DoAlpha(0f, duration);
            }

            // Start particles
            if (itemParticles != null)
            {
                itemParticles.Play();
            }

            // Show name after half duration
            yield return new WaitForSecondsRealtime(duration * 0.5f);
            
            if (itemNameText != null)
            {
                itemNameText.gameObject.SetActive(true);
                itemNameText.GetComponent<CanvasGroup>()?.DoAlpha(1f, duration * 0.5f);
            }

            yield return new WaitForSecondsRealtime(duration * 0.5f);
        }

        /// <summary>
        /// Instantly reveal item (for skip)
        /// </summary>
        public void InstantReveal()
        {
            float scaleMultiplier = GetRarityScaleMultiplier(rewardData.Rarity);
            transform.localScale = Vector3.one * scaleMultiplier;

            if (itemNameText != null)
            {
                itemNameText.gameObject.SetActive(true);
                var canvasGroup = itemNameText.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }
            }

            if (itemParticles != null)
            {
                itemParticles.Play();
            }
        }

        /// <summary>
        /// Get reward data
        /// </summary>
        public RewardData GetReward()
        {
            return rewardData;
        }

        // Helper methods (same as RewardItemBehavior)
        private Sprite GetRewardIcon(RewardData reward)
        {
            if (reward.Type == RewardType.Equipment && reward.EquipmentData != null)
            {
                return reward.EquipmentData.GetIcon();
            }
            return null;
        }

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

        private float GetRarityScaleMultiplier(EquipmentRarity rarity)
        {
            return rarity switch
            {
             EquipmentRarity.Epic => 1.1f,
             EquipmentRarity.Legendary => 1.2f,
                _ => 1f
            };
        }
    }
}