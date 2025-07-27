using OctoberStudio.User;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace User.UI
{
     public class UserProfileUI : MonoBehaviour
    {
        [Header("User Level Display")]
        [SerializeField] protected TextMeshProUGUI userLevelText;
        [SerializeField] protected TextMeshProUGUI totalXPText;
        [SerializeField] protected Slider levelProgressSlider;
        [SerializeField] protected TextMeshProUGUI xpToNextLevelText;

        [Header("Stats Display")]
        [SerializeField] protected TextMeshProUGUI totalGamesText;
        [SerializeField] protected TextMeshProUGUI bestTimeText;
        [SerializeField] protected TextMeshProUGUI stagesCompletedText;

        [Header("Animations")]
        [SerializeField] protected Animation levelUpAnimation;

        protected UserProfileSave profileSave;
        private bool isInitialized = false;


        // protected virtual void Start()
        // {
        //     if (UserProfileManager.Instance?.ProfileSave != null)
        //     {
        //         profileSave = UserProfileManager.Instance.ProfileSave;
        //         
        //         profileSave.onUserLevelChanged += OnUserLevelChanged;
        //         profileSave.onTotalXPChanged += OnTotalXPChanged;
        //         UpdateUI();
        //     }
        // }
        public virtual void InitializeUI()
        {
            if (isInitialized)
            {
                Debug.LogWarning($"[UserProfileUI] {name} already initialized!");
                return;
            }

            if (UserProfileManager.Instance?.ProfileSave == null)
            {
                Debug.LogError($"[UserProfileUI] {name} - UserProfileManager or ProfileSave not ready!");
                return;
            }

            profileSave = UserProfileManager.Instance.ProfileSave;
            
            // Subscribe to events
            profileSave.onUserLevelChanged += OnUserLevelChanged;
            profileSave.onTotalXPChanged += OnTotalXPChanged;

            // Subscribe to manager events
            UserProfileManager.Instance.onUserLevelUp += OnManagerLevelUp;

            // Initial update
            UpdateUI();
            
            isInitialized = true;
            Debug.Log($"[UserProfileUI] {name} initialized successfully");
        }
        protected virtual void Awake()
        {
            // If manager is already ready, register immediately
            if (UserProfileManager.Instance != null && UserProfileManager.Instance.IsDataReady)
            {
                UserProfileManager.Instance.RegisterNewUIComponent(this);
            }
        }

        protected virtual void OnDestroy()
        {
            if (!isInitialized) return;
            if (profileSave != null)
            {
                profileSave.onUserLevelChanged -= OnUserLevelChanged;
                profileSave.onTotalXPChanged -= OnTotalXPChanged;
            }

            if (UserProfileManager.Instance != null)
            {
                UserProfileManager.Instance.onUserLevelUp -= OnManagerLevelUp;
            }
        }

        protected virtual void UpdateUI()
        {
            if (profileSave == null) return;

            // User level
            if (userLevelText != null)
                userLevelText.text = $"Level {profileSave.UserLevel}";

            // Total XP
            if (totalXPText != null)
                totalXPText.text = $"Total XP: {profileSave.TotalXP:N0}";

            // Level progress
            if (levelProgressSlider != null)
                levelProgressSlider.value = profileSave.GetLevelProgress();

            // XP to next level
            if (xpToNextLevelText != null)
            {
                long xpToNext = profileSave.GetXPRequiredForNextLevel();
                if (xpToNext > 0)
                    xpToNextLevelText.text = $"{xpToNext:N0} XP to next level";
                else
                    xpToNextLevelText.text = "MAX LEVEL";
            }

            // Stats
            if (totalGamesText != null)
                totalGamesText.text = $"Games Played: {profileSave.TotalGamesPlayed}";

            if (bestTimeText != null)
            {
                int minutes = Mathf.FloorToInt(profileSave.BestSurvivalTime);
                int seconds = Mathf.FloorToInt((profileSave.BestSurvivalTime % 1) * 60);
                bestTimeText.text = $"Best Time: {minutes}:{seconds:00}";
            }

            if (stagesCompletedText != null)
                stagesCompletedText.text = $"Stages: {profileSave.TotalStagesCompleted}";
        }

        protected virtual void OnUserLevelChanged(int newLevel)
        {
            UpdateUI();
            if (levelUpAnimation != null)
                levelUpAnimation.Play();
        }

        protected virtual void OnTotalXPChanged(long newXP)
        {
            UpdateUI();
        }
        protected virtual void OnManagerLevelUp(int newLevel)
        {
            if (levelUpAnimation != null)
                levelUpAnimation.Play();
        }
        public void RefreshUI()
        {
            if (isInitialized)
            {
                UpdateUI();
            }
        }

        // Debug method
        [ContextMenu("Force Initialize")]
        public void ForceInitialize()
        {
            isInitialized = false;
            InitializeUI();
        }
    }
}