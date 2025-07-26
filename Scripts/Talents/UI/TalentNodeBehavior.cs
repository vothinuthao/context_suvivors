using DG.Tweening;
using OctoberStudio.Upgrades;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Talents.Data;
using Talents.Manager;
using Talents.Config;

namespace Talents.UI
{
    /// <summary>
    /// Updated talent node with proper icon loading and layout configuration support
    /// </summary>
    public class TalentNodeBehavior : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image nodeIcon;
        [SerializeField] private Image nodeBackground;
        [SerializeField] private Image nodeBorder;
        [SerializeField] private Button nodeButton;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text bonusText;
        [SerializeField] private TMP_Text costText;

        [Header("Visual Elements")]
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject learnedIcon;
        [SerializeField] private Image currencyIcon;
        [SerializeField] private CanvasGroup nodeCanvasGroup;

        [Header("Visual States")]
        [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color learnedColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color insufficientColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);

        [Header("Animation")]
        [SerializeField] private float touchScale = 0.9f;
        [SerializeField] private float animDuration = 0.15f;

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
        private TalentLayoutConfig layoutConfig;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            originalScale = transform.localScale;
            
            // Get canvas group or create one
            if (nodeCanvasGroup == null)
                nodeCanvasGroup = GetComponent<CanvasGroup>();
            if (nodeCanvasGroup == null)
                nodeCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            SetupButton();
        }

        private void SetupButton()
        {
            if (nodeButton == null)
                nodeButton = GetComponent<Button>();
            
            if (nodeButton == null)
                nodeButton = gameObject.AddComponent<Button>();

            nodeButton.onClick.AddListener(OnButtonClicked);

            // Mobile optimization - disable navigation
            var navigation = nodeButton.navigation;
            navigation.mode = Navigation.Mode.None;
            nodeButton.navigation = navigation;
            nodeButton.transition = Selectable.Transition.None;
        }

        /// <summary>
        /// Initialize node with talent data and layout config
        /// </summary>
        public void Initialize(TalentModel talentModel)
        {
            TalentModel = talentModel;
            layoutConfig = TalentDatabase.Instance?.LayoutConfig;
            IsInitialized = true;

            // Apply layout configuration
            ApplyLayoutConfiguration();
            
            // Setup visual elements
            SetupIcon(talentModel);
            SetupTexts(talentModel);
            SetupCurrencyIcon(talentModel);
            
            // Update visual state
            UpdateVisualState();
        }

        /// <summary>
        /// Apply layout configuration to node size and position
        /// </summary>
        private void ApplyLayoutConfiguration()
        {
            if (layoutConfig == null) return;

            // Set node size based on type
            Vector2 nodeSize = TalentModel.NodeType == TalentNodeType.Normal ? 
                layoutConfig.NormalNodeSize : layoutConfig.SpecialNodeSize;
            
            rectTransform.sizeDelta = nodeSize;
            
            // Set position from talent model
            rectTransform.anchoredPosition = new Vector2(TalentModel.PositionX, TalentModel.PositionY);
            
            // Apply icon size
            if (nodeIcon != null)
            {
                var iconRect = nodeIcon.GetComponent<RectTransform>();
                if (iconRect != null)
                {
                    iconRect.sizeDelta = layoutConfig.IconSize;
                }
            }
        }

        /// <summary>
        /// Setup node icon with proper path resolution
        /// </summary>
        private void SetupIcon(TalentModel talent)
        {
            if (nodeIcon == null) return;

            StartCoroutine(LoadIconCoroutine(talent));
        }

        /// <summary>
        /// Coroutine to load icon asynchronously
        /// </summary>
        private System.Collections.IEnumerator LoadIconCoroutine(TalentModel talent)
        {
            yield return new WaitForEndOfFrame(); // Wait for initialization

            Sprite iconSprite = null;
            
            // Try to load icon from specified path
            if (!string.IsNullOrEmpty(talent.IconPath))
            {
                string fullPath = layoutConfig != null ? 
                    layoutConfig.IconBasePath + talent.IconPath : 
                    "Icons/Talents/" + talent.IconPath;
                
                iconSprite = Resources.Load<Sprite>(fullPath);
                
                if (iconSprite == null)
                {
                    // Try without base path
                    iconSprite = Resources.Load<Sprite>(talent.IconPath);
                }
                
                if (iconSprite == null)
                {
                    Debug.LogWarning($"[TalentNodeBehavior] Could not load icon at path: {fullPath} for talent {talent.Name}");
                }
            }
            
            // Use fallback icon if loading failed
            if (iconSprite == null)
            {
                iconSprite = LoadFallbackIcon(talent);
            }
            
            // Apply icon
            if (iconSprite != null)
            {
                nodeIcon.sprite = iconSprite;
                nodeIcon.color = Color.white;
            }
            else
            {
                // Create simple colored icon as last resort
                CreateSimpleIcon(talent);
            }
        }

        /// <summary>
        /// Load fallback icon based on talent type
        /// </summary>
        private Sprite LoadFallbackIcon(TalentModel talent)
        {
            string fallbackPath = "";
            
            if (talent.NodeType == TalentNodeType.Normal)
            {
                fallbackPath = GetDefaultNormalIconPath(talent);
            }
            else
            {
                fallbackPath = layoutConfig?.DefaultSpecialIcon ?? "special_default";
            }
            
            // Try to load fallback
            var fallbackSprite = Resources.Load<Sprite>("Icons/Talents/" + fallbackPath);
            if (fallbackSprite == null)
            {
                fallbackSprite = Resources.Load<Sprite>("Icons/Talents/default_talent");
            }
            
            return fallbackSprite;
        }

        /// <summary>
        /// Get default icon path for normal talents
        /// </summary>
        private string GetDefaultNormalIconPath(TalentModel talent)
        {
            if (talent.Name.Contains("Attack") || talent.StatType == UpgradeType.Damage)
                return "atk_icon";
            if (talent.Name.Contains("Defense") || talent.Name.Contains("Armor"))
                return "def_icon";
            if (talent.Name.Contains("Speed") || talent.StatType == UpgradeType.Speed)
                return "speed_icon";
            if (talent.Name.Contains("Heal") || talent.Name.Contains("Health"))
                return "heal_icon";
            
            return layoutConfig?.DefaultNormalIcon ?? "default_normal";
        }

        /// <summary>
        /// Create simple colored icon as last resort
        /// </summary>
        private void CreateSimpleIcon(TalentModel talent)
        {
            if (nodeIcon == null) return;
            
            // Create simple colored square
            var texture = new Texture2D(1, 1);
            Color iconColor = GetTalentTypeColor(talent);
            texture.SetPixel(0, 0, iconColor);
            texture.Apply();
            
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            nodeIcon.sprite = sprite;
            nodeIcon.color = Color.white;
            
            Debug.LogWarning($"[TalentNodeBehavior] Using generated icon for talent {talent.Name}");
        }

        /// <summary>
        /// Get color based on talent type
        /// </summary>
        private Color GetTalentTypeColor(TalentModel talent)
        {
            if (talent.NodeType == TalentNodeType.Special)
                return new Color(1f, 0.8f, 0f); // Gold
                
            // Colors for normal stats
            if (talent.Name.Contains("Attack")) return new Color(1f, 0.3f, 0.3f); // Red
            if (talent.Name.Contains("Defense")) return new Color(0.3f, 0.3f, 1f); // Blue
            if (talent.Name.Contains("Speed")) return new Color(0.3f, 1f, 0.3f); // Green
            if (talent.Name.Contains("Heal")) return new Color(1f, 1f, 0.3f); // Yellow
            
            return Color.white;
        }

        /// <summary>
        /// Setup text elements with proper sizing
        /// </summary>
        private void SetupTexts(TalentModel talent)
        {
            // Set name with appropriate font size
            if (nameText != null)
            {
                nameText.text = GetDisplayName(talent);
                nameText.color = Color.white;
                
                // Auto-adjust font size based on node size
                if (layoutConfig != null)
                {
                    float baseFontSize = TalentModel.NodeType == TalentNodeType.Normal ? 16f : 18f;
                    nameText.fontSize = baseFontSize * (layoutConfig.NormalNodeSize.x / 120f); // Scale based on node size
                }
            }

            // Set bonus text
            if (bonusText != null)
            {
                if (talent.NodeType == TalentNodeType.Normal)
                {
                    bonusText.text = $"+{talent.StatValue:F0}";
                    bonusText.color = Color.green;
                }
                else
                {
                    bonusText.text = $"Lv.{talent.RequiredPlayerLevel}";
                    bonusText.color = Color.yellow;
                }
                
                // Auto-adjust font size
                if (layoutConfig != null)
                {
                    float baseFontSize = 14f;
                    bonusText.fontSize = baseFontSize * (layoutConfig.NormalNodeSize.x / 120f);
                }
            }

            // Set cost text
            if (costText != null)
            {
                costText.text = talent.Cost.ToString();
                costText.color = Color.white;
                
                // Auto-adjust font size
                if (layoutConfig != null)
                {
                    float baseFontSize = 12f;
                    costText.fontSize = baseFontSize * (layoutConfig.NormalNodeSize.x / 120f);
                }
            }
        }

        /// <summary>
        /// Get display name for node
        /// </summary>
        private string GetDisplayName(TalentModel talent)
        {
            if (talent.NodeType == TalentNodeType.Normal)
            {
                // Shorten normal stat names for better display
                if (talent.Name.Contains("Attack")) return "ATK";
                if (talent.Name.Contains("Defense")) return "DEF";
                if (talent.Name.Contains("Speed")) return "SPD";
                if (talent.Name.Contains("Heal")) return "HEAL";
            }
            
            // For special nodes, truncate long names
            return talent.Name.Length > 8 ? talent.Name.Substring(0, 8) + "..." : talent.Name;
        }

        /// <summary>
        /// Setup currency icon
        /// </summary>
        private void SetupCurrencyIcon(TalentModel talent)
        {
            if (currencyIcon == null) return;

            string currencyIconPath = talent.NodeType == TalentNodeType.Normal ? 
                "Icons/UI/gold_icon" : "Icons/UI/orc_icon";
                
            var sprite = Resources.Load<Sprite>(currencyIconPath);
            if (sprite != null)
            {
                currencyIcon.sprite = sprite;
                currencyIcon.gameObject.SetActive(true);
            }
            else
            {
                // Create simple colored icon for currency
                var texture = new Texture2D(1, 1);
                Color currencyColor = talent.NodeType == TalentNodeType.Normal ? 
                    new Color(1f, 0.8f, 0f) : new Color(0.5f, 0.3f, 0.1f); // Gold vs Brown
                texture.SetPixel(0, 0, currencyColor);
                texture.Apply();
                
                var currencySprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
                currencyIcon.sprite = currencySprite;
                currencyIcon.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Update visual state based on current progress
        /// </summary>
        public void UpdateVisualState()
        {
            if (!IsInitialized || TalentManager.Instance == null)
                return;

            ProgressInfo = TalentManager.Instance.GetTalentProgressInfo(TalentModel.ID);

            UpdateNodeAppearance();
            UpdateCostDisplay();
            UpdateInteractionState();
            UpdateStatusIcons();
        }

        /// <summary>
        /// Update node appearance based on unlock status
        /// </summary>
        private void UpdateNodeAppearance()
        {
            Color backgroundColor = lockedColor;
            Color borderColor = lockedColor;
            float alpha = 1f;

            switch (ProgressInfo.UnlockStatus)
            {
                case TalentUnlockStatus.Locked:
                    backgroundColor = lockedColor;
                    borderColor = lockedColor;
                    alpha = 0.6f;
                    break;
                    
                case TalentUnlockStatus.Available:
                    backgroundColor = availableColor;
                    borderColor = availableColor;
                    alpha = 1f;
                    break;
                    
                case TalentUnlockStatus.Learned:
                    backgroundColor = learnedColor;
                    borderColor = learnedColor;
                    alpha = 1f;
                    break;
                    
                case TalentUnlockStatus.InsufficientPoints:
                    backgroundColor = insufficientColor;
                    borderColor = insufficientColor;
                    alpha = 0.8f;
                    break;
            }

            // Apply colors
            if (nodeBackground != null)
                nodeBackground.color = backgroundColor;

            if (nodeBorder != null)
                nodeBorder.color = borderColor;

            // Apply overall alpha
            if (nodeCanvasGroup != null)
                nodeCanvasGroup.alpha = alpha;
        }

        /// <summary>
        /// Update cost display
        /// </summary>
        private void UpdateCostDisplay()
        {
            if (costText == null) return;

            if (ProgressInfo.UnlockStatus == TalentUnlockStatus.Learned)
            {
                costText.text = "✓";
                costText.color = learnedColor;
            }
            else
            {
                costText.text = TalentModel.Cost.ToString();
                
                // Color based on affordability
                bool canAfford = ProgressInfo.UnlockStatus == TalentUnlockStatus.Available;
                costText.color = canAfford ? Color.white : insufficientColor;
            }
        }

        /// <summary>
        /// Update interaction state
        /// </summary>
        private void UpdateInteractionState()
        {
            bool canInteract = ProgressInfo.UnlockStatus == TalentUnlockStatus.Available;
            
            if (nodeButton != null)
                nodeButton.interactable = canInteract;
        }

        /// <summary>
        /// Update status icons
        /// </summary>
        private void UpdateStatusIcons()
        {
            bool showLock = ProgressInfo.UnlockStatus == TalentUnlockStatus.Locked;
            bool showLearned = ProgressInfo.UnlockStatus == TalentUnlockStatus.Learned;

            if (lockIcon != null)
                lockIcon.SetActive(showLock);

            if (learnedIcon != null)
                learnedIcon.SetActive(showLearned);
        }

        /// <summary>
        /// Handle button click
        /// </summary>
        private void OnButtonClicked()
        {
            if (!IsInitialized) return;

            AnimateTouchFeedback();
            OnNodeClicked?.Invoke(this);
        }

        /// <summary>
        /// Animate touch feedback
        /// </summary>
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
            
            // Scale pulse animation
            sequence.Append(transform.DOScale(originalScale * 1.3f, 0.2f).SetEase(Ease.OutBack));
            sequence.Append(transform.DOScale(originalScale, 0.3f).SetEase(Ease.InOutQuad));
            
            // Border flash animation
            if (nodeBorder != null)
            {
                var originalColor = nodeBorder.color;
                sequence.Insert(0, nodeBorder.DOColor(Color.white, 0.1f)
                    .SetLoops(4, LoopType.Yoyo)
                    .OnComplete(() => nodeBorder.color = originalColor));
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
                {
                    nodeBorder.color = Color.yellow;
                    nodeBorder.DOColor(Color.white, 0.5f).SetLoops(-1, LoopType.Yoyo);
                }
                else
                {
                    DOTween.Kill(nodeBorder);
                    UpdateNodeAppearance();
                }
            }
        }

        /// <summary>
        /// Get tooltip text for this node
        /// </summary>
        public string GetTooltip()
        {
            if (!IsInitialized) return "";

            string tooltip = $"<b>{TalentModel.Name}</b>\n";
            tooltip += $"{TalentModel.Description}\n\n";
            
            if (TalentModel.NodeType == TalentNodeType.Normal)
            {
                tooltip += $"Stat Bonus: +{TalentModel.StatValue:F1} {TalentModel.StatType}\n";
                tooltip += $"Cost: {TalentModel.Cost} Gold\n";
            }
            else
            {
                tooltip += $"Special Ability\n";
                tooltip += $"Required Level: {TalentModel.RequiredPlayerLevel}\n";
                tooltip += $"Cost: {TalentModel.Cost} Orc\n";
            }

            // Add status info
            switch (ProgressInfo.UnlockStatus)
            {
                case TalentUnlockStatus.Learned:
                    tooltip += "<color=green>✓ LEARNED</color>";
                    break;
                case TalentUnlockStatus.Available:
                    tooltip += "<color=yellow>◉ AVAILABLE</color>";
                    break;
                case TalentUnlockStatus.InsufficientPoints:
                    tooltip += "<color=red>✗ INSUFFICIENT CURRENCY</color>";
                    break;
                case TalentUnlockStatus.Locked:
                    tooltip += "<color=red>🔒 LOCKED</color>";
                    break;
            }

            return tooltip;
        }

        /// <summary>
        /// Public helper methods
        /// </summary>
        public bool CanInteract()
        {
            return IsInitialized && ProgressInfo.UnlockStatus == TalentUnlockStatus.Available;
        }

        public void ForceRefresh()
        {
            if (IsInitialized)
                UpdateVisualState();
        }

        public int GetZoneLevel()
        {
            return TalentModel?.RequiredPlayerLevel ?? 1;
        }

        public bool IsNormalNode()
        {
            return TalentModel?.NodeType == TalentNodeType.Normal;
        }

        public bool IsSpecialNode()
        {
            return TalentModel?.NodeType == TalentNodeType.Special;
        }

        public string GetStatType()
        {
            if (!IsNormalNode()) return "";
            
            if (TalentModel.Name.Contains("Attack")) return "ATK";
            if (TalentModel.Name.Contains("Defense")) return "DEF";
            if (TalentModel.Name.Contains("Speed")) return "SPD";
            if (TalentModel.Name.Contains("Heal")) return "HEAL";
            return "";
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        private void OnDestroy()
        {
            DOTween.Kill(gameObject);
            
            if (nodeButton != null)
                nodeButton.onClick.RemoveAllListeners();
                
            OnNodeClicked?.RemoveAllListeners();
        }
    }
}