using Common.Scripts.Equipment.UI;
using OctoberStudio.Audio;
using OctoberStudio.Easing;
using OctoberStudio.Upgrades.UI;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.UI
{
    public class MainMenuScreenBehavior : MonoBehaviour
    {
        private Canvas canvas;

        [SerializeField] LobbyWindowBehavior lobbyWindow;
        [SerializeField] UpgradesWindowBehavior upgradesWindow;
        [SerializeField] SettingsWindowBehavior settingsWindow;
        [SerializeField] CharactersWindowBehavior charactersWindow;
        [SerializeField] EquipmentWindowBehavior equipmentsWindow;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            equipmentsWindow.Close();
        }

        private void Start()
        {
            lobbyWindow.Init(ShowUpgrades, ShowSettings, ShowCharacters,ShowEquips);
            upgradesWindow.Init(HideUpgrades);
            settingsWindow.Init(HideSettings);
            charactersWindow.Init(HideCharacters);
            equipmentsWindow.Init(HideEquipments);
        }

        private void ShowUpgrades()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            lobbyWindow.Close();
            upgradesWindow.Open();
        }

        private void HideUpgrades()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            upgradesWindow.Close();
            lobbyWindow.Open();
        }

        private void ShowCharacters()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            lobbyWindow.Close();
            charactersWindow.Open();
        }
        private void ShowEquips()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            lobbyWindow.Close();
            equipmentsWindow.Open();
        }

        private void HideCharacters()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            charactersWindow.Close();
            lobbyWindow.Open();
        }
        private void HideEquipments()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            equipmentsWindow.Close();
            lobbyWindow.Open();
        }

        private void ShowSettings()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            lobbyWindow.Close();
            settingsWindow.Open();
        }

        private void HideSettings()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            settingsWindow.Close();
            lobbyWindow.Open();
        }

        private void OnDestroy()
        {
            charactersWindow.Clear();
            upgradesWindow.Clear();
        }
    }
}