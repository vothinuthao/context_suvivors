using OctoberStudio.Equipment;
using OctoberStudio.Harvest;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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

        [SerializeField] TextMeshProUGUI countupText;
        [SerializeField] TextMeshProUGUI quickHarvestLimitText;

        [Header("Reward per hour Display")] [SerializeField]
        private TextMeshProUGUI coinsPerHourLabel;

        [SerializeField] private TextMeshProUGUI expPerHourLabel;
        
        [Header("Top Panel")] [SerializeField] TextMeshProUGUI stageNumberLabel;
        [SerializeField] Image progressFillImage;

        [Header("Harvest Settings")] [SerializeField]
        float baseCoinsPerSecond = 10f;

        [SerializeField] float maxOfflineHours = 24f;
        [SerializeField] float harvestCooldownMinutes = 3f; // 3 minutes for harvest cooldown

        [SerializeField] int quickHarvestEnergyCost = 15;
        [SerializeField] int quickHarvestLimit = 3;
        [SerializeField] private RectTransform rewardItemContainer;
        [SerializeField] private RectTransform rewardItemPreviewContainer;
        [SerializeField] private GameObject rewardItemPrefab;

        [SerializeField] Button closePopupButton;
        [SerializeField] private Button resetEnergyButton;
        [SerializeField] private Button resetHarvestLimitButton;

        [Header("Tooltip")]
        [SerializeField] private GameObject harvestTooltip;
        [SerializeField] private TextMeshProUGUI tooltipText;
        private DateTime lastHarvestTime;
        private float remainingSeconds;
        private bool canHarvest;
        private int remainingQuickHarvests;
        private int currentStageLevel;
        private List<HarvestRewardData> pendingRewards; // Rewards waiting to be collected
        private bool hasPendingRewards = false;
        private HarvestSave harvestSave;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        [Header("Reward Icons")]
        [SerializeField] private Sprite goldIcon;
        [SerializeField] private Sprite expIcon;
        [SerializeField] private Sprite energyIcon;
        [SerializeField] private Sprite gemIcon;
        [SerializeField] private Sprite characterPiecesIcon;

        [Header("New Reward System")]
        [SerializeField] private HarvestRewardConfig rewardConfig;
        [SerializeField] private bool useNewRewardSystem = true;
        
        private void Start()
        {

        }

        public void Init()
        {
            // LoadHarvestData() - replaced with LoadHarvestSaveData() call below
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

            // Initialize new reward system
            InitializeNewRewardSystem();

            // Initialize tooltip
            if (harvestTooltip != null)
                harvestTooltip.SetActive(false);

            // Initialize pending rewards
            pendingRewards = new List<HarvestRewardData>();

            // Load harvest save data and calculate offline time
            LoadHarvestSaveData();
        }

        private void InitializeNewRewardSystem()
        {
            if (useNewRewardSystem && rewardConfig != null)
            {
                Debug.Log("[HarvestWindow] New reward system initialized");
            }
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
            // Update display continuously
            UpdateHarvestDisplay();

            // Update button interactivity
            int energyCost = 15;
            bool hasEnoughEnergy = GameController.EnergyManager != null &&
                                   GameController.EnergyManager.Energy >= energyCost;

            quickHarvestButton.interactable = (remainingQuickHarvests > 0 && hasEnoughEnergy);
            harvestButton.interactable = true; // Always interactive to show tooltips

            // Update preview every few seconds to show current rewards
            UpdatePreviewPeriodically();
        }

        private float lastPreviewUpdate = 0f;
        private void UpdatePreviewPeriodically()
        {
            // Update preview every 5 seconds to keep it current with elapsed time
            if (Time.time - lastPreviewUpdate >= 5f)
            {
                lastPreviewUpdate = Time.time;
                ShowRewardPreview();
            }
        }


        private void UpdateHarvestDisplay()
        {
            // Show elapsed time since harvest timer started (count up from 00:00:00)
            float elapsedSeconds = GetElapsedHarvestTime();
            TimeSpan timeSpan = TimeSpan.FromSeconds(elapsedSeconds);
            countupText.text = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";

            // Debug để kiểm tra display
            if (Time.frameCount % 300 == 0)
            {
                Debug.Log($"[Display] Showing: {countupText.text} (elapsed: {elapsedSeconds:F1}s)");
            }

            // Progress based on 3-minute minimum for harvest button
            float minHarvestTime = harvestCooldownMinutes * 60f; // 3 minutes in seconds
            float progress = Mathf.Min(elapsedSeconds / minHarvestTime, 1f);
            progressFillImage.fillAmount = progress;

            // Change text color to indicate harvest availability
            if (elapsedSeconds >= minHarvestTime)
            {
                countupText.color = Color.green; // Ready to harvest (3+ minutes)
            }
            else
            {
                countupText.color = Color.yellow; // Still counting up to 3 minutes
            }
        }

        private void UpdateEnergyDisplay()
        {
            // ✅ Cập nhật lượt Quick Harvest
            quickHarvestLimitText.text = $"Limit: {remainingQuickHarvests}/{quickHarvestLimit}";
        }

        private void QuickHarvest()
        {
            if (remainingQuickHarvests > 0 &&
                GameController.EnergyManager.TrySpendEnergy(quickHarvestEnergyCost))
            {
                remainingQuickHarvests--;

                // Generate immediate rewards for Quick Harvest (small amount)
                int currentWorld = GameController.SaveManager.GetSave<StageSave>("Stage").MaxReachedStageId + 1;
                float quickHarvestTimeHours = 0.05f; // Small reward for quick harvest
                var quickRewards = GenerateRewardsForTime(currentWorld, quickHarvestTimeHours);

                // Show and apply quick harvest rewards immediately
                DisplayRewardsOnPopup(quickRewards);
                ApplyRewards(quickRewards);
                rewardPopup.SetActive(true);

                // Save changes (SaveHarvestData already calls Save())
                SaveHarvestData();
                UpdateEnergyDisplay();
                UpdateHarvestDisplay();

                Debug.Log("Quick Harvest rewards collected immediately!");
            }
            else
            {
                Debug.Log("Not enough Energy to Quick Harvest!");
                ShowEnergyTooltip();
            }
        }


        private void NormalHarvest()
        {
            var rewardsData = GetCurrentHarvestRewards();

            if (rewardsData.canHarvest)
            {
                // Show and apply harvest rewards
                DisplayRewardsOnPopup(rewardsData.rewards);
                ApplyRewards(rewardsData.rewards);
                rewardPopup.SetActive(true);

                // Clear preview container after collecting rewards
                if (rewardItemPreviewContainer != null)
                {
                    foreach (Transform child in rewardItemPreviewContainer)
                        Destroy(child.gameObject);
                }

                // Reset harvest timer ONLY when harvest is collected
                Debug.Log("[HarvestWindow] 🔄 RESETTING TIMER after harvest...");

                if (harvestSave != null)
                {
                    DateTime oldTime = new DateTime(harvestSave.LastHarvestTicks);
                    harvestSave.UpdateHarvestTime(); // Reset timer to current time
                    DateTime newTime = new DateTime(harvestSave.LastHarvestTicks);

                    Debug.Log($"[HarvestWindow] ✅ Timer Reset: {oldTime:HH:mm:ss} → {newTime:HH:mm:ss}");
                    Debug.Log($"[HarvestWindow] New elapsed should be 0: {GetElapsedHarvestTime():F1} seconds");
                }

                // Reset quick harvest limits
                remainingQuickHarvests = quickHarvestLimit;

                // Save all changes (AFTER timer reset)
                SaveHarvestData();

                // Update displays immediately to show reset timer
                UpdateEnergyDisplay();
                UpdateHarvestDisplay();

                if (enableDebugLogs)
                {
                    Debug.Log($"[HarvestWindow] Harvest collected! Rewards for: {rewardsData.elapsedHours:F2} hours");
                    Debug.Log($"  - Preview cleared and timer reset to 0");
                }
                else
                {
                    Debug.Log($"Harvest collected! Preview cleared. Rewards for {rewardsData.elapsedHours:F2} hours.");
                }
            }
            else
            {
                ShowHarvestNotReadyTooltip();
            }
        }

        /// <summary>
        /// Get the current harvest rewards and status - used by both NormalHarvest and ShowRewardPreview
        /// Only returns rewards when 3+ minutes have elapsed
        /// </summary>
        private (bool canHarvest, List<HarvestRewardData> rewards, float elapsedHours) GetCurrentHarvestRewards()
        {
            float elapsedTime = GetElapsedHarvestTime();
            bool canHarvest = elapsedTime >= harvestCooldownMinutes * 60f; // 3+ minutes

            int currentWorld = GameController.SaveManager.GetSave<StageSave>("Stage").MaxReachedStageId + 1;
            float elapsedHours = elapsedTime / 3600f; // Convert seconds to hours

            List<HarvestRewardData> rewards = new List<HarvestRewardData>();

            if (canHarvest)
            {
                // Only generate rewards when 3+ minutes have passed
                rewards = GenerateRewardsForTime(currentWorld, elapsedHours);
            }
            // If under 3 minutes, return empty rewards list

            return (canHarvest, rewards, elapsedHours);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            ShowRewardPreview();
        }

        /// <summary>
        /// Load harvest save data and initialize timer
        /// </summary>
        private void LoadHarvestSaveData()
        {
            Debug.Log("[HarvestWindow] 🔄 LOADING harvest save data...");
            harvestSave = GameController.SaveManager.GetSave<HarvestSave>("Harvest");
            if (harvestSave == null)
            {
                Debug.LogError("[HarvestWindow] Could not load HarvestSave data!");
                return;
            }

            Debug.Log($"[HarvestWindow] 📁 LOADED: LastHarvestTicks = {harvestSave.LastHarvestTicks}");
            DateTime loadTime = new DateTime(harvestSave.LastHarvestTicks);
            Debug.Log($"[HarvestWindow] 📁 Loaded harvest time: {loadTime:yyyy-MM-dd HH:mm:ss}");

            // Load quick harvest data
            remainingQuickHarvests = harvestSave.RemainingQuickHarvests;

            // Check for offline rewards if enough time has passed
            float elapsedTime = GetElapsedHarvestTime();
            Debug.Log($"[HarvestWindow] 📁 Calculated elapsed time: {elapsedTime:F1} seconds ({elapsedTime/60f:F1} minutes)");

            if (elapsedTime >= harvestCooldownMinutes * 60f) // 3+ minutes
            {
                Debug.Log($"[HarvestWindow] Offline rewards available! Elapsed time: {elapsedTime:F1} seconds ({elapsedTime/60f:F1} minutes)");
                // Player can harvest offline rewards when they click the harvest button
            }
            else
            {
                Debug.Log($"[HarvestWindow] Loaded harvest data. Elapsed time: {elapsedTime:F1} seconds ({elapsedTime/60f:F1} minutes) - not ready for harvest yet");
            }
        }

        /// <summary>
        /// Tính thời gian từ lần harvest cuối đến hiện tại
        /// </summary>
        private float GetElapsedHarvestTime()
        {
            if (harvestSave == null || harvestSave.LastHarvestTicks == 0)
            {
                return 0f;
            }

            DateTime lastHarvestTime = new DateTime(harvestSave.LastHarvestTicks);
            DateTime currentTime = DateTime.UtcNow;
            TimeSpan elapsed = currentTime - lastHarvestTime;
            float elapsedSeconds = (float)elapsed.TotalSeconds;

            // Debug để kiểm tra
            if (Time.frameCount % 300 == 0) // Log mỗi 5 giây
            {
                Debug.Log($"[Timer] Last harvest: {lastHarvestTime:HH:mm:ss}, Current: {currentTime:HH:mm:ss}, Elapsed: {elapsedSeconds:F1}s");
            }

            return Mathf.Max(0f, elapsedSeconds);
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

            // Clear both reward containers
            foreach (Transform child in rewardItemContainer)
                Destroy(child.gameObject);

            if (rewardItemPreviewContainer != null)
            {
                foreach (Transform child in rewardItemPreviewContainer)
                    Destroy(child.gameObject);
            }

            if (enableDebugLogs)
                Debug.Log("[HarvestWindow] Window closed (timer state preserved)");
        }

        private void DisplayRewardsOnPopup(List<HarvestRewardData> rewards)
        {
            // Clear rewards before showing new ones
            foreach (Transform child in rewardItemContainer)
                Destroy(child.gameObject);

            foreach (var reward in rewards)
            {
                var itemGO = Instantiate(rewardItemPrefab, rewardItemContainer);
                var itemUI = itemGO.GetComponent<RewardItemUI>();
                itemUI.Init(reward);
            }
        }
        // LoadHarvestData method removed - replaced with LoadHarvestSaveData

        private void SaveHarvestData()
        {
            if (harvestSave != null)
            {
                Debug.Log($"[HarvestWindow] 💾 SAVING: LastHarvestTicks = {harvestSave.LastHarvestTicks}");
                DateTime saveTime = new DateTime(harvestSave.LastHarvestTicks);
                Debug.Log($"[HarvestWindow] 💾 Saving harvest time: {saveTime:yyyy-MM-dd HH:mm:ss}");

                harvestSave.RemainingQuickHarvests = remainingQuickHarvests;
                harvestSave.RemainingSeconds = remainingSeconds;
                harvestSave.CanHarvest = canHarvest;

                GameController.SaveManager.Save();
                Debug.Log("[HarvestWindow] ✅ Data saved to disk!");
            }
        }

        /// <summary>
        /// Reset timer về 00:00:00 khi người chơi nhận harvest
        /// </summary>
        private void ResetHarvestTimer()
        {
            if (harvestSave != null)
            {
                DateTime oldTime = new DateTime(harvestSave.LastHarvestTicks);
                harvestSave.UpdateHarvestTime(); // Cập nhật thời gian harvest = hiện tại
                DateTime newTime = new DateTime(harvestSave.LastHarvestTicks);

                Debug.Log($"[HarvestWindow] ✅ Timer Reset: {oldTime:HH:mm:ss} → {newTime:HH:mm:ss}");
                Debug.Log($"[HarvestWindow] Timer should now show 00:00:00 and count up from there");
            }
        }



        private List<HarvestRewardData> GenerateDummyRewards(int world)
        {
            // Use new reward system if available
            if (useNewRewardSystem && rewardConfig != null)
            {
                return GenerateNewSystemRewards(world);
            }

            // Fallback to old system
            return GenerateOldSystemRewards(world);
        }

        private List<HarvestRewardData> GenerateNewSystemRewards(int world)
        {
            // Calculate hours offline
            float hoursOffline = (maxOfflineHours * 3600f - remainingSeconds) / 3600f;
            hoursOffline = Mathf.Max(hoursOffline, 0.1f); // Minimum 0.1 hour

            // Generate rewards using new config system
            var rewards = rewardConfig.GenerateRewards(world, hoursOffline);

            // Set icon and name for each reward
            foreach (var reward in rewards)
            {
                SetRewardUIData(reward);
            }

            Debug.Log($"[HarvestWindow] Generated {rewards.Count} rewards using new system for {hoursOffline:F1} hours offline");
            return rewards;
        }

        private void SetRewardUIData(HarvestRewardData reward)
        {
            switch (reward.rewardHarvestType)
            {
                case RewardHarvestType.Gold:
                    reward.icon = goldIcon;
                    reward.name = "Gold";
                    break;

                case RewardHarvestType.Exp:
                    reward.icon = expIcon;
                    reward.name = "EXP";
                    break;

                case RewardHarvestType.Gem:
                    reward.icon = gemIcon;
                    reward.name = "Gems";
                    break;

                case RewardHarvestType.CharacterPieces:
                    reward.icon = characterPiecesIcon;
                    reward.name = "Character Pieces";
                    break;

                case RewardHarvestType.Equipment:
                    if (EquipmentDatabase.Instance != null)
                    {
                        var equip = EquipmentDatabase.Instance.GetRandomEquipmentByRarity(reward.equipmentRarity);
                        if (equip != null)
                        {
                            reward.icon = equip.GetIcon();
                            reward.name = equip.GetDisplayName();
                            reward.equipmentData = equip;
                        }
                    }
                    break;

            }
        }

        private List<HarvestRewardData> GenerateOldSystemRewards(int world)
        {
            var rewards = new List<HarvestRewardData>();

            rewards.Add(new HarvestRewardData
            {
                rewardHarvestType = RewardHarvestType.Gold,
                icon = goldIcon,
                name = "Gold",
                amount = GetCoinsPerHour(world) * 24
            });

            rewards.Add(new HarvestRewardData
            {
                rewardHarvestType = RewardHarvestType.Exp,
                icon = expIcon,
                name = "EXP",
                amount = GetExpPerHour(world) * 24
            });

            // Equipment ngẫu nhiên (ví dụ 2 món)
            for (int i = 0; i < 2; i++)
            {
                if (EquipmentDatabase.Instance == null) return rewards;
                var equip = EquipmentDatabase.Instance.GetRandomEquipmentByRarity(EquipmentRarity.Rare);
                if (equip != null)
                {
                    rewards.Add(new HarvestRewardData
                    {
                        rewardHarvestType = RewardHarvestType.Equipment,
                        icon = equip.GetIcon(),
                        name = equip.GetDisplayName(),
                        amount = 1,
                        equipmentData = equip,
                        equipmentRarity = EquipmentRarity.Rare
                    });
                }
            }

            return rewards;
        }
        private void ApplyRewards(List<HarvestRewardData> rewards)
        {
            foreach (var reward in rewards)
            {
                if (reward == null || reward.amount <= 0) continue;

                switch (reward.rewardHarvestType)
                {
                    case RewardHarvestType.Gold:
                        GameController.CurrenciesManager?.Add("gold", reward.amount);
                        break;

                    case RewardHarvestType.Gem:
                        GameController.CurrenciesManager?.Add("gem", reward.amount);
                        break;

                    case RewardHarvestType.Exp:
                        // Nếu bạn có ExperienceManager thì gọi ở đây
                        // GameController.ExperienceManager?.AddExp(reward.amount);
                        break;

                    case RewardHarvestType.CharacterPieces:
                        var charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
                        if (charactersSave != null)
                        {
                            int selectedCharacterId = charactersSave.SelectedCharacterId;
                            charactersSave.AddCharacterPieces(selectedCharacterId, reward.amount);
                        }
                        break;

                    case RewardHarvestType.Equipment:
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

        /// <summary>
        /// Generate rewards for a specific time duration
        /// </summary>
        private List<HarvestRewardData> GenerateRewardsForTime(int world, float hoursOffline)
        {
            if (useNewRewardSystem && rewardConfig != null)
            {
                var rewards = rewardConfig.GenerateRewards(world, hoursOffline);

                // Set icon and name for each reward
                foreach (var reward in rewards)
                {
                    SetRewardUIData(reward);
                }

                return rewards;
            }

            // Fallback to old system
            return GenerateOldSystemRewards(world);
        }

        /// <summary>
        /// Display rewards in preview container
        /// </summary>
        private void DisplayRewardsInPreview(List<HarvestRewardData> rewards)
        {
            if (rewardItemPreviewContainer == null || rewardItemPrefab == null || rewards == null) return;

            // Clear existing preview items
            foreach (Transform child in rewardItemPreviewContainer)
                Destroy(child.gameObject);

            // Spawn each reward item in preview
            foreach (var reward in rewards)
            {
                var itemGO = Instantiate(rewardItemPrefab, rewardItemPreviewContainer);
                var itemUI = itemGO.GetComponent<RewardItemUI>();
                itemUI.Init(reward);
            }

            Debug.Log($"Displayed {rewards.Count} rewards in preview container");
        }

        /// <summary>
        /// Show reward preview by spawning items into rewardItemPreviewContainer
        /// Uses the exact same calculation as NormalHarvest to show synchronized rewards
        /// Shows no rewards when under 3 minutes
        /// </summary>
        public void ShowRewardPreview()
        {
            if (rewardItemPreviewContainer == null || rewardItemPrefab == null) return;

            // Clear existing preview items
            foreach (Transform child in rewardItemPreviewContainer)
                Destroy(child.gameObject);

            // Use the same method as NormalHarvest to ensure perfect synchronization
            var rewardsData = GetCurrentHarvestRewards();

            if (rewardsData.canHarvest && rewardsData.rewards.Count > 0)
            {
                // Show rewards when 3+ minutes have passed
                foreach (var reward in rewardsData.rewards)
                {
                    var itemGO = Instantiate(rewardItemPrefab, rewardItemPreviewContainer);
                    var itemUI = itemGO.GetComponent<RewardItemUI>();
                    itemUI.Init(reward);
                }

                Debug.Log($"[HarvestWindow] Preview showing {rewardsData.rewards.Count} rewards for {rewardsData.elapsedHours:F2} hours elapsed");
            }
            else
            {
                // Under 3 minutes - show no rewards
                Debug.Log($"[HarvestWindow] No rewards shown - need {harvestCooldownMinutes} minutes minimum (currently {GetElapsedHarvestTime()/60f:F1} minutes)");
            }
        }

        /// <summary>
        /// Show tooltip when harvest is not ready yet
        /// </summary>
        private void ShowHarvestNotReadyTooltip()
        {
            if (harvestTooltip == null) return;

            string tooltipMessage;
            float elapsedTime = GetElapsedHarvestTime();
            float minHarvestTime = harvestCooldownMinutes * 60f; // 3 minutes

            if (elapsedTime < minHarvestTime)
            {
                float remainingTime = minHarvestTime - elapsedTime;
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);

                tooltipMessage = $"Harvest not ready yet!\nTime remaining: {minutes:D2}:{seconds:D2}";
            }
            else
            {
                tooltipMessage = "Harvest is ready! Click to collect rewards based on elapsed time.";
            }

            if (tooltipText != null)
                tooltipText.text = tooltipMessage;

            ShowTooltip();
        }

        /// <summary>
        /// Show tooltip when not enough energy
        /// </summary>
        private void ShowEnergyTooltip()
        {
            if (harvestTooltip == null) return;

            string tooltipMessage = $"Not enough Energy!\nRequired: {quickHarvestEnergyCost} Energy for Quick Harvest.";

            if (tooltipText != null)
                tooltipText.text = tooltipMessage;

            ShowTooltip();
        }

        /// <summary>
        /// Show tooltip with auto-hide after 3 seconds
        /// </summary>
        private void ShowTooltip()
        {
            if (harvestTooltip == null) return;

            harvestTooltip.SetActive(true);

            // Auto-hide tooltip after 3 seconds
            CancelInvoke(nameof(HideTooltip));
            Invoke(nameof(HideTooltip), 3f);
        }

        /// <summary>
        /// Hide tooltip
        /// </summary>
        private void HideTooltip()
        {
            if (harvestTooltip != null)
                harvestTooltip.SetActive(false);
        }

        /// <summary>
        /// Debug method to simulate offline time for testing
        /// </summary>
        [ContextMenu("Debug: Simulate 10 Minutes Offline")]
        public void DebugSimulateOfflineTime()
        {
            if (harvestSave != null)
            {
                // Simulate harvest 10 minutes ago
                DateTime tenMinutesAgo = DateTime.UtcNow.AddMinutes(-10);
                harvestSave.LastHarvestTicks = tenMinutesAgo.Ticks;
                SaveHarvestData();
                GameController.SaveManager.Save();

                Debug.Log($"[HarvestWindow] Simulated harvest 10 minutes ago. Elapsed time: {GetElapsedHarvestTime():F1} seconds");
                UpdateHarvestDisplay();
            }
        }

        /// <summary>
        /// Debug method to simulate longer offline time for testing
        /// </summary>
        [ContextMenu("Debug: Simulate 2 Hours Offline")]
        public void DebugSimulateLongOfflineTime()
        {
            if (harvestSave != null)
            {
                // Simulate harvest 2 hours ago
                DateTime twoHoursAgo = DateTime.UtcNow.AddHours(-2);
                harvestSave.LastHarvestTicks = twoHoursAgo.Ticks;
                SaveHarvestData();
                GameController.SaveManager.Save();

                Debug.Log($"[HarvestWindow] Simulated harvest 2 hours ago. Elapsed time: {GetElapsedHarvestTime():F1} seconds");
                UpdateHarvestDisplay();
            }
        }

        /// <summary>
        /// Debug method to reset harvest timer for testing
        /// </summary>
        [ContextMenu("Debug: Reset Harvest Timer")]
        public void DebugResetHarvestTimer()
        {
            if (harvestSave != null)
            {
                ResetHarvestTimer();
                SaveHarvestData();

                Debug.Log($"[HarvestWindow] DEBUG: Manually reset harvest timer. Elapsed time: {GetElapsedHarvestTime():F1} seconds");
                UpdateHarvestDisplay();
                ShowRewardPreview(); // Update preview to match new timer
            }
        }

        /// <summary>
        /// Debug method to simulate under 3-minute scenario
        /// </summary>
        [ContextMenu("Debug: Simulate Under 3 Minutes")]
        public void DebugSimulateUnder3Minutes()
        {
            if (harvestSave != null)
            {
                // Simulate 1 minute ago (under 3-minute requirement)
                DateTime oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
                // harvestSave.HarvestStartTicks = oneMinuteAgo.Ticks;
                SaveHarvestData();
                GameController.SaveManager.Save();

                Debug.Log($"[HarvestWindow] Simulated 1 minute offline (under 3-minute minimum). Elapsed time: {GetElapsedHarvestTime():F1} seconds");
                UpdateHarvestDisplay();
                ShowRewardPreview();

                // Run verification
                DebugVerifyRewardSync();
            }
        }

        /// <summary>
        /// Debug method to verify save data persistence
        /// </summary>
        [ContextMenu("Debug: Verify Save Persistence")]
        public void DebugVerifySavePersistence()
        {
            if (harvestSave == null)
            {
                Debug.LogError("[HarvestWindow] HarvestSave is null!");
                return;
            }

            // DateTime startTime = new DateTime(harvestSave.HarvestStartTicks);
            DateTime lastTime = new DateTime(harvestSave.LastHarvestTicks);
            float elapsedTime = GetElapsedHarvestTime();


            // Force a save to test persistence
            SaveHarvestData();
            Debug.Log("Force saved data to disk");
        }

        /// <summary>
        /// Debug method to verify synchronization between preview and harvest rewards
        /// </summary>
        [ContextMenu("Debug: Verify Reward Synchronization")]
        public void DebugVerifyRewardSync()
        {
            var rewardsData = GetCurrentHarvestRewards();
            float elapsedMinutes = GetElapsedHarvestTime() / 60f;

            Debug.Log("=== REWARD SYNCHRONIZATION TEST ===");
            Debug.Log($"Elapsed time: {GetElapsedHarvestTime():F1} seconds ({elapsedMinutes:F1} minutes, {rewardsData.elapsedHours:F3} hours)");
            Debug.Log($"3-minute requirement: {(elapsedMinutes >= harvestCooldownMinutes ? "✅ MET" : "❌ NOT MET")}");
            Debug.Log($"Can harvest: {rewardsData.canHarvest}");
            Debug.Log($"Rewards count: {rewardsData.rewards.Count}");

            if (rewardsData.rewards.Count > 0)
            {
                foreach (var reward in rewardsData.rewards)
                {
                    Debug.Log($"  - {reward.rewardHarvestType}: {reward.amount} ({reward.name})");
                }
            }
            else
            {
                Debug.Log("  NO REWARDS - Under 3-minute minimum!");
            }

            Debug.Log("Preview and NormalHarvest will use IDENTICAL rewards!");
            Debug.Log("===================================");

            // Force update preview to verify
            ShowRewardPreview();
        }

    }
}