using TwoSleepyCats.CSVReader.Attributes;
using TwoSleepyCats.CSVReader.Core;

namespace TwoSleepyCats.Samples.AdvancedSample
{
    /// <summary>
    /// Guild model
    /// CSV: id,name,leader_id,member_count,guild_type
    /// </summary>
    public class GuildModel : ICsvModel
    {
        [CsvColumn("id")]
        public int ID { get; set; }
        
        [CsvColumn("name")]
        public string Name { get; set; }
        
        [CsvColumn("leader_id")]
        public int LeaderID { get; set; }
        
        [CsvColumn("member_count")]
        public int MemberCount { get; set; }
        
        [CsvColumn("guild_type")]
        public GuildType Type { get; set; }
        
        public string GetCsvFileName() => "guilds.csv";
        public void OnDataLoaded() { }
        public bool ValidateData() => ID > 0 && !string.IsNullOrEmpty(Name);
    }
}