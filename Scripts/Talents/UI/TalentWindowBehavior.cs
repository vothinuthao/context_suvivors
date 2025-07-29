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
using Talents.Config;

namespace Talents.UI
{
    /// <summary>
    /// Fixed talent window with proper positioning and zone grouping
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
        [SerializeField] private GameObject zoneLabelPrefab;

        [Header("Line Rendering")]
        [SerializeField] private GameObject connectionLinePrefab;
        [SerializeField] private Transform connectionLineParent;

        [Header("Tooltip")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TMP_Text tooltipText;
        [SerializeField] private RectTransform tooltipRectTransform;

        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private TMP_Text confirmationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("Layout Debug")]
        [SerializeField] private bool showLayoutDebug = false;
        [SerializeField] private Color debugZoneColor = Color.yellow;

        // Node management
        private Dictionary<int, TalentNodeBehavior> talentNodes = new Dictionary<int, TalentNodeBehavior>();
        private List<TalentNodeBehavior> activeNodes = new List<TalentNodeBehavior>();
        private List<GameObject> connectionLines = new List<GameObject>();
        
        // Zone management
        private Dictionary<int, Transform> zoneContainers = new Dictionary<int, Transform>();
        private List<GameObject> activeZoneLabels = new List<GameObject>();

        // Layout
        private TalentLayoutConfig layoutConfig;
        private float actualContentHeight = 0f;

        // State
        private bool isInitialized = false;
        private System.Action confirmationCallback;

        // Events
        public UnityEvent OnTalentTreeUpdated;

        /// <summary>
        /// Initialize the talent window
        /// </summary>
        public void Init()
        {

            if (resetAllButton != null)
                resetAllButton.onClick.AddListener(ShowResetConfirmation);

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmButtonClicked);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelButtonClicked);

            layoutConfig = TalentDatabase.Instance?.LayoutConfig;
            SetupContentArea();
            SetupConnectionLineParent();
            SubscribeToEvents();
            HideUIElements();

            isInitialized = true;
        }

        /// <summary>
        /// Setup content area với proper bounds
        /// </summary>
        private void SetupContentArea()
        {
            if (talentTreeContent == null) return;

            // Set content anchors cho bottom-up scrolling
            talentTreeContent.anchorMin = new Vector2(0.5f, 0f);
            talentTreeContent.anchorMax = new Vector2(0.5f, 0f);
            talentTreeContent.pivot = new Vector2(0.5f, 0f);
            talentTreeContent.anchoredPosition = Vector2.zero;

            // Setup scroll rect
            if (talentScrollRect != null)
            {
                talentScrollRect.horizontal = false;
                talentScrollRect.vertical = true;
                talentScrollRect.movementType = ScrollRect.MovementType.Elastic;
                talentScrollRect.verticalNormalizedPosition = 0f; // Start at bottom
                
                // Ensure content size fitter is setup correctly
                var contentSizeFitter = talentTreeContent.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter == null)
                {
                    contentSizeFitter = talentTreeContent.gameObject.AddComponent<ContentSizeFitter>();
                }
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
        }

        /// <summary>
        /// Setup connection line parent
        /// </summary>
        private void SetupConnectionLineParent()
        {
            if (connectionLineParent == null)
            {
                var lineParentObj = new GameObject("ConnectionLines");
                lineParentObj.transform.SetParent(talentTreeContent, false);
                connectionLineParent = lineParentObj.transform;
                
                // Set to render behind nodes
                connectionLineParent.SetAsFirstSibling();
            }
        }

        private void SubscribeToEvents()
        {
            if (TalentManager.Instance != null)
            {
                TalentManager.Instance.OnCurrencyChanged.AddListener(UpdateCurrencyUI);
                TalentManager.Instance.OnTalentLearned.AddListener(OnTalentLearned);
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
                BuildZoneBasedTalentTree();
                UpdateCurrencyUI();
                StartCoroutine(ScrollToCurrentProgressionDelayed());
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
        /// Build talent tree với uniform spacing và exact content height
        /// </summary>
        private void BuildZoneBasedTalentTree()
        {
            if (!TalentDatabase.Instance.IsDataLoaded || layoutConfig == null) return;

            ClearTalentTree();

            // Get all talents sorted by position
            var allTalents = TalentDatabase.Instance.GetAllTalents()
                .OrderBy(t => t.PositionY)
                .ToList();

            // Create nodes with exact positioning - NO zone containers
            foreach (var talent in allTalents)
            {
                CreateTalentNodeDirect(talent);
            }

            // Create zone labels separately if needed
            if (layoutConfig.ShowZoneLabels)
            {
                CreateZoneLabelsOnly();
            }

            CreateConnectionLines();
            UpdateAllNodeStates();
            
            // Set exact content height from database calculation
            SetExactContentSize();
        }

        /// <summary>
        /// Create talent node directly without zone containers
        /// </summary>
        private void CreateTalentNodeDirect(TalentModel talent)
        {
            var nodeObj = Instantiate(talentNodePrefab, talentTreeContent);
            var node = nodeObj.GetComponent<TalentNodeBehavior>();
            
            node.Initialize(talent);
            node.OnNodeClicked.AddListener(OnTalentNodeClicked);

            // Set exact position from talent model
            var nodeRect = nodeObj.GetComponent<RectTransform>();
            nodeRect.anchoredPosition = new Vector2(talent.PositionX, talent.PositionY);

            // Set size based on type
            Vector2 nodeSize = talent.NodeType == TalentNodeType.Normal ? 
                layoutConfig.NormalNodeSize : layoutConfig.SpecialNodeSize;
            nodeRect.sizeDelta = nodeSize;

            talentNodes[talent.ID] = node;
            activeNodes.Add(node);
        }

        /// <summary>
        /// Create zone labels only for visual separation (no containers)
        /// </summary>
        private void CreateZoneLabelsOnly()
        {
            var activeZones = TalentDatabase.Instance.GetActiveZones();
            
            foreach (int zoneLevel in activeZones)
            {
                var zoneTalents = TalentDatabase.Instance.GetTalentsInZone(zoneLevel);
                if (zoneTalents.Count == 0) continue;

                // Calculate label position based on first talent in zone
                var firstTalent = zoneTalents.OrderBy(t => t.PositionY).First();
                float labelY = firstTalent.PositionY + layoutConfig.ZoneLabelOffsetY;

                CreateZoneLabelAtPosition(zoneLevel, labelY);
            }
        }

        /// <summary>
        /// Create zone label at specific position
        /// </summary>
        private void CreateZoneLabelAtPosition(int zoneLevel, float labelY)
        {
            GameObject labelObj;
            
            if (zoneLabelPrefab != null)
            {
                labelObj = Instantiate(zoneLabelPrefab, talentTreeContent);
            }
            else
            {
                labelObj = CreateDefaultZoneLabel(zoneLevel);
            }

            // Setup label positioning
            var labelRect = labelObj.GetComponent<RectTransform>();
            if (labelRect != null)
            {
                labelRect.sizeDelta = layoutConfig.ZoneLabelSize;
                labelRect.anchoredPosition = new Vector2(0f, labelY);
            }

            // Update label text
            var labelText = labelObj.GetComponentInChildren<TMP_Text>();
            if (labelText != null)
            {
                labelText.text = $"LEVEL {zoneLevel}";
                labelText.fontSize = layoutConfig.ZoneLabelFontSize;
                labelText.color = layoutConfig.ZoneLabelColor;
                labelText.alignment = TextAlignmentOptions.Center;
            }

            activeZoneLabels.Add(labelObj);
        }

        /// <summary>
        /// Create default zone label without container
        /// </summary>
        private GameObject CreateDefaultZoneLabel(int zoneLevel)
        {
            var labelObj = new GameObject($"ZoneLabel_{zoneLevel}");
            labelObj.transform.SetParent(talentTreeContent, false);

            // Add background image
            var bgImage = labelObj.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.5f); // Semi-transparent
            bgImage.raycastTarget = false;

            // Add text
            var textObj = new GameObject("LabelText");
            textObj.transform.SetParent(labelObj.transform, false);
            
            var text = textObj.AddComponent<TMP_Text>();
            text.text = $"LEVEL {zoneLevel}";
            text.fontSize = layoutConfig.ZoneLabelFontSize;
            text.color = layoutConfig.ZoneLabelColor;
            text.alignment = TextAlignmentOptions.Center;

            // Setup text rect to fill parent
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return labelObj;
        }

        /// <summary>
        /// Set exact content size based on database calculation
        /// </summary>
        private void SetExactContentSize()
        {
            if (talentTreeContent == null) return;

            // Get exact height from database
            float requiredHeight = TalentDatabase.Instance.CalculateRequiredContentHeight();
            
            // Width based on column positions plus node sizes
            float requiredWidth = Mathf.Abs(layoutConfig.NormalColumnX) + Mathf.Abs(layoutConfig.SpecialColumnX) + 
                                 Mathf.Max(layoutConfig.NormalNodeSize.x, layoutConfig.SpecialNodeSize.x) + 100f;

            // Set exact size
            talentTreeContent.sizeDelta = new Vector2(requiredWidth, requiredHeight);

            Debug.Log($"[TalentWindow] Content size set to: {requiredWidth} x {requiredHeight}");

            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(talentTreeContent);
        }

        /// <summary>
        /// Create connection lines between nodes
        /// </summary>
        private void CreateConnectionLines()
        {
            if (!layoutConfig.ShowConnections) return;

            CreateNormalColumnConnections();
            CreateSpecialColumnConnections();
        }

        /// <summary>
        /// Create connections for normal column
        /// </summary>
        private void CreateNormalColumnConnections()
        {
            // Group normal nodes by stat type
            var statGroups = new Dictionary<string, List<TalentNodeBehavior>>();
            
            foreach (var node in activeNodes.Where(n => n.IsNormalNode()))
            {
                string statType = node.GetStatType();
                if (!statGroups.ContainsKey(statType))
                    statGroups[statType] = new List<TalentNodeBehavior>();
                
                statGroups[statType].Add(node);
            }

            // Create vertical lines for each stat type
            foreach (var group in statGroups.Values)
            {
                group.Sort((a, b) => a.GetZoneLevel().CompareTo(b.GetZoneLevel()));
                
                for (int i = 0; i < group.Count - 1; i++)
                {
                    CreateConnectionLine(group[i], group[i + 1]);
                }
            }
        }

        /// <summary>
        /// Create connections for special column
        /// </summary>
        private void CreateSpecialColumnConnections()
        {
            var specialNodes = activeNodes.Where(n => n.IsSpecialNode())
                .OrderBy(n => n.GetZoneLevel())
                .ToList();

            for (int i = 0; i < specialNodes.Count - 1; i++)
            {
                CreateConnectionLine(specialNodes[i], specialNodes[i + 1]);
            }
        }

        /// <summary>
        /// Create single connection line
        /// </summary>
        private void CreateConnectionLine(TalentNodeBehavior fromNode, TalentNodeBehavior toNode)
        {
            GameObject lineObj;
            
            if (connectionLinePrefab != null)
            {
                lineObj = Instantiate(connectionLinePrefab, connectionLineParent);
            }
            else
            {
                lineObj = CreateDefaultConnectionLine();
            }

            // Setup line
            SetupConnectionLine(lineObj, fromNode.transform, toNode.transform);
            connectionLines.Add(lineObj);
        }

        /// <summary>
        /// Create default connection line
        /// </summary>
        private GameObject CreateDefaultConnectionLine()
        {
            var lineObj = new GameObject("ConnectionLine");
            lineObj.transform.SetParent(connectionLineParent, false);

            var lineImage = lineObj.AddComponent<Image>();
            lineImage.color = layoutConfig.InactiveConnectionColor;
            lineImage.raycastTarget = false;

            return lineObj;
        }

        /// <summary>
        /// Setup connection line geometry
        /// </summary>
        private void SetupConnectionLine(GameObject lineObj, Transform startTransform, Transform endTransform)
        {
            var lineImage = lineObj.GetComponent<Image>();
            var lineRect = lineObj.GetComponent<RectTransform>();
            
            if (lineRect == null) return;

            // Get world positions và convert to local
            Vector3 startPos = talentTreeContent.InverseTransformPoint(startTransform.position);
            Vector3 endPos = talentTreeContent.InverseTransformPoint(endTransform.position);

            // Calculate line properties
            Vector3 direction = endPos - startPos;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Position và setup
            Vector3 midpoint = (startPos + endPos) * 0.5f;
            lineRect.anchoredPosition = midpoint;
            lineRect.sizeDelta = new Vector2(distance, layoutConfig.ConnectionLineWidth);
            lineRect.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // Anchors
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>
        /// Update content size properly
        /// </summary>
        private void UpdateContentSize()
        {
            if (talentTreeContent == null) return;

            // Calculate optimal content size
            Vector2 optimalSize = layoutConfig.GetOptimalContentSize();
            optimalSize.y = Mathf.Max(optimalSize.y, actualContentHeight);

            talentTreeContent.sizeDelta = optimalSize;

            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(talentTreeContent);
        }

        /// <summary>
        /// Scroll to current progression với delay
        /// </summary>
        private IEnumerator ScrollToCurrentProgressionDelayed()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame(); // Extra frame cho layout

            // if (talentScrollRect == null) return;

            var currentNode = TalentManager.Instance?.GetCurrentProgressionNode();
            if (currentNode != null && talentNodes.TryGetValue(currentNode.ID, out var node))
            {
                ScrollToNode(node);
            }
            else
            {
                // Scroll to bottom (starting nodes)
                talentScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>
        /// Scroll to specific node
        /// </summary>
        private void ScrollToNode(TalentNodeBehavior targetNode)
        {
            if (talentScrollRect == null || targetNode == null) return;

            var nodeWorldPos = targetNode.transform.position;
            var nodeLocalPos = talentTreeContent.InverseTransformPoint(nodeWorldPos);
            
            var contentHeight = talentTreeContent.sizeDelta.y;
            var viewportHeight = talentScrollRect.viewport.rect.height;

            // Calculate normalized position
            float normalizedPos = Mathf.Clamp01((nodeLocalPos.y + viewportHeight * 0.5f) / contentHeight);
            talentScrollRect.verticalNormalizedPosition = normalizedPos;
        }

        /// <summary>
        /// Clear talent tree
        /// </summary>
        private void ClearTalentTree()
        {
            // Clear nodes
            foreach (var node in activeNodes)
            {
                if (node != null)
                    Destroy(node.gameObject);
            }
            activeNodes.Clear();
            talentNodes.Clear();

            // Clear zone containers
            foreach (var container in zoneContainers.Values)
            {
                if (container != null)
                    Destroy(container.gameObject);
            }
            zoneContainers.Clear();

            // Clear zone labels
            foreach (var label in activeZoneLabels)
            {
                if (label != null)
                    Destroy(label);
            }
            activeZoneLabels.Clear();

            // Clear connection lines
            foreach (var line in connectionLines)
            {
                if (line != null)
                    Destroy(line);
            }
            connectionLines.Clear();
        }

        /// <summary>
        /// Update all node states
        /// </summary>
        private void UpdateAllNodeStates()
        {
            foreach (var node in activeNodes)
            {
                node.UpdateVisualState();
            }

            UpdateConnectionLineStates();
        }

        /// <summary>
        /// Update connection line states
        /// </summary>
        private void UpdateConnectionLineStates()
        {
            foreach (var line in connectionLines)
            {
                var lineImage = line.GetComponent<Image>();
                if (lineImage != null)
                {
                    // Simple implementation - all inactive for now
                    lineImage.color = layoutConfig.InactiveConnectionColor;
                }
            }
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
        /// Show learn confirmation
        /// </summary>
        private void ShowLearnConfirmation(TalentNodeBehavior node)
        {
            var talent = node.TalentModel;
            string currencyName = talent.NodeType == TalentNodeType.Normal ? "Gold" : "Orc";
            
            string message = $"Learn {talent.Name}?\nCost: {talent.Cost} {currencyName}";

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
                    goldCoinsText.text = $"Gold: {TalentManager.Instance.GetGoldCoins()}";

                if (orcText != null)
                    orcText.text = $"Orc: {TalentManager.Instance.GetOrc()}";
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
        /// Show reset confirmation
        /// </summary>
        private void ShowResetConfirmation()
        {
            if (confirmationPanel == null) return;

            confirmationText.text = "Reset all talents?\nThis will refund all spent currency.";
            confirmationPanel.SetActive(true);

            confirmationCallback = () => {
                TalentManager.Instance?.ResetAllTalents();
                UpdateAllNodeStates();
                UpdateCurrencyUI();
            };
        }

        /// <summary>
        /// Confirmation button handlers
        /// </summary>
        private void OnConfirmButtonClicked()
        {
            confirmationCallback?.Invoke();
            confirmationCallback = null;
            
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);
        }

        private void OnCancelButtonClicked()
        {
            confirmationCallback = null;
            
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);
        }

        /// <summary>
        /// Public methods
        /// </summary>
        public void RefreshTalentTree()
        {
            if (gameObject.activeSelf)
                BuildZoneBasedTalentTree();
        }

        public void Clear()
        {
            ClearTalentTree();

            if (TalentManager.Instance != null)
            {
                TalentManager.Instance.OnCurrencyChanged.RemoveListener(UpdateCurrencyUI);
                TalentManager.Instance.OnTalentLearned.RemoveListener(OnTalentLearned);
            }
        }

        private void OnEnable()
        {
            if (isInitialized)
                StartCoroutine(RefreshTalentTreeDelayed());
        }

        private IEnumerator RefreshTalentTreeDelayed()
        {
            yield return new WaitForEndOfFrame();
            RefreshTalentTree();
        }

        private void OnDisable()
        {
            HideTooltip();
        }

        private void OnDestroy()
        {
            Clear();
        }

        // Debug methods
        [ContextMenu("Debug Layout Info")]
        public void DebugLayoutInfo()
        {
            if (layoutConfig == null) return;

            Debug.Log($"=== LAYOUT DEBUG INFO ===");
            Debug.Log($"Content Size: {talentTreeContent.sizeDelta}");
            Debug.Log($"Actual Content Height: {actualContentHeight}");
            Debug.Log($"Zone Count: {zoneContainers.Count}");
            Debug.Log($"Active Nodes: {activeNodes.Count}");
            Debug.Log($"Layout Config: {layoutConfig.name}");
            Debug.Log($"Node Spacing: {layoutConfig.NodeSpacing}");
            Debug.Log($"Zone Spacing: {layoutConfig.ZoneSpacing}");
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
    }
}