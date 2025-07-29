using Common.Scripts.Equipment.UI;
using OctoberStudio.Audio;
using OctoberStudio.Easing;
using OctoberStudio.Equipment;
using OctoberStudio.Input;
using OctoberStudio.Save;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    public class HarvestWindowBehavior : MonoBehaviour
    {
        [SerializeField] StagesDatabase stagesDatabase;
        private StageSave save;

        [Header("UI Elements")]
        [SerializeField] Button quickHarvestButton;
        [SerializeField] Button harvestButton;
        [SerializeField] GameObject rewardPopup;
        [SerializeField] TMP_Text countdownText;
        //[SerializeField] Text energyText;
        [SerializeField] TMP_Text quickHarvestLimitText;

    

       [Header("Top Panel")]
        [SerializeField] TMP_Text stageNumberLabel;
        [SerializeField] Image progressFillImage;
        
        [Header("Harvest Settings")]
        [SerializeField] float baseCoinsPerSecond = 10f;
        [SerializeField] float maxOfflineHours = 24f;
        //[SerializeField] int quickHarvestEnergyCost = 15;
        [SerializeField] int quickHarvestLimit = 3;

        //[Header("Harvest Display")]
        //[SerializeField] ScalingLabelBehavior offlineCoinsLabel;
        //[SerializeField] ScalingLabelBehavior offlineTimeLabel;
      

        //[Header("Animation")]
        //[SerializeField] ParticleSystem collectParticles;
        
        //[Header("Inventory")]
        //[SerializeField] private GameObject inventoryItemPrefab;
        //[SerializeField] private RectTransform inventoryContent;
        //[SerializeField] private ScrollRect inventoryScrollRect;

        //public event UnityAction OnBackPressed;
        private DateTime lastHarvestTime;
        private float remainingSeconds;
        private bool canHarvest;
        private int remainingQuickHarvests;
        private void Start()
        {
            save = GameController.SaveManager.GetSave<StageSave>("Stage");

            save.onSelectedStageChanged += InitStage;

         
        }

        public void Init()
        {
            quickHarvestButton.onClick.AddListener(QuickHarvest);
            harvestButton.onClick.AddListener(NormalHarvest);
            rewardPopup.SetActive(false);

            lastHarvestTime = DateTime.Now;
            remainingSeconds = maxOfflineHours * 3600f;
            remainingQuickHarvests = quickHarvestLimit;
            canHarvest = false;

            UpdateHarvestDisplay();
            UpdateEnergyDisplay();

        }
        private void Update()
        {
            if (!canHarvest)
            {
                remainingSeconds -= Time.deltaTime;
                if (remainingSeconds <= 0)
                {
                    remainingSeconds = 0;
                    canHarvest = true;
                    remainingQuickHarvests = quickHarvestLimit;
                    UpdateEnergyDisplay();
                }
                UpdateHarvestDisplay();
            }

            quickHarvestButton.interactable = remainingQuickHarvests > 0;
            harvestButton.interactable = canHarvest;
        }

        public void InitStage(int stageId)
        {
            
            var stage = stagesDatabase.GetStage(stageId);

           
            stageNumberLabel.text = $"Stage {stageId + 1}";
           
        }
        private void UpdateHarvestDisplay()
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(remainingSeconds);
            countdownText.text = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";

            float progress = 1f - (remainingSeconds / (maxOfflineHours * 3600f));
            progressFillImage.fillAmount = progress;

            //int offlineCoins = Mathf.FloorToInt((maxOfflineHours * 3600f - remainingSeconds) * baseCoinsPerSecond);
            //offlineCoinsLabel.SetAmount(offlineCoins);
        }

        private void UpdateEnergyDisplay()
        {
            //energyText.text = $"Energy: {currentEnergy}";
            quickHarvestLimitText.text = $"Limit: {remainingQuickHarvests}";
        }

        private void QuickHarvest()
        {
            if (remainingQuickHarvests > 0)
            {
                remainingQuickHarvests--;
                ShowRewardPopup();
                UpdateEnergyDisplay();
            }
        }
        private void NormalHarvest()
        {
            if (canHarvest)
            {
                ShowRewardPopup();
            }
        }

        public void Open()
        {
            gameObject.SetActive(true);
            //EasingManager.DoNextFrame(() => RefreshInventory());
        }
        private void ShowRewardPopup()
        {
            rewardPopup.SetActive(true);

            lastHarvestTime = DateTime.Now;
            remainingSeconds = maxOfflineHours * 3600f;
            canHarvest = false;

            UpdateHarvestDisplay();
        }

        //public void Open()
        //{
        //    gameObject.SetActive(true);
        //    CalculateOfflineRewards();
        //    UpdateHarvestDisplay();
        //    EasingManager.DoNextFrame(() => {
        //        if (hasOfflineRewards)
        //        {
        //            EventSystem.current.SetSelectedGameObject(collectAllButton.gameObject);
        //        }
        //        else
        //        {
        //            EventSystem.current.SetSelectedGameObject(backButton.gameObject);
        //        }
        //        GameController.InputManager.InputAsset.UI.Back.performed += OnBackInputClicked;
        //    });

        //    GameController.InputManager.onInputChanged += OnInputChanged;
        //}

        public void Close()
        {
            gameObject.SetActive(false);
            
            //GameController.InputManager.InputAsset.UI.Back.performed -= OnBackInputClicked;
            //GameController.InputManager.onInputChanged -= OnInputChanged;
        }

        //private void CalculateOfflineRewards()
        //{
        //    var lastSaveTime = System.DateTime.Now.AddHours(-2); // Simulate 2 hours offline
        //    var currentTime = System.DateTime.Now;
            
        //    offlineTime = (float)(currentTime - lastSaveTime).TotalSeconds;
            
        //    // Cap offline time to maximum
        //    float maxOfflineSeconds = maxOfflineHours * 3600f;
        //    offlineTime = Mathf.Min(offlineTime, maxOfflineSeconds);
            
        //    // Calculate offline coins
        //    offlineCoins = Mathf.FloorToInt(offlineTime * baseCoinsPerSecond);
            
        //    hasOfflineRewards = offlineCoins > 0;
        //}

        //private void UpdateHarvestDisplay()
        //{
        //    if (offlineTimeLabel != null)
        //    {
        //        float hours = offlineTime / 3600f;
        //        string timeText = hours >= 1f ? 
        //            $"{hours:F1}h" : 
        //            $"{offlineTime / 60f:F0}m";
        //        // offlineTimeLabel.label.text = timeText;
        //    }
            
        //    if (offlineCoinsLabel != null)
        //    {
        //        offlineCoinsLabel.SetAmount(offlineCoins);
        //    }
            
        //    if (progressFillImage != null)
        //    {
        //        float progress = offlineTime / (maxOfflineHours * 3600f);
        //        progressFillImage.fillAmount = progress;
        //    }
            
        //    // Enable/disable collect button
        //    // collectAllButton.interactable = hasOfflineRewards;
        //}

        //private void OnCollectAllClicked()
        //{
        //    if (!hasOfflineRewards) return;

        //    GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

        //    // Add coins to player
        //    GameController.TempGold?.Deposit(offlineCoins);

        //    // Play collect animation
        //    StartCoroutine(CollectAnimation());
        //}

        //private IEnumerator CollectAnimation()
        //{
        //    // Play particle effect
        //    if (collectParticles != null)
        //    {
        //        collectParticles.Play();
        //    }

        //    // Animate coins counting up
        //    int startCoins = 0;
        //    float duration = 1.5f;
        //    float elapsed = 0f;

        //    while (elapsed < duration)
        //    {
        //        elapsed += Time.unscaledDeltaTime;
        //        float progress = elapsed / duration;

        //        int currentDisplayCoins = Mathf.FloorToInt(Mathf.Lerp(startCoins, offlineCoins, progress));
        //        offlineCoinsLabel.SetAmount(currentDisplayCoins);

        //        yield return null;
        //    }

        //    // Reset rewards
        //    offlineCoins = 0;
        //    hasOfflineRewards = false;

        //    // Update display
        //    UpdateHarvestDisplay();

        //    // Focus back button
        //   //EventSystem.current.SetSelectedGameObject(backButton.gameObject);
        //}

        
    }


    //private void OnInputChanged(InputType prevInput, InputType inputType)
    //{
    //    if (prevInput == InputType.UIJoystick)
    //    {
    //        EasingManager.DoNextFrame(() =>
    //        {
    //            if (hasOfflineRewards && collectAllButton.interactable)
    //            {
    //                EventSystem.current.SetSelectedGameObject(collectAllButton.gameObject);
    //            }
    //            else
    //            {
    //                EventSystem.current.SetSelectedGameObject(backButton.gameObject);
    //            }
    //        });
    //    }
    //}

    //private void OnDestroy()
    //    {
    //        if (GameController.InputManager != null)
    //        {
    //            GameController.InputManager.InputAsset.UI.Back.performed -= OnBackInputClicked;
    //            GameController.InputManager.onInputChanged -= OnInputChanged;
    //        }
    //    }

        // Public methods for external access
        //public bool HasOfflineRewards()
        //{
        //    return hasOfflineRewards;
        //}

        //public int GetOfflineCoins()
        //{
        //    return offlineCoins;
        //}

        //public float GetOfflineTime()
        //{
        //    return offlineTime;
        //}
    //}
}