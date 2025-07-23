using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Talents.Helper
{
    public class TalentConnectionRenderer : MonoBehaviour
    {
        [Header("Line Settings")]
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Color lineColor = Color.green;
        [SerializeField] private float lineWidth = 3f;
        [SerializeField] private bool useUILines = true; // True = UI Image, False = LineRenderer

        [Header("Animation")]
        [SerializeField] private bool animateLines = true;
        [SerializeField] private float animationSpeed = 2f;
        [SerializeField] private Gradient animationGradient;

        private List<LineRenderer> worldLines = new List<LineRenderer>();
        private List<Image> uiLines = new List<Image>();
        private RectTransform parentTransform;

        private void Awake()
        {
            parentTransform = GetComponent<RectTransform>();
        
            // Create default gradient if not assigned
            if (animationGradient == null)
            {
                animationGradient = new Gradient();
                var colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(Color.green, 0f),
                    new GradientColorKey(Color.cyan, 0.5f),
                    new GradientColorKey(Color.green, 1f)
                };
                var alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                };
                animationGradient.SetKeys(colorKeys, alphaKeys);
            }
        }

        /// <summary>
        /// Draw vertical connection line cho một column
        /// </summary>
        public void DrawVerticalConnection(Vector2 startPos, Vector2 endPos, string connectionName = "Connection")
        {
            if (useUILines)
            {
                DrawUILine(startPos, endPos, connectionName);
            }
            else
            {
                DrawWorldLine(startPos, endPos, connectionName);
            }
        }

        /// <summary>
        /// Draw connection line giữa multiple points
        /// </summary>
        public void DrawMultiPointConnection(Vector2[] points, string connectionName = "MultiConnection")
        {
            if (points.Length < 2) return;

            for (int i = 0; i < points.Length - 1; i++)
            {
                DrawVerticalConnection(points[i], points[i + 1], $"{connectionName}_{i}");
            }
        }

        /// <summary>
        /// Draw UI-based line (recommended for UI)
        /// </summary>
        private void DrawUILine(Vector2 startPos, Vector2 endPos, string lineName)
        {
            // Create line GameObject
            GameObject lineObj = new GameObject($"UILine_{lineName}");
            lineObj.transform.SetParent(transform);

            // Add Image component for the line
            var lineImage = lineObj.AddComponent<Image>();
            lineImage.color = lineColor;
            lineImage.raycastTarget = false;

            // Setup RectTransform
            var lineRect = lineObj.GetComponent<RectTransform>();
        
            // Calculate line properties
            Vector2 direction = endPos - startPos;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Position at midpoint
            Vector2 midpoint = (startPos + endPos) * 0.5f;
            lineRect.anchoredPosition = midpoint;

            // Set size and rotation
            lineRect.sizeDelta = new Vector2(distance, lineWidth);
            lineRect.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // Set anchor and pivot for proper rotation
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);

            // Add to tracking list
            uiLines.Add(lineImage);

            // Add animation if enabled
            if (animateLines)
            {
                StartCoroutine(AnimateUILine(lineImage));
            }

            Debug.Log($"[TalentConnectionRenderer] Drew UI line {lineName} from {startPos} to {endPos} (distance: {distance:F1})");
        }

        /// <summary>
        /// Draw WorldSpace line using LineRenderer
        /// </summary>
        private void DrawWorldLine(Vector2 startPos, Vector2 endPos, string lineName)
        {
            // Create line GameObject
            GameObject lineObj = new GameObject($"WorldLine_{lineName}");
            lineObj.transform.SetParent(transform);

            // Add LineRenderer
            var lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.material = lineMaterial ?? CreateDefaultLineMaterial();
            lineRenderer.startColor = lineColor;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = false;
            lineRenderer.sortingOrder = -1; // Behind UI elements

            // Set line points
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);

            // Add to tracking list
            worldLines.Add(lineRenderer);

            // Add animation if enabled
            if (animateLines)
            {
                StartCoroutine(AnimateWorldLine(lineRenderer));
            }

            Debug.Log($"[TalentConnectionRenderer] Drew World line {lineName} from {startPos} to {endPos}");
        }

        /// <summary>
        /// Create default line material
        /// </summary>
        private Material CreateDefaultLineMaterial()
        {
            var material = new Material(Shader.Find("Sprites/Default"));
            material.color = lineColor;
            return material;
        }

        /// <summary>
        /// Animate UI line color
        /// </summary>
        private System.Collections.IEnumerator AnimateUILine(Image lineImage)
        {
            float time = 0f;
            Color originalColor = lineImage.color;

            while (lineImage != null && gameObject.activeInHierarchy)
            {
                time += Time.deltaTime * animationSpeed;
                float gradientTime = (Mathf.Sin(time) + 1f) * 0.5f; // 0 to 1 oscillation

                Color animatedColor = animationGradient.Evaluate(gradientTime);
                animatedColor.a = originalColor.a; // Preserve alpha
                lineImage.color = animatedColor;

                yield return null;
            }
        }

        /// <summary>
        /// Animate WorldSpace line color
        /// </summary>
        private System.Collections.IEnumerator AnimateWorldLine(LineRenderer lineRenderer)
        {
            float time = 0f;
            Color originalColor = lineRenderer.startColor;

            while (lineRenderer != null && gameObject.activeInHierarchy)
            {
                time += Time.deltaTime * animationSpeed;
                float gradientTime = (Mathf.Sin(time) + 1f) * 0.5f; // 0 to 1 oscillation

                Color animatedColor = animationGradient.Evaluate(gradientTime);
                animatedColor.a = originalColor.a; // Preserve alpha
                lineRenderer.startColor = animatedColor;

                yield return null;
            }
        }

        /// <summary>
        /// Clear all connection lines
        /// </summary>
        public void ClearAllLines()
        {
            // Clear UI lines
            foreach (var line in uiLines)
            {
                if (line != null)
                {
                    DestroyImmediate(line.gameObject);
                }
            }
            uiLines.Clear();

            // Clear world lines
            foreach (var line in worldLines)
            {
                if (line != null)
                {
                    DestroyImmediate(line.gameObject);
                }
            }
            worldLines.Clear();

            Debug.Log("[TalentConnectionRenderer] Cleared all connection lines");
        }

        /// <summary>
        /// Draw connections cho Base Stats column
        /// </summary>
        public void DrawBaseStatsConnections(Vector2[] nodePositions)
        {
            if (nodePositions.Length < 2) return;

            // Draw vertical line connecting all base stats
            DrawMultiPointConnection(nodePositions, "BaseStats");
        }

        /// <summary>
        /// Draw connections cho Special Skills column
        /// </summary>
        public void DrawSpecialSkillsConnections(Vector2[] nodePositions)
        {
            if (nodePositions.Length < 2) return;

            // Draw vertical line connecting all special skills
            DrawMultiPointConnection(nodePositions, "SpecialSkills");
        }

        /// <summary>
        /// Update line colors based on talent unlock status
        /// </summary>
        public void UpdateLineColors(bool[] unlockStates)
        {
            Color activeColor = Color.green;
            Color inactiveColor = Color.gray;

            // Update UI lines
            for (int i = 0; i < uiLines.Count && i < unlockStates.Length; i++)
            {
                if (uiLines[i] != null)
                {
                    uiLines[i].color = unlockStates[i] ? activeColor : inactiveColor;
                }
            }

            // Update world lines
            for (int i = 0; i < worldLines.Count && i < unlockStates.Length; i++)
            {
                if (worldLines[i] != null)
                {
                    worldLines[i].startColor = unlockStates[i] ? activeColor : inactiveColor;
                }
            }
        }

        /// <summary>
        /// Set line animation enabled/disabled
        /// </summary>
        public void SetAnimationEnabled(bool enabled)
        {
            animateLines = enabled;
        }

        /// <summary>
        /// Cleanup when destroyed
        /// </summary>
        private void OnDestroy()
        {
            ClearAllLines();
        }

        // Debug methods
        [ContextMenu("Test Draw Base Stats")]
        public void TestDrawBaseStats()
        {
            Vector2[] testPositions = new Vector2[]
            {
                new Vector2(-200, 300),   // ATK
                new Vector2(-200, 220),   // HP
                new Vector2(-200, 140),   // Armor
                new Vector2(-200, 60)     // Healing
            };

            DrawBaseStatsConnections(testPositions);
        }

        [ContextMenu("Test Draw Special Skills")]
        public void TestDrawSpecialSkills()
        {
            Vector2[] testPositions = new Vector2[]
            {
                new Vector2(200, 300),    // Lucky Dog
                new Vector2(200, 220),    // Rogue
                new Vector2(200, 140),    // Athlete
                new Vector2(200, 60)      // Berserker
            };

            DrawSpecialSkillsConnections(testPositions);
        }

        [ContextMenu("Clear All Lines")]
        public void ClearAllLinesDebug()
        {
            ClearAllLines();
        }
    }
}