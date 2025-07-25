using Common.Scripts.Equipment.UI;
using OctoberStudio.Audio;
using OctoberStudio.Easing;
using OctoberStudio.Upgrades.UI;
using Talents.UI;
using UnityEngine;

namespace OctoberStudio.UI
{
    public class MainMenuScreenBehavior : MonoBehaviour
    {
        [Header("Bottom Navigation")]
        [SerializeField] BottomNavigationBehavior bottomNavigation;

        [Header("Windows")]
        [SerializeField] LobbyWindowBehavior lobbyWindow;
        [SerializeField] UpgradesWindowBehavior upgradesWindow;
        [SerializeField] SettingsWindowBehavior settingsWindow;
        [SerializeField] CharactersWindowBehavior charactersWindow;
        [SerializeField] EquipmentWindowBehavior equipmentWindow;
        [SerializeField] TalentWindowBehavior talentWindow;

        [Header("Shop & Harvest (Future Windows)")]
        [SerializeField] GameObject shopWindow;                     // Shop tab - placeholder
        [SerializeField] GameObject harvestWindow;                  // Harvest tab - placeholder

        private Canvas canvas;
        private BottomNavigationBehavior.NavigationTab currentActiveTab;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            
        }

        private void Start()
        {
            CloseAllWindows();
            InitializeWindows();
            bottomNavigation.OnTabSelected += OnTabSelected;
            OnTabSelected(BottomNavigationBehavior.NavigationTab.Battle);
        }

        private void InitializeWindows()
        {
            lobbyWindow.Init(ShowUpgrades, ShowSettings, ShowCharacters, ShowEquipments,ShowTalents);
            upgradesWindow.Init(ReturnToLobby);
            settingsWindow.Init(ReturnToLobby);
            charactersWindow.Init(ReturnToLobby);
            equipmentWindow.Init(ReturnToLobby);
            talentWindow.Init(ReturnToLobby);
        }

        private void OnTabSelected(BottomNavigationBehavior.NavigationTab tab)
        {
            if (currentActiveTab == tab) return;
            CloseCurrentWindow();
            switch (tab)
            {
                case BottomNavigationBehavior.NavigationTab.Shop:
                    ShowShop();
                    break;

                case BottomNavigationBehavior.NavigationTab.Equipment:
                    ShowEquipments();
                    break;

                case BottomNavigationBehavior.NavigationTab.Battle:
                    ShowLobby();
                    break;

                case BottomNavigationBehavior.NavigationTab.Talents:
                    // ShowUpgrades();
                    ShowTalents();
                    break;

                case BottomNavigationBehavior.NavigationTab.Harvest:
                    ShowHarvest();
                    break;
            }

            currentActiveTab = tab;
        }

        private void CloseCurrentWindow()
        {
            // Close all windows
            lobbyWindow.Close();
            upgradesWindow.Close();
            settingsWindow.Close();
            charactersWindow.Close();
            equipmentWindow.Close();
            talentWindow.Close();
            
            // Close placeholder windows
            if (shopWindow != null) shopWindow.SetActive(false);
            if (harvestWindow != null) harvestWindow.SetActive(false);
        }

        private void CloseAllWindows()
        {
            equipmentWindow.Close();
            CloseCurrentWindow();
        }

        // ===== TAB WINDOW METHODS =====
        
        private void ShowLobby()
        {
            lobbyWindow.Open();
        }

        private void ShowShop()
        {
            // Placeholder for shop window
            if (shopWindow != null)
            {
                shopWindow.SetActive(true);
            }
            else
            {
                Debug.Log("Shop window not implemented yet");
            }
        }
        private void ShowTalents()
        {
            talentWindow.Open();
        }

        private void ShowEquipments()
        {
            equipmentWindow.Open();
        }

        private void ShowUpgrades()
        {
            upgradesWindow.Open();
        }

        private void ShowHarvest()
        {
            // Placeholder for harvest window
            if (harvestWindow != null)
            {
                harvestWindow.SetActive(true);
            }
            else
            {
                Debug.Log("Harvest window not implemented yet");
            }
        }

        // ===== LOBBY SUB-WINDOW METHODS =====
        // These are called from lobby window buttons

        private void ShowSettings()
        {
            lobbyWindow.Close();
            settingsWindow.Open();
        }

        private void ShowCharacters()
        {
            lobbyWindow.Close();
            charactersWindow.Open();
        }

        // ===== RETURN METHODS =====

        private void ReturnToLobby()
        {
            CloseCurrentWindow();
            
            bottomNavigation.SelectTab(BottomNavigationBehavior.NavigationTab.Battle);
            ShowLobby();
        }

        private void HideSettings()
        {
            settingsWindow.Close();
            ShowLobby();
        }

        private void HideCharacters()
        {
            charactersWindow.Close();
            ShowLobby();
        }


        public void SetTabEnabled(BottomNavigationBehavior.NavigationTab tab, bool enabled)
        {
            bottomNavigation.SetTabEnabled(tab, enabled);
        }

        public BottomNavigationBehavior.NavigationTab GetCurrentTab()
        {
            return currentActiveTab;
        }
        
        private void OnDestroy()
        {
            if (bottomNavigation != null)
            {
                bottomNavigation.OnTabSelected -= OnTabSelected;
            }
            
            charactersWindow.Clear();
            upgradesWindow.Clear();
            talentWindow.Clear();
        }
    }
}