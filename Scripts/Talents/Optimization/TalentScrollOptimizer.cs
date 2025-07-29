using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Talents.UI;

namespace Talents.Optimization
{
    /// <summary>
    /// Optimizes talent scroll view performance by culling off-screen nodes
    /// </summary>
    public class TalentScrollOptimizer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform content;
        [SerializeField] private float cullingBuffer = 100f; // Extra space around viewport
        [SerializeField] private bool enableCulling = true;
        [SerializeField] private int maxVisibleNodes = 20; // Maximum nodes to keep active
        [SerializeField] private bool actuallyDisableGameObject = false; // New: Option to disable GameObject for better perf

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private int currentVisibleNodes = 0;
        [SerializeField] private int totalNodes = 0;

        // Node management
        private List<TalentNodeBehavior> allNodes = new List<TalentNodeBehavior>();
        private HashSet<TalentNodeBehavior> visibleNodes = new HashSet<TalentNodeBehavior>(); // Use HashSet for fast lookup
        private Dictionary<TalentNodeBehavior, bool> nodeVisibility = new Dictionary<TalentNodeBehavior, bool>();

        // Viewport bounds
        private RectTransform viewportRect;
        private Bounds viewportBounds;

        // Performance tracking
        private float lastUpdateTime = 0f;
        private float updateInterval = 0.1f; // Update every 100ms

        // Event: Called when a node's visibility changes
        public System.Action<TalentNodeBehavior, bool> OnNodeVisibilityChanged;

        private void Start()
        {
            if (scrollRect == null)
                scrollRect = GetComponent<ScrollRect>();

            if (content == null)
                content = scrollRect.content;

            viewportRect = scrollRect.viewport;

            // Subscribe to scroll events
            scrollRect.onValueChanged.AddListener(OnScrollValueChanged);

            // Listen for viewport/content size changes
            if (viewportRect != null)
                viewportRect.hasChanged = false;
            if (content != null)
                content.hasChanged = false;

            // Initialize
            RefreshNodeList();
            UpdateVisibility();
        }

        /// <summary>
        /// Refresh the list of all nodes
        /// </summary>
        public void RefreshNodeList()
        {
            allNodes.Clear();
            nodeVisibility.Clear();

            // Find all talent nodes in content
            var nodes = content.GetComponentsInChildren<TalentNodeBehavior>(true);
            allNodes.AddRange(nodes);

            totalNodes = allNodes.Count;

            // Initialize visibility dictionary
            foreach (var node in allNodes)
            {
                nodeVisibility[node] = true;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[TalentScrollOptimizer] Found {totalNodes} talent nodes");
            }
        }

        /// <summary>
        /// Handle scroll value changed
        /// </summary>
        private void OnScrollValueChanged(Vector2 scrollValue)
        {
            if (!enableCulling)
                return;

            // Throttle updates for performance
            if (Time.time - lastUpdateTime < updateInterval)
                return;

            UpdateVisibility();
            lastUpdateTime = Time.time;
        }

        /// <summary>
        /// Unity callback: called when RectTransform dimensions change (viewport/content resize)
        /// </summary>
        private void OnRectTransformDimensionsChange()
        {
            if (enableCulling)
            {
                UpdateVisibility();
            }
        }

        /// <summary>
        /// Update node visibility based on viewport
        /// </summary>
        private void UpdateVisibility()
        {
            if (!enableCulling || viewportRect == null || content == null)
                return;

            // Calculate viewport bounds in content space
            UpdateViewportBounds();

            visibleNodes.Clear();
            int activeCount = 0;

            // Check each node
            foreach (var node in allNodes)
            {
                if (node == null || !node.IsInitialized)
                    continue;

                bool shouldBeVisible = IsNodeInViewport(node);
                bool currentlyVisible = nodeVisibility[node];

                // Only update if state changes
                if (shouldBeVisible != currentlyVisible)
                {
                    SetNodeVisibility(node, shouldBeVisible);
                    nodeVisibility[node] = shouldBeVisible;
                    OnNodeVisibilityChanged?.Invoke(node, shouldBeVisible);
                }

                if (shouldBeVisible)
                {
                    visibleNodes.Add(node);
                    activeCount++;
                }
            }

            // Limit visible nodes if too many
            if (activeCount > maxVisibleNodes)
            {
                LimitVisibleNodes();
            }

            currentVisibleNodes = visibleNodes.Count;

            if (showDebugInfo && Time.frameCount % 30 == 0) // Debug every 30 frames
            {
                Debug.Log($"[TalentScrollOptimizer] Visible nodes: {currentVisibleNodes}/{totalNodes}");
            }
        }

        /// <summary>
        /// Update viewport bounds in content space
        /// </summary>
        private void UpdateViewportBounds()
        {
            if (viewportRect == null || content == null)
                return;

            // Get viewport corners in world space
            Vector3[] viewportCorners = new Vector3[4];
            viewportRect.GetWorldCorners(viewportCorners);

            // Convert to content space
            Vector3 min = content.InverseTransformPoint(viewportCorners[0]);
            Vector3 max = content.InverseTransformPoint(viewportCorners[2]);

            // Add buffer
            min.x -= cullingBuffer;
            min.y -= cullingBuffer;
            max.x += cullingBuffer;
            max.y += cullingBuffer;

            // Create bounds
            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min;
            viewportBounds = new Bounds(center, size);
        }

        /// <summary>
        /// Check if a node is within the viewport
        /// </summary>
        private bool IsNodeInViewport(TalentNodeBehavior node)
        {
            if (node == null || !node.IsInitialized)
                return false;

            var nodeRect = node.GetComponent<RectTransform>();
            if (nodeRect == null)
                return true; // Keep visible if no rect

            Vector3 nodePosition = nodeRect.anchoredPosition;
            
            // Simple bounds check
            return viewportBounds.Contains(nodePosition);
        }

        /// <summary>
        /// Set node visibility
        /// </summary>
        private void SetNodeVisibility(TalentNodeBehavior node, bool visible)
        {
            if (node == null)
                return;

            if (actuallyDisableGameObject)
            {
                if (node.gameObject.activeSelf != visible)
                    node.gameObject.SetActive(visible);
                return;
            }

            var canvasGroup = node.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = node.gameObject.AddComponent<CanvasGroup>();

            if (canvasGroup.alpha == (visible ? 1f : 0f))
                return; // Already in correct state

            if (visible)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
            else
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        /// <summary>
        /// Limit visible nodes when too many are active
        /// </summary>
        private void LimitVisibleNodes()
        {
            if (visibleNodes.Count <= maxVisibleNodes)
                return;

            // Sort by distance from viewport center
            var viewportCenter = viewportBounds.center;
            var sortedNodes = new List<TalentNodeBehavior>(visibleNodes);
            sortedNodes.Sort((a, b) => {
                var distA = Vector3.Distance(a.transform.localPosition, viewportCenter);
                var distB = Vector3.Distance(b.transform.localPosition, viewportCenter);
                return distA.CompareTo(distB);
            });

            // Hide nodes that are furthest away
            for (int i = maxVisibleNodes; i < sortedNodes.Count; i++)
            {
                SetNodeVisibility(sortedNodes[i], false);
                nodeVisibility[sortedNodes[i]] = false;
                OnNodeVisibilityChanged?.Invoke(sortedNodes[i], false);
                visibleNodes.Remove(sortedNodes[i]);
            }
        }

        /// <summary>
        /// Force update all nodes visibility
        /// </summary>
        public void ForceUpdateVisibility()
        {
            UpdateVisibility();
        }

        /// <summary>
        /// Enable/disable culling
        /// </summary>
        public void SetCullingEnabled(bool enabled)
        {
            enableCulling = enabled;
            
            if (!enabled)
            {
                // Make all nodes visible
                foreach (var node in allNodes)
                {
                    SetNodeVisibility(node, true);
                    nodeVisibility[node] = true;
                }
            }
            else
            {
                UpdateVisibility();
            }
        }

        /// <summary>
        /// Set maximum visible nodes
        /// </summary>
        public void SetMaxVisibleNodes(int maxNodes)
        {
            maxVisibleNodes = Mathf.Max(1, maxNodes);
            UpdateVisibility();
        }

        /// <summary>
        /// Get performance statistics
        /// </summary>
        public PerformanceStats GetPerformanceStats()
        {
            return new PerformanceStats
            {
                TotalNodes = totalNodes,
                VisibleNodes = currentVisibleNodes,
                CullingEnabled = enableCulling,
                MaxVisibleNodes = maxVisibleNodes,
                CullingBuffer = cullingBuffer,
                UpdateInterval = updateInterval
            };
        }

        /// <summary>
        /// Update method for continuous optimization
        /// </summary>
        private void Update()
        {
            if (enableCulling && Time.time - lastUpdateTime > updateInterval)
            {
                UpdateVisibility();
                lastUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// Handle component enabled
        /// </summary>
        private void OnEnable()
        {
            RefreshNodeList();
            UpdateVisibility();
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        private void OnDestroy()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            }
        }

        // Debug methods
        [ContextMenu("Refresh Node List")]
        public void RefreshNodeListDebug()
        {
            RefreshNodeList();
        }

        [ContextMenu("Force Update Visibility")]
        public void ForceUpdateVisibilityDebug()
        {
            ForceUpdateVisibility();
        }

        [ContextMenu("Toggle Culling")]
        public void ToggleCullingDebug()
        {
            SetCullingEnabled(!enableCulling);
        }

        [ContextMenu("Log Performance Stats")]
        public void LogPerformanceStats()
        {
            var stats = GetPerformanceStats();
            Debug.Log($"[TalentScrollOptimizer] Performance Stats:\n" +
                     $"Total Nodes: {stats.TotalNodes}\n" +
                     $"Visible Nodes: {stats.VisibleNodes}\n" +
                     $"Culling Enabled: {stats.CullingEnabled}\n" +
                     $"Max Visible: {stats.MaxVisibleNodes}\n" +
                     $"Buffer: {stats.CullingBuffer}\n" +
                     $"Update Interval: {stats.UpdateInterval}");
        }

        /// <summary>
        /// Performance statistics structure
        /// </summary>
        [System.Serializable]
        public struct PerformanceStats
        {
            public int TotalNodes;
            public int VisibleNodes;
            public bool CullingEnabled;
            public int MaxVisibleNodes;
            public float CullingBuffer;
            public float UpdateInterval;
        }
    }
}