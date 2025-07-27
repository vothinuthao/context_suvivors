using System;
using System.Collections.Generic;
using System.Linq;
using OctoberStudio.Equipment;
using OctoberStudio.User.Data;
using TwoSleepyCats.CSVReader.Core;
using UnityEngine;

namespace User.Manager
{
    public class UserLevelDatabase : MonoBehaviour
    {
        private static UserLevelDatabase instance;
        public static UserLevelDatabase Instance => instance;
        [Header("Loading Settings")]
        [SerializeField] private bool loadOnStart = true;
        
        [Header("Debug Info")]
        [SerializeField, ReadOnly] private int totalLevels = 0;
        [SerializeField, ReadOnly] private bool isDataLoaded = false;
        [SerializeField, ReadOnly] private string loadStatus = "Not Loaded";

        // Level data organized by level number
        private Dictionary<int, UserLevelModel> levelsByNumber = new Dictionary<int, UserLevelModel>();
        private List<UserLevelModel> allLevels = new List<UserLevelModel>();

        // Events
        public event System.Action OnDataLoaded;
        public event System.Action<string> OnLoadingError;

        // Properties
        public bool IsDataLoaded => isDataLoaded;
        public int TotalLevels => totalLevels;
        public int MaxLevel => allLevels.Count > 0 ? allLevels.Max(l => l.Level) : 60;
        
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
        
        protected void Start()
        {
            if (loadOnStart)
            {
                LoadLevelData();
            }
        }

        public async void LoadLevelData()
        {
            try
            {
                loadStatus = "Loading...";
                Debug.Log("[UserLevelDatabase] Loading user level data from CSV...");
                
                var levelData = await CsvDataManager.Instance.LoadAsync<UserLevelModel>();
                
                ProcessLevelData(levelData);
                
                isDataLoaded = true;
                loadStatus = $"Loaded {totalLevels} levels";
                OnDataLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                isDataLoaded = false;
                loadStatus = $"Error: {ex.Message}";
                OnLoadingError?.Invoke(ex.Message);
                Debug.LogError($"[UserLevelDatabase] Failed to load level data: {ex.Message}");
            }
        }

        private void ProcessLevelData(List<UserLevelModel> levelData)
        {
            // Clear existing data
            ClearData();

            // Process and validate each level
            foreach (var level in levelData)
            {
                if (level.ValidateData())
                {
                    allLevels.Add(level);
                    levelsByNumber[level.Level] = level;
                }
            }

            // Sort by level number
            allLevels.Sort((a, b) => a.Level.CompareTo(b.Level));
            totalLevels = allLevels.Count;

            Debug.Log($"[UserLevelDatabase] Processed {totalLevels} user levels (Max: {MaxLevel})");
        }

        private void ClearData()
        {
            allLevels.Clear();
            levelsByNumber.Clear();
            totalLevels = 0;
        }

        /// <summary>
        /// Get level config by level number
        /// </summary>
        public UserLevelModel GetLevelConfig(int level)
        {
            if (!isDataLoaded)
            {
                Debug.LogWarning("[UserLevelDatabase] Data not loaded yet!");
                return null;
            }

            return levelsByNumber.GetValueOrDefault(level);
        }

        /// <summary>
        /// Get XP required to reach specific level (from level 1)
        /// </summary>
        public long GetXPRequiredForLevel(int level)
        {
            if (level <= 1) return 0;

            var levelConfig = GetLevelConfig(level);
            return levelConfig?.CumulativeXP ?? GetFallbackXP(level);
        }

        /// <summary>
        /// Get XP required to go from current level to next level
        /// </summary>
        public long GetXPRequiredToLevelUp(int currentLevel)
        {
            if (currentLevel >= MaxLevel) return 0;

            var nextLevelConfig = GetLevelConfig(currentLevel + 1);
            return nextLevelConfig?.RequiredXP ?? GetFallbackXPToNext(currentLevel);
        }

        /// <summary>
        /// Calculate what level player should be at with given total XP
        /// </summary>
        public int CalculateLevelFromXP(long totalXP)
        {
            if (!isDataLoaded || totalXP <= 0) return 1;

            int level = 1;
            foreach (var levelConfig in allLevels)
            {
                if (totalXP >= levelConfig.CumulativeXP)
                {
                    level = levelConfig.Level;
                }
                else
                {
                    break;
                }
            }

            return level;
        }
        
        public float GetLevelProgress(int currentLevel, long currentXP)
        {
            if (currentLevel >= MaxLevel) return 1f;

            long currentLevelXP = GetXPRequiredForLevel(currentLevel);
            long nextLevelXP = GetXPRequiredForLevel(currentLevel + 1);
            
            if (nextLevelXP <= currentLevelXP) return 1f;

            long progressXP = currentXP - currentLevelXP;
            long requiredXP = nextLevelXP - currentLevelXP;

            return Mathf.Clamp01((float)progressXP / requiredXP);
        }

        /// <summary>
        /// Get all rewards for specific level
        /// </summary>
        public UserLevelModel GetLevelRewards(int level)
        {
            var levelConfig = GetLevelConfig(level);
            return levelConfig?.HasAnyReward() == true ? levelConfig : null;
        }

        /// <summary>
        /// Get all levels that have character rewards
        /// </summary>
        public UserLevelModel[] GetLevelsWithCharacterRewards()
        {
            if (!isDataLoaded) return Array.Empty<UserLevelModel>();
            
            return allLevels.Where(l => l.HasCharacterReward()).ToArray();
        }

        /// <summary>
        /// Get all levels that unlock features
        /// </summary>
        public UserLevelModel[] GetLevelsWithFeatureUnlocks()
        {
            if (!isDataLoaded) return Array.Empty<UserLevelModel>();
            
            return allLevels.Where(l => l.HasFeatureReward()).ToArray();
        }

        /// <summary>
        /// Check if level exists and is valid
        /// </summary>
        public bool IsValidLevel(int level)
        {
            return level >= 1 && level <= MaxLevel && levelsByNumber.ContainsKey(level);
        }

        /// <summary>
        /// Get level name for display
        /// </summary>
        public string GetLevelName(int level)
        {
            var levelConfig = GetLevelConfig(level);
            return levelConfig?.LevelName ?? $"Level {level}";
        }

        // Fallback methods if CSV data is missing
        private long GetFallbackXP(int level)
        {
            // Fallback exponential formula: 500 * level^2
            return 500L * level * level;
        }

        private long GetFallbackXPToNext(int currentLevel)
        {
            return GetFallbackXP(currentLevel + 1) - GetFallbackXP(currentLevel);
        }

        /// <summary>
        /// Force reload data from CSV
        /// </summary>
        [ContextMenu("Reload Level Data")]
        public async void ReloadLevelData()
        {
            try
            {
                isDataLoaded = false;
                loadStatus = "Reloading...";
                var levelData = await CsvDataManager.Instance.ForceReloadAsync<UserLevelModel>();
                ProcessLevelData(levelData);
                
                isDataLoaded = true;
                loadStatus = $"Reloaded {totalLevels} levels";
                OnDataLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                isDataLoaded = false;
                loadStatus = $"Reload Error: {ex.Message}";
                OnLoadingError?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// Debug: Log level progression table
        /// </summary>
        [ContextMenu("Log Level Progression")]
        public void LogLevelProgression()
        {
            if (!isDataLoaded)
            {
                Debug.Log("[UserLevelDatabase] Data not loaded");
                return;
            }

            Debug.Log("=== USER LEVEL PROGRESSION ===");
            foreach (var level in allLevels.Take(10)) // First 10 levels
            {
                string rewards = "";
                if (level.HasCharacterReward()) rewards += $"Char#{level.RewardsCharacterId} ";
                if (level.HasCurrencyReward()) rewards += $"Gold+{level.RewardsCurrencyAmount} ";
                if (level.HasFeatureReward()) rewards += $"Feature:{level.RewardsFeatureName} ";
                
                Debug.Log($"Lv.{level.Level}: {level.LevelName} | XP: {level.RequiredXP} | Total: {level.CumulativeXP} | {rewards}");
            }
            
            if (allLevels.Count > 10)
            {
                Debug.Log($"... and {allLevels.Count - 10} more levels");
            }
        }

        /// <summary>
        /// Debug: Simulate level progression
        /// </summary>
        [ContextMenu("Simulate Level Progression")]
        public void SimulateLevelProgression()
        {
            Debug.Log("=== LEVEL PROGRESSION SIMULATION ===");
            
            long[] testXPValues = { 0, 100, 500, 1500, 5000, 15000, 50000, 150000, 500000, 1000000 };
            
            foreach (long xp in testXPValues)
            {
                int level = CalculateLevelFromXP(xp);
                float progress = GetLevelProgress(level, xp);
                string levelName = GetLevelName(level);
                
                Debug.Log($"XP: {xp:N0} → Level {level} ({levelName}) | Progress: {progress:P1}");
            }
        }
    }
}