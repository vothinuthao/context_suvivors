using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Talents.Helper
{
    /// <summary>
    /// Simple connection renderer for mobile Evolution Chart - just vertical lines
    /// </summary>
    public class TalentConnectionRenderer : MonoBehaviour
    {
        [Header("Line Settings")]
        [SerializeField] private Color lineColor = new Color(0.5f, 0.8f, 1f, 0.6f);
        [SerializeField] private float lineWidth = 4f;
        [SerializeField] private bool showConnections = true;

        [Header("Mobile Optimization")]
        [SerializeField] private bool useSimpleLines = true; // True for better mobile performance
        
        private List<GameObject> connectionLines = new List<GameObject>();

        
        
        /// <summary>
        /// Draw simple vertical connections for a column
        /// </summary>
        public void DrawColumnConnections(Vector2[] positions, string columnName)
        {
            if (!showConnections || positions.Length < 2) return;

            // Sort positions from bottom to top
            var sortedPositions = new List<Vector2>(positions);
            sortedPositions.Sort((a, b) => a.y.CompareTo(b.y));

            // Draw lines between consecutive nodes
            for (int i = 0; i < sortedPositions.Count - 1; i++)
            {
                Vector2 start = sortedPositions[i];
                Vector2 end = sortedPositions[i + 1];
                
                DrawSingleLine(start, end, $"{columnName}_Line_{i}");
            }
        }

        /// <summary>
        /// Draw base stats connections (left column)
        /// </summary>
        public void DrawBaseStatsConnections(Vector2[] nodePositions)
        {
            DrawColumnConnections(nodePositions, "BaseStats");
        }

        /// <summary>
        /// Draw special skills connections (right column)
        /// </summary>
        public void DrawSpecialSkillsConnections(Vector2[] nodePositions)
        {
            DrawColumnConnections(nodePositions, "SpecialSkills");
        }

        /// <summary>
        /// Draw a single line between two points
        /// </summary>
        private void DrawSingleLine(Vector2 start, Vector2 end, string lineName)
        {
            if (useSimpleLines)
            {
                DrawUILine(start, end, lineName);
            }
            else
            {
                DrawLineRenderer(start, end, lineName);
            }
        }

        /// <summary>
        /// Draw UI-based line (recommended for mobile)
        /// </summary>
        private void DrawUILine(Vector2 start, Vector2 end, string lineName)
        {
            // Create line GameObject
            GameObject lineObj = new GameObject($"Line_{lineName}");
            lineObj.transform.SetParent(transform);

            // Add Image component
            var lineImage = lineObj.AddComponent<Image>();
            lineImage.color = lineColor;
            lineImage.raycastTarget = false;

            // Setup RectTransform
            var rectTransform = lineObj.GetComponent<RectTransform>();
            
            // Calculate line properties
            Vector2 direction = end - start;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Position at midpoint
            Vector2 midpoint = (start + end) * 0.5f;
            rectTransform.anchoredPosition = midpoint;

            // Set size and rotation
            rectTransform.sizeDelta = new Vector2(distance, lineWidth);
            rectTransform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // Set anchors for proper positioning
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // Add to tracking list
            connectionLines.Add(lineObj);
        }

        /// <summary>
        /// Draw LineRenderer-based line (fallback)
        /// </summary>
        private void DrawLineRenderer(Vector2 start, Vector2 end, string lineName)
        {
            // Create line GameObject
            GameObject lineObj = new GameObject($"LineRenderer_{lineName}");
            lineObj.transform.SetParent(transform);

            // Add LineRenderer
            var lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.material = CreateDefaultLineMaterial();
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = false;
            lineRenderer.sortingOrder = -1; // Behind nodes

            // Set line points
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);

            // Add to tracking list
            connectionLines.Add(lineObj);
        }

        /// <summary>
        /// Create default material for LineRenderer
        /// </summary>
        private Material CreateDefaultLineMaterial()
        {
            var material = new Material(Shader.Find("Sprites/Default"));
            material.color = lineColor;
            return material;
        }

        /// <summary>
        /// Update line colors based on unlock states
        /// </summary>
        public void UpdateLineColors(bool[] unlockStates)
        {
            Color activeColor = new Color(0.3f, 1f, 0.3f, 0.8f); // Green
            Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.4f); // Gray

            for (int i = 0; i < connectionLines.Count && i < unlockStates.Length; i++)
            {
                if (connectionLines[i] == null) continue;

                Color targetColor = unlockStates[i] ? activeColor : inactiveColor;

                // Update UI Image
                var image = connectionLines[i].GetComponent<Image>();
                if (image != null)
                {
                    image.color = targetColor;
                }

                // Update LineRenderer
                var lineRenderer = connectionLines[i].GetComponent<LineRenderer>();
            }
        }

        /// <summary>
        /// Clear all connection lines
        /// </summary>
        public void ClearAllLines()
        {
            foreach (var line in connectionLines)
            {
                if (line != null)
                {
                    DestroyImmediate(line);
                }
            }
            connectionLines.Clear();
        }

        /// <summary>
        /// Toggle connection visibility
        /// </summary>
        public void SetConnectionsVisible(bool visible)
        {
            showConnections = visible;
            
            foreach (var line in connectionLines)
            {
                if (line != null)
                {
                    line.SetActive(visible);
                }
            }
        }

        /// <summary>
        /// Set line color
        /// </summary>
        public void SetLineColor(Color color)
        {
            lineColor = color;
            
            foreach (var line in connectionLines)
            {
                if (line == null) continue;

                var image = line.GetComponent<Image>();
                if (image != null)
                {
                    image.color = color;
                }
            }
        }

        /// <summary>
        /// Get connection statistics
        /// </summary>
        public int GetConnectionCount()
        {
            return connectionLines.Count;
        }

        /// <summary>
        /// Cleanup when destroyed
        /// </summary>
        private void OnDestroy()
        {
            ClearAllLines();
        }

        // Debug methods
        [ContextMenu("Test Base Stats Connections")]
        public void TestBaseStatsConnections()
        {
            Vector2[] testPositions = new Vector2[]
            {
                new Vector2(-200, 0),     // ATK I
                new Vector2(-200, 400),   // HP I
                new Vector2(-200, 800),   // Armor I
                new Vector2(-200, 1200)   // Healing I
            };

            DrawBaseStatsConnections(testPositions);
        }

        [ContextMenu("Test Special Skills Connections")]
        public void TestSpecialSkillsConnections()
        {
            Vector2[] testPositions = new Vector2[]
            {
                new Vector2(200, 0),      // Lucky Dog
                new Vector2(200, 450),    // Rogue
                new Vector2(200, 900),    // Athlete
                new Vector2(200, 1350)    // Berserker
            };

            DrawSpecialSkillsConnections(testPositions);
        }

        [ContextMenu("Clear All Lines")]
        public void ClearAllLinesDebug()
        {
            ClearAllLines();
        }

        [ContextMenu("Toggle Connections")]
        public void ToggleConnectionsDebug()
        {
            SetConnectionsVisible(!showConnections);
        }
    }
}