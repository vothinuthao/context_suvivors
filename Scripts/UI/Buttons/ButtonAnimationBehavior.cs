using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OctoberStudio.UI
{
    [RequireComponent(typeof(Button))]
    public class ButtonAnimationBehavior : MonoBehaviour
    {
        [Header("Scale Animation Settings")]
        [SerializeField] private Vector3 normalScale = Vector3.one;
        [SerializeField] private Vector3 hoverScale = new Vector3(1.05f, 1.05f, 1.05f);
        [SerializeField] private Vector3 pressedScale = new Vector3(0.95f, 0.95f, 0.95f);
        [SerializeField] private Vector3 selectedScale = new Vector3(1.1f, 1.1f, 1.1f);

        [Header("Animation Timing")]
        [SerializeField] private float hoverDuration = 0.15f;
        [SerializeField] private float pressDuration = 0.1f;
        [SerializeField] private float normalDuration = 0.2f;

        [Header("Pulse Animation (When Selected)")]
        [SerializeField] private bool enablePulse = true;
        [SerializeField] private float pulseDuration = 1.5f;
        [SerializeField] private Vector3 pulseScale = new Vector3(1.15f, 1.15f, 1.15f);

        [Header("Animation Settings")]
        [SerializeField] private Ease scaleEase = Ease.OutBack;
        [SerializeField] private bool animateOnAwake = true;
        [SerializeField] private bool resetScaleOnDisable = true;

        private Button button;
        private RectTransform rectTransform;
        private Vector3 originalScale;
        private Tweener currentTween;
        private Tweener pulseTween;
        private bool isSelected = false;
        private bool isPressed = false;

        private void Awake()
        {
            button = GetComponent<Button>();
            rectTransform = GetComponent<RectTransform>();
            originalScale = rectTransform.localScale;
            normalScale = originalScale;

            SetupButtonEvents();
        }

        private void Start()
        {
            if (animateOnAwake)
            {
                // Animate in from small scale
                rectTransform.localScale = Vector3.zero;
                AnimateToScale(normalScale, normalDuration, Ease.OutBack);
            }
        }

        private void SetupButtonEvents()
        {
            // Remove existing listeners to avoid duplicates
            button.onClick.RemoveListener(OnButtonClick);
            
            // Add animation listeners
            button.onClick.AddListener(OnButtonClick);
        }

        #region Public Animation Methods

        public void OnButtonHover()
        {
            if (!button.interactable || isPressed) return;
            
            StopPulse();
            AnimateToScale(hoverScale, hoverDuration);
        }

        public void OnButtonUnhover()
        {
            if (!button.interactable || isPressed) return;
            
            Vector3 targetScale = isSelected ? selectedScale : normalScale;
            AnimateToScale(targetScale, normalDuration);
            
            if (isSelected && enablePulse)
            {
                StartPulse();
            }
        }

        public void OnButtonPress()
        {
            if (!button.interactable) return;
            
            isPressed = true;
            StopPulse();
            AnimateToScale(pressedScale, pressDuration);
        }

        public void OnButtonRelease()
        {
            if (!button.interactable) return;
            
            isPressed = false;
            Vector3 targetScale = isSelected ? selectedScale : normalScale;
            AnimateToScale(targetScale, normalDuration);
            
            if (isSelected && enablePulse)
            {
                StartPulse();
            }
        }

        public void OnButtonSelect()
        {
            isSelected = true;
            
            if (!isPressed)
            {
                AnimateToScale(selectedScale, normalDuration);
                
                if (enablePulse)
                {
                    StartPulse();
                }
            }
        }

        public void OnButtonDeselect()
        {
            isSelected = false;
            StopPulse();
            
            if (!isPressed)
            {
                AnimateToScale(normalScale, normalDuration);
            }
        }

        public void OnButtonClick()
        {
            // Quick punch animation for feedback
            rectTransform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 5, 0.5f);
        }

        #endregion

        #region Animation Helpers

        private void AnimateToScale(Vector3 targetScale, float duration, Ease ease = Ease.Unset)
        {
            // Kill current animation
            currentTween?.Kill();
            
            // Start new animation
            Ease animationEase = ease == Ease.Unset ? scaleEase : ease;
            currentTween = rectTransform.DOScale(targetScale, duration)
                .SetEase(animationEase)
                .SetUpdate(true); // Ignore timescale
        }

        private void StartPulse()
        {
            if (!enablePulse || !isSelected) return;
            
            StopPulse();
            
            pulseTween = rectTransform.DOScale(pulseScale, pulseDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void StopPulse()
        {
            pulseTween?.Kill();
            pulseTween = null;
        }

        #endregion

        #region Unity Events Integration

        // Method to easily connect with Unity Events in Inspector
        public void PlayHoverAnimation() => OnButtonHover();
        public void PlayUnhoverAnimation() => OnButtonUnhover();
        public void PlayPressAnimation() => OnButtonPress();
        public void PlayReleaseAnimation() => OnButtonRelease();
        public void PlaySelectAnimation() => OnButtonSelect();
        public void PlayDeselectAnimation() => OnButtonDeselect();

        #endregion

        #region Utility Methods

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
            
            if (interactable)
            {
                AnimateToScale(normalScale, normalDuration);
            }
            else
            {
                StopPulse();
                AnimateToScale(normalScale * 0.9f, normalDuration);
            }
        }

        public void ResetToNormal()
        {
            isSelected = false;
            isPressed = false;
            StopPulse();
            AnimateToScale(normalScale, normalDuration);
        }

        public void SetSelected(bool selected)
        {
            if (selected)
            {
                OnButtonSelect();
            }
            else
            {
                OnButtonDeselect();
            }
        }

        public void PlayBounceAnimation()
        {
            rectTransform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 8, 0.7f)
                .SetUpdate(true);
        }

        public void PlayShakeAnimation()
        {
            rectTransform.DOShakeScale(0.3f, 0.1f, 10, 90f)
                .SetUpdate(true);
        }

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            if (rectTransform != null)
            {
                rectTransform.localScale = normalScale;
            }
        }

        private void OnDisable()
        {
            if (resetScaleOnDisable && rectTransform != null)
            {
                // Kill all tweens
                currentTween?.Kill();
                StopPulse();
                
                // Reset scale immediately
                rectTransform.localScale = originalScale;
            }
        }

        private void OnDestroy()
        {
            // Cleanup
            currentTween?.Kill();
            StopPulse();
            
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClick);
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Test Hover")]
        private void TestHover()
        {
            OnButtonHover();
        }

        [ContextMenu("Test Press")]
        private void TestPress()
        {
            OnButtonPress();
        }

        [ContextMenu("Test Select")]
        private void TestSelect()
        {
            OnButtonSelect();
        }

        [ContextMenu("Test Bounce")]
        private void TestBounce()
        {
            PlayBounceAnimation();
        }

        #endregion
    }
}