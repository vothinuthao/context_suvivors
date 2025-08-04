using OctoberStudio.Equipment;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace OctoberStudio.UI
{
    public class HarvestWindowBehavior : MonoBehaviour
    {
        [SerializeField] StagesDatabase stagesDatabase;
        private StageSave save;

        [Header("UI Elements")] [SerializeField]
        Button quickHarvestButton;
        [SerializeField] Button harvestButton;
        [SerializeField] GameObject rewardPopup;

        [SerializeField] TMP_Text countdownText;
        [SerializeField] TMP_Text quickHarvestLimitText;

        [Header("Reward per hour Display")] [SerializeField]
        private TMP_Text coinsPerHourLabel;

        [SerializeField] private TMP_Text expPerHourLabel;
        
        [Header("Top Panel")] [SerializeField] TMP_Text stageNumberLabel;
        [SerializeField] Image progressFillImage;

        [Header("Harvest Settings")] [SerializeField]
        float baseCoinsPerSecond = 10f;

        [SerializeField] float maxOfflineHours = 24f;

        [SerializeField] int quickHarvestEnergyCost = 15;
        [SerializeField] int quickHarvestLimit = 3;
        [SerializeField] private RectTransform rewardItemContainer;
        [SerializeField] private GameObject rewardItemPrefab;

        [SerializeField] Button closePopupButton;
        [SerializeField] private Button resetEnergyButton;
        [SerializeField] private Button resetHarvestLimitButton;
        //public event UnityAction OnBackPressed;
        private DateTime lastHarvestTime;
        private float remainingSeconds;
        private bool canHarvest;
        private int remainingQuickHarvests;
        private int currentStageLevel;

        [Header("Reward Icons")]
        [SerializeField] private Sprite goldIcon;
        [SerializeField] private Sprite expIcon;
        [SerializeField] private Sprite energyIcon;
// Nếu sau này cần gem:
        //SerializeField] private Sprite gemIcon;
        
        private void Start()
        {

        }

        public void Init()
        {
            LoadHarvestData();
            quickHarvestButton.onClick.AddListener(QuickHarvest);
            harvestButton.onClick.AddListener(NormalHarvest);
            rewardPopup.SetActive(false);
            closePopupButton.onClick.AddListener(() => rewardPopup.SetActive(false));
            lastHarvestTime = DateTime.Now;
            // remainingSeconds = maxOfflineHours * 3600f;
            resetEnergyButton.onClick.AddListener(ResetEnergy);
            resetHarvestLimitButton.onClick.AddListener(ResetQuickHarvestLimit);
            UpdateHarvestDisplay();
            UpdateEnergyDisplay();
            UpdateHighestUnlockedMapDisplay();
            var stageSave = GameController.SaveManager.GetSave<StageSave>("Stage");
            InitStage(stageSave.MaxReachedStageId);
        }
        private void ResetEnergy()
        {
            if (GameController.EnergyManager != null)
            {
                GameController.EnergyManager.ResetEnergy();
                Debug.Log("✅ Energy reset to max.");
            }
        }

        private void ResetQuickHarvestLimit()
        {
            remainingQuickHarvests = quickHarvestLimit;
            SaveHarvestData();
            GameController.SaveManager.Save();
            UpdateEnergyDisplay();
            Debug.Log("✅ Quick Harvest limit reset.");
        }
        public void InitStage(int stageId)
        {
            // Sửa lại thành world thay vì stage level
            StageSave stageSave = GameController.SaveManager.GetSave<StageSave>("Stage");
            int currentWorld = stageSave.MaxReachedStageId + 1;

            UpdateRewardsPerHourDisplay(currentWorld);
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
                    SaveHarvestData();
                    GameController.SaveManager.Save();
                    UpdateEnergyDisplay();
                }

                UpdateHarvestDisplay();
            }
            int energyCost = 15;
            bool hasEnoughEnergy = GameController.EnergyManager != null &&
                                   GameController.EnergyManager.Energy >= energyCost;

            quickHarvestButton.interactable = (remainingQuickHarvests > 0 && hasEnoughEnergy);
            harvestButton.interactable = canHarvest;
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
            // ✅ Cập nhật lượt Quick Harvest
            quickHarvestLimitText.text = $"Limit: {remainingQuickHarvests}/{quickHarvestLimit}";
        }

        private void QuickHarvest()
        {
            int quickHarvestEnergyCost = 15;

            if (remainingQuickHarvests > 0 &&
                GameController.EnergyManager.TrySpendEnergy(quickHarvestEnergyCost)) // ✅ thêm điều kiện này
            {
                remainingQuickHarvests--;
                SaveHarvestData();
                GameController.SaveManager.Save();
                UpdateEnergyDisplay();
                ShowRewardPopup();
                
            }
            else
            {
                Debug.Log("Not enough Energy to Quick Harvest!");
                // Optional: Hiện popup hoặc feedback UI ở đây
            }
        }


        private void NormalHarvest()
        {
            if (canHarvest)
            {
                canHarvest = false;
                remainingSeconds = maxOfflineHours * 3600f;
        
                SaveHarvestData();                         // Lưu ngay
                GameController.SaveManager.Save();
                UpdateEnergyDisplay();                     // Update UI
                UpdateHarvestDisplay();                    // Update thanh thời gian

                ShowRewardPopup();                         // Cuối cùng mới hiển thị reward
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

                // Tạo rewards
                int currentWorld = GameController.SaveManager.GetSave<StageSave>("Stage").MaxReachedStageId + 1;
                var rewards = GenerateDummyRewards(currentWorld);

                // ✅ Hiển thị popup
                DisplayRewardsOnPopup(rewards);

                // ✅ Áp dụng phần thưởng vào kho
                ApplyRewards(rewards);

                // Reset countdown
                lastHarvestTime = DateTime.Now;
                remainingSeconds = maxOfflineHours * 3600f;
                canHarvest = false;
                SaveHarvestData();
                GameController.SaveManager.Save();
                UpdateHarvestDisplay();
            }

        private void UpdateHighestUnlockedMapDisplay()
        {
            // Lấy dữ liệu từ StageSave
            StageSave stageSave = GameController.SaveManager.GetSave<StageSave>("Stage");
            int maxUnlockedStage = stageSave.MaxReachedStageId;

            // Cập nhật UI hiển thị World
            stageNumberLabel.text = $"World {maxUnlockedStage + 1}";
        }

        private int GetCoinsPerHour(int world)
        {
            // Giả sử Level 1 là 400 coins, mỗi level tăng thêm 50 coins
            int baseCoins = 100;
            int coinsIncrementPerWorld = 50;

            return baseCoins + coinsIncrementPerWorld * (world - 1);
        }

        private int GetExpPerHour(int world)
        {
            // Giả sử Level 1 là 100 exp, mỗi level tăng thêm 20 exp
            int baseExp = 100;
            int expIncrementPerWorld = 20;

            return baseExp + expIncrementPerWorld * (world - 1);
        }

        private void UpdateRewardsPerHourDisplay(int world)
        {
            int coinsPerHour = GetCoinsPerHour(world);
            int expPerHour = GetExpPerHour(world);

            coinsPerHourLabel.text = $"{coinsPerHour}/h";
            expPerHourLabel.text = $"{expPerHour}/h";
        }

        public void Close()
        {
            gameObject.SetActive(false);
            foreach (Transform child in rewardItemContainer)
                Destroy(child.gameObject);
        }

        private void DisplayRewardsOnPopup(List<HarvestReward> rewards)
        {
            // Clear rewards before showing new ones
            foreach (Transform child in rewardItemContainer)
                Destroy(child.gameObject);

            foreach (var reward in rewards)
            {
                var itemGO = Instantiate(rewardItemPrefab, rewardItemContainer);
                var itemUI = itemGO.GetComponent<RewardItemUI>();
                itemUI.Init(reward.icon, $"{reward.name} x{reward.amount}");
                
            }
        }
        private void LoadHarvestData()
        {
            var save = GameController.SaveManager.GetSave<HarvestSave>("Harvest");

            // Tính thời gian offline
            long currentTicks = DateTime.UtcNow.Ticks;
            float secondsPassed = (float)TimeSpan.FromTicks(currentTicks - save.LastHarvestTicks).TotalSeconds;

            remainingQuickHarvests = save.RemainingQuickHarvests;
            remainingSeconds = Mathf.Max(0f, save.RemainingSeconds - secondsPassed);
            canHarvest = remainingSeconds <= 0f;

            // Nếu harvest đã sẵn sàng thì reset lượt quick
            if (canHarvest)
                remainingQuickHarvests = quickHarvestLimit;
        }

        private void SaveHarvestData()
        {
            var save = GameController.SaveManager.GetSave<HarvestSave>("Harvest");
            save.RemainingQuickHarvests = remainingQuickHarvests;
            save.RemainingSeconds = remainingSeconds;
            save.CanHarvest = canHarvest;
            save.LastHarvestTicks = DateTime.UtcNow.Ticks;
        }



        private List<HarvestReward> GenerateDummyRewards(int world)
        {
            var rewards = new List<HarvestReward>();

            rewards.Add(new HarvestReward
            {
                rewardType = RewardType.Gold,
                icon = goldIcon,
                name = "Gold",
                amount = GetCoinsPerHour(world) * 24
            });

            rewards.Add(new HarvestReward
            {
                rewardType = RewardType.Exp,
                icon = expIcon,
                name = "EXP",
                amount = GetExpPerHour(world) * 24
            });

            rewards.Add(new HarvestReward
            {
                rewardType = RewardType.Energy,
                icon = energyIcon,
                name = "Energy",
                amount = UnityEngine.Random.Range(3, 6)
            });

// Gem – nếu cần trong tương lai
// rewards.Add(new HarvestReward
// {
//     icon = gemIcon,
//     name = "Gem",
//     amount = 5
// });

            // Equipment ngẫu nhiên (ví dụ 2 món)
            
            for (int i = 0; i < 2; i++)
            {
                if (EquipmentDatabase.Instance == null) return rewards;
                var equip = EquipmentDatabase.Instance.GetRandomEquipmentByRarity(EquipmentRarity.Rare);
                if (equip != null)
                {
                    rewards.Add(new HarvestReward
                    {
                        rewardType = RewardType.Equipment,
                        icon = equip.GetIcon(),
                        name = equip.GetDisplayName(),
                        amount = 1,
                        equipmentData = equip
                    });
                }
            }

            return rewards;
        }
        private void ApplyRewards(List<HarvestReward> rewards)
        {
            foreach (var reward in rewards)
            {
                if (reward == null || reward.amount <= 0) continue;

                switch (reward.rewardType)
                {
                    case RewardType.Gold:
                        GameController.CurrenciesManager?.Add("gold", reward.amount);
                        break;

                    case RewardType.Gem:
                        GameController.CurrenciesManager?.Add("gem", reward.amount);
                        break;

                    case RewardType.Energy:
                        GameController.EnergyManager?.AddEnergy(reward.amount);
                        break;

                    case RewardType.Exp:
                        // Nếu bạn có ExperienceManager thì gọi ở đây
                        // GameController.ExperienceManager?.AddExp(reward.amount);
                        break;

                    case RewardType.Equipment:
                        if (reward.equipmentData != null)
                        {
                            var equip = reward.equipmentData;
                            GameController.SaveManager
                                .GetSave<EquipmentSave>("Equipment")
                                .AddToInventory(equip.EquipmentType, equip.ID, 1);
                        }
                        break;
                }

            }
        }

        [System.Serializable]
        public class HarvestReward
        {
            public RewardType rewardType;
            public Sprite icon;
            public string name;
            public int amount;
            public EquipmentModel equipmentData;
        }
        public enum RewardType
        {
            Gold,
            Exp,
            Energy,
            Gem,
            Equipment
        }
    }
}