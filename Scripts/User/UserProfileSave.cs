using OctoberStudio.Save;
using UnityEngine;
using UnityEngine.Events;
using User.Manager;

namespace OctoberStudio.User
{
    public class UserProfileSave : ISave
    {
        [SerializeField] protected int userLevel = 1;
        [SerializeField] protected long totalXP = 0;
        [SerializeField] protected int totalGamesPlayed = 0;
        [SerializeField] protected float bestSurvivalTime = 0f;
        [SerializeField] protected int totalStagesCompleted = 0;

        public int UserLevel => userLevel;
        public long TotalXP => totalXP;
        public int TotalGamesPlayed => totalGamesPlayed;
        public float BestSurvivalTime => bestSurvivalTime;
        public int TotalStagesCompleted => totalStagesCompleted;

        // Events cho UI updates
        public UnityAction<int> onUserLevelChanged;
        public UnityAction<long> onTotalXPChanged;

        public virtual void Init()
        {
            if (userLevel < 1) userLevel = 1;
            if (totalXP < 0) totalXP = 0;
        }

        protected virtual long CalculateStageXP(float survivalTimeMinutes, int stageNumber, bool isVictory)
        {
            // Base XP từ survival time (1 XP per 10 seconds)
            long survivalXP = (long)(survivalTimeMinutes * 6f);
            
            // Stage bonus (higher stages give more XP)
            long stageBonus = stageNumber * 10;
            
            // Victory bonus
            long victoryBonus = isVictory ? 100 : 0;
            
            // First time bonus (if best time)
            long firstTimeBonus = survivalTimeMinutes > bestSurvivalTime ? 50 : 0;

            return survivalXP + stageBonus + victoryBonus + firstTimeBonus;
        }

        protected virtual void CheckLevelUp()
        {
            // long requiredXP = GetXPRequiredForLevel(userLevel + 1);
            //
            // while (totalXP >= requiredXP && userLevel < GetMaxLevel())
            // {
            //     LevelUp();
            //     requiredXP = GetXPRequiredForLevel(userLevel + 1);
            // }
            if (UserProfileManager.Instance != null)
            {
                var manager = UserProfileManager.Instance;
                long requiredXP = manager.GetXPRequiredForLevel(userLevel + 1);
                int maxLevel = manager.GetMaxLevel();
                
                while (totalXP >= requiredXP && userLevel < maxLevel)
                {
                    LevelUp();
                    requiredXP = manager.GetXPRequiredForLevel(userLevel + 1);
                }
            }
            else
            {
                // Fallback to original logic
                CheckLevelUp();
            }
        }

        protected virtual void LevelUp()
        {
            userLevel++;
            onUserLevelChanged?.Invoke(userLevel);
            
            Debug.Log($"User Level Up! New Level: {userLevel}");
        }

        public virtual long GetXpRequiredForLevel(int level)
        {
            // Fallback XP calculation: adjust as needed for your game
            const int baseXP = 100;
            const int perLevelXP = 50;
            if (level <= 1) return 0;
            return baseXP + perLevelXP * (level - 1);
        }
        
        
        public virtual float GetLevelProgress()
        {
            if (userLevel >= GetMaxLevel()) return 1f;
            long requiredXp = GetXpRequiredForLevel(userLevel + 1);
            if (requiredXp <= 0) return 1f;
            return (float)(totalXP - GetXpRequiredForLevel(userLevel)) / (requiredXp - GetXpRequiredForLevel(userLevel));
        }

        public virtual long GetXPRequiredForNextLevel()
        {
            if (userLevel >= GetMaxLevel()) return 0;
            return GetXpRequiredForLevel(userLevel + 1) - totalXP;
        }
        protected virtual int GetMaxLevel()
        {
            if (UserLevelDatabase.Instance.IsDataLoaded)
            {
                return UserLevelDatabase.Instance.MaxLevel;
            }

            return 60;
        }

        // Debug/Admin methods
        public virtual void SetLevel(int level)
        {
            level = Mathf.Clamp(level, 1, GetMaxLevel());
            userLevel = level;
            totalXP = GetXpRequiredForLevel(level);
            
            onUserLevelChanged?.Invoke(userLevel);
            onTotalXPChanged?.Invoke(totalXP);
        }

        public virtual void AddDebugXP(long xp)
        {
            totalXP += xp;
            onTotalXPChanged?.Invoke(totalXP);
            CheckLevelUp();
        }

        public virtual void Flush()
        {
            // Required by ISave interface
        }
        public virtual void AddSessionCompletionXP(GameSessionData sessionData, long xpGained)
        {
            totalXP += xpGained;
            totalGamesPlayed++;
            
            if (sessionData.isVictory)
            {
                totalStagesCompleted++;
            }
            
            float survivalMinutes = sessionData.GetSurvivalTimeMinutes();
            if (survivalMinutes > bestSurvivalTime)
            {
                bestSurvivalTime = survivalMinutes;
            }

            onTotalXPChanged?.Invoke(totalXP);
            CheckLevelUp();
        }
    }
}