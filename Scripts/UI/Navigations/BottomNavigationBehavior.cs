using OctoberStudio.Audio;
using OctoberStudio.Input;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    public class BottomNavigationBehavior : MonoBehaviour
    {
        [Header("Navigation Buttons")]
        [SerializeField] Button shopButton;
        [SerializeField] Button equipmentButton;
        [SerializeField] Button battleButton; // Lobby/Main button
        [SerializeField] Button talentsButton;
        [SerializeField] Button harvestButton;

        [Header("Button States")]
        [SerializeField] Color selectedColor = Color.white;
        [SerializeField] Color unselectedColor = Color.gray;
        
        [Header("Button Icons")]
        [SerializeField] Image shopIcon;
        [SerializeField] Image equipmentIcon;
        [SerializeField] Image battleIcon;
        [SerializeField] Image talentsIcon;
        [SerializeField] Image harvestIcon;

        public enum NavigationTab
        {
            Shop,
            Equipment, 
            Battle,
            Talents,
            Harvest
        }

        public event UnityAction<NavigationTab> OnTabSelected;

        private NavigationTab currentTab = NavigationTab.Battle;
        private Button[] allButtons;
        private Image[] allIcons;

        private void Awake()
        {
            // Setup button arrays for easy management
            allButtons = new Button[] { shopButton, equipmentButton, battleButton, talentsButton, harvestButton };
            allIcons = new Image[] { shopIcon, equipmentIcon, battleIcon, talentsIcon, harvestIcon };

            // Setup button listeners
            shopButton.onClick.AddListener(() => SelectTab(NavigationTab.Shop));
            equipmentButton.onClick.AddListener(() => SelectTab(NavigationTab.Equipment));
            battleButton.onClick.AddListener(() => SelectTab(NavigationTab.Battle));
            talentsButton.onClick.AddListener(() => SelectTab(NavigationTab.Talents));
            harvestButton.onClick.AddListener(() => SelectTab(NavigationTab.Harvest));

            // Setup navigation
            SetupButtonNavigation();
        }

        private void Start()
        {
            // Set default selected tab
            SelectTab(NavigationTab.Battle);
            
            // Setup input handling
            GameController.InputManager.onInputChanged += OnInputChanged;
        }

        public void SelectTab(NavigationTab tab)
        {
            if (currentTab == tab) return;

            // Play sound
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
            
            currentTab = tab;
            UpdateButtonStates();
            
            // Notify listeners
            OnTabSelected?.Invoke(tab);
        }

        private void UpdateButtonStates()
        {
            for (int i = 0; i < allButtons.Length; i++)
            {
                bool isSelected = (NavigationTab)i == currentTab;
                
                // Update button visual state
                var colors = allButtons[i].colors;
                colors.normalColor = isSelected ? selectedColor : unselectedColor;
                colors.selectedColor = isSelected ? selectedColor : unselectedColor;
                allButtons[i].colors = colors;
                
                // Update icon color
                if (allIcons[i] != null)
                {
                    allIcons[i].color = isSelected ? selectedColor : unselectedColor;
                }
            }
        }

        private void SetupButtonNavigation()
        {
            // Setup horizontal navigation between buttons
            for (int i = 0; i < allButtons.Length; i++)
            {
                var navigation = new Navigation();
                navigation.mode = Navigation.Mode.Explicit;
                
                // Left navigation
                int leftIndex = (i - 1 + allButtons.Length) % allButtons.Length;
                navigation.selectOnLeft = allButtons[leftIndex];
                
                // Right navigation  
                int rightIndex = (i + 1) % allButtons.Length;
                navigation.selectOnRight = allButtons[rightIndex];
                
                allButtons[i].navigation = navigation;
            }
        }

        public void SetTabEnabled(NavigationTab tab, bool enabled)
        {
            int index = (int)tab;
            if (index >= 0 && index < allButtons.Length)
            {
                allButtons[index].interactable = enabled;
                
                if (allIcons[index] != null)
                {
                    var iconColor = allIcons[index].color;
                    iconColor.a = enabled ? 1f : 0.5f;
                    allIcons[index].color = iconColor;
                }
            }
        }

        public NavigationTab GetCurrentTab()
        {
            return currentTab;
        }

        public void FocusCurrentTab()
        {
            int index = (int)currentTab;
            if (index >= 0 && index < allButtons.Length)
            {
                EventSystem.current.SetSelectedGameObject(allButtons[index].gameObject);
            }
        }

        private void OnInputChanged(InputType prevInput, InputType inputType)
        {
            if (prevInput == InputType.UIJoystick)
            {
                FocusCurrentTab();
            }
        }

        private void OnDestroy()
        {
            if (GameController.InputManager != null)
            {
                GameController.InputManager.onInputChanged -= OnInputChanged;
            }
        }
    }
}