using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// Talent window with proper zone hierarchy and bottom-up spawning
    /// Structure: Zone contains ZoneLabel + all nodes of that level (Normal + Special)
    /// </summary>
    public class TalentWindowBehavior : MonoBehaviour
    {
        [Header("UI References")]
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
        
        // Zone management - proper hierarchy
        private Dictionary<int, Transform> zoneContainers = new Dictionary<int, Transform>();
        private Dictionary<int, GameObject> zoneLabels = new Dictionary<int, GameObject>();

        // Layout
        private TalentLayoutConfig layoutConfig;

        // State
        private bool _isInitialized = false;
        private System.Action confirmationCallback;

        // Events
        public UnityEvent OnTalentTreeUpdated;

        /// <summary>
        /// Initialize the talent window
        /// </summary>
        public void Init()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmButtonClicked);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelButtonClicked);

            layoutConfig = TalentDatabase.Instance?.LayoutConfig;
            SetupContentArea();
            SetupConnectionLineParent();
            SubscribeToEvents();
            HideUIElements();

            _isInitialized = true;
        }

        /// <summary>
        /// Setup content area for bottom-up scrolling
        /// </summary>
        private void SetupContentArea()
        {
            if (talentTreeContent == null) return;

            // Set content anchors for bottom-up scrolling
            talentTreeContent.anchorMin = new Vector2(0.5f, 0f);    // Bottom center
            talentTreeContent.anchorMax = new Vector2(0.5f, 0f);    // Bottom center  
            talentTreeContent.pivot = new Vector2(0.5f, 0f);        // Bottom pivot
            talentTreeContent.anchoredPosition = Vector2.zero;

            // Setup scroll rect for vertical scrolling from bottom
            if (talentScrollRect != null)
            {
                talentScrollRect.horizontal = false;
                talentScrollRect.vertical = true;
                talentScrollRect.movementType = ScrollRect.MovementType.Elastic;
                talentScrollRect.verticalNormalizedPosition = 0f; // Start at bottom
                
                // Content size fitter
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
            
            if (_isInitialized)
            {
                BuildProperZoneHierarchy();
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
        /// Build talent tree with proper zone hierarchy
        /// Structure: Zone_X -> ZoneLabel_X + Nodes of level X
        /// </summary>
        private void BuildProperZoneHierarchy()
        {
            if (!TalentDatabase.Instance.IsDataLoaded || layoutConfig == null) return;

            ClearTalentTree();

            // Get all zones (levels) sorted from 1 to max
            var activeZones = TalentDatabase.Instance.GetActiveZones();
            float currentY = layoutConfig.StartY;

            // Build zones from level 1 to max (bottom to top)
            foreach (int zoneLevel in activeZones.OrderBy(z => z))
            {
                currentY = CreateZoneWithProperHierarchy(zoneLevel, currentY);
            }

            CreateConnectionLines();
            UpdateAllNodeStates();
            SetExactContentSize();
        }

        /// <summary>
        /// Create zone with proper hierarchy: Zone -> ZoneLabel + Nodes
        /// </summary>
        private float CreateZoneWithProperHierarchy(int zoneLevel, float startY)
        {
            // Step 1: Create zone container
            Transform zoneContainer = CreateZoneContainer(zoneLevel, startY);
            
            // Step 2: Create zone label as child of zone container
            CreateZoneLabelInContainer(zoneLevel, zoneContainer);
            
            // Step 3: Get all talents for this zone
            var normalTalents = TalentDatabase.Instance.GetNormalTalentsInZone(zoneLevel);
            var specialTalent = TalentDatabase.Instance.GetSpecialTalentInZone(zoneLevel);
            
            // Step 4: Create normal nodes as children of zone container
            float nodeYOffset = 60f; // Space for zone label
            foreach (var talent in normalTalents.OrderBy(t => GetNodeOrder(t)))
            {
                CreateTalentNodeInZoneContainer(talent, zoneContainer, nodeYOffset);
                nodeYOffset += layoutConfig.NodeSpacing;
            }
            
            // Step 5: Create special node as child of zone container (if exists)
            if (specialTalent != null)
            {
                float specialYOffset = 60f + (layoutConfig.NodeSpacing * 1.5f); // Center in zone
                CreateTalentNodeInZoneContainer(specialTalent, zoneContainer, specialYOffset);
            }
            
            // Step 6: Calculate total zone height
            int totalNodesInZone = normalTalents.Count + (specialTalent != null ? 1 : 0);
            float zoneHeight = 60f + (normalTalents.Count * layoutConfig.NodeSpacing) + 40f; // Label + nodes + padding
            
            return startY + zoneHeight;
        }

        /// <summary>
        /// Create zone container with proper anchoring
        /// </summary>
        private Transform CreateZoneContainer(int zoneLevel, float startY)
        {
            var zoneObj = new GameObject($"Zone_{zoneLevel}");
            zoneObj.transform.SetParent(talentTreeContent, false);

            // Setup RectTransform for zone container
            var zoneRect = zoneObj.AddComponent<RectTransform>();
            zoneRect.anchorMin = new Vector2(0.5f, 0f);  // Bottom center anchor
            zoneRect.anchorMax = new Vector2(0.5f, 0f);  // Bottom center anchor
            zoneRect.pivot = new Vector2(0.5f, 0f);      // Bottom pivot
            zoneRect.anchoredPosition = new Vector2(0f, startY);
            
            // Calculate zone size (width: column span, height: will be set by content)
            float zoneWidth = Mathf.Abs(layoutConfig.NormalColumnX) + Mathf.Abs(layoutConfig.SpecialColumnX) + 200f;
            zoneRect.sizeDelta = new Vector2(zoneWidth, 100f); // Initial height, will adjust

            // Debug visualization
            if (showLayoutDebug)
            {
                var debugImage = zoneObj.AddComponent<Image>();
                debugImage.color = new Color(debugZoneColor.r, debugZoneColor.g, debugZoneColor.b, 0.1f);
                debugImage.raycastTarget = false;
            }

            zoneContainers[zoneLevel] = zoneObj.transform;
            return zoneObj.transform;
        }

        /// <summary>
        /// Create zone label inside zone container
        /// </summary>
        private void CreateZoneLabelInContainer(int zoneLevel, Transform zoneContainer)
        {
            GameObject labelObj;
            
            if (zoneLabelPrefab != null)
            {
                labelObj = Instantiate(zoneLabelPrefab, zoneContainer);
            }
            else
            {
                labelObj = CreateDefaultZoneLabel(zoneLevel);
                labelObj.transform.SetParent(zoneContainer, false);
            }

            // Setup zone label positioning within container
            var labelRect = labelObj.GetComponent<RectTransform>();
            if (labelRect != null)
            {
                labelRect.anchorMin = new Vector2(0.5f, 0f);   // Bottom center of zone
                labelRect.anchorMax = new Vector2(0.5f, 0f);   // Bottom center of zone
                labelRect.pivot = new Vector2(0.5f, 0f);       // Bottom pivot
                labelRect.anchoredPosition = new Vector2(0f, layoutConfig.ZoneLabelOffsetY);
                labelRect.sizeDelta = layoutConfig.ZoneLabelSize;
            }

            // Setup label text
            var labelText = labelObj.GetComponentInChildren<TMP_Text>();
            if (labelText != null)
            {
                labelText.text = $"LEVEL {zoneLevel}";
                labelText.fontSize = layoutConfig.ZoneLabelFontSize;
                labelText.color = layoutConfig.ZoneLabelColor;
                labelText.alignment = TextAlignmentOptions.Center;
            }

            zoneLabels[zoneLevel] = labelObj;
        }

        /// <summary>
        /// Create default zone label
        /// </summary>
        private GameObject CreateDefaultZoneLabel(int zoneLevel)
        {
            var labelObj = new GameObject($"ZoneLabel_{zoneLevel}");

            // Add background
            var bgImage = labelObj.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.6f);
            bgImage.raycastTarget = false;

            // Add text as child
            var textObj = new GameObject("LabelText");
            textObj.transform.SetParent(labelObj.transform, false);
            
            var text = textObj.AddComponent<TMP_Text>();
            text.text = $"LEVEL {zoneLevel}";
            text.fontSize = layoutConfig.ZoneLabelFontSize;
            text.color = layoutConfig.ZoneLabelColor;
            text.alignment = TextAlignmentOptions.Center;

            // Setup text to fill parent
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return labelObj;
        }

        /// <summary>
        /// Create talent node inside zone container as child
        /// </summary>
        private void CreateTalentNodeInZoneContainer(TalentModel talent, Transform zoneContainer, float yOffset)
        {
            var nodeObj = Instantiate(talentNodePrefab, zoneContainer); // Parent = zone container
            var node = nodeObj.GetComponent<TalentNodeBehavior>();
            
            node.Initialize(talent);
            node.OnNodeClicked.AddListener(OnTalentNodeClicked);

            // Position node within zone container
            var nodeRect = nodeObj.GetComponent<RectTransform>();
            
            // X position based on node type (relative to zone container)
            float nodeX = talent.NodeType == TalentNodeType.Normal ? 
                layoutConfig.NormalColumnX : layoutConfig.SpecialColumnX;
            
            // Y position within zone (relative to zone container bottom)
            nodeRect.anchorMin = new Vector2(0.5f, 0f);     // Bottom center of zone
            nodeRect.anchorMax = new Vector2(0.5f, 0f);     // Bottom center of zone
            nodeRect.pivot = new Vector2(0.5f, 0.5f);       // Center pivot for node
            nodeRect.anchoredPosition = new Vector2(nodeX, yOffset);

            // Set node size
            Vector2 nodeSize = talent.NodeType == TalentNodeType.Normal ? 
                layoutConfig.NormalNodeSize : layoutConfig.SpecialNodeSize;
            nodeRect.sizeDelta = nodeSize;

            talentNodes[talent.ID] = node;
            activeNodes.Add(node);
        }

        /// <summary>
        /// Get node order for consistent positioning
        /// </summary>
        private int GetNodeOrder(TalentModel talent)
        {
            if (talent.Name.Contains("ATK")) return 0;
            if (talent.Name.Contains("DEF")) return 1;
            if (talent.Name.Contains("SPEED")) return 2;
            if (talent.Name.Contains("HEAL")) return 3;
            return 4;
        }

        /// <summary>
        /// Create connection lines between nodes in different zones
        /// </summary>
        private void CreateConnectionLines()
        {
            if (!layoutConfig.ShowConnections) return;

            CreateNormalColumnConnections();
            CreateSpecialColumnConnections();
        }

        /// <summary>
        /// Create connections for normal column (by stat type)
        /// </summary>
        private void CreateNormalColumnConnections()
        {
            // Group normal nodes by stat type across all zones
            var statGroups = new Dictionary<string, List<TalentNodeBehavior>>();
            
            foreach (var node in activeNodes.Where(n => n.IsNormalNode()))
            {
                string statType = node.GetStatType();
                if (!statGroups.ContainsKey(statType))
                    statGroups[statType] = new List<TalentNodeBehavior>();
                
                statGroups[statType].Add(node);
            }

            // Create vertical connections for each stat type
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
        /// Create single connection line between nodes
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
        /// Setup connection line between two world positions
        /// </summary>
        private void SetupConnectionLine(GameObject lineObj, Transform startTransform, Transform endTransform)
        {
            var lineRect = lineObj.GetComponent<RectTransform>();
            if (lineRect == null) return;

            // Convert world positions to content local positions
            Vector3 startWorldPos = startTransform.position;
            Vector3 endWorldPos = endTransform.position;
            
            Vector3 startPos = talentTreeContent.InverseTransformPoint(startWorldPos);
            Vector3 endPos = talentTreeContent.InverseTransformPoint(endWorldPos);

            // Calculate line properties
            Vector3 direction = endPos - startPos;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Setup line transform
            Vector3 midpoint = (startPos + endPos) * 0.5f;
            lineRect.anchoredPosition = midpoint;
            lineRect.sizeDelta = new Vector2(distance, layoutConfig.ConnectionLineWidth);
            lineRect.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // Proper anchoring
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>
        /// Set exact content size based on total zone height
        /// </summary>
        private void SetExactContentSize()
        {
            if (talentTreeContent == null) return;

            // Calculate exact content size from database
            float requiredHeight = TalentDatabase.Instance.CalculateRequiredContentHeight();
            float requiredWidth = Mathf.Abs(layoutConfig.NormalColumnX) + Mathf.Abs(layoutConfig.SpecialColumnX) + 
                                 Mathf.Max(layoutConfig.NormalNodeSize.x, layoutConfig.SpecialNodeSize.x) + 200f;

            talentTreeContent.sizeDelta = new Vector2(requiredWidth, requiredHeight);

            Debug.Log($"[TalentWindow] Content size set to: {requiredWidth} x {requiredHeight}");
            
            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(talentTreeContent);
        }

        /// <summary>
        /// Scroll to current progression with delay
        /// </summary>
        private IEnumerator ScrollToCurrentProgressionDelayed()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame(); // Extra frame for layout

            if (talentScrollRect == null) // return if not initialized
                yield break;

            var currentNode = TalentManager.Instance?.GetCurrentProgressionNode();
            if (currentNode != null && talentNodes.TryGetValue(currentNode.ID, out var node))
            {
                ScrollToNode(node);
            }
            else
            {
                talentScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>
        /// Scroll to specific node
        /// </summary>
        private void ScrollToNode(TalentNodeBehavior targetNode)
        {
            if (talentScrollRect == null || targetNode == null) return;

            // Get node's world position and convert to scroll position
            var nodeWorldPos = targetNode.transform.position;
            var nodeLocalPos = talentTreeContent.InverseTransformPoint(nodeWorldPos);
            
            var contentHeight = talentTreeContent.sizeDelta.y;
            var viewportHeight = talentScrollRect.viewport.rect.height;

            // Calculate normalized position (0 = bottom, 1 = top)
            float normalizedPos = Mathf.Clamp01((nodeLocalPos.y + viewportHeight * 0.5f) / contentHeight);
            talentScrollRect.verticalNormalizedPosition = normalizedPos;
        }

        /// <summary>
        /// Clear talent tree completely
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

            // Clear zone containers (will destroy labels and nodes too)
            foreach (var container in zoneContainers.Values)
            {
                if (container != null)
                    Destroy(container.gameObject);
            }
            zoneContainers.Clear();
            zoneLabels.Clear();

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
                    // Basic implementation - can be enhanced with progression logic
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
        /// Show learn confirmation dialog
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
        /// Handle talent learned event
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
                BuildProperZoneHierarchy();
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
            if (_isInitialized)
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
        [ContextMenu("Debug Zone Hierarchy")]
        public void DebugZoneHierarchy()
        {
            Debug.Log($"=== ZONE HIERARCHY DEBUG ===");
            Debug.Log($"Total Zones: {zoneContainers.Count}");
            Debug.Log($"Total Nodes: {activeNodes.Count}");
            
            foreach (var zone in zoneContainers)
            {
                int zoneLevel = zone.Key;
                Transform container = zone.Value;
                int childCount = container.childCount;
                
                Debug.Log($"Zone {zoneLevel}: {childCount} children");
                for (int i = 0; i < container.childCount; i++)
                {
                    var child = container.GetChild(i);
                    Debug.Log($"  - {child.name}");
                }
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
    }
}