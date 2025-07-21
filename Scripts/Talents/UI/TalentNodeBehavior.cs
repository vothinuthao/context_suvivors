using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Talents.Data;
using Talents.Manager;

namespace Talents.UI
{
    /// <summary>
    /// Behavior for individual talent nodes in the talent tree
    /// </summary>
    public class TalentNodeBehavior : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image nodeIcon;
        [SerializeField] private Image nodeBackground;
        [SerializeField] private Image nodeBorder;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject maxLevelIcon;
        [SerializeField] private Button nodeButton;

        [Header("Connection Lines")]
        [SerializeField] private LineRenderer connectionLine;
        [SerializeField] private Transform[] connectionPoints;

        [Header("Visual States")]
        [SerializeField] private Color lockedColor = Color.gray;
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color learnedColor = Color.green;
        [SerializeField] private Color maxLevelColor = Color.yellow;
        [SerializeField] private Color insufficientPointsColor = Color.red;

        [Header("Node Types")]
        [SerializeField] private Color normalNodeColor = Color.white;
        [SerializeField] private Color specialNodeColor = Color.yellow;

        // Properties
        public TalentModel TalentModel { get; private set; }
        public TalentProgressInfo ProgressInfo { get; private set; }
        public bool IsInitialized { get; private set; }

        // Events
        public UnityEvent<TalentNodeBehavior> OnNodeClicked;

        // Cached components
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;

        // Connection management
        private TalentNodeBehavior parentNode;
        private List<TalentNodeBehavior> childNodes = new List<TalentNodeBehavior>();

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Setup button for mobile touch
            if (nodeButton != null)
            {
                nodeButton.onClick.AddListener(() => OnNodeClicked?.Invoke(this));
            }
        }

        /// <summary>
        /// Initialize the node with talent data
        /// </summary>
        public void Initialize(TalentModel talentModel)
        {
            TalentModel = talentModel;
            IsInitialized = true;

            // Set position
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = talentModel.Position;
            }

            // Load and set icon
            if (nodeIcon != null && talentModel.Icon != null)
            {
                nodeIcon.sprite = talentModel.Icon;
            }

            // Set node type color
            SetNodeTypeVisuals();

            // Update visual state
            UpdateVisualState();

            // Setup connections
            SetupConnections();
        }

        /// <summary>
        /// Set visual style based on node type
        /// </summary>
        private void SetNodeTypeVisuals()
        {
            if (nodeBackground != null)
            {
                Color typeColor = TalentModel.NodeType == TalentNodeType.Normal ? normalNodeColor : specialNodeColor;
                nodeBackground.color = typeColor;
            }
        }

        /// <summary>
        /// Update the visual state of the node
        /// </summary>
        public void UpdateVisualState()
        {
            if (!IsInitialized || TalentManager.Instance == null)
                return;

            // Get current progress info
            ProgressInfo = TalentManager.Instance.GetTalentProgressInfo(TalentModel.ID);

            // Update level text
            UpdateLevelText();

            // Update cost text
            UpdateCostText();

            // Update visual state based on unlock status
            UpdateNodeAppearance();

            // Update interaction state
            UpdateInteractionState();
        }

        /// <summary>
        /// Update level display
        /// </summary>
        private void UpdateLevelText()
        {
            if (levelText != null)
            {
                if (ProgressInfo.CurrentLevel > 0)
                {
                    levelText.text = $"{ProgressInfo.CurrentLevel}/{ProgressInfo.MaxLevel}";
                    levelText.gameObject.SetActive(true);
                }
                else
                {
                    levelText.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Update cost display
        /// </summary>
        private void UpdateCostText()
        {
            if (costText != null)
            {
                if (ProgressInfo.UnlockStatus == TalentUnlockStatus.Available || 
                    ProgressInfo.UnlockStatus == TalentUnlockStatus.InsufficientPoints)
                {
                    costText.text = ProgressInfo.NextLevelCost.ToString();
                    costText.gameObject.SetActive(true);
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
                    break;
                case TalentUnlockStatus.InsufficientPoints:
                    targetColor = insufficientPointsColor;
                    break;
                case TalentUnlockStatus.MaxLevel:
                    targetColor = maxLevelColor;
                    showMaxLevel = true;
                    break;
            }

            // Apply color to border
            if (nodeBorder != null)
            {
                nodeBorder.color = targetColor;
            }

            // Show/hide lock icon
            if (lockIcon != null)
            {
                lockIcon.SetActive(showLock);
            }

            // Show/hide max level icon
            if (maxLevelIcon != null)
            {
                maxLevelIcon.SetActive(showMaxLevel);
            }
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

            if (canvasGroup != null)
            {
                canvasGroup.alpha = ProgressInfo.UnlockStatus == TalentUnlockStatus.Locked ? 0.5f : 1f;
            }
        }

        /// <summary>
        /// Setup connection lines to parent talents
        /// </summary>
        private void SetupConnections()
        {
            if (connectionLine == null)
                return;

            var dependencies = TalentDatabase.Instance.GetTalentDependencies(TalentModel.ID);
            
            if (dependencies.Count > 0)
            {
                // For simplicity, connect to first dependency
                var parentTalentId = dependencies[0];
                var parentTalent = TalentDatabase.Instance.GetTalentById(parentTalentId);
                
                if (parentTalent != null)
                {
                    DrawConnectionLine(parentTalent.Position);
                }
            }
            else
            {
                // No connections, disable line renderer
                connectionLine.enabled = false;
            }
        }

        /// <summary>
        /// Draw connection line to parent node
        /// </summary>
        private void DrawConnectionLine(Vector2 parentPosition)
        {
            if (connectionLine == null)
                return;

            connectionLine.enabled = true;
            connectionLine.positionCount = 2;
            connectionLine.useWorldSpace = false;

            // Set line points
            Vector3 startPoint = parentPosition - (Vector2)rectTransform.anchoredPosition;
            Vector3 endPoint = Vector3.zero;

            connectionLine.SetPosition(0, startPoint);
            connectionLine.SetPosition(1, endPoint);

            // Update line color based on unlock status
            UpdateConnectionLineColor();
        }

        /// <summary>
        /// Update connection line color based on node status
        /// </summary>
        private void UpdateConnectionLineColor()
        {
            if (connectionLine == null)
                return;

            Color lineColor = ProgressInfo.UnlockStatus == TalentUnlockStatus.Locked ? 
                             Color.gray : Color.white;

            connectionLine.startColor = lineColor;
            connectionLine.endColor = lineColor;
        }


        /// <summary>
        /// Get tooltip text for this node
        /// </summary>
        public string GetTooltipText()
        {
            if (!IsInitialized || TalentManager.Instance == null)
                return "Unknown Talent";

            return TalentManager.Instance.GetTalentTooltip(TalentModel.ID);
        }

        /// <summary>
        /// Set node as highlighted (for prerequisite visualization)
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (nodeBorder != null)
            {
                nodeBorder.enabled = highlighted;
            }
        }

        /// <summary>
        /// Animate node (for when talent is learned)
        /// </summary>
        public void AnimateLearn()
        {
            // Simple scale animation using LeanTween
            gameObject.transform.DOScale(Vector3.one * 1.2f, 0.2f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => {
                    gameObject.transform.DOScale(Vector3.one, 0.2f)
                        .SetEase(Ease.InBack);
                });
        }

        /// <summary>
        /// Update connections when talent tree changes
        /// </summary>
        public void UpdateConnections()
        {
            SetupConnections();
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        private void OnDestroy()
        {
            if (nodeButton != null)
            {
                nodeButton.onClick.RemoveAllListeners();
            }
        }

        // Debug methods
        [ContextMenu("Force Update Visual State")]
        public void ForceUpdateVisualState()
        {
            UpdateVisualState();
        }

        [ContextMenu("Log Node Info")]
        public void LogNodeInfo()
        {
            if (IsInitialized)
            {
                Debug.Log($"[TalentNode] {TalentModel.Name} - Level: {ProgressInfo.CurrentLevel}/{ProgressInfo.MaxLevel} - Status: {ProgressInfo.UnlockStatus}");
            }
        }
    }
}