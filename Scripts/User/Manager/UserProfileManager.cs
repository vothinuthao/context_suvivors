using OctoberStudio.User.Data;
using TwoSleepyCats.Patterns.Singleton;
using UnityEngine;
using UnityEngine.Events;
using User.Manager;
using User.UI;

namespace OctoberStudio.User
{
    public class UserProfileManager : MonoBehaviour
    {
        private static UserProfileManager instance;
        public static UserProfileManager Instance => instance;
        protected UserProfileSave profileSave;
        public UserProfileSave ProfileSave => profileSave;

        [Header("UI Reference")]
        [SerializeField] protected UserProfileUI userProfileUI;
        
        [Header("CSV Configuration")]
        [SerializeField] private bool useCSVConfig = true;
        // Events
        public UnityAction<int> onUserLevelUp;
        public UnityAction<long> onUserGainXP;
        public UnityAction<UnlockReward> onUnlockReward;
        public UnityAction onProfileDataReady;
        private bool isDataReady = false;
        private bool isInitialized = false;
        
        protected virtual void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        protected virtual void Start()
        {
            // profileSave = GameController.SaveManager.GetSave<UserProfileSave>("UserProfile");
            // profileSave.Init();
            //
            // // Subscribe to events
            // profileSave.onUserLevelChanged += OnUserLevelChanged;
            // profileSave.onTotalXPChanged += OnTotalXPChanged;
            // if (UserLevelDatabase.Instance != null)
            // {
            //     UserLevelDatabase.Instance.OnDataLoaded += OnLevelDatabaseLoaded;
            //     UserLevelDatabase.Instance.OnLoadingError += OnLevelDatabaseError;
            // }
            if (isInitialized) return;
            InitializeProfileSystem();
        }
        private void InitializeProfileSystem()
        {
            isInitialized = true;
            
            LoadProfileData();
            
            SetupLevelDatabase();
            
            MarkDataReadyAndInitUI();
        }
        private void LoadProfileData()
        {
            profileSave = GameController.SaveManager.GetSave<UserProfileSave>("UserProfile");
            profileSave.Init();
            profileSave.onUserLevelChanged += OnUserLevelChanged;
            profileSave.onTotalXPChanged += OnTotalXPChanged;
            
            Debug.Log($"[UserProfileManager] Profile loaded - Level: {profileSave.UserLevel}, XP: {profileSave.TotalXP}");
        }

        private void SetupLevelDatabase()
        {
            if (UserLevelDatabase.Instance != null)
            {
                if (UserLevelDatabase.Instance.IsDataLoaded)
                {
                    OnLevelDatabaseReady();
                }
                else
                {
                    UserLevelDatabase.Instance.OnDataLoaded += OnLevelDatabaseReady;
                    UserLevelDatabase.Instance.OnLoadingError += OnLevelDatabaseError;
                }
            }
            else
            {
                Debug.LogWarning("[UserProfileManager] UserLevelDatabase not found, using fallback config");
                useCSVConfig = false;
            }
        }
        private void MarkDataReadyAndInitUI()
        {
            isDataReady = true;
            InitializeUIComponent();
            
            onProfileDataReady?.Invoke();
        }
        private void InitializeUIComponent()
        {
            if (userProfileUI != null)
            {
                userProfileUI.InitializeUI();
            }
        }

        public void RegisterNewUIComponent(UserProfileUI uiComponent)
        {
            if (isDataReady)
            {
                uiComponent.InitializeUI();
                Debug.Log($"[UserProfileManager] Immediately initialized new UI component: {uiComponent.name}");
            }
            else
            {
                Debug.LogWarning($"[UserProfileManager] UI component {uiComponent.name} registered but data not ready yet");
            }
        }
        
        public bool IsDataReady => isDataReady;
        
        private void OnLevelDatabaseReady()
        {
            if (profileSave != null && useCSVConfig)
            {
                ValidateUserLevel();
            }
        }

        protected virtual void OnDestroy()
        {
            if (profileSave != null)
            {
                profileSave.onUserLevelChanged -= OnUserLevelChanged;
                profileSave.onTotalXPChanged -= OnTotalXPChanged;
            }
            if (UserLevelDatabase.Instance != null)
            {
                UserLevelDatabase.Instance.OnDataLoaded -= OnLevelDatabaseReady;
                UserLevelDatabase.Instance.OnLoadingError -= OnLevelDatabaseError;
            }
        }

        private void OnLevelDatabaseLoaded()
        {
            Debug.Log("[UserProfileManager] Level database loaded successfully");
            
            if (profileSave != null && useCSVConfig)
            {
                ValidateUserLevel();
            }
        }
        private void OnLevelDatabaseError(string error)
        {
            useCSVConfig = false;
        }
        private void ValidateUserLevel()
        {
            if (!UserLevelDatabase.Instance.IsDataLoaded) return;
            int calculatedLevel = UserLevelDatabase.Instance.CalculateLevelFromXP(profileSave.TotalXP);
            
            if (calculatedLevel != profileSave.UserLevel)
            {
                Debug.Log($"[UserProfileManager] Adjusting user level from {profileSave.UserLevel} to {calculatedLevel} based on XP");
                profileSave.SetLevel(calculatedLevel);
            }
        }
        
        public virtual void OnStageCompleted(float survivalTimeSeconds, int stageNumber, bool isVictory = true)
        {
            var sessionData = new GameSessionData
            {
                survivalTimeSeconds = survivalTimeSeconds,
                stageNumber = stageNumber,
                isVictory = isVictory,
                enemiesKilled = 0, // Unknown in legacy call
                finalXPLevel = 1,  // Unknown in legacy call
                finalXP = 0        // Unknown in legacy call
            };
            OnGameSessionCompleted(sessionData);
        }

        // Alternative method for failed runs
        public virtual void OnGameOver(float survivalTimeSeconds, int stagesReached)
        {
            OnStageCompleted(survivalTimeSeconds, stagesReached, false);
        }

        protected virtual void OnUserLevelChanged(int newLevel)
        {
            CheckLevelRewards(newLevel);
            PlayLevelUpEffects();
            onUserLevelUp?.Invoke(newLevel);
        }

        protected virtual void OnTotalXPChanged(long newXP)
        {
            onUserGainXP?.Invoke(newXP);
        }

        protected void CheckLevelRewards(int level)
        {
            if (useCSVConfig && UserLevelDatabase.Instance.IsDataLoaded)
            {
                var rewards = UserLevelDatabase.Instance.GetLevelRewards(level);
                if (rewards != null)
                {
                    ApplyCSVRewards(rewards);
                }
            }
            else
            {
                CheckLevelRewards(level);
            }
        }
        private void ApplyCSVRewards(UserLevelModel levelRewards)
        {
            string rewardText = $"Level {levelRewards.Level} Rewards: ";
            
            // Character reward
            if (levelRewards.HasCharacterReward())
            {
                UnlockCharacter(levelRewards.RewardsCharacterId);
            }
            
            // Currency reward
            if (levelRewards.HasCurrencyReward())
            {
                GrantCurrency("gold", levelRewards.RewardsCurrencyAmount);
            }
            if (levelRewards.HasFeatureReward())
            {
                UnlockFeature(levelRewards.RewardsFeatureName);
            }
        }
        protected virtual void UnlockReward(UnlockReward reward)
        {
            switch (reward.rewardType)
            {
                case RewardType.Character:
                    UnlockCharacter(reward.rewardId);
                    break;
                case RewardType.Currency:
                    GrantCurrency(reward.currencyType, reward.rewardAmount);
                    break;
                case RewardType.Feature:
                    UnlockFeature(reward.featureName);
                    break;
            }

            onUnlockReward?.Invoke(reward);
            Debug.Log($"Unlocked reward: {reward.rewardName} at level {reward.unlockLevel}");
        }

        protected virtual void UnlockCharacter(int characterId)
        {
            var charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
            if (!charactersSave.HasCharacterBeenBought(characterId))
            {
                charactersSave.AddBoughtCharacter(characterId);
                Debug.Log($"Unlocked character ID: {characterId}");
            }
        }

        protected virtual void GrantCurrency(string currencyType, int amount)
        {
            var currencySave = GameController.SaveManager.GetSave<CurrencySave>(currencyType);
            currencySave.Deposit(amount);
            Debug.Log($"Granted {amount} {currencyType}");
        }

        protected virtual void UnlockFeature(string featureName)
        {
            // Unlock features like new game modes, etc.
            PlayerPrefs.SetInt($"Feature_{featureName}", 1);
            Debug.Log($"Unlocked feature: {featureName}");
        }

        protected virtual void PlayLevelUpEffects()
        {
            // Play sound effect
            GameController.AudioManager?.PlaySound("UserLevelUp".GetHashCode());
            
            // Play vibration
            GameController.VibrationManager?.MediumVibration();
        }
        public virtual void OnGameSessionCompleted(GameSessionData sessionData)
        {
            if (!isDataReady)
            {
                Debug.LogWarning("[UserProfileManager] Trying to complete session but data not ready!");
                return;
            }

            long baseXP = CalculateSessionXP(sessionData);
            profileSave.AddSessionCompletionXP(sessionData, baseXP);
            GameController.SaveManager?.Save(false);
        }
        public string GetLevelName(int level)
        {
            if (useCSVConfig && UserLevelDatabase.Instance.IsDataLoaded)
            {
                return UserLevelDatabase.Instance.GetLevelName(level);
            }
            
            return $"Level {level}";
        }

        public int GetMaxLevel()
        {
            if (useCSVConfig && UserLevelDatabase.Instance.IsDataLoaded)
            {
                return UserLevelDatabase.Instance.MaxLevel;
            }
            
            return 60; // Fallback
        }
        public long GetXPRequiredForLevel(int level)
        {
            if (useCSVConfig && UserLevelDatabase.Instance.IsDataLoaded)
            {
                return UserLevelDatabase.Instance.GetXPRequiredForLevel(level);
            }
            
            // Fallback to original calculation
            return profileSave.GetXpRequiredForLevel(level);
        }

        public long GetXPRequiredToLevelUp(int currentLevel)
        {
            if (useCSVConfig && UserLevelDatabase.Instance.IsDataLoaded)
            {
                return UserLevelDatabase.Instance.GetXPRequiredToLevelUp(currentLevel);
            }
            
            // Fallback
            return GetXPRequiredForLevel(currentLevel + 1) - profileSave.TotalXP;
        }
        protected virtual long CalculateSessionXP(GameSessionData sessionData)
        {
            float survivalMinutes = sessionData.GetSurvivalTimeMinutes();
            
            long survivalXP = (long)(survivalMinutes * 6f);
            long stageBonus = sessionData.stageNumber * 15;
            long victoryBonus = sessionData.isVictory ? 150 : 50;
            long killBonus = sessionData.enemiesKilled * 2;
            long levelBonus = sessionData.finalXPLevel * 10;
            
            float timeMultiplier = survivalMinutes > 10f ? 1.5f : 1f;
            long totalXP = (long)((survivalXP + stageBonus + victoryBonus + killBonus + levelBonus) * timeMultiplier);
            
            return (long)Mathf.Max(totalXP, 10);
        }
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugAddXP(long xp)
        {
            profileSave.AddDebugXP(xp);
            GameController.SaveManager?.Save(false);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugSetLevel(int level)
        {
            profileSave.SetLevel(level);
            GameController.SaveManager?.Save(false);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugCompleteStage(float minutes = 5f, int stage = 1)
        {
            OnStageCompleted(minutes * 60f, stage, true);
        }
    }

    [System.Serializable]
    public class UnlockReward
    {
        public string rewardName;
        public int unlockLevel;
        public RewardType rewardType;
        
        [Header("Character Reward")]
        public int rewardId; // Character ID or other ID
        
        [Header("Currency Reward")]
        public string currencyType = "gold";
        public int rewardAmount;
        
        [Header("Feature Reward")]
        public string featureName;
    }

    public enum RewardType
    {
        Character,
        Currency,
        Feature
    }
}