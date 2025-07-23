using System.Collections.Generic;
using DG.Tweening;
using OctoberStudio.Upgrades;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Talents.Data;
using Talents.Manager;

namespace Talents.UI
{
    /// <summary>
    /// Mobile-optimized talent node behavior - Click only, no hover
    /// </summary>
    public class TalentNodeBehavior : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image nodeIcon;
        [SerializeField] private Image nodeBackground;
        [SerializeField] private Image nodeBorder;
        [SerializeField] private Button nodeButton; // Main click button
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text requirementText;

        [Header("Visual Elements")]
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject maxLevelIcon;
        [SerializeField] private GameObject learnedIcon;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image currencyIcon;

        [Header("Visual States")]
        [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color learnedColor = new Color(0.3f, 1f, 0.3f);
        [SerializeField] private Color maxLevelColor = new Color(1f, 0.8f, 0f);
        [SerializeField] private Color insufficientPointsColor = new Color(1f, 0.3f, 0.3f);

        [Header("Mobile Touch Settings")]
        [SerializeField] private float touchScaleFactor = 0.95f; // Scale when pressed
        [SerializeField] private float animationDuration = 0.1f;

        // Properties
        public TalentModel TalentModel { get; private set; }
        public TalentProgressInfo ProgressInfo { get; private set; }
        public bool IsInitialized { get; private set; }

        // Events - Mobile focused
        public UnityEvent<TalentNodeBehavior> OnNodeClicked = new UnityEvent<TalentNodeBehavior>();

        // Cached components
        private RectTransform rectTransform;
        // private CanvasGroup canvasGroup;

        // Animation state
        private bool isAnimating = false;
        private Vector3 originalScale;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            // canvasGroup = GetComponent<CanvasGroup>();
            originalScale = transform.localScale;

            // if (canvasGroup == null)
            // {
            //     canvasGroup = gameObject.AddComponent<CanvasGroup>();
            // }

            SetupMobileButton();
        }

        /// <summary>
        /// Setup button for mobile touch
        /// </summary>
        private void SetupMobileButton()
        {
            // Main node button
            if (nodeButton != null)
            {
                nodeButton.onClick.AddListener(OnButtonClicked);
            }
            else
            {
                // If no button assigned, add one to the main GameObject
                nodeButton = GetComponent<Button>();
                if (nodeButton == null)
                {
                    nodeButton = gameObject.AddComponent<Button>();
                }
                nodeButton.onClick.AddListener(OnButtonClicked);
            }

            // Mobile-friendly button setup
            if (nodeButton != null)
            {
                // Larger touch area for mobile
                var navigation = nodeButton.navigation;
                navigation.mode = Navigation.Mode.None; // Disable navigation for touch
                nodeButton.navigation = navigation;
                
                // Disable transition animations (we'll handle manually)
                nodeButton.transition = Selectable.Transition.None;
            }
        }

        /// <summary>
        /// Initialize the node với talent data
        /// </summary>
        public void Initialize(TalentModel talentModel)
        {
            TalentModel = talentModel;
            IsInitialized = true;

            // Set position từ talent model
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(talentModel.PositionX, talentModel.PositionY);
            }

            // Load và set icon
            SetIcon(talentModel);

            // Set basic info
            SetBasicInfo(talentModel);

            // Set node type styling
            SetNodeTypeVisuals(talentModel);

            // Update visual state
            UpdateVisualState();

            Debug.Log($"[TalentNode] Initialized {talentModel.Name} at ({talentModel.PositionX}, {talentModel.PositionY})");
        }

        /// <summary>
        /// Set talent icon
        /// </summary>
        private void SetIcon(TalentModel talent)
        {
            if (nodeIcon != null)
            {
                if (talent.Icon != null)
                {
                    nodeIcon.sprite = talent.Icon;
                    nodeIcon.enabled = true;
                }
                else
                {
                    // Try loading icon if not loaded yet
                    var icon = Resources.Load<Sprite>($"Icons/Talents/{talent.IconPath}");
                    if (icon != null)
                    {
                        nodeIcon.sprite = icon;
                        nodeIcon.enabled = true;
                    }
                    else
                    {
                        Debug.LogWarning($"[TalentNode] Could not load icon for {talent.Name}: {talent.IconPath}");
                        nodeIcon.enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Set basic talent information
        /// </summary>
        private void SetBasicInfo(TalentModel talent)
        {
            // Set name
            if (nameText != null)
            {
                nameText.text = talent.Name;
            }

            // Set description based on talent type
            if (descriptionText != null)
            {
                if (talent.NodeType == TalentNodeType.Normal)
                {
                    // For normal talents, show stat bonus
                    descriptionText.text = $"+{talent.StatValue} {GetStatDisplayName(talent.StatType)}";
                }
                else
                {
                    // For special skills, show description
                    string desc = talent.Description;
                    if (desc.Length > 50) // Truncate if too long for mobile
                    {
                        desc = desc.Substring(0, 47) + "...";
                    }
                    descriptionText.text = desc;
                }
            }

            // Set cost with currency display
            if (costText != null)
            {
                costText.text = $"{talent.Cost}";
                
                // Set currency icon if available
                if (currencyIcon != null)
                {
                    var iconSprite = talent.NodeType == TalentNodeType.Normal ? 
                        Resources.Load<Sprite>("Icons/UI/gold_icon") : 
                        Resources.Load<Sprite>("Icons/UI/dna_icon");
                    
                    if (iconSprite != null)
                    {
                        currencyIcon.sprite = iconSprite;
                        currencyIcon.enabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Get display name for stat type
        /// </summary>
        private string GetStatDisplayName(UpgradeType statType)
        {
            switch (statType)
            {
                case UpgradeType.Damage: return "ATK";
                case UpgradeType.Health: return "HP";
                case UpgradeType.Speed: return "SPD";
                case UpgradeType.XPMultiplier: return "XP";
                case UpgradeType.MagnetRadius: return "MAG";
                case UpgradeType.CooldownReduction: return "CDR";
                case UpgradeType.Luck: return "LUCK";
                default: return statType.ToString();
            }
        }

        /// <summary>
        /// Set visual style based on node type
        /// </summary>
        private void SetNodeTypeVisuals(TalentModel talent)
        {
            if (nodeBackground != null)
            {
                Color typeColor = GetNodeTypeColor(talent);
                nodeBackground.color = typeColor;
            }

            // Setup progress slider for normal talents
            if (talent.NodeType == TalentNodeType.Normal && progressSlider != null)
            {
                progressSlider.gameObject.SetActive(true);
                progressSlider.maxValue = talent.MaxLevel;
                progressSlider.interactable = false; // Read-only display
            }
            else if (progressSlider != null)
            {
                progressSlider.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Get color based on node type and stat
        /// </summary>
        private Color GetNodeTypeColor(TalentModel talent)
        {
            if (talent.NodeType == TalentNodeType.Special)
            {
                return new Color(1f, 0.8f, 0f, 1f); // Gold for special skills
            }

            // Color coding for base stats
            switch (talent.StatType)
            {
                case UpgradeType.Damage:
                    return new Color(1f, 0.3f, 0.3f, 1f); // Red for ATK
                case UpgradeType.Health:
                    return new Color(0.3f, 1f, 0.3f, 1f); // Green for HP
                default:
                    // Check by stat name for Armor/Healing
                    if (talent.StatTypeString != null)
                    {
                        if (talent.StatTypeString.ToLower().Contains("armor"))
                            return new Color(0.3f, 0.3f, 1f, 1f); // Blue for Armor
                        if (talent.StatTypeString.ToLower().Contains("healing"))
                            return new Color(1f, 1f, 0.3f, 1f); // Yellow for Healing
                    }
                    return new Color(0.7f, 0.7f, 0.7f, 1f); // Gray default
            }
        }

        /// <summary>
        /// Update the visual state của node
        /// </summary>
        public void UpdateVisualState()
        {
            if (!IsInitialized || TalentManager.Instance == null)
                return;

            // Get current progress info
            ProgressInfo = TalentManager.Instance.GetTalentProgressInfo(TalentModel.ID);

            // Update level display
            UpdateLevelDisplay();

            // Update cost display
            UpdateCostDisplay();

            // Update visual appearance
            UpdateNodeAppearance();

            // Update interaction state
            UpdateInteractionState();

            // Update progress for normal talents
            UpdateProgressDisplay();

            // Update requirement display for special skills
            UpdateRequirementDisplay();
        }

        /// <summary>
        /// Update level display
        /// </summary>
        private void UpdateLevelDisplay()
        {
            if (levelText != null)
            {
                if (TalentModel.NodeType == TalentNodeType.Normal)
                {
                    levelText.text = $"{ProgressInfo.CurrentLevel}/{ProgressInfo.MaxLevel}";
                    levelText.gameObject.SetActive(true);
                }
                else
                {
                    // For special skills, show learned status
                    if (ProgressInfo.CurrentLevel > 0)
                    {
                        levelText.text = "LEARNED";
                        levelText.color = learnedColor;
                    }
                    else
                    {
                        levelText.text = "LOCKED";
                        levelText.color = lockedColor;
                    }
                    levelText.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Update cost display
        /// </summary>
        private void UpdateCostDisplay()
        {
            if (costText != null)
            {
                if (ProgressInfo.UnlockStatus == TalentUnlockStatus.MaxLevel)
                {
                    costText.text = "MAX";
                    costText.color = maxLevelColor;
                }
                else if (ProgressInfo.NextLevelCost > 0)
                {
                    costText.text = ProgressInfo.NextLevelCost.ToString();
                    
                    // Color based on affordability
                    bool canAfford = ProgressInfo.UnlockStatus == TalentUnlockStatus.Available;
                    costText.color = canAfford ? Color.white : insufficientPointsColor;
                }
                else
                {
                    costText.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Update node appearance based on status
        /// </summary>
        private void UpdateNodeAppearance()
        {
            Color targetColor = lockedColor;
            bool showLock = false;
            bool showMaxLevel = false;
            bool showLearned = false;

            switch (ProgressInfo.UnlockStatus)
            {
                case TalentUnlockStatus.Locked:
                    targetColor = lockedColor;
                    showLock = true;
                    break;
                case TalentUnlockStatus.Available:
                    targetColor = availableColor;
                    break;
                case TalentUnlockStatus.Learned:
                    targetColor = learnedColor;
                    showLearned = true;
                    break;
                case TalentUnlockStatus.InsufficientPoints:
                    targetColor = insufficientPointsColor;
                    break;
                case TalentUnlockStatus.MaxLevel:
                    targetColor = maxLevelColor;
                    showMaxLevel = true;
                    showLearned = true;
                    break;
            }

            // Apply color to border
            if (nodeBorder != null)
            {
                nodeBorder.color = targetColor;
            }

            // Show/hide status icons
            if (lockIcon != null)
                lockIcon.SetActive(showLock);
            if (maxLevelIcon != null)
                maxLevelIcon.SetActive(showMaxLevel);
            if (learnedIcon != null)
                learnedIcon.SetActive(showLearned);
        }

        /// <summary>
        /// Update interaction state
        /// </summary>
        private void UpdateInteractionState()
        {
            bool isInteractable = ProgressInfo.UnlockStatus == TalentUnlockStatus.Available;

            if (nodeButton != null)
            {
                nodeButton.interactable = isInteractable;
            }

            // if (canvasGroup != null)
            // {
            //     canvasGroup.alpha = ProgressInfo.UnlockStatus == TalentUnlockStatus.Locked ? 0.6f : 1f;
            //     canvasGroup.interactable = isInteractable;
            // }
        }

        /// <summary>
        /// Update progress display for normal talents
        /// </summary>
        private void UpdateProgressDisplay()
        {
            if (TalentModel.NodeType == TalentNodeType.Normal && progressSlider != null)
            {
                progressSlider.value = ProgressInfo.CurrentLevel;
                
                // Color the progress bar based on progress
                var fillImage = progressSlider.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                {
                    float progress = (float)ProgressInfo.CurrentLevel / ProgressInfo.MaxLevel;
                    fillImage.color = Color.Lerp(Color.red, Color.green, progress);
                }
            }
        }

        /// <summary>
        /// Update requirement display for special skills
        /// </summary>
        private void UpdateRequirementDisplay()
        {
            if (TalentModel.NodeType == TalentNodeType.Special && requirementText != null)
            {
                if (ProgressInfo.CurrentLevel > 0)
                {
                    requirementText.text = "LEARNED";
                    requirementText.color = learnedColor;
                }
                else
                {
                    requirementText.text = $"Level {TalentModel.RequiredPlayerLevel}";
                    
                    // Check if player meets level requirement
                    // TODO: Get actual player level
                    int playerLevel = 10; // Placeholder
                    bool meetsLevel = playerLevel >= TalentModel.RequiredPlayerLevel;
                    requirementText.color = meetsLevel ? Color.white : insufficientPointsColor;
                }
                requirementText.gameObject.SetActive(true);
            }
            else if (requirementText != null)
            {
                requirementText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Handle mobile touch/click
        /// </summary>
        private void OnButtonClicked()
        {
            if (!IsInitialized)
                return;

            // Mobile-optimized click feedback
            AnimateTouchFeedback();

            // Trigger click event
            OnNodeClicked?.Invoke(this);

            Debug.Log($"[TalentNode] Touched: {TalentModel.Name} (Status: {ProgressInfo.UnlockStatus})");
        }

        /// <summary>
        /// Mobile touch feedback animation
        /// </summary>
        private void AnimateTouchFeedback()
        {
            if (isAnimating)
                return;

            isAnimating = true;

            // Quick scale down then back up for touch feedback
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(originalScale * touchScaleFactor, animationDuration));
            sequence.Append(transform.DOScale(originalScale, animationDuration));
            sequence.OnComplete(() => isAnimating = false);
        }

        /// <summary>
        /// Animate node when talent is learned
        /// </summary>
        public void AnimateLearn()
        {
            if (isAnimating)
                return;

            isAnimating = true;

            // Mobile-friendly learn animation - less dramatic
            var sequence = DOTween.Sequence();
            
            // Scale up moderately
            sequence.Append(transform.DOScale(originalScale * 1.2f, 0.2f).SetEase(Ease.OutBack));
            
            // Scale back to normal
            sequence.Append(transform.DOScale(originalScale, 0.2f).SetEase(Ease.InBack));
            
            // Flash effect
            if (nodeBorder != null)
            {
                var originalColor = nodeBorder.color;
                sequence.Insert(0, nodeBorder.DOColor(Color.white, 0.1f).SetLoops(3, LoopType.Yoyo));
            }

            sequence.OnComplete(() => {
                isAnimating = false;
                UpdateVisualState(); // Refresh state after animation
            });

            Debug.Log($"[TalentNode] Animated learn for {TalentModel.Name}");
        }

        /// <summary>
        /// Set highlighted state (for prerequisite visualization)
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (nodeBorder != null)
            {
                if (highlighted)
                {
                    nodeBorder.color = Color.yellow;
                    nodeBorder.enabled = true;
                }
                else
                {
                    UpdateNodeAppearance(); // Restore normal border color
                }
            }
        }

        /// <summary>
        /// Get tooltip content cho mobile touch-and-hold
        /// </summary>
        public string GetTooltipContent()
        {
            if (!IsInitialized || TalentManager.Instance == null)
                return "Unknown Talent";

            return TalentManager.Instance.GetTalentTooltip(TalentModel.ID);
        }

        /// <summary>
        /// Check if node can be interacted with
        /// </summary>
        public bool CanInteract()
        {
            return IsInitialized && ProgressInfo.UnlockStatus == TalentUnlockStatus.Available;
        }

        /// <summary>
        /// Force refresh visual state
        /// </summary>
        public void ForceRefresh()
        {
            if (IsInitialized)
            {
                UpdateVisualState();
            }
        }

        /// <summary>
        /// Cleanup when destroyed
        /// </summary>
        private void OnDestroy()
        {
            // Stop any running animations
            DOTween.Kill(gameObject);
            
            // Remove button listeners
            if (nodeButton != null)
            {
                nodeButton.onClick.RemoveAllListeners();
            }

            // Clear events
            OnNodeClicked?.RemoveAllListeners();
        }

        // Debug methods
        [ContextMenu("Force Update Visual State")]
        public void ForceUpdateVisualStateDebug()
        {
            UpdateVisualState();
        }

        [ContextMenu("Test Learn Animation")]
        public void TestLearnAnimationDebug()
        {
            AnimateLearn();
        }

        [ContextMenu("Test Touch Feedback")]
        public void TestTouchFeedbackDebug()
        {
            AnimateTouchFeedback();
        }

        [ContextMenu("Log Node Info")]
        public void LogNodeInfoDebug()
        {
            if (IsInitialized)
            {
                Debug.Log($"[TalentNode] {TalentModel.Name}:");
                Debug.Log($"  Type: {TalentModel.NodeType}");
                Debug.Log($"  Level: {ProgressInfo.CurrentLevel}/{ProgressInfo.MaxLevel}");
                Debug.Log($"  Status: {ProgressInfo.UnlockStatus}");
                Debug.Log($"  Cost: {ProgressInfo.NextLevelCost}");
                Debug.Log($"  Can Interact: {CanInteract()}");
            }
        }
    }
}