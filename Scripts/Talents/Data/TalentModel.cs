using OctoberStudio.Upgrades;
using TwoSleepyCats.CSVReader.Attributes;
using TwoSleepyCats.CSVReader.Core;
using UnityEngine;

namespace Talents.Data
{
    [System.Serializable]
    public class TalentModel : ICsvModel
    {
        [CsvColumn("id")] public int ID { get; set; }
        
        [CsvColumn("icon")] public string IconPath { get; set; }
        
        [CsvColumn("name")] public string Name { get; set; }
        
        [CsvColumn("description")] public string Description { get; set; }
        
        [CsvColumn("stat_value")] public float StatValue { get; set; }
        
        [CsvColumn("stat_type")] public string StatTypeString { get; set; }
        
        [CsvColumn("node_type")] public string NodeTypeString { get; set; }
        
        [CsvColumn("position_x")] public float PositionX { get; set; }
        
        [CsvColumn("position_y")] public float PositionY { get; set; }
        
        [CsvColumn("required_talent_id", isOptional: true)]
        public int RequiredTalentId { get; set; } = -1;
        
        [CsvColumn("cost")] public int Cost { get; set; }
        
        [CsvColumn("max_level")] public int MaxLevel { get; set; } = 1;
        
        // Parsed properties
        [CsvIgnore]
        public UpgradeType StatType { get; private set; }
        
        [CsvIgnore]
        public TalentNodeType NodeType { get; private set; }
        
        [CsvIgnore]
        public Sprite Icon { get; private set; }
        
        [CsvIgnore]
        public Vector2 Position => new Vector2(PositionX, PositionY);

        public string GetCsvFileName()
        {
            return "talentConfig.csv";
        }

        public void OnDataLoaded()
        {
            // Parse stat type
            if (System.Enum.TryParse<UpgradeType>(StatTypeString, true, out var parsedStatType))
            {
                StatType = parsedStatType;
            }
            else
            {
                StatType = UpgradeType.Health; // Default fallback
            }
            
            // Parse node type
            if (System.Enum.TryParse<TalentNodeType>(NodeTypeString, true, out var parsedNodeType))
            {
                NodeType = parsedNodeType;
            }
            else
            {
                NodeType = TalentNodeType.Normal; // Default fallback
            }
            
            // Load icon
            if (!string.IsNullOrEmpty(IconPath))
            {
                Icon = Resources.Load<Sprite>($"Icons/Talents/{IconPath}");
            }
        }

        public bool ValidateData()
        {
            return ID > 0 && !string.IsNullOrEmpty(Name) && Cost >= 0 && MaxLevel > 0;
        }
    }
    
    public enum TalentNodeType
    {
        Normal,
        Special
    }
}