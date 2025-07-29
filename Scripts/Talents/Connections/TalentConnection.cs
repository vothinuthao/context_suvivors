using UnityEngine;
using UnityEngine.UI;
using Talents.UI;
using Talents.Connections;
using DG.Tweening;

namespace Talents.Connections
{
    /// <summary>
    /// Individual connection line between two talent nodes
    /// </summary>
    public class TalentConnection : MonoBehaviour
    {
        [Header("Line Rendering")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Image connectionImage; // Alternative UI-based rendering
        [SerializeField] private RectTransform connectionRect;

        [Header("Animation")]
        [SerializeField] private bool useAnimation = true;
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Effects")]
        [SerializeField] private ParticleSystem activationEffect;
        [SerializeField] private AudioClip activationSound;

        // Connection data
        public int FromTalentId { get; private set; }
        public int ToTalentId { get; private set; }
        public TalentConnectionSystem.ConnectionState CurrentState { get; private set; }

        // Node references
        private TalentNodeBehavior fromNode;
        private TalentNodeBehavior toNode;

        // Visual state
        private Color targetColor;
        private Color currentColor;
        private bool isHighlighted;
        private float highlightIntensity = 1.5f;

        // Animation state
        private bool isAnimating;
        private float animationTimer;
        private Color animationStartColor;
        private Color animationEndColor;

        // Cached components
        private Canvas parentCanvas;
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
            
            // Create connection rect if not assigned
            if (connectionRect == null && connectionImage != null)
            {
                connectionRect = connectionImage.GetComponent<RectTransform>();
            }
        }

        /// <summary>
        /// Initialize the connection
        /// </summary>
        public void Initialize(float lineWidth, Material material)
        {
            // Setup LineRenderer if available
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 2;
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;
                lineRenderer.useWorldSpace = false;
                lineRenderer.sortingOrder = -1; // Behind nodes
                
                if (material != null)
                {
                    lineRenderer.material = material;
                }
            }

            // Setup UI Image if available
            if (connectionImage != null)
            {
                connectionImage.raycastTarget = false;
            }

            // Initial state
            CurrentState = TalentConnectionSystem.ConnectionState.Locked;
            targetColor = Color.gray;
            currentColor = targetColor;
            
            UpdateVisualState();
        }

        /// <summary>
        /// Setup connection between two nodes
        /// </summary>
        public void SetupConnection(TalentNodeBehavior fromNode, TalentNodeBehavior toNode, int fromTalentId, int toTalentId)
        {
            this.fromNode = fromNode;
            this.toNode = toNode;
            this.FromTalentId = fromTalentId;
            this.ToTalentId = toTalentId;

            UpdateConnectionGeometry();
        }

        /// <summary>
        /// Update connection geometry based on node positions
        /// </summary>
        private void UpdateConnectionGeometry()
        {
            if (fromNode == null || toNode == null)
                return;

            var fromPos = fromNode.transform.localPosition;
            var toPos = toNode.transform.localPosition;

            // Update LineRenderer if available
            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, fromPos);
                lineRenderer.SetPosition(1, toPos);
            }

            // Update UI Image if available
            if (connectionImage != null && connectionRect != null)
            {
                UpdateUIConnection(fromPos, toPos);
            }
        }

        /// <summary>
        /// Update UI-based connection rendering
        /// </summary>
        private void UpdateUIConnection(Vector3 fromPos, Vector3 toPos)
        {
            var direction = toPos - fromPos;
            var distance = direction.magnitude;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Position at midpoint
            var midpoint = (fromPos + toPos) * 0.5f;
            connectionRect.localPosition = midpoint;

            // Set size and rotation
            connectionRect.sizeDelta = new Vector2(distance, connectionRect.sizeDelta.y);
            connectionRect.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        /// <summary>
        /// Set connection state and color
        /// </summary>
        public void SetState(TalentConnectionSystem.ConnectionState state, Color color, bool animate = true)
        {
            CurrentState = state;
            targetColor = color;

            if (animate && useAnimation)
            {
                StartColorAnimation(currentColor, targetColor);
            }
            else
            {
                currentColor = targetColor;
                UpdateVisualState();
            }
        }

        /// <summary>
        /// Set highlighted state
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            isHighlighted = highlighted;
            UpdateVisualState();
        }

        /// <summary>
        /// Start color animation
        /// </summary>
        private void StartColorAnimation(Color startColor, Color endColor)
        {
            if (isAnimating)
                return;

            isAnimating = true;
            animationTimer = 0f;
            animationStartColor = startColor;
            animationEndColor = endColor;
        }

        /// <summary>
        /// Update visual state
        /// </summary>
        private void UpdateVisualState()
        {
            Color displayColor = currentColor;
            
            // Apply highlight effect
            if (isHighlighted)
            {
                displayColor = Color.Lerp(displayColor, Color.white, 0.5f);
                displayColor *= highlightIntensity;
            }

            // Apply to LineRenderer
            if (lineRenderer != null)
            {
                lineRenderer.startColor = displayColor;
                lineRenderer.endColor = displayColor;
            }

            // Apply to UI Image
            if (connectionImage != null)
            {
                connectionImage.color = displayColor;
            }
        }

        /// <summary>
        /// Animate connection activation
        /// </summary>
        public void AnimateActivation()
        {
            // Play activation effect
            if (activationEffect != null)
            {
                activationEffect.Play();
            }

            // Play activation sound
            if (activationSound != null && parentCanvas != null)
            {
                AudioSource.PlayClipAtPoint(activationSound, parentCanvas.transform.position);
            }

            // Scale animation
            if (useAnimation)
            {
                // DOTween scale up and down
                transform.DOScale(Vector3.one * 1.2f, animationDuration * 0.5f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => {
                        transform.DOScale(Vector3.one, animationDuration * 0.5f)
                            .SetEase(Ease.InBack);
                    });
            }

            // Color pulse animation
            StartColorPulse();
        }

        /// <summary>
        /// Start color pulse animation
        /// </summary>
        private void StartColorPulse()
        {
            var brightColor = targetColor * 2f;
            brightColor.a = targetColor.a;

            // DOTween color pulse animation
            DOTween.To(() => currentColor, x => { currentColor = x; UpdateVisualState(); }, brightColor, animationDuration * 0.5f)
                .OnComplete(() => {
                    DOTween.To(() => currentColor, x => { currentColor = x; UpdateVisualState(); }, targetColor, animationDuration * 0.5f);
                });
        }

        /// <summary>
        /// Update animation
        /// </summary>
        private void Update()
        {
            if (isAnimating)
            {
                animationTimer += Time.deltaTime;
                float progress = animationTimer / animationDuration;

                if (progress >= 1f)
                {
                    progress = 1f;
                    isAnimating = false;
                }

                // Apply animation curve
                float curveValue = animationCurve.Evaluate(progress);
                currentColor = Color.Lerp(animationStartColor, animationEndColor, curveValue);
                UpdateVisualState();
            }

            // Update geometry if nodes have moved
            if (fromNode != null && toNode != null)
            {
                UpdateConnectionGeometry();
            }
        }

        /// <summary>
        /// Get connection length
        /// </summary>
        public float GetLength()
        {
            if (fromNode == null || toNode == null)
                return 0f;

            return Vector3.Distance(fromNode.transform.position, toNode.transform.position);
        }

        /// <summary>
        /// Check if connection is valid
        /// </summary>
        public bool IsValid()
        {
            return fromNode != null && toNode != null && FromTalentId > 0 && ToTalentId > 0;
        }

        /// <summary>
        /// Get connection direction
        /// </summary>
        public Vector3 GetDirection()
        {
            if (fromNode == null || toNode == null)
                return Vector3.zero;

            return (toNode.transform.position - fromNode.transform.position).normalized;
        }

        /// <summary>
        /// Get connection midpoint
        /// </summary>
        public Vector3 GetMidpoint()
        {
            if (fromNode == null || toNode == null)
                return Vector3.zero;

            return (fromNode.transform.position + toNode.transform.position) * 0.5f;
        }

        /// <summary>
        /// Show connection info for debugging
        /// </summary>
        public void ShowDebugInfo()
        {
            Debug.Log($"[TalentConnection] {FromTalentId} -> {ToTalentId} | State: {CurrentState} | Length: {GetLength():F1} | Color: {currentColor}");
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        public void Cleanup()
        {
            // Stop any running animations
            DOTween.Kill(gameObject);
            
            // Stop particle effects
            if (activationEffect != null)
            {
                activationEffect.Stop();
            }

            // Reset state
            fromNode = null;
            toNode = null;
            FromTalentId = 0;
            ToTalentId = 0;
            isAnimating = false;
            isHighlighted = false;
        }

        /// <summary>
        /// Handle component destruction
        /// </summary>
        private void OnDestroy()
        {
            Cleanup();
        }

        // Debug methods
        [ContextMenu("Show Debug Info")]
        public void ShowDebugInfoContext()
        {
            ShowDebugInfo();
        }

        [ContextMenu("Test Activation Animation")]
        public void TestActivationAnimation()
        {
            AnimateActivation();
        }

        [ContextMenu("Toggle Highlight")]
        public void ToggleHighlight()
        {
            SetHighlighted(!isHighlighted);
        }
    }
}