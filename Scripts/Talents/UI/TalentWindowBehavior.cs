using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using OctoberStudio;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Talents.Data;
using Talents.Manager;
using Talents.Helper;

namespace Talents.UI
{
    /// <summary>
    /// Talent window with auto-generated layout support
    /// </summary>
    public class TalentWindowBehavior : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button resetAllButton;
        [SerializeField] private TMP_Text goldCoinsText;
        [SerializeField] private TMP_Text orcText;
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

        [Header("Connection Lines")]
        [SerializeField] private TalentConnectionRenderer connectionRenderer;
        [SerializeField] private bool showConnectionLines = true;

        [Header("Performance")]
        [SerializeField] private bool useObjectPooling = true;
        [SerializeField] private int initialPoolSize = 100;

        // Node management
        private Dictionary<int, TalentNodeBehavior> talentNodes = new Dictionary<int, TalentNodeBehavior>();
        private List<TalentNodeBehavior> nodePool = new List<TalentNodeBehavior>();
        private List<TalentNodeBehavior> activeNodes = new List<TalentNodeBehavior>();

        // Layout tracking
        private List<Vector2> talentPositions = new List<Vector2>();

        // State
        private bool isInitialized = false;
        private System.Action confirmationCallback;

        // Events
        public UnityEvent OnTalentTreeUpdated;

        /// <summary>
        /// Initialize the talent window with proper content setup
        /// </summary>
        public void Init(UnityAction onBackButtonClicked)
        {
            if (backButton != null)
                backButton.onClick.AddListener(onBackButtonClicked);

            if (resetAllButton != null)
                resetAllButton.onClick.AddListener(ShowResetConfirmation);

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmButtonClicked);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelButtonClicked);

            InitializeObjectPool();
            SetupConnectionRenderer();
            SetupContentArea();  // Setup content area properly
            SubscribeToEvents();
            HideUIElements();

            isInitialized = true;
        }

        /// <summary>
        /// Setup content area for proper node fitting
        /// </summary>
        private void SetupContentArea()
        {
            if (talentTreeContent == null) return;

            // Set content anchors and pivot to center for proper positioning
            talentTreeContent.anchorMin = new Vector2(0.5f, 0f);    // Bottom
            talentTreeContent.anchorMax = new Vector2(0.5f, 0f);    // Bottom  
            talentTreeContent.pivot = new Vector2(0.5f, 0f);  
            talentTreeContent.anchoredPosition = Vector2.zero;

            // Ensure scroll rect is setup for vertical scrolling
            if (talentScrollRect != null)
            {
                talentScrollRect.horizontal = false;
                talentScrollRect.vertical = true;
                talentScrollRect.movementType = ScrollRect.MovementType.Elastic;
                
                // Start scroll position at bottom
                talentScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void InitializeObjectPool()
        {
            if (!useObjectPooling || talentNodePrefab == null) return;

            nodePool.Clear();
            for (int i = 0; i < initialPoolSize; i++)
            {
                var node = Instantiate(talentNodePrefab, talentTreeContent).GetComponent<TalentNodeBehavior>();
                node.gameObject.SetActive(false);
                nodePool.Add(node);
            }
        }

        private void SetupConnectionRenderer()
        {
            if (connectionRenderer == null)
            {
                connectionRenderer = GetComponentInChildren<TalentConnectionRenderer>();
                
                if (connectionRenderer == null)
                {
                    GameObject connectionObj = new GameObject("SimpleConnectionRenderer");
                    connectionObj.transform.SetParent(talentTreeContent);
                    connectionRenderer = connectionObj.AddComponent<TalentConnectionRenderer>();
                }
            }
        }

        private void OnDestroy()
        {
            Clear();
        }

        private void SubscribeToEvents()
        {
            if (TalentManager.Instance != null)
            {
                TalentManager.Instance.OnGoldCoinsChanged.AddListener(UpdateCurrencyUI);
                TalentManager.Instance.OnTalentLearned.AddListener(OnTalentLearned);
                TalentManager.Instance.OnTalentUpgraded.AddListener(OnTalentUpgraded);
            }
        }

        private void HideUIElements()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
            
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);
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
                UpdateCurrencyUI();
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
        /// Build the talent tree with auto-generated layout
        /// </summary>
        private void BuildTalentTree()
        {
            if (!TalentDatabase.Instance.IsDataLoaded) return;

            ClearTalentTree();
            ClearConnectionLines();
            ResetPositionArrays();

            BuildTalentsFromLinearProgression();
            DrawConnectionLines();
            UpdateAllNodeStates();
            UpdateCurrencyUI();
            UpdateContentSize();
            
            // Auto-scroll to bottom to show starting nodes
            StartCoroutine(ScrollToBottomAfterFrame());
        }

        /// <summary>
        /// Build talents from linear progression system with mobile-optimized single column layout
        /// </summary>
        private void BuildTalentsFromLinearProgression()
        {
            var allTalents = TalentDatabase.Instance.GetAllTalents();

            // Single column layout - all talents in center
            talentPositions.Clear();
            
            foreach (var talent in allTalents)
            {
                var node = CreateTalentNode(talent);
                Vector2 position = new Vector2(talent.PositionX, talent.PositionY);
                
                // Apply linear styling with level indicators
                ApplyLinearTalentStyling(node, talent);
                
                node.transform.localPosition = position;
                talentPositions.Add(position);
            }
        }

        /// <summary>
        /// Apply styling for linear talent system with level indicators
        /// </summary>
        private void ApplyLinearTalentStyling(TalentNodeBehavior node, TalentModel talent)
        {
            // Get stat-specific color
            Color statColor = GetLinearStatColor(talent);
            SetNodeColor(node, statColor);
            
            // Add level indicator "Lv.X"
            AddLevelIndicator(node, talent.RequiredPlayerLevel);
            
            // Add stat boost indicator
            AddStatBoostIndicator(node, talent);
        }

        /// <summary>
        /// Get color for linear stat types
        /// </summary>
        private Color GetLinearStatColor(TalentModel talent)
        {
            if (talent.Name.Contains("ATK"))
                return new Color(1f, 0.3f, 0.3f); // Red for ATK
            else if (talent.Name.Contains("DEF"))
                return new Color(0.3f, 0.3f, 1f); // Blue for DEF
            else if (talent.Name.Contains("Speed"))
                return new Color(0.3f, 1f, 0.3f); // Green for Speed
            else if (talent.Name.Contains("Heal"))
                return new Color(1f, 1f, 0.3f); // Yellow for Heal
            else
                return Color.white;
        }

        /// <summary>
        /// Add level indicator showing "Lv.X"
        /// </summary>
        private void AddLevelIndicator(TalentNodeBehavior node, int level)
        {
            var texts = node.GetComponentsInChildren<TMP_Text>();
            foreach (var text in texts)
            {
                if (text.name.Contains("Level") || text.name.Contains("Requirement"))
                {
                    text.text = $"Lv.{level}";
                    text.color = Color.white;
                    text.fontSize = 14f;
                    text.gameObject.SetActive(true);
                    break;
                }
            }
        }

        /// <summary>
        /// Add stat boost indicator showing the boost value
        /// </summary>
        private void AddStatBoostIndicator(TalentNodeBehavior node, TalentModel talent)
        {
            var texts = node.GetComponentsInChildren<TMP_Text>();
            foreach (var text in texts)
            {
                if (text.name.Contains("Value") || text.name.Contains("Boost"))
                {
                    string statType = talent.Name.Replace(" Boost", "");
                    if (statType == "Speed")
                        text.text = $"+{talent.StatValue:F2}";
                    else
                        text.text = $"+{talent.StatValue:F0}";
                    
                    text.color = GetLinearStatColor(talent);
                    text.fontSize = 12f;
                    text.gameObject.SetActive(true);
                    break;
                }
            }
        }

        /// <summary>
        /// Set node color
        /// </summary>
        private void SetNodeColor(TalentNodeBehavior node, Color color)
        {
            var images = node.GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                if (img.name.Contains("Background") || img.name.Contains("Border"))
                {
                    img.color = color;
                }
            }
        }

        /// <summary>
        /// Get pooled node or create new one
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

            return Instantiate(talentNodePrefab, talentTreeContent).GetComponent<TalentNodeBehavior>();
        }

        /// <summary>
        /// Return node to pool
        /// </summary>
        private void ReturnNodeToPool(TalentNodeBehavior node)
        {
            if (useObjectPooling)
                node.gameObject.SetActive(false);
            else
                Destroy(node.gameObject);
        }

        /// <summary>
        /// Create talent node
        /// </summary>
        private TalentNodeBehavior CreateTalentNode(TalentModel talent)
        {
            var node = GetPooledNode();
            node.Initialize(talent);
            node.OnNodeClicked.AddListener(OnTalentNodeClicked);

            talentNodes[talent.ID] = node;
            activeNodes.Add(node);

            return node;
        }

        /// <summary>
        /// Clear all talent nodes
        /// </summary>
        private void ClearTalentTree()
        {
            foreach (var node in activeNodes)
                ReturnNodeToPool(node);

            activeNodes.Clear();
            talentNodes.Clear();
        }

        /// <summary>
        /// Clear connection lines
        /// </summary>
        private void ClearConnectionLines()
        {
            if (connectionRenderer != null)
                connectionRenderer.ClearAllLines();
        }

        /// <summary>
        /// Reset position arrays
        /// </summary>
        private void ResetPositionArrays()
        {
            talentPositions.Clear();
        }

        /// <summary>
        /// Draw connection lines for linear progression
        /// </summary>
        private void DrawConnectionLines()
        {
            if (!showConnectionLines || connectionRenderer == null) return;

            // Draw linear progression connections (single column) using existing base stats method
            if (talentPositions.Count > 1)
                connectionRenderer.DrawBaseStatsConnections(talentPositions.ToArray());
        }

        /// <summary>
        /// Scroll to bottom after content is built (coroutine to wait for layout)
        /// </summary>
        private IEnumerator ScrollToBottomAfterFrame()
        {
            yield return new WaitForEndOfFrame();
            
            if (talentScrollRect != null)
            {
                // Scroll to bottom to show starting nodes (ATK I, Lucky Dog)
                talentScrollRect.verticalNormalizedPosition = 0f;
                
                // Force layout rebuild
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(talentTreeContent);
            }
        }

        /// <summary>
        /// Update content size to fit all nodes properly for mobile single column layout
        /// </summary>
        private void UpdateContentSize()
        {
            if (talentTreeContent == null) return;

            if (!talentPositions.Any()) return;

            // Calculate bounds for single column layout
            float minY = talentPositions.Min(p => p.y);
            float maxY = talentPositions.Max(p => p.y);

            // Add padding for mobile touch targets
            float nodePadding = 100f;
            float extraPadding = 100f; // Extra padding for mobile

            // Mobile optimized: Fixed width for single column, dynamic height
            float totalWidth = 600f; // Fixed width for mobile single column
            float totalHeight = (maxY - minY) + nodePadding + extraPadding * 2;

            // Set content size - mobile optimized
            talentTreeContent.sizeDelta = new Vector2(totalWidth, totalHeight);
            talentTreeContent.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Update all node visual states
        /// </summary>
        private void UpdateAllNodeStates()
        {
            foreach (var node in activeNodes)
                node.UpdateVisualState();

            UpdateConnectionLineColors();
        }

        /// <summary>
        /// Update connection line colors for linear progression
        /// </summary>
        private void UpdateConnectionLineColors()
        {
            if (connectionRenderer == null) return;

            // Create unlock states for connection lines in linear progression
            bool[] unlockStates = new bool[talentPositions.Count];

            for (int i = 0; i < talentPositions.Count; i++)
            {
                var position = talentPositions[i];
                var talent = FindTalentAtPosition(position);
                if (talent != null)
                {
                    unlockStates[i] = TalentManager.Instance?.GetTalentLevel(talent.ID) > 0;
                }
            }

            connectionRenderer.UpdateLineColors(unlockStates);
        }

        /// <summary>
        /// Find talent at specific position
        /// </summary>
        private TalentModel FindTalentAtPosition(Vector2 position)
        {
            var allTalents = TalentDatabase.Instance.GetAllTalents();
            return allTalents.FirstOrDefault(t => 
                Mathf.Approximately(t.PositionX, position.x) && 
                Mathf.Approximately(t.PositionY, position.y));
        }

        /// <summary>
        /// Handle talent node clicked
        /// </summary>
        private void OnTalentNodeClicked(TalentNodeBehavior node)
        {
            var talent = node.TalentModel;

            if (TalentManager.Instance.CanLearnTalent(talent.ID))
            {
                ShowLearnConfirmation(node);
            }
            else
            {
                ShowTooltip(node);
            }
        }

        /// <summary>
        /// Show learn confirmation for linear talent system
        /// </summary>
        private void ShowLearnConfirmation(TalentNodeBehavior node)
        {
            var talent = node.TalentModel;
            var currentLevel = TalentManager.Instance.GetTalentLevel(talent.ID);
            var cost = TalentDatabase.Instance.GetTalentCost(talent.ID, currentLevel + 1);

            // Linear system uses Gold for all talents
            string message = $"Learn {talent.Name}?\nCost: {cost} Gold";

            if (confirmationPanel != null && confirmationText != null)
            {
                confirmationText.text = message;
                confirmationPanel.SetActive(true);

                confirmationCallback = () => {
                    if (TalentManager.Instance.LearnTalent(talent.ID))
                    {
                        node.UpdateVisualState();
                        UpdateAllNodeStates();
                        UpdateCurrencyUI();
                        OnTalentTreeUpdated?.Invoke();
                    }
                };
            }
        }

        /// <summary>
        /// Show tooltip
        /// </summary>
        private void ShowTooltip(TalentNodeBehavior node)
        {
            if (tooltipPanel == null || tooltipText == null) return;

            tooltipText.text = node.GetTooltip();
            tooltipPanel.SetActive(true);

            if (tooltipRectTransform != null)
                tooltipRectTransform.anchoredPosition = new Vector2(0, -200);
        }

        /// <summary>
        /// Hide tooltip
        /// </summary>
        private void HideTooltip()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }

        /// <summary>
        /// Update currency UI
        /// </summary>
        private void UpdateCurrencyUI()
        {
            if (TalentManager.Instance != null)
            {
                if (goldCoinsText != null)
                {
                    var goldSave = GameController.SaveManager.GetSave<CurrencySave>("gold");
                    goldCoinsText.text = $"Gold: {goldSave?.Amount ?? 0}";
                }

                if (orcText != null)
                {
                    var orcSave = GameController.SaveManager.GetSave<CurrencySave>("orc");
                    orcText.text = $"Orc: {orcSave?.Amount ?? 0}";
                }
            }
        }

        /// <summary>
        /// Handle talent learned
        /// </summary>
        private void OnTalentLearned(TalentModel talent)
        {
            if (talentNodes.TryGetValue(talent.ID, out var node))
            {
                node.AnimateLearn();
                node.UpdateVisualState();
            }
            UpdateAllNodeStates();
        }

        /// <summary>
        /// Handle talent upgraded
        /// </summary>
        private void OnTalentUpgraded(TalentModel talent)
        {
            if (talentNodes.TryGetValue(talent.ID, out var node))
                node.UpdateVisualState();
        }

        /// <summary>
        /// Show reset confirmation
        /// </summary>
        private void ShowResetConfirmation()
        {
            if (confirmationPanel == null) return;

            confirmationText.text = "Reset all talents?\nThis will refund all spent points.";
            confirmationPanel.SetActive(true);

            confirmationCallback = () => {
                TalentManager.Instance?.ResetAllTalents();
                UpdateAllNodeStates();
                UpdateCurrencyUI();
            };
        }

        /// <summary>
        /// Handle confirmation
        /// </summary>
        private void OnConfirmButtonClicked()
        {
            confirmationCallback?.Invoke();
            confirmationCallback = null;
            
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);
        }

        /// <summary>
        /// Handle cancel
        /// </summary>
        private void OnCancelButtonClicked()
        {
            confirmationCallback = null;
            
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);
        }

        /// <summary>
        /// Refresh talent tree
        /// </summary>
        public void RefreshTalentTree()
        {
            if (gameObject.activeSelf)
                BuildTalentTree();
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        public void Clear()
        {
            ClearTalentTree();
            ClearConnectionLines();

            if (TalentManager.Instance != null)
            {
                TalentManager.Instance.OnGoldCoinsChanged.RemoveListener(UpdateCurrencyUI);
                TalentManager.Instance.OnTalentLearned.RemoveListener(OnTalentLearned);
                TalentManager.Instance.OnTalentUpgraded.RemoveListener(OnTalentUpgraded);
            }
        }

        private void OnEnable()
        {
            if (isInitialized)
                RefreshTalentTree();
        }

        private void OnDisable()
        {
            HideTooltip();
        }

        [ContextMenu("Debug Viewport Info")]
        public void DebugViewportInfo()
        {
            if (talentScrollRect?.viewport != null)
            {
                var viewportSize = talentScrollRect.viewport.rect.size;
                var contentSize = talentTreeContent.sizeDelta;
                
                Debug.Log($"=== MOBILE VIEWPORT DEBUG (Linear System) ===");
                Debug.Log($"Viewport Size: {viewportSize.x:F0} x {viewportSize.y:F0}");
                Debug.Log($"Content Size: {contentSize.x:F0} x {contentSize.y:F0}");
                Debug.Log($"Node Spacing: 450px (target)");
                Debug.Log($"Nodes per viewport: {viewportSize.y / 450f:F1}");
                Debug.Log($"Linear Talents: {talentPositions.Count}");
                
                if (talentPositions.Count > 0)
                {
                    var firstNode = talentPositions.First();
                    var lastNode = talentPositions.Last();
                    var totalHeight = lastNode.y - firstNode.y;
                    Debug.Log($"Linear progression height: {totalHeight:F0}px");
                    Debug.Log($"Total screens needed: {totalHeight / viewportSize.y:F1}");
                }
            }
        }

        // Debug methods
        [ContextMenu("Debug Content Bounds")]
        public void DebugContentBounds()
        {
            if (!isInitialized) return;

            if (!talentPositions.Any()) return;

            float minY = talentPositions.Min(p => p.y);
            float maxY = talentPositions.Max(p => p.y);

            Debug.Log($"=== CONTENT BOUNDS DEBUG (Linear System) ===");
            Debug.Log($"Content Size: {talentTreeContent.sizeDelta}");
            Debug.Log($"Content Position: {talentTreeContent.anchoredPosition}");
            Debug.Log($"Content Anchor: {talentTreeContent.anchorMin} to {talentTreeContent.anchorMax}");
            Debug.Log($"Content Pivot: {talentTreeContent.pivot}");
            Debug.Log($"Node Bounds: Y({minY} to {maxY})");
            Debug.Log($"Linear Talents: {talentPositions.Count}");
            Debug.Log($"Layout: Single Column (Mobile Optimized)");
            
            if (talentScrollRect != null)
            {
                Debug.Log($"Scroll Position: {talentScrollRect.verticalNormalizedPosition}");
                Debug.Log($"Viewport Size: {talentScrollRect.viewport.rect.size}");
            }
        }

        [ContextMenu("Force Scroll to Bottom")]
        public void ForceScrollToBottom()
        {
            if (talentScrollRect != null)
            {
                talentScrollRect.verticalNormalizedPosition = 0f;
                Debug.Log("Forced scroll to bottom");
            }
        }

        [ContextMenu("Refresh Layout")]
        public void RefreshLayoutDebug()
        {
            if (talentTreeContent != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(talentTreeContent);
                Debug.Log("Layout refreshed");
            }
        }
    }
}