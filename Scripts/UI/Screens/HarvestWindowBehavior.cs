using OctoberStudio.Audio;
using OctoberStudio.Easing;
using OctoberStudio.Input;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    public class HarvestWindowBehavior : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] Button backButton;
        [SerializeField] Button collectAllButton;
        
        [Header("Harvest Display")]
        [SerializeField] ScalingLabelBehavior offlineCoinsLabel;
        [SerializeField] ScalingLabelBehavior offlineTimeLabel;
        [SerializeField] Image progressFillImage;
        
        [Header("Harvest Settings")]
        [SerializeField] float baseCoinsPerSecond = 10f;
        [SerializeField] float maxOfflineHours = 24f;
        
        [Header("Animation")]
        [SerializeField] ParticleSystem collectParticles;

        public event UnityAction OnBackPressed;

        private float offlineTime;
        private int offlineCoins;
        private bool hasOfflineRewards;

        private void Awake()
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
            // collectAllButton.onClick.AddListener(OnCollectAllClicked);
        }

        public void Init(UnityAction onBackButtonClicked)
        {
            OnBackPressed = onBackButtonClicked;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            CalculateOfflineRewards();
            UpdateHarvestDisplay();
            EasingManager.DoNextFrame(() => {
                if (hasOfflineRewards)
                {
                    EventSystem.current.SetSelectedGameObject(collectAllButton.gameObject);
                }
                else
                {
                    EventSystem.current.SetSelectedGameObject(backButton.gameObject);
                }
                GameController.InputManager.InputAsset.UI.Back.performed += OnBackInputClicked;
            });
            
            GameController.InputManager.onInputChanged += OnInputChanged;
        }

        public void Close()
        {
            gameObject.SetActive(false);
            
            GameController.InputManager.InputAsset.UI.Back.performed -= OnBackInputClicked;
            GameController.InputManager.onInputChanged -= OnInputChanged;
        }

        private void CalculateOfflineRewards()
        {
            var lastSaveTime = System.DateTime.Now.AddHours(-2); // Simulate 2 hours offline
            var currentTime = System.DateTime.Now;
            
            offlineTime = (float)(currentTime - lastSaveTime).TotalSeconds;
            
            // Cap offline time to maximum
            float maxOfflineSeconds = maxOfflineHours * 3600f;
            offlineTime = Mathf.Min(offlineTime, maxOfflineSeconds);
            
            // Calculate offline coins
            offlineCoins = Mathf.FloorToInt(offlineTime * baseCoinsPerSecond);
            
            hasOfflineRewards = offlineCoins > 0;
        }

        private void UpdateHarvestDisplay()
        {
            if (offlineTimeLabel != null)
            {
                float hours = offlineTime / 3600f;
                string timeText = hours >= 1f ? 
                    $"{hours:F1}h" : 
                    $"{offlineTime / 60f:F0}m";
                // offlineTimeLabel.label.text = timeText;
            }
            
            if (offlineCoinsLabel != null)
            {
                offlineCoinsLabel.SetAmount(offlineCoins);
            }
            
            if (progressFillImage != null)
            {
                float progress = offlineTime / (maxOfflineHours * 3600f);
                progressFillImage.fillAmount = progress;
            }
            
            // Enable/disable collect button
            // collectAllButton.interactable = hasOfflineRewards;
        }

        private void OnCollectAllClicked()
        {
            if (!hasOfflineRewards) return;
            
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            
            // Add coins to player
            GameController.TempGold?.Deposit(offlineCoins);
            
            // Play collect animation
            StartCoroutine(CollectAnimation());
        }

        private IEnumerator CollectAnimation()
        {
            // Play particle effect
            if (collectParticles != null)
            {
                collectParticles.Play();
            }
            
            // Animate coins counting up
            int startCoins = 0;
            float duration = 1.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                
                int currentDisplayCoins = Mathf.FloorToInt(Mathf.Lerp(startCoins, offlineCoins, progress));
                offlineCoinsLabel.SetAmount(currentDisplayCoins);
                
                yield return null;
            }
            
            // Reset rewards
            offlineCoins = 0;
            hasOfflineRewards = false;
            
            // Update display
            UpdateHarvestDisplay();
            
            // Focus back button
            EventSystem.current.SetSelectedGameObject(backButton.gameObject);
        }

        private void OnBackButtonClicked()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            OnBackPressed?.Invoke();
        }

        private void OnBackInputClicked(InputAction.CallbackContext context)
        {
            OnBackButtonClicked();
        }

        private void OnInputChanged(InputType prevInput, InputType inputType)
        {
            if (prevInput == InputType.UIJoystick)
            {
                EasingManager.DoNextFrame(() => {
                    if (hasOfflineRewards && collectAllButton.interactable)
                    {
                        EventSystem.current.SetSelectedGameObject(collectAllButton.gameObject);
                    }
                    else
                    {
                        EventSystem.current.SetSelectedGameObject(backButton.gameObject);
                    }
                });
            }
        }

        private void OnDestroy()
        {
            if (GameController.InputManager != null)
            {
                GameController.InputManager.InputAsset.UI.Back.performed -= OnBackInputClicked;
                GameController.InputManager.onInputChanged -= OnInputChanged;
            }
        }

        // Public methods for external access
        public bool HasOfflineRewards()
        {
            return hasOfflineRewards;
        }

        public int GetOfflineCoins()
        {
            return offlineCoins;
        }

        public float GetOfflineTime()
        {
            return offlineTime;
        }
    }
}