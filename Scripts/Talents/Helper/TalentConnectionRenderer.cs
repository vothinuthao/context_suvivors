using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Talents.Manager;
using Talents.UI;

namespace Talents.Helper
{
    /// <summary>
    /// Zone-based connection renderer for talent tree
    /// Creates vertical lines connecting nodes in same columns
    /// </summary>
    public class TalentConnectionRenderer : MonoBehaviour
    {
        [Header("Line Settings")]
        [SerializeField] private Color activeLineColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
        [SerializeField] private Color inactiveLineColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
        [SerializeField] private float lineWidth = 3f;
        [SerializeField] private bool showConnections = true;

        [Header("Line Materials")]
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Sprite lineSprite;

        // Connection tracking
        private List<GameObject> normalColumnLines = new List<GameObject>();
        private List<GameObject> specialColumnLines = new List<GameObject>();
        private Dictionary<string, List<GameObject>> statTypeLines = new Dictionary<string, List<GameObject>>();

        // Cache
        private Transform parentTransform;

        private void Awake()
        {
            parentTransform = transform;
            
            // Initialize stat type line collections
            statTypeLines["ATK"] = new List<GameObject>();
            statTypeLines["DEF"] = new List<GameObject>();
            statTypeLines["SPEED"] = new List<GameObject>();
            statTypeLines["HEAL"] = new List<GameObject>();
        }

        /// <summary>
        /// Create connections for all talent nodes
        /// </summary>
        public void CreateAllConnections(Dictionary<int, TalentNodeBehavior> talentNodes)
        {
            if (!showConnections) return;

            ClearAllLines();
            
            CreateNormalColumnConnections(talentNodes);
            CreateSpecialColumnConnections(talentNodes);
            
            UpdateConnectionStates();
        }

        /// <summary>
        /// Create connections for normal column (left side)
        /// </summary>
        private void CreateNormalColumnConnections(Dictionary<int, TalentNodeBehavior> talentNodes)
        {
            // Group normal nodes by stat type
            var normalNodes = talentNodes.Values
                .Where(n => n.IsNormalNode())
                .ToList();

            var statGroups = new Dictionary<string, List<TalentNodeBehavior>>();
            
            foreach (var node in normalNodes)
            {
                string statType = node.GetStatType();
                if (!statGroups.ContainsKey(statType))
                    statGroups[statType] = new List<TalentNodeBehavior>();
                
                statGroups[statType].Add(node);
            }

            // Create vertical connections for each stat type
            foreach (var group in statGroups)
            {
                string statType = group.Key;
                var nodes = group.Value.OrderBy(n => n.GetZoneLevel()).ToList();
                
                for (int i = 0; i < nodes.Count - 1; i++)
                {
                    var line = CreateConnectionLine(nodes[i], nodes[i + 1], $"Normal_{statType}_{i}");
                    normalColumnLines.Add(line);
                    statTypeLines[statType].Add(line);
                }
            }
        }

        /// <summary>
        /// Create connections for special column (right side)
        /// </summary>
        private void CreateSpecialColumnConnections(Dictionary<int, TalentNodeBehavior> talentNodes)
        {
            var specialNodes = talentNodes.Values
                .Where(n => n.IsSpecialNode())
                .OrderBy(n => n.GetZoneLevel())
                .ToList();

            for (int i = 0; i < specialNodes.Count - 1; i++)
            {
                var line = CreateConnectionLine(specialNodes[i], specialNodes[i + 1], $"Special_{i}");
                specialColumnLines.Add(line);
            }
        }

        /// <summary>
        /// Create a single connection line between two nodes
        /// </summary>
        private GameObject CreateConnectionLine(TalentNodeBehavior fromNode, TalentNodeBehavior toNode, string lineName)
        {
            var lineObj = new GameObject($"Connection_{lineName}");
            lineObj.transform.SetParent(parentTransform);

            // Add Image component for UI-based line rendering
            var lineImage = lineObj.AddComponent<Image>();
            lineImage.color = inactiveLineColor;
            lineImage.raycastTarget = false;

            // Set sprite if available
            if (lineSprite != null)
                lineImage.sprite = lineSprite;

            // Setup RectTransform
            var rectTransform = lineObj.GetComponent<RectTransform>();
            SetupLineTransform(rectTransform, fromNode.transform.position, toNode.transform.position);

            return lineObj;
        }

        /// <summary>
        /// Setup line transform for proper positioning and rotation
        /// </summary>
        private void SetupLineTransform(RectTransform lineRect, Vector3 startPos, Vector3 endPos)
        {
            // Calculate line properties
            Vector3 direction = endPos - startPos;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Position at midpoint
            Vector3 midpoint = (startPos + endPos) * 0.5f;
            lineRect.position = midpoint;

            // Set size and rotation
            lineRect.sizeDelta = new Vector2(distance, lineWidth);
            lineRect.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // Set proper anchors and pivot
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>
        /// Update connection states based on learned talents
        /// </summary>
        public void UpdateConnectionStates()
        {
            if (TalentManager.Instance == null) return;

            UpdateNormalColumnStates();
            UpdateSpecialColumnStates();
        }

        /// <summary>
        /// Update normal column connection states
        /// </summary>
        private void UpdateNormalColumnStates()
        {
            foreach (var statType in statTypeLines.Keys)
            {
                var lines = statTypeLines[statType];
                
                for (int i = 0; i < lines.Count; i++)
                {
                    bool shouldBeActive = IsNormalConnectionActive(statType, i);
                    SetLineActive(lines[i], shouldBeActive);
                }
            }
        }

        /// <summary>
        /// Update special column connection states
        /// </summary>
        private void UpdateSpecialColumnStates()
        {
            for (int i = 0; i < specialColumnLines.Count; i++)
            {
                bool shouldBeActive = IsSpecialConnectionActive(i);
                SetLineActive(specialColumnLines[i], shouldBeActive);
            }
        }

        /// <summary>
        /// Check if normal connection should be active
        /// </summary>
        private bool IsNormalConnectionActive(string statType, int connectionIndex)
        {
            if (!TalentDatabase.Instance.IsDataLoaded) return false;

            // Get normal nodes for this stat type
            var allTalents = TalentDatabase.Instance.GetAllTalents();
            var statNodes = allTalents
                .Where(t => t.NodeType == Talents.Data.TalentNodeType.Normal && GetStatTypeFromTalent(t) == statType)
                .OrderBy(t => t.RequiredPlayerLevel)
                .ToList();

            // Check if the node AFTER this connection is learned
            if (connectionIndex + 1 < statNodes.Count)
            {
                var targetNode = statNodes[connectionIndex + 1];
                return TalentManager.Instance.IsTalentLearned(targetNode.ID);
            }

            return false;
        }

        /// <summary>
        /// Check if special connection should be active
        /// </summary>
        private bool IsSpecialConnectionActive(int connectionIndex)
        {
            if (!TalentDatabase.Instance.IsDataLoaded) return false;

            // Get special nodes
            var allTalents = TalentDatabase.Instance.GetAllTalents();
            var specialNodes = allTalents
                .Where(t => t.NodeType == Talents.Data.TalentNodeType.Special)
                .OrderBy(t => t.RequiredPlayerLevel)
                .ToList();

            // Check if the node AFTER this connection is learned
            if (connectionIndex + 1 < specialNodes.Count)
            {
                var targetNode = specialNodes[connectionIndex + 1];
                return TalentManager.Instance.IsTalentLearned(targetNode.ID);
            }

            return false;
        }

        /// <summary>
        /// Get stat type from talent model
        /// </summary>
        private string GetStatTypeFromTalent(Talents.Data.TalentModel talent)
        {
            if (talent.Name.Contains("Attack")) return "ATK";
            if (talent.Name.Contains("Defense")) return "DEF";
            if (talent.Name.Contains("Speed")) return "SPEED";
            if (talent.Name.Contains("Healing")) return "HEAL";
            return "UNKNOWN";
        }

        /// <summary>
        /// Set line active/inactive state
        /// </summary>
        private void SetLineActive(GameObject line, bool active)
        {
            if (line == null) return;

            var lineImage = line.GetComponent<Image>();
            if (lineImage != null)
            {
                lineImage.color = active ? activeLineColor : inactiveLineColor;
            }
        }

        /// <summary>
        /// Animate connection activation
        /// </summary>
        public void AnimateConnectionActivation(TalentNodeBehavior activatedNode)
        {
            if (activatedNode == null) return;

            // Find and animate the connection TO this node
            AnimateConnectionToNode(activatedNode);
        }

        /// <summary>
        /// Animate connection to specific node
        /// </summary>
        private void AnimateConnectionToNode(TalentNodeBehavior targetNode)
        {
            GameObject connectionLine = null;

            if (targetNode.IsNormalNode())
            {
                string statType = targetNode.GetStatType();
                if (statTypeLines.ContainsKey(statType))
                {
                    var lines = statTypeLines[statType];
                    // Find the line that ends at this node
                    // This would require more complex tracking, simplified for now
                    if (lines.Count > 0)
                        connectionLine = lines.LastOrDefault();
                }
            }
            else if (targetNode.IsSpecialNode())
            {
                if (specialColumnLines.Count > 0)
                    connectionLine = specialColumnLines.LastOrDefault();
            }

            if (connectionLine != null)
            {
                AnimateLineActivation(connectionLine);
            }
        }

        /// <summary>
        /// Animate line activation with visual effect
        /// </summary>
        private void AnimateLineActivation(GameObject line)
        {
            var lineImage = line.GetComponent<Image>();
            if (lineImage == null) return;

            // Simple color transition animation
            var originalColor = lineImage.color;
            lineImage.color = Color.white;

            // Use simple lerp animation (can be enhanced with DOTween)
            StartCoroutine(AnimateColorTransition(lineImage, Color.white, activeLineColor, 0.5f));
        }

        /// <summary>
        /// Simple color transition coroutine
        /// </summary>
        private System.Collections.IEnumerator AnimateColorTransition(Image image, Color fromColor, Color toColor, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                image.color = Color.Lerp(fromColor, toColor, t);
                yield return null;
            }
            
            image.color = toColor;
        }

        /// <summary>
        /// Clear all connection lines
        /// </summary>
        public void ClearAllLines()
        {
            ClearLineList(normalColumnLines);
            ClearLineList(specialColumnLines);
            
            foreach (var statLines in statTypeLines.Values)
            {
                ClearLineList(statLines);
            }
        }

        /// <summary>
        /// Clear a list of line objects
        /// </summary>
        private void ClearLineList(List<GameObject> lines)
        {
            foreach (var line in lines)
            {
                if (line != null)
                    DestroyImmediate(line);
            }
            lines.Clear();
        }

        /// <summary>
        /// Set connections visible/invisible
        /// </summary>
        public void SetConnectionsVisible(bool visible)
        {
            showConnections = visible;
            
            SetLineListVisible(normalColumnLines, visible);
            SetLineListVisible(specialColumnLines, visible);
        }

        /// <summary>
        /// Set visibility for list of lines
        /// </summary>
        private void SetLineListVisible(List<GameObject> lines, bool visible)
        {
            foreach (var line in lines)
            {
                if (line != null)
                    line.SetActive(visible);
            }
        }

        /// <summary>
        /// Update line colors
        /// </summary>
        public void UpdateLineColors(Color newActiveColor, Color newInactiveColor)
        {
            activeLineColor = newActiveColor;
            inactiveLineColor = newInactiveColor;
            
            UpdateConnectionStates(); // Refresh all line colors
        }

        /// <summary>
        /// Get connection statistics for debugging
        /// </summary>
        public ConnectionStats GetConnectionStats()
        {
            int totalLines = normalColumnLines.Count + specialColumnLines.Count;
            int activeLines = 0;

            // Count active lines
            foreach (var line in normalColumnLines)
            {
                if (line != null && IsLineActive(line))
                    activeLines++;
            }
            
            foreach (var line in specialColumnLines)
            {
                if (line != null && IsLineActive(line))
                    activeLines++;
            }

            return new ConnectionStats
            {
                TotalLines = totalLines,
                ActiveLines = activeLines,
                NormalColumnLines = normalColumnLines.Count,
                SpecialColumnLines = specialColumnLines.Count,
                ConnectionsVisible = showConnections
            };
        }

        /// <summary>
        /// Check if line is currently active (visual state)
        /// </summary>
        private bool IsLineActive(GameObject line)
        {
            var lineImage = line.GetComponent<Image>();
            return lineImage != null && lineImage.color == activeLineColor;
        }

        /// <summary>
        /// Force refresh all connections
        /// </summary>
        public void RefreshConnections()
        {
            UpdateConnectionStates();
        }

        /// <summary>
        /// Cleanup on destroy
        /// </summary>
        private void OnDestroy()
        {
            ClearAllLines();
        }

        /// <summary>
        /// Connection statistics structure
        /// </summary>
        [System.Serializable]
        public struct ConnectionStats
        {
            public int TotalLines;
            public int ActiveLines;
            public int NormalColumnLines;
            public int SpecialColumnLines;
            public bool ConnectionsVisible;
        }
    }
}