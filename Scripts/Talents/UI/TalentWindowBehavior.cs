using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using OctoberStudio;
using OctoberStudio.Currency;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Talents.Data;
using Talents.Manager;
using Talents.UI;
using OctoberStudio.Extensions;
using OctoberStudio.Upgrades;
using Talents.Helper;
using UnityEngine.Serialization;

namespace Talents.UI
{
    /// <summary>
    /// Main talent window behavior - handles the talent tree UI with TalentConnectionRenderer
    /// </summary>
    public class TalentWindowBehavior : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button resetAllButton;
        [SerializeField] private TMP_Text goldCoinsText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text orcText;

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

        [Header("2-Column Layout")]
        [SerializeField] private float leftColumnX = -200f;   // Base Stats column
        [SerializeField] private float rightColumnX = 200f;   // Special Skills column  
        [SerializeField] private float nodeSpacing = 20f;     // Khoảng cách giữa các nodes
        [SerializeField] private float startY = 300f;         // Y position bắt đầu

        [Header("Connection Lines")]
        [SerializeField] private TalentConnectionRenderer connectionRenderer;
        [SerializeField] private bool showConnectionLines = true;

        [Header("Performance")]
        [SerializeField] private bool useObjectPooling = true;
        [SerializeField] private int initialPoolSize = 70;

        // Node management
        private Dictionary<int, TalentNodeBehavior> talentNodes = new Dictionary<int, TalentNodeBehavior>();
        private List<TalentNodeBehavior> nodePool = new List<TalentNodeBehavior>();
        private List<TalentNodeBehavior> activeNodes = new List<TalentNodeBehavior>();

        // Track positions for connection lines
        private List<Vector2> baseStatsPositions = new List<Vector2>();
        private List<Vector2> specialSkillsPositions = new List<Vector2>();

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

            // Setup connection renderer
            SetupConnectionRenderer();

            // Subscribe to talent manager events
            if (TalentManager.Instance != null)
            {
                TalentManager.Instance.OnGoldCoinsChanged.AddListener(UpdateCurrencyUI);
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
        /// Setup connection renderer
        /// </summary>
        private void SetupConnectionRenderer()
        {
            // Find TalentConnectionRenderer if not assigned
            if (connectionRenderer == null)
            {
                connectionRenderer = GetComponentInChildren<TalentConnectionRenderer>();
                
                if (connectionRenderer == null)
                {
                    // Create connection renderer if not found
                    GameObject connectionObj = new GameObject("ConnectionRenderer");
                    connectionObj.transform.SetParent(talentTreeContent);
                    connectionRenderer = connectionObj.AddComponent<TalentConnectionRenderer>();
                    
                    Debug.Log("[TalentWindow] Created TalentConnectionRenderer automatically");
                }
            }
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
        /// Build the talent tree UI - 2 column layout với TalentConnectionRenderer
        /// </summary>
        private void BuildTalentTree()
        {
            if (!TalentDatabase.Instance.IsDataLoaded)
            {
                Debug.LogWarning("[TalentWindow] Talent database not loaded yet");
                return;
            }

            // Clear existing
            ClearTalentTree();

            // Clear connection lines
            if (connectionRenderer != null)
            {
                connectionRenderer.ClearAllLines();
            }

            // Reset position arrays
            baseStatsPositions.Clear();
            specialSkillsPositions.Clear();

            // Build talents from CSV data
            BuildTalentsFromCSV();

            // Draw connection lines using TalentConnectionRenderer
            if (showConnectionLines && connectionRenderer != null)
            {
                DrawAllConnectionLines();
            }

            // Update states and UI
            UpdateAllNodeStates();
            UpdateCurrencyUI();
            UpdateContentSize();

            Debug.Log($"[TalentWindow] Built talent tree: {baseStatsPositions.Count} normal, {specialSkillsPositions.Count} special");
        }

        /// <summary>
        /// Build talents from CSV data
        /// </summary>
        private void BuildTalentsFromCSV()
        {
            var allTalents = TalentDatabase.Instance.GetAllTalents();

            foreach (var talent in allTalents)
            {
                var node = CreateTalentNode(talent);

                // Use position from CSV data
                Vector2 position = new Vector2(talent.PositionX, talent.PositionY);
                node.transform.localPosition = position;

                // Track positions for connection lines và apply styling
                if (talent.NodeType == TalentNodeType.Normal)
                {
                    baseStatsPositions.Add(position);
                    SetNodeColor(node, GetBaseStatColor(talent.StatType));
                }
                else if (talent.NodeType == TalentNodeType.Special)
                {
                    specialSkillsPositions.Add(position);
                    SetSpecialSkillStyling(node, talent);
                }

                Debug.Log($"[TalentWindow] Created {talent.NodeType} {talent.Name} at ({talent.PositionX}, {talent.PositionY})");
            }

            // Sort positions for proper connection order (top to bottom)
            baseStatsPositions.Sort((a, b) => b.y.CompareTo(a.y));
            specialSkillsPositions.Sort((a, b) => b.y.CompareTo(a.y));
        }

        /// <summary>
        /// Get color for base stat type
        /// </summary>
        private Color GetBaseStatColor(UpgradeType statType)
        {
            switch (statType)
            {
                case UpgradeType.Damage:
                    return new Color(1f, 0.3f, 0.3f); // Red for ATK
                case UpgradeType.Health:
                    return new Color(0.3f, 1f, 0.3f); // Green for HP
                default:
                    // For Armor and Healing, check the name or use different colors
                    return new Color(0.3f, 0.3f, 1f); // Blue for others
            }
        }

        /// <summary>
        /// Draw all connection lines using TalentConnectionRenderer
        /// </summary>
        private void DrawAllConnectionLines()
        {
            // Draw base stats connections (left column)
            if (baseStatsPositions.Count > 1)
            {
                connectionRenderer.DrawBaseStatsConnections(baseStatsPositions.ToArray());
                Debug.Log($"[TalentWindow] Drew base stats connections: {baseStatsPositions.Count} nodes");
            }

            // Draw special skills connections (right column)
            if (specialSkillsPositions.Count > 1)
            {
                connectionRenderer.DrawSpecialSkillsConnections(specialSkillsPositions.ToArray());
                Debug.Log($"[TalentWindow] Drew special skills connections: {specialSkillsPositions.Count} nodes");
            }
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
        /// Create a talent node
        /// </summary>
        private TalentNodeBehavior CreateTalentNode(TalentModel talent)
        {
            var node = GetPooledNode();
            node.Initialize(talent);

            // Subscribe to click event
            node.OnNodeClicked.AddListener(OnTalentNodeClicked);

            // Add to collections
            talentNodes[talent.ID] = node;
            activeNodes.Add(node);

            // Enhanced display
            EnhanceNodeDisplay(node, talent);

            return node;
        }

        /// <summary>
        /// Enhanced node display
        /// </summary>
        private void EnhanceNodeDisplay(TalentNodeBehavior node, TalentModel talent)
        {
            var textComponents = node.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
            
            foreach (var textComp in textComponents)
            {
                string compName = textComp.name.ToLower();
                
                if (compName.Contains("name") || compName.Contains("title"))
                {
                    // Hiển thị tên talent
                    textComp.text = talent.Name;
                    textComp.fontSize = 16f;
                    textComp.fontStyle = TMPro.FontStyles.Bold;
                }
                else if (compName.Contains("description") || compName.Contains("subtitle"))
                {
                    // Hiển thị description hoặc stat bonus
                    if (talent.NodeType == TalentNodeType.Normal)
                    {
                        var currentLevel = TalentManager.Instance?.GetTalentLevel(talent.ID) ?? 0;
                        var bonus = talent.StatValue * (currentLevel + 1);
                        textComp.text = $"+{bonus}";
                        textComp.fontSize = 20f;
                        textComp.color = Color.white;
                    }
                    else
                    {
                        textComp.text = talent.Description;
                        textComp.fontSize = 12f;
                        textComp.color = Color.gray;
                    }
                }
                else if (compName.Contains("cost") || compName.Contains("price"))
                {
                    // Hiển thị cost với currency icon
                    var cost = talent.Cost;
                    var currencyIcon = talent.NodeType == TalentNodeType.Normal ? "💰" : "🧬";
                    textComp.text = $"{currencyIcon}{cost}";
                    textComp.fontSize = 14f;
                    textComp.color = Color.yellow;
                }
                else if (compName.Contains("level") || compName.Contains("progress"))
                {
                    // Hiển thị current level / max level
                    var currentLevel = TalentManager.Instance?.GetTalentLevel(talent.ID) ?? 0;
                    
                    if (talent.NodeType == TalentNodeType.Normal)
                    {
                        textComp.text = $"Level {currentLevel}/{talent.MaxLevel}";
                    }
                    else
                    {
                        textComp.text = currentLevel > 0 ? "LEARNED" : $"Requires Lv.{talent.RequiredPlayerLevel}";
                    }
                    
                    textComp.fontSize = 10f;
                    textComp.color = currentLevel > 0 ? Color.green : Color.gray;
                }
            }
            
            // Add progress bar cho Normal talents
            if (talent.NodeType == TalentNodeType.Normal)
            {
                AddProgressBar(node, talent);
            }
        }

        /// <summary>
        /// Add progress bar for normal talents
        /// </summary>
        private void AddProgressBar(TalentNodeBehavior node, TalentModel talent)
        {
            // Tìm hoặc tạo progress bar
            var progressBar = node.GetComponentInChildren<Slider>();
            if (progressBar == null)
            {
                // Create progress bar nếu chưa có
                GameObject progressObj = new GameObject("ProgressBar");
                progressObj.transform.SetParent(node.transform);

                var slider = progressObj.AddComponent<Slider>();
                var rectTrans = progressObj.GetComponent<RectTransform>();
                rectTrans.anchoredPosition = new Vector2(0, -20);
                rectTrans.sizeDelta = new Vector2(100, 10);

                progressBar = slider;
            }

            // Update progress bar value
            var currentLevel = TalentManager.Instance?.GetTalentLevel(talent.ID) ?? 0;
            progressBar.maxValue = talent.MaxLevel;
            progressBar.value = currentLevel;
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
        /// Set special skill styling
        /// </summary>
        private void SetSpecialSkillStyling(TalentNodeBehavior node, TalentModel talent)
        {
            // Set gold/premium color for special skills
            var specialColor = new Color(1f, 0.8f, 0f); // Gold color
            SetNodeColor(node, specialColor);

            // Add level requirement badge
            var texts = node.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.name.Contains("Level") || text.name.Contains("Requirement"))
                {
                    text.text = $"Lv.{talent.RequiredPlayerLevel}";
                    text.color = Color.yellow;
                    break;
                }
            }
        }

        /// <summary>
        /// Update content size for 2 columns
        /// </summary>
        private void UpdateContentSize()
        {
            if (talentTreeContent == null) return;

            // Calculate bounds for all nodes
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            var allPositions = baseStatsPositions.Concat(specialSkillsPositions);
            foreach (var pos in allPositions)
            {
                minX = Mathf.Min(minX, pos.x);
                maxX = Mathf.Max(maxX, pos.x);
                minY = Mathf.Min(minY, pos.y);
                maxY = Mathf.Max(maxY, pos.y);
            }

            // Add padding
            float padding = 150f;
            float width = (maxX - minX) + padding * 2;
            float height = (maxY - minY) + padding * 2;

            talentTreeContent.sizeDelta = new Vector2(width, height);

            Debug.Log($"[TalentWindow] Updated content size: {width:F0} x {height:F0}");
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

            // Update connection line colors based on talent states
            UpdateConnectionLineColors();
        }

        /// <summary>
        /// Update connection line colors based on talent unlock status
        /// </summary>
        private void UpdateConnectionLineColors()
        {
            if (connectionRenderer == null) return;

            bool[] normalUnlockStates = new bool[baseStatsPositions.Count];
            for (int i = 0; i < baseStatsPositions.Count; i++)
            {
                var position = baseStatsPositions[i];
                var talent = FindTalentAtPosition(position);
                if (talent != null)
                {
                    normalUnlockStates[i] = TalentManager.Instance?.GetTalentLevel(talentId: talent.ID) > 0;
                }
            }

            // Create unlock states array for special skills
            bool[] specialUnlockStates = new bool[specialSkillsPositions.Count];
            for (int i = 0; i < specialSkillsPositions.Count; i++)
            {
                var position = specialSkillsPositions[i];
                var talent = FindTalentAtPosition(position);
                if (talent != null)
                {
                    specialUnlockStates[i] = TalentManager.Instance?.GetTalentLevel(talent.ID) > 0;
                }
            }

            // Update line colors
            connectionRenderer.UpdateLineColors(normalUnlockStates.Concat(specialUnlockStates).ToArray());
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
                // Hiển thị confirmation với đúng currency
                ShowLearnConfirmation(node);
            }
            else
            {
                // Show tooltip with requirement info
                ShowTooltip(node);
            }
        }

        /// <summary>
        /// Show learn confirmation dialog
        /// </summary>
        private void ShowLearnConfirmation(TalentNodeBehavior node)
        {
            var talent = node.TalentModel;
            var currentLevel = TalentManager.Instance.GetTalentLevel(talent.ID);
            var cost = TalentDatabase.Instance.GetTalentCost(talent.ID, currentLevel + 1);

            string confirmationMessage;

            if (talent.NodeType == TalentNodeType.Normal)
            {
                confirmationMessage = $"Upgrade {talent.Name} to Level {currentLevel + 1}?\n" +
                                     $"Cost: {cost} Gold Coins";
            }
            else
            {
                confirmationMessage = $"Learn {talent.Name}?\n" +
                                     $"Cost: {cost} Gold DNA\n" +
                                     $"Requires Player Level {talent.RequiredPlayerLevel}";
            }

            if (confirmationPanel != null && confirmationText != null)
            {
                confirmationText.text = confirmationMessage;
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
        /// Show tooltip for talent node
        /// </summary>
        private void ShowTooltip(TalentNodeBehavior node)
        {
            if (tooltipPanel == null || tooltipText == null)
                return;

            var talent = node.TalentModel;
            var progressInfo = TalentManager.Instance.GetTalentProgressInfo(talent.ID);

            string tooltipContent = $"<b>{talent.Name}</b>\n";
            tooltipContent += $"{talent.Description}\n\n";

            if (talent.NodeType == TalentNodeType.Normal)
            {
                tooltipContent += $"<color=yellow>Base Stat</color>\n";
                tooltipContent += $"Progress: {progressInfo.CurrentLevel}/{progressInfo.MaxLevel}\n";
                tooltipContent += $"Currency: Gold Coins\n";
            }
            else
            {
                tooltipContent += $"<color=cyan>Special Skill</color>\n";
                tooltipContent += $"Unlock Level: {talent.RequiredPlayerLevel}\n";
                tooltipContent += $"Currency: Gold DNA\n";
            }

            if (progressInfo.NextLevelCost > 0)
            {
                tooltipContent += $"Next Cost: {progressInfo.NextLevelCost}\n";
            }

            // Hiển thị lý do không thể học
            var unlockStatus = progressInfo.UnlockStatus;
            if (unlockStatus != TalentUnlockStatus.Available)
            {
                switch (unlockStatus)
                {
                    case TalentUnlockStatus.Locked:
                        tooltipContent += $"<color=red>Requires Player Level {talent.RequiredPlayerLevel}</color>";
                        break;
                    case TalentUnlockStatus.InsufficientPoints:
                        var currencyName = talent.NodeType == TalentNodeType.Normal ? "Gold Coins" : "Gold DNA";
                        tooltipContent += $"<color=red>Not enough {currencyName}</color>";
                        break;
                    case TalentUnlockStatus.MaxLevel:
                        tooltipContent += $"<color=green>Max Level Reached</color>";
                        break;
                }
            }

            tooltipText.text = tooltipContent;
            EnableToolTip(true);

            if (tooltipRectTransform != null)
            {
                tooltipRectTransform.anchoredPosition = new Vector2(0, -200);
            }
        }

        /// <summary>
        /// Enable/disable tooltip
        /// </summary>
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
                    orcText.text = $"DNA: {orcSave?.Amount ?? 0}";
                }
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
                UpdateCurrencyUI();
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
            
            // Clear connection lines
            if (connectionRenderer != null)
            {
                connectionRenderer.ClearAllLines();
            }

            // Unsubscribe from events
            if (TalentManager.Instance != null)
            {
                TalentManager.Instance.OnGoldCoinsChanged.RemoveListener(UpdateCurrencyUI);
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

        [ContextMenu("Toggle Connection Lines")]
        public void ToggleConnectionLines()
        {
            showConnectionLines = !showConnectionLines;

            if (showConnectionLines && connectionRenderer != null)
            {
                DrawAllConnectionLines();
            }
            else if (connectionRenderer != null)
            {
                connectionRenderer.ClearAllLines();
            }

            Debug.Log($"[TalentWindow] Connection lines: {(showConnectionLines ? "ON" : "OFF")}");
        }

        [ContextMenu("Test Connection Renderer")]
        public void TestConnectionRenderer()
        {
            if (connectionRenderer != null)
            {
                connectionRenderer.TestDrawBaseStats();
                connectionRenderer.TestDrawSpecialSkills();
            }
            else
            {
                Debug.LogWarning("[TalentWindow] TalentConnectionRenderer not found!");
            }
        }

        [ContextMenu("Log Talent Tree State")]
        public void LogTalentTreeState()
        {
            Debug.Log($"[TalentWindow] Active nodes: {activeNodes.Count}, Pooled nodes: {nodePool.Count}");
            Debug.Log($"[TalentWindow] Base stats positions: {baseStatsPositions.Count}, Special skills: {specialSkillsPositions.Count}");
            Debug.Log($"[TalentWindow] Connection renderer: {(connectionRenderer != null ? "Found" : "Missing")}");
        }
    }
}