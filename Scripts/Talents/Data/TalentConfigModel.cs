using TwoSleepyCats.CSVReader.Attributes;
using TwoSleepyCats.CSVReader.Core;

namespace Talents.Data
{
    /// <summary>
    /// Configuration model for linear talent system stat formulas
    /// </summary>
    [System.Serializable]
    public class TalentConfigModel : ICsvModel
    {
        [CsvColumn("stat_type")] public string StatType { get; set; }
        
        [CsvColumn("base_value")] public float BaseValue { get; set; }
        
        [CsvColumn("multiplier")] public float Multiplier { get; set; }
        
        [CsvColumn("cost_base")] public int CostBase { get; set; }
        
        [CsvColumn("cost_per_level")] public int CostPerLevel { get; set; }
        
        [CsvColumn("icon")] public string Icon { get; set; }

        public string GetCsvFileName()
        {
            return "talentConfig.csv";
        }

        public void OnDataLoaded()
        {
            // No additional processing needed for configuration data
        }
    }
}