using OctoberStudio.Audio;
using OctoberStudio.Easing;
using OctoberStudio.Input;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    public class ShopWindowBehavior : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] Button backButton;
        [SerializeField] ScrollRect scrollView;
        [SerializeField] Transform itemsContainer;
        
        [Header("Shop Items")]
        [SerializeField] GameObject shopItemPrefab;
        
        [Header("Currency Display")]
        [SerializeField] ScalingLabelBehavior coinsLabel;
        [SerializeField] ScalingLabelBehavior gemsLabel;

        public event UnityAction OnBackPressed;

        private void Awake()
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        public void Init(UnityAction onBackButtonClicked)
        {
            OnBackPressed = onBackButtonClicked;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            
            // Update currency display
            UpdateCurrencyDisplay();
            
            // Setup input handling
            EasingManager.DoNextFrame(() => {
                EventSystem.current.SetSelectedGameObject(backButton.gameObject);
                GameController.InputManager.InputAsset.UI.Back.performed += OnBackInputClicked;
            });
            
            GameController.InputManager.onInputChanged += OnInputChanged;
        }

        public void Close()
        {
            gameObject.SetActive(false);
            
            // Cleanup input handling
            GameController.InputManager.InputAsset.UI.Back.performed -= OnBackInputClicked;
            GameController.InputManager.onInputChanged -= OnInputChanged;
        }

        private void UpdateCurrencyDisplay()
        {
            if (coinsLabel != null)
            {
                int currentCoins = GameController.TempGold?.Amount ?? 0;
                coinsLabel.SetAmount(currentCoins);
            }
            
            if (gemsLabel != null)
            {
                // Placeholder for gems system
                gemsLabel.SetAmount(0);
            }
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
                    EventSystem.current.SetSelectedGameObject(backButton.gameObject);
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

        // Placeholder methods for future shop functionality
        public void RefreshShopItems()
        {
            // TODO: Implement shop items loading
            Debug.Log("Shop items refresh - not implemented yet");
        }

        public void PurchaseItem(int itemId)
        {
            // TODO: Implement purchase logic
            Debug.Log($"Purchase item {itemId} - not implemented yet");
        }
    }
}