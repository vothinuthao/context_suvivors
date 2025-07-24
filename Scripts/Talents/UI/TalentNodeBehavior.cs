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
    /// Mobile-optimized talent node - Simple click interaction only
    /// </summary>
    public class TalentNodeBehavior : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image nodeIcon;
        [SerializeField] private Image nodeBackground;
        [SerializeField] private Image nodeBorder;
        [SerializeField] private Button nodeButton;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text costText;

        [Header("Visual Elements")]
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject maxLevelIcon;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image currencyIcon;

        [Header("Visual States")]
        [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color learnedColor = new Color(0.3f, 1f, 0.3f);
        [SerializeField] private Color maxLevelColor = new Color(1f, 0.8f, 0f);
        [SerializeField] private Color insufficientColor = new Color(1f, 0.3f, 0.3f);

        [Header("Animation")]
        [SerializeField] private float touchScale = 0.95f;
        [SerializeField] private float animDuration = 0.1f;

        // Properties
        public TalentModel TalentModel { get; private set; }
        public TalentProgressInfo ProgressInfo { get; private set; }
        public bool IsInitialized { get; private set; }

        // Events
        public UnityEvent<TalentNodeBehavior> OnNodeClicked = new UnityEvent<TalentNodeBehavior>();

        // Cache
        private RectTransform rectTransform;
        private Vector3 originalScale;
        private bool isAnimating = false;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            originalScale = transform.localScale;
            SetupButton();
        }

        private void SetupButton()
        {
            if (nodeButton != null)
            {
                nodeButton.onClick.AddListener(OnButtonClicked);
            }
            else
            {
                nodeButton = GetComponent<Button>();
                if (nodeButton == null)
                    nodeButton = gameObject.AddComponent<Button>();
                nodeButton.onClick.AddListener(OnButtonClicked);
            }

            // Mobile optimization
            var navigation = nodeButton.navigation;
            navigation.mode = Navigation.Mode.None;
            nodeButton.navigation = navigation;
            nodeButton.transition = Selectable.Transition.None;
        }

        /// <summary>
        /// Initialize node with talent data
        /// </summary>
        public void Initialize(TalentModel talentModel)
        {
            TalentModel = talentModel;
            IsInitialized = true;

            // Set position
            rectTransform.anchoredPosition = new Vector2(talentModel.PositionX, talentModel.PositionY);

            // Setup UI
            SetIcon(talentModel);
            SetBasicInfo(talentModel);
            SetNodeTypeVisuals(talentModel);
            UpdateVisualState();
        }

        private void SetIcon(TalentModel talent)
        {
            if (nodeIcon == null) return;

            if (talent.Icon != null)
            {
                nodeIcon.sprite = talent.Icon;
            }
            else
            {
                var icon = Resources.Load<Sprite>($"Icons/Talents/{talent.IconPath}");
                if (icon != null)
                    nodeIcon.sprite = icon;
            }
        }

        private void SetBasicInfo(TalentModel talent)
        {
            // Set name
            if (nameText != null)
                nameText.text = talent.Name;

            // Set cost with currency
            if (costText != null)
                costText.text = talent.Cost.ToString();

            // Set currency icon
            if (currencyIcon != null)
            {
                var iconPath = talent.NodeType == TalentNodeType.Normal ? 
                    "Icons/UI/gold_icon" : "Icons/UI/orc_icon";
                var sprite = Resources.Load<Sprite>(iconPath);
                if (sprite != null)
                    currencyIcon.sprite = sprite;
            }
        }

        private void SetNodeTypeVisuals(TalentModel talent)
        {
            if (nodeBackground != null)
            {
                Color bgColor = GetNodeColor(talent);
                nodeBackground.color = bgColor;
            }

            // Setup progress for normal talents
            if (progressSlider != null)
            {
                bool showProgress = talent.NodeType == TalentNodeType.Normal;
                progressSlider.gameObject.SetActive(showProgress);
                
                if (showProgress)
                {
                    progressSlider.maxValue = talent.MaxLevel;
                    progressSlider.interactable = false;
                }
            }
        }

        private Color GetNodeColor(TalentModel talent)
        {
            if (talent.NodeType == TalentNodeType.Special)
                return new Color(1f, 0.8f, 0f); // Gold

            // Base stat colors
            switch (talent.StatType)
            {
                case UpgradeType.Damage:
                    return new Color(1f, 0.3f, 0.3f); // Red
                case UpgradeType.Health:
                    return new Color(0.3f, 1f, 0.3f); // Green
                default:
                    return new Color(0.3f, 0.3f, 1f); // Blue
            }
        }

        /// <summary>
        /// Update visual state based on talent progress
        /// </summary>
        public void UpdateVisualState()
        {
            if (!IsInitialized || TalentManager.Instance == null)
                return;

            ProgressInfo = TalentManager.Instance.GetTalentProgressInfo(TalentModel.ID);

            UpdateLevelDisplay();
            UpdateCostDisplay();
            UpdateNodeAppearance();
            UpdateInteraction();
            UpdateProgress();
        }

        private void UpdateLevelDisplay()
        {
            if (levelText == null) return;

            if (TalentModel.NodeType == TalentNodeType.Normal)
            {
                levelText.text = $"{ProgressInfo.CurrentLevel}/{ProgressInfo.MaxLevel}";
            }
            else
            {
                levelText.text = ProgressInfo.CurrentLevel > 0 ? "LEARNED" : $"Lv.{TalentModel.RequiredPlayerLevel}";
                levelText.color = ProgressInfo.CurrentLevel > 0 ? learnedColor : Color.gray;
            }
        }

        private void UpdateCostDisplay()
        {
            if (costText == null) return;

            if (ProgressInfo.UnlockStatus == TalentUnlockStatus.MaxLevel)
            {
                costText.text = "MAX";
                costText.color = maxLevelColor;
            }
            else if (ProgressInfo.NextLevelCost > 0)
            {
                costText.text = ProgressInfo.NextLevelCost.ToString();
                bool canAfford = ProgressInfo.UnlockStatus == TalentUnlockStatus.Available;
                costText.color = canAfford ? Color.white : insufficientColor;
            }
        }

        private void UpdateNodeAppearance()
        {
            Color borderColor = lockedColor;
            bool showLock = false;
            bool showMaxLevel = false;

            switch (ProgressInfo.UnlockStatus)
            {
                case TalentUnlockStatus.Locked:
                    borderColor = lockedColor;
                    showLock = true;
                    break;
                case TalentUnlockStatus.Available:
                    borderColor = availableColor;
                    break;
                case TalentUnlockStatus.Learned:
                    borderColor = learnedColor;
                    break;
                case TalentUnlockStatus.InsufficientPoints:
                    borderColor = insufficientColor;
                    break;
                case TalentUnlockStatus.MaxLevel:
                    borderColor = maxLevelColor;
                    showMaxLevel = true;
                    break;
            }

            if (nodeBorder != null)
                nodeBorder.color = borderColor;
            
            if (lockIcon != null)
                lockIcon.SetActive(showLock);
            
            if (maxLevelIcon != null)
                maxLevelIcon.SetActive(showMaxLevel);
        }

        private void UpdateInteraction()
        {
            bool canInteract = ProgressInfo.UnlockStatus == TalentUnlockStatus.Available;
            
            if (nodeButton != null)
                nodeButton.interactable = canInteract;
        }

        private void UpdateProgress()
        {
            if (TalentModel.NodeType == TalentNodeType.Normal && progressSlider != null)
            {
                progressSlider.value = ProgressInfo.CurrentLevel;
                
                var fillImage = progressSlider.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                {
                    float progress = (float)ProgressInfo.CurrentLevel / ProgressInfo.MaxLevel;
                    fillImage.color = Color.Lerp(Color.red, Color.green, progress);
                }
            }
        }

        private void OnButtonClicked()
        {
            if (!IsInitialized) return;

            AnimateTouchFeedback();
            OnNodeClicked?.Invoke(this);
        }

        private void AnimateTouchFeedback()
        {
            if (isAnimating) return;

            isAnimating = true;
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(originalScale * touchScale, animDuration));
            sequence.Append(transform.DOScale(originalScale, animDuration));
            sequence.OnComplete(() => isAnimating = false);
        }

        /// <summary>
        /// Animate when talent is learned
        /// </summary>
        public void AnimateLearn()
        {
            if (isAnimating) return;

            isAnimating = true;
            var sequence = DOTween.Sequence();
            
            // Scale animation
            sequence.Append(transform.DOScale(originalScale * 1.2f, 0.2f).SetEase(Ease.OutBack));
            sequence.Append(transform.DOScale(originalScale, 0.2f).SetEase(Ease.InBack));
            
            // Border flash
            if (nodeBorder != null)
            {
                var originalColor = nodeBorder.color;
                sequence.Insert(0, nodeBorder.DOColor(Color.white, 0.1f).SetLoops(3, LoopType.Yoyo));
            }

            sequence.OnComplete(() => {
                isAnimating = false;
                UpdateVisualState();
            });
        }

        /// <summary>
        /// Set highlighted state
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (nodeBorder != null)
            {
                if (highlighted)
                    nodeBorder.color = Color.yellow;
                else
                    UpdateNodeAppearance();
            }
        }

        /// <summary>
        /// Get tooltip for mobile
        /// </summary>
        public string GetTooltip()
        {
            if (!IsInitialized) return "";

            string tooltip = $"<b>{TalentModel.Name}</b>\n";
            
            if (TalentModel.NodeType == TalentNodeType.Normal)
            {
                tooltip += $"Level: {ProgressInfo.CurrentLevel}/{ProgressInfo.MaxLevel}\n";
                tooltip += $"Bonus: +{TalentModel.StatValue * (ProgressInfo.CurrentLevel + 1)}\n";
            }
            else
            {
                tooltip += $"{TalentModel.Description}\n";
                tooltip += $"Requires Level: {TalentModel.RequiredPlayerLevel}\n";
            }

            if (ProgressInfo.NextLevelCost > 0)
            {
                string currency = TalentModel.NodeType == TalentNodeType.Normal ? "Gold" : "Orc";
                tooltip += $"Cost: {ProgressInfo.NextLevelCost} {currency}";
            }

            return tooltip;
        }

        /// <summary>
        /// Check if can interact
        /// </summary>
        public bool CanInteract()
        {
            return IsInitialized && ProgressInfo.UnlockStatus == TalentUnlockStatus.Available;
        }

        /// <summary>
        /// Force refresh
        /// </summary>
        public void ForceRefresh()
        {
            if (IsInitialized)
                UpdateVisualState();
        }

        private void OnDestroy()
        {
            DOTween.Kill(gameObject);
            
            if (nodeButton != null)
                nodeButton.onClick.RemoveAllListeners();
                
            OnNodeClicked?.RemoveAllListeners();
        }
    }
}