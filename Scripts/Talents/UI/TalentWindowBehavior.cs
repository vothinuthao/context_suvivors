using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Talents.Data;
using Talents.Manager;
using Talents.UI;
using OctoberStudio.Extensions;

namespace Talents.UI
{
    /// <summary>
    /// Main talent window behavior - handles the talent tree UI
    /// </summary>
    public class TalentWindowBehavior : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button resetAllButton;
        [SerializeField] private TMP_Text talentPointsText;
        [SerializeField] private TMP_Text titleText;

        [Header("Talent Tree")]
        [SerializeField] private ScrollRect talentScrollRect;
        [SerializeField] private RectTransform talentTreeContent;
        [SerializeField] private GameObject talentNodePrefab;

        [Header("Tooltip")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TMP_Text tooltipText;
        [SerializeField] private RectTransform tooltipRectTransform;

        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private TMP_Text confirmationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("Tree Layout")]
        [SerializeField] private float nodeSpacing = 100f;
        [SerializeField] private float leftTreeX = -200f;
        [SerializeField] private float rightTreeX = 200f;
        [SerializeField] private float treeStartY = 300f;

        [Header("Performance")]
        [SerializeField] private bool useObjectPooling = true;
        [SerializeField] private int initialPoolSize = 70;

        // Node management
        private Dictionary<int, TalentNodeBehavior> talentNodes = new Dictionary<int, TalentNodeBehavior>();
        private List<TalentNodeBehavior> nodePool = new List<TalentNodeBehavior>();
        private List<TalentNodeBehavior> activeNodes = new List<TalentNodeBehavior>();

        // State
        private bool isInitialized = false;
        private System.Action confirmationCallback;

        // Events
        public UnityEvent OnTalentTreeUpdated;

        /// <summary>
        /// Initialize the talent window
        /// </summary>
        public void Init(UnityAction onBackButtonClicked)
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(onBackButtonClicked);
            }

            if (resetAllButton != null)
            {
                resetAllButton.onClick.AddListener(ShowResetConfirmation);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }

            // Initialize object pool
            if (useObjectPooling)
            {
                InitializeObjectPool();
            }

            // Subscribe to talent manager events
            if (TalentManager.Instance != null)
            {
                TalentManager.Instance.OnGoldCoinsChanged.AddListener(UpdateTalentPointsUI);
                TalentManager.Instance.OnTalentLearned.AddListener(OnTalentLearned);
                TalentManager.Instance.OnTalentUpgraded.AddListener(OnTalentUpgraded);
            }

            // Hide tooltip and confirmation initially
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
            
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);

            isInitialized = true;
        }

        /// <summary>
        /// Initialize object pool for talent nodes
        /// </summary>
        private void InitializeObjectPool()
        {
            if (talentNodePrefab == null)
                return;

            nodePool.Clear();
            
            for (int i = 0; i < initialPoolSize; i++)
            {
                var node = Instantiate(talentNodePrefab, talentTreeContent).GetComponent<TalentNodeBehavior>();
                node.gameObject.SetActive(false);
                nodePool.Add(node);
            }
        }

        /// <summary>
        /// Get a node from the pool or create new one
        /// </summary>
        private TalentNodeBehavior GetPooledNode()
        {
            if (useObjectPooling)
            {
                foreach (var node in nodePool)
                {
                    if (!node.gameObject.activeSelf)
                    {
                        node.gameObject.SetActive(true);
                        return node;
                    }
                }
            }

            // Create new node if pool is empty or pooling is disabled
            return Instantiate(talentNodePrefab, talentTreeContent).GetComponent<TalentNodeBehavior>();
        }

        /// <summary>
        /// Return a node to the pool
        /// </summary>
        private void ReturnNodeToPool(TalentNodeBehavior node)
        {
            if (useObjectPooling)
            {
                node.gameObject.SetActive(false);
            }
            else
            {
                Destroy(node.gameObject);
            }
        }

        /// <summary>
        /// Open the talent window
        /// </summary>
        public void Open()
        {
            gameObject.SetActive(true);
            
            if (isInitialized)
            {
                BuildTalentTree();
                UpdateTalentPointsUI(TalentManager.Instance?.CurrentGoldCoins ?? 0);
            }
        }

        /// <summary>
        /// Close the talent window
        /// </summary>
        public void Close()
        {
            gameObject.SetActive(false);
            HideTooltip();
        }

        /// <summary>
        /// Build the talent tree UI
        /// </summary>
        private void BuildTalentTree()
        {
            if (!TalentDatabase.Instance.IsDataLoaded)
            {
                Debug.LogWarning("[TalentWindow] Talent database not loaded yet");
                return;
            }

            // Clear existing nodes
            ClearTalentTree();

            // Build normal talents (left side)
            BuildNormalTalents();

            // Build special talents (right side)
            BuildSpecialTalents();

            // Update all node visual states
            UpdateAllNodeStates();
        }

        /// <summary>
        /// Clear all talent nodes
        /// </summary>
        private void ClearTalentTree()
        {
            foreach (var node in activeNodes)
            {
                ReturnNodeToPool(node);
            }

            activeNodes.Clear();
            talentNodes.Clear();
        }

        /// <summary>
        /// Build normal talent nodes (left side)
        /// </summary>
        private void BuildNormalTalents()
        {
            var normalTalents = TalentDatabase.Instance.NormalTalents;
            
            for (int i = 0; i < normalTalents.Count; i++)
            {
                var talent = normalTalents[i];
                var node = CreateTalentNode(talent);
                
                // Position in left column
                float yPosition = treeStartY - (i * nodeSpacing);
                node.transform.localPosition = new Vector3(leftTreeX, yPosition, 0);
            }
        }

        /// <summary>
        /// Build special talent nodes (right side)
        /// </summary>
        private void BuildSpecialTalents()
        {
            var specialTalents = TalentDatabase.Instance.SpecialTalents;
            
            for (int i = 0; i < specialTalents.Count; i++)
            {
                var talent = specialTalents[i];
                var node = CreateTalentNode(talent);
                
                // Position in right column
                float yPosition = treeStartY - (i * nodeSpacing);
                node.transform.localPosition = new Vector3(rightTreeX, yPosition, 0);
            }
        }

        /// <summary>
        /// Create a talent node
        /// </summary>
        private TalentNodeBehavior CreateTalentNode(TalentModel talent)
        {
            var node = GetPooledNode();
            node.Initialize(talent);
            // Subscribe only to click event for mobile
            node.OnNodeClicked.AddListener(OnTalentNodeClicked);
            
            // Add to collections
            talentNodes[talent.ID] = node;
            activeNodes.Add(node);
            
            return node;
        }

        /// <summary>
        /// Update all node visual states
        /// </summary>
        private void UpdateAllNodeStates()
        {
            foreach (var node in activeNodes)
            {
                node.UpdateVisualState();
            }
        }

        /// <summary>
        /// Handle talent node clicked
        /// </summary>
        private void OnTalentNodeClicked(TalentNodeBehavior node)
        {
            // Show tooltip on tap
            ShowTooltip(node);
            // Update all nodes after learning a talent
            UpdateAllNodeStates();
            UpdateTalentPointsUI(TalentManager.Instance?.CurrentGoldCoins ?? 0);
            OnTalentTreeUpdated?.Invoke();
        }

        /// <summary>
        /// Show tooltip for talent node
        /// </summary>
        private void ShowTooltip(TalentNodeBehavior node)
        {
            if (tooltipPanel == null || tooltipText == null)
                return;
            tooltipText.text = node.GetTooltipText();
            EnableToolTip(true);
            if (tooltipRectTransform != null)
            {
                tooltipRectTransform.anchoredPosition = new Vector2(0, -200); 
            }
        }
        public void EnableToolTip(bool enable)
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(enable);
            }
        }

        /// <summary>
        /// Hide tooltip
        /// </summary>
        private void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Update talent points UI
        /// </summary>
        private void UpdateTalentPointsUI(int points)
        {
            if (talentPointsText != null)
            {
                talentPointsText.text = $"Talent Points: {points}";
            }
        }
        // private void UpdateTalentPointsUI(int points)
        // {
        //     if (talentPointsText != null)
        //     {
        //         talentPointsText.text = $"Talent Points: {points}";
        //     }
        // }

        /// <summary>
        /// Handle talent learned event
        /// </summary>
        private void OnTalentLearned(TalentModel talent)
        {
            if (talentNodes.TryGetValue(talent.ID, out var node))
            {
                node.AnimateLearn();
                node.UpdateVisualState();
            }

            // Update all nodes to refresh availability
            UpdateAllNodeStates();
        }

        /// <summary>
        /// Handle talent upgraded event
        /// </summary>
        private void OnTalentUpgraded(TalentModel talent)
        {
            if (talentNodes.TryGetValue(talent.ID, out var node))
            {
                node.UpdateVisualState();
            }
        }

        /// <summary>
        /// Show reset confirmation dialog
        /// </summary>
        private void ShowResetConfirmation()
        {
            if (confirmationPanel == null)
                return;

            confirmationText.text = "Are you sure you want to reset all talents?\nThis will refund all spent talent points.";
            confirmationPanel.SetActive(true);
            
            confirmationCallback = () => {
                TalentManager.Instance?.ResetAllTalents();
                UpdateAllNodeStates();
                UpdateTalentPointsUI(TalentManager.Instance?.CurrentGoldCoins ?? 0);
            };
        }

        /// <summary>
        /// Handle confirmation button clicked
        /// </summary>
        private void OnConfirmButtonClicked()
        {
            confirmationCallback?.Invoke();
            confirmationCallback = null;
            
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);
        }

        /// <summary>
        /// Handle cancel button clicked
        /// </summary>
        private void OnCancelButtonClicked()
        {
            confirmationCallback = null;
            
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);
        }


        /// <summary>
        /// Scroll to specific talent
        /// </summary>
        public void ScrollToTalent(int talentId)
        {
            if (talentNodes.TryGetValue(talentId, out var node))
            {
                var nodeRect = node.GetComponent<RectTransform>();
                if (nodeRect != null && talentScrollRect != null)
                {
                    // Calculate scroll position
                    var contentRect = talentScrollRect.content;
                    var viewportRect = talentScrollRect.viewport;
                    
                    var targetPosition = contentRect.anchoredPosition;
                    targetPosition.y = nodeRect.anchoredPosition.y;
                    
                    // Smooth scroll
                    contentRect.DOAnchorPos(targetPosition, 0.5f)
                        .SetEase(Ease.OutQuad);
                }
            }
        }

        /// <summary>
        /// Refresh the talent tree
        /// </summary>
        public void RefreshTalentTree()
        {
            if (gameObject.activeSelf)
            {
                BuildTalentTree();
            }
        }

        /// <summary>
        /// Clear and cleanup
        /// </summary>
        public void Clear()
        {
            ClearTalentTree();
            
            // Unsubscribe from events
            if (TalentManager.Instance != null)
            {
                TalentManager.Instance.OnGoldCoinsChanged.RemoveListener(UpdateTalentPointsUI);
                TalentManager.Instance.OnTalentLearned.RemoveListener(OnTalentLearned);
                TalentManager.Instance.OnTalentUpgraded.RemoveListener(OnTalentUpgraded);
            }
        }

        /// <summary>
        /// Handle window becoming active
        /// </summary>
        private void OnEnable()
        {
            if (isInitialized)
            {
                RefreshTalentTree();
            }
        }

        /// <summary>
        /// Handle window becoming inactive
        /// </summary>
        private void OnDisable()
        {
            HideTooltip();
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        private void OnDestroy()
        {
            Clear();
        }

        // Debug methods
        [ContextMenu("Refresh Talent Tree")]
        public void RefreshTalentTreeDebug()
        {
            RefreshTalentTree();
        }

        [ContextMenu("Log Talent Tree State")]
        public void LogTalentTreeState()
        {
            Debug.Log($"[TalentWindow] Active nodes: {activeNodes.Count}, Pooled nodes: {nodePool.Count}");
            Debug.Log($"[TalentWindow] Talent points: {TalentManager.Instance?.CurrentGoldCoins ?? 0}");
        }
    }
}