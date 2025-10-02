using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using OctoberStudio.User;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Talents.Data;
using Talents.Manager;
using Talents.Config;
using UnityEngine.Rendering;

namespace Talents.UI
{
    public class TalentWindowBehavior : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI goldCoinsText;
        [SerializeField] private TextMeshProUGUI orcText;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Talent Tree")]
        [SerializeField] private ScrollRect talentScrollRect;
        [SerializeField] private RectTransform talentTreeContent;
        [SerializeField] private GameObject talentNodePrefab;
        [SerializeField] private GameObject zoneLabelPrefab;

        [Header("Line Rendering")]
        [SerializeField] private GameObject connectionLinePrefab;
        [SerializeField] private GameObject specialConnectionLinePrefab; // Special connection line for Special nodes
        [SerializeField] private Transform connectionLineParent;

        [Header("Level Progress Line")]
        [SerializeField] private GameObject levelLinePrefab; // Prefab that moves to current level zone
        [SerializeField] private Talents.Background.WaveBackgroundController waveBackgroundController; // Wave background that rises to current progress

        [Header("Tooltip")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipText;
        [SerializeField] private RectTransform tooltipRectTransform;
        [SerializeField] private Button btnCloseTooltips;

        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private TextMeshProUGUI confirmationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("Layout Debug")]
        [SerializeField] private bool showLayoutDebug = false;
        [SerializeField] private Color debugZoneColor = Color.yellow;

        // Node management
        private Dictionary<int, TalentNodeBehavior> talentNodes = new Dictionary<int, TalentNodeBehavior>();
        private List<TalentNodeBehavior> activeNodes = new List<TalentNodeBehavior>();
        private List<GameObject> connectionLines = new List<GameObject>();
        
        private Dictionary<int, Transform> zoneContainers = new Dictionary<int, Transform>();
        private Dictionary<int, GameObject> zoneLabels = new Dictionary<int, GameObject>();

        // Layout
        private TalentLayoutConfig layoutConfig;

        // State
        private bool _isInitialized = false;
        private System.Action confirmationCallback;

        // Level Line Instance
        private GameObject currentLevelLineInstance;

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
            btnCloseTooltips.onClick.AddListener(HideUIElements);
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

        /// <summary>
        /// Setup level line and wave background for current progress indication
        /// </summary>
        private void SetupLevelLine()
        {
            // Setup WaveBackgroundController in TalentTreeContent if assigned
            SetupWaveBackgroundInContent();

            // Initial update for both systems
            UpdateLevelLinePosition();
        }

        /// <summary>
        /// Setup wave background controller in talent tree content
        /// </summary>
        private void SetupWaveBackgroundInContent()
        {
            if (waveBackgroundController == null) return;

            // Move WaveBackgroundController to TalentTreeContent for proper positioning
            var waveTransform = waveBackgroundController.transform;
            if (waveTransform.parent != talentTreeContent)
            {
                waveTransform.SetParent(talentTreeContent, false);

                // Set wave background to render behind everything
                waveTransform.SetAsFirstSibling();

                // Setup positioning within content
                var waveRect = waveTransform.GetComponent<RectTransform>();
                if (waveRect != null)
                {
                    // Position at bottom of content, full width
                    waveRect.anchorMin = new Vector2(0f, 0f);
                    waveRect.anchorMax = new Vector2(1f, 0f);
                    waveRect.pivot = new Vector2(0.5f, 0f);
                    waveRect.anchoredPosition = Vector2.zero;
                    waveRect.sizeDelta = new Vector2(0f, 0f); // Auto width, height will be controlled by WaveBackgroundController
                }
            }
        }

        /// <summary>
        /// Update level line position based on current player level
        /// </summary>
        private void UpdateLevelLinePosition()
        {
            if (TalentManager.Instance == null) return;

            int currentPlayerLevel = GetCurrentPlayerLevel();

            MoveLevelLineToPrefabPosition(currentPlayerLevel);
            UpdateWaveBackgroundToCurrentProgress();
        }

        /// <summary>
        /// Get current player level from game system
        /// </summary>
        private int GetCurrentPlayerLevel()
        {
            if (UserProfileManager.Instance != null && UserProfileManager.Instance.IsDataReady)
            {
                return UserProfileManager.Instance.ProfileSave.UserLevel;
            }

            return 1;
        }

        /// <summary>
        /// Move level line prefab to position of target level zone
        /// </summary>
        private void MoveLevelLineToPrefabPosition(int targetLevel)
        {
            if (levelLinePrefab == null) return;

            // Validate target level exists in zone containers
            if (!zoneContainers.ContainsKey(targetLevel))
            {
                var availableZones = zoneContainers.Keys.OrderBy(z => Mathf.Abs(z - targetLevel));
                int closestZone = availableZones.FirstOrDefault();

                if (closestZone != 0)
                {
                    targetLevel = closestZone;
                }
                else
                {
                    return;
                }
            }

            // Create instance if it doesn't exist
            if (currentLevelLineInstance == null)
            {
                currentLevelLineInstance = Instantiate(levelLinePrefab, talentTreeContent);

                // Set render order (above wave background, below nodes)
                var siblingIndex = talentTreeContent.childCount > 1 ? 1 : 0;
                currentLevelLineInstance.transform.SetSiblingIndex(siblingIndex);
            }

            // Calculate position for target level zone
            Vector2 targetPosition = CalculateZonePosition(targetLevel);

            // Move levelLine to target position with animation
            var levelLineRect = currentLevelLineInstance.GetComponent<RectTransform>();
            if (levelLineRect != null)
            {
                levelLineRect.DOAnchorPos(targetPosition, 1.0f)
                    .SetEase(DG.Tweening.Ease.OutQuart);
            }
        }

        /// <summary>
        /// Move Level Line to Zone_2 and position at first node (node 1)
        /// </summary>
        public void MoveLevelLineToZone2FirstNode()
        {
            const int zone2Level = 2;

            if (levelLinePrefab == null)
            {
                Debug.LogWarning("[TalentWindow] Level Line prefab is not assigned");
                return;
            }

            // Validate Zone_2 exists
            if (!zoneContainers.ContainsKey(zone2Level))
            {
                Debug.LogWarning("[TalentWindow] Zone_2 (level 2) does not exist in zone containers");
                return;
            }

            // Get first node in Zone_2 (should be ATK based on GetNodeOrder method)
            var zone2Talents = TalentDatabase.Instance.GetNormalTalentsInZone(zone2Level);
            var firstNode = zone2Talents.OrderBy(t => GetNodeOrder(t)).FirstOrDefault();

            if (firstNode == null)
            {
                Debug.LogWarning("[TalentWindow] No normal talents found in Zone_2");
                return;
            }

            // Create level line instance if it doesn't exist
            if (currentLevelLineInstance == null)
            {
                currentLevelLineInstance = Instantiate(levelLinePrefab, talentTreeContent);

                // Set render order (above wave background, below nodes)
                var siblingIndex = talentTreeContent.childCount > 1 ? 1 : 0;
                currentLevelLineInstance.transform.SetSiblingIndex(siblingIndex);
            }

            // Calculate position for first node in Zone_2
            Vector2 targetPosition = CalculateFirstNodePositionInZone(zone2Level, firstNode);

            // Move level line to target position with animation
            var levelLineRect = currentLevelLineInstance.GetComponent<RectTransform>();
            if (levelLineRect != null)
            {
                levelLineRect.DOAnchorPos(targetPosition, 1.0f)
                    .SetEase(DG.Tweening.Ease.OutQuart)
                    .OnComplete(() => {
                        Debug.Log($"[TalentWindow] Level Line moved to Zone_2 first node: {firstNode.Name} at position {targetPosition}");
                    });
            }
        }

        /// <summary>
        /// Calculate position for first node in specified zone
        /// </summary>
        private Vector2 CalculateFirstNodePositionInZone(int zoneLevel, TalentModel firstNode)
        {
            if (layoutConfig == null) return Vector2.zero;

            // Get zone container position
            if (zoneContainers.TryGetValue(zoneLevel, out Transform zoneContainer))
            {
                // Calculate the position relative to the zone container
                // First node is positioned at nodeYOffset = 60f (space for zone label)
                Vector3 zoneLocalPos = talentTreeContent.InverseTransformPoint(zoneContainer.position);
                float firstNodeYOffset = 60f; // This matches the yOffset used in CreateZoneWithProperHierarchy

                // X position should be at normal column (left side for normal nodes)
                float targetX = layoutConfig.NormalColumnX;
                float targetY = zoneLocalPos.y + firstNodeYOffset;

                return new Vector2(targetX, targetY);
            }
            else
            {
                // Fallback calculation if zone container not found
                float zoneStartY = layoutConfig.CalculateZoneStartY(zoneLevel);
                float firstNodeY = zoneStartY + 60f; // Add offset for zone label
                return new Vector2(layoutConfig.NormalColumnX, firstNodeY);
            }
        }

        /// <summary>
        /// Calculate position for zone container
        /// </summary>
        private Vector2 CalculateZonePosition(int targetLevel)
        {
            if (layoutConfig == null) return Vector2.zero;

            // Find the zone container for target level
            if (zoneContainers.TryGetValue(targetLevel, out Transform zoneContainer))
            {
                // Get local position of zone container
                Vector3 zoneLocalPos = talentTreeContent.InverseTransformPoint(zoneContainer.position);
                return new Vector2(0f, zoneLocalPos.y); // Center horizontally, use zone Y position
            }
            else
            {
                // Fallback calculation based on layout config
                float zoneStartY = layoutConfig.CalculateZoneStartY(targetLevel);
                return new Vector2(0f, zoneStartY);
            }
        }

        /// <summary>
        /// Update wave background to rise to current progress
        /// </summary>
        private void UpdateWaveBackgroundToCurrentProgress()
        {
            if (waveBackgroundController == null) return;

            // Get highest unlocked level (current progress)
            int highestUnlockedLevel = CalculateHighestUnlockedLevel();
            int maxLevel = TalentDatabase.Instance?.MaxPlayerLevel ?? 30;

            // Update wave background to rise to current progress
            waveBackgroundController.UpdateWaveHeight(highestUnlockedLevel, maxLevel);

            Debug.Log($"[TalentWindow] Wave background updated to level {highestUnlockedLevel}/{maxLevel}");
        }

        /// <summary>
        /// Calculate highest unlocked level from talent system
        /// </summary>
        private int CalculateHighestUnlockedLevel()
        {
            if (TalentManager.Instance == null) return 0;

            int highestLevel = 0;
            var allTalents = TalentDatabase.Instance.GetAllTalents();

            foreach (var talent in allTalents)
            {
                if (TalentManager.Instance.IsTalentLearned(talent.ID))
                {
                    highestLevel = Mathf.Max(highestLevel, talent.RequiredPlayerLevel);
                }
            }

            return highestLevel;
        }

        private void SubscribeToEvents()
        {
            if (TalentManager.Instance != null)
            {
                TalentManager.Instance.OnCurrencyChanged.AddListener(UpdateCurrencyUI);
                TalentManager.Instance.OnTalentLearned.AddListener(OnTalentLearned);
                TalentManager.Instance.OnTalentLearned.AddListener(OnTalentLearnedUpdateLevelLine);
            }

            // Subscribe to player level changes
            if (OctoberStudio.User.UserProfileManager.Instance != null)
            {
                OctoberStudio.User.UserProfileManager.Instance.onUserLevelUp += OnPlayerLevelUp;
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
                SetupLevelLine();
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
            UpdateLevelLinePosition();
        }
        
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
                float specialYOffset = layoutConfig.SpecialNodeOffsetY; // Use config value
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
            float zoneWidth = Mathf.Abs(layoutConfig.NormalColumnX) + Mathf.Abs(layoutConfig.SpecialColumnX) + 200f;
            zoneRect.sizeDelta = new Vector2(zoneWidth, 100f);

            if (showLayoutDebug)
            {
                var debugImage = zoneObj.AddComponent<Image>();
                debugImage.color = new Color(debugZoneColor.r, debugZoneColor.g, debugZoneColor.b, 0.1f);
                debugImage.raycastTarget = false;
            }

            zoneContainers[zoneLevel] = zoneObj.transform;
            return zoneObj.transform;
        }
        private void CreateZoneLabelInContainer(int zoneLevel, Transform zoneContainer)
        {
            if (zoneLabelPrefab != null)
            {
                var labelObj = Instantiate(zoneLabelPrefab, zoneContainer);
                labelObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().SetText(zoneLevel.ToString());

                // Setup zone label positioning using config
                var labelRect = labelObj.GetComponent<RectTransform>();
                if (labelRect != null)
                {
                    labelRect.anchorMin = new Vector2(0.5f, 0f);   // Bottom center of zone
                    labelRect.anchorMax = new Vector2(0.5f, 0f);   // Bottom center of zone
                    labelRect.pivot = new Vector2(0.5f, 0f);       // Bottom pivot
                    labelRect.anchoredPosition = new Vector2(0f, layoutConfig.ZoneLabelPositionY);
                    labelRect.sizeDelta = layoutConfig.ZoneLabelSize;
                }

                zoneLabels[zoneLevel] = labelObj;
            }
            // else
            // {
            //     // labelObj = CreateDefaultZoneLabel(zoneLevel);
            //     // labelObj.transform.SetParent(zoneContainer, false);
            // }

            // Setup zone label positioning within container
            // var labelRect = labelObj.GetComponent<RectTransform>();
            // if (labelRect != null)
            // {
            //     labelRect.anchorMin = new Vector2(0.5f, 0f);   // Bottom center of zone
            //     labelRect.anchorMax = new Vector2(0.5f, 0f);   // Bottom center of zone
            //     labelRect.pivot = new Vector2(0.5f, 0f);       // Bottom pivot
            //     labelRect.anchoredPosition = new Vector2(0f, layoutConfig.ZoneLabelOffsetY);
            //     labelRect.sizeDelta = layoutConfig.ZoneLabelSize;
            // }
            //
            // // Setup label text
            // var labelText = labelObj.GetComponentInChildren<TextMeshProUGUI>();
            // if (labelText != null)
            // {
            //     labelText.text = $"LEVEL {zoneLevel}";
            //     labelText.fontSize = layoutConfig.ZoneLabelFontSize;
            //     labelText.alignment = TextAlignmentOptions.Center;
            // }
    
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
            
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = $"LEVEL {zoneLevel}";
            text.fontSize = layoutConfig.ZoneLabelFontSize;
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
                CreateSpecialConnectionLine(specialNodes[i], specialNodes[i + 1]);
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
                SetupConnectionLine(lineObj, fromNode.transform, toNode.transform);
                connectionLines.Add(lineObj);
            }
            // else
            // {
            //     lineObj = CreateDefaultConnectionLine();
            // }


        }

        /// <summary>
        /// Create special connection line between special nodes
        /// </summary>
        private void CreateSpecialConnectionLine(TalentNodeBehavior fromNode, TalentNodeBehavior toNode)
        {
            GameObject lineObj;

            if (specialConnectionLinePrefab != null)
            {
                lineObj = Instantiate(specialConnectionLinePrefab, connectionLineParent);
                SetupConnectionLine(lineObj, fromNode.transform, toNode.transform);
                connectionLines.Add(lineObj);

                // Apply special styling to the connection line
                ApplySpecialConnectionStyling(lineObj);
            }
            else
            {
                // Fallback to regular connection line if special prefab is not available
                CreateConnectionLine(fromNode, toNode);

                // Find the last added line and apply special styling
                if (connectionLines.Count > 0)
                {
                    ApplySpecialConnectionStyling(connectionLines[connectionLines.Count - 1]);
                }
            }
        }

        /// <summary>
        /// Apply special styling to connection lines for special nodes
        /// </summary>
        private void ApplySpecialConnectionStyling(GameObject lineObj)
        {
            var lineImage = lineObj.GetComponent<Image>();
            if (lineImage != null)
            {
                // Make special connection lines golden and slightly thicker
                lineImage.color = new Color(1f, 0.8f, 0f, 0.8f); // Golden color

                var lineRect = lineObj.GetComponent<RectTransform>();
                if (lineRect != null)
                {
                    // Make special lines slightly thicker
                    Vector2 currentSize = lineRect.sizeDelta;
                    lineRect.sizeDelta = new Vector2(currentSize.x, currentSize.y * 1.5f);
                }
            }
        }


        /// <summary>
        /// Setup connection line between two world positions
        /// </summary>
        private void SetupConnectionLine(GameObject lineObj, Transform startTransform, Transform endTransform)
        {
            var lineRect = lineObj.GetComponent<RectTransform>();
            if (lineRect == null) return;

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
            yield return new WaitForEndOfFrame();

            if (talentScrollRect == null)
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

            var nodeWorldPos = targetNode.transform.position;
            var nodeLocalPos = talentTreeContent.InverseTransformPoint(nodeWorldPos);
            
            var contentHeight = talentTreeContent.sizeDelta.y;
            var viewportHeight = talentScrollRect.viewport.rect.height;
            float normalizedPos = Mathf.Clamp01((nodeLocalPos.y + viewportHeight * 0.5f) / contentHeight);
            talentScrollRect.verticalNormalizedPosition = normalizedPos;
        }

        private void ClearTalentTree()
        {
            foreach (var node in activeNodes)
            {
                if (node != null)
                    Destroy(node.gameObject);
            }
            activeNodes.Clear();
            talentNodes.Clear();

            foreach (var container in zoneContainers.Values)
            {
                if (container != null)
                    Destroy(container.gameObject);
            }
            zoneContainers.Clear();
            zoneLabels.Clear();

            // Clear level line instance
            if (currentLevelLineInstance != null)
            {
                Destroy(currentLevelLineInstance);
                currentLevelLineInstance = null;
            }

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
        /// Handle talent learned event for level line updates
        /// </summary>
        private void OnTalentLearnedUpdateLevelLine(TalentModel talent)
        {
            // Update level line position when a new talent is learned
            // This might change the current unlockable level
            UpdateLevelLinePosition();

            // Check if we should continue to Zone_2 progression
            CheckAndContinueToZone2Progression(talent);
        }

        /// <summary>
        /// Check if talent progression should continue to Zone_2 and move Level Line accordingly
        /// </summary>
        private void CheckAndContinueToZone2Progression(TalentModel learnedTalent)
        {
            // Check if we've completed Zone_1 (level 1) and can progress to Zone_2
            if (learnedTalent.RequiredPlayerLevel == 1 && CanProgressToZone2())
            {
                // Move Level Line to Zone_2 first node
                MoveLevelLineToZone2FirstNode();
                Debug.Log("[TalentWindow] Talent progression continuing to Zone_2");
            }
        }

        /// <summary>
        /// Check if player can progress to Zone_2 (has completed requirements for level 1)
        /// </summary>
        private bool CanProgressToZone2()
        {
            if (TalentManager.Instance == null) return false;

            // Get all Zone_1 talents
            var zone1Talents = TalentDatabase.Instance.GetNormalTalentsInZone(1);

            // Check if at least one talent from Zone_1 is learned (typical progression requirement)
            bool hasZone1Progress = zone1Talents.Any(t => TalentManager.Instance.IsTalentLearned(t.ID));

            // Also check if player level allows Zone_2 access
            int currentPlayerLevel = GetCurrentPlayerLevel();
            bool hasLevelRequirement = currentPlayerLevel >= 2;

            return hasZone1Progress && hasLevelRequirement;
        }

        /// <summary>
        /// Handle player level up event
        /// </summary>
        private void OnPlayerLevelUp(int newLevel)
        {
            // Update level line position when player level changes
            UpdateLevelLinePosition();

            Debug.Log($"[TalentWindow] Player leveled up to {newLevel}, updating level line position");
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
                TalentManager.Instance.OnTalentLearned.RemoveListener(OnTalentLearnedUpdateLevelLine);
            }

            // Unsubscribe from player level events
            if (OctoberStudio.User.UserProfileManager.Instance != null)
            {
                OctoberStudio.User.UserProfileManager.Instance.onUserLevelUp -= OnPlayerLevelUp;
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

        /// <summary>
        /// Public method to manually trigger Level Line movement to Zone_2 first node
        /// Can be called from UI or other systems
        /// </summary>
        [ContextMenu("Move Level Line to Zone 2 First Node")]
        public void TriggerLevelLineToZone2()
        {
            MoveLevelLineToZone2FirstNode();
        }

        /// <summary>
        /// Public method to test Zone_2 progression logic
        /// </summary>
        [ContextMenu("Test Zone 2 Progression")]
        public void TestZone2Progression()
        {
            bool canProgress = CanProgressToZone2();
            Debug.Log($"[TalentWindow] Can progress to Zone_2: {canProgress}");

            if (canProgress)
            {
                MoveLevelLineToZone2FirstNode();
            }
            else
            {
                Debug.Log("[TalentWindow] Zone_2 progression requirements not met");

                // Show current status
                var zone1Talents = TalentDatabase.Instance.GetNormalTalentsInZone(1);
                int learnedCount = zone1Talents.Count(t => TalentManager.Instance.IsTalentLearned(t.ID));
                int currentLevel = GetCurrentPlayerLevel();

                Debug.Log($"[TalentWindow] Zone_1 talents learned: {learnedCount}/{zone1Talents.Count}");
                Debug.Log($"[TalentWindow] Current player level: {currentLevel} (need >= 2)");
            }
        }

    }
}