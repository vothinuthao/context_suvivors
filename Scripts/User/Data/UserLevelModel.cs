using TwoSleepyCats.CSVReader.Attributes;
using TwoSleepyCats.CSVReader.Core;
using UnityEngine;

namespace OctoberStudio.User.Data
{
    [System.Serializable]
    public class UserLevelModel : ICsvModel
    {
        [CsvColumn("level")] public int Level { get; set; }
        [CsvColumn("required_xp")] public long RequiredXP { get; set; }
        [CsvColumn("cumulative_xp")] public long CumulativeXP { get; set; }
        [CsvColumn("rewards_character_id")] public int RewardsCharacterId { get; set; } = -1;
        [CsvColumn("rewards_currency_amount")] public int RewardsCurrencyAmount { get; set; }
        [CsvColumn("rewards_feature_name")] public string RewardsFeatureName { get; set; } = "";
        [CsvColumn("level_name")] public string LevelName { get; set; } = "";

        public string GetCsvFileName() => "user_levels.csv";

        public void OnDataLoaded()
        {
            // Validation or post-processing if needed
        }

        public bool ValidateData()
        {
            bool isValid = Level > 0 && Level <= 60 && RequiredXP >= 0 && CumulativeXP >= 0;
            
            if (!isValid)
            {
                Debug.LogError($"[UserLevelModel] Invalid level data - Level: {Level}, RequiredXP: {RequiredXP}, CumulativeXP: {CumulativeXP}");
            }
            
            return isValid;
        }

        public bool HasCharacterReward() => RewardsCharacterId > 0;
        public bool HasCurrencyReward() => RewardsCurrencyAmount > 0;
        public bool HasFeatureReward() => !string.IsNullOrEmpty(RewardsFeatureName);
        public bool HasAnyReward() => HasCharacterReward() || HasCurrencyReward() || HasFeatureReward();

        public override string ToString()
        {
            return $"Level {Level}: {LevelName} (XP: {RequiredXP}, Total: {CumulativeXP})";
        }
    }
}