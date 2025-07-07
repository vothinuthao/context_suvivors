using System.Linq;
using TwoSleepyCats.CSVReader.Attributes;
using TwoSleepyCats.CSVReader.Core;
using UnityEngine;

namespace TwoSleepyCats.Samples.AdvancedSample
{
    /// <summary>
    /// Advanced item with custom converter and validation
    /// CSV: item_id,name,rarity,stats,color,owner_id
    /// </summary>
    public class AdvancedItemModel : ICsvModel
    {
        [CsvColumn("item_id")]
        public int ID { get; set; }
        
        [CsvColumn("name")]
        public string Name { get; set; }
        
        [CsvColumn("rarity")]
        public ItemRarity Rarity { get; set; }
        
        // TEMPORARY FIX: Use string instead of custom converter
        [CsvColumn("stats", isOptional: true)]
        public string StatsString { get; set; }
        
        [CsvColumn("color", autoConvert: true)]
        public Color ItemColor { get; set; }
        
        [CsvColumn("owner_id")]
        public int OwnerID { get; set; }
        
        [CsvColumn("tags", isOptional: true)]
        public string TagsString { get; set; }
        
        // Helper properties
        [CsvIgnore]
        public ItemStats Stats 
        { 
            get 
            {
                if (string.IsNullOrEmpty(StatsString))
                    return new ItemStats();
                    
                try
                {
                    var parts = StatsString.Split(',');
                    if (parts.Length >= 4)
                    {
                        return new ItemStats
                        {
                            Attack = int.Parse(parts[0].Trim()),
                            Defense = int.Parse(parts[1].Trim()),
                            Speed = int.Parse(parts[2].Trim()),
                            CritChance = float.Parse(parts[3].Trim())
                        };
                    }
                }
                catch { }
                
                return new ItemStats();
            }
        }
        
        [CsvIgnore]
        public string[] Tags 
        { 
            get 
            {
                if (string.IsNullOrEmpty(TagsString))
                    return new string[] { "misc" };
                    
                return TagsString.Split(',').Select(s => s.Trim()).ToArray();
            }
        }
        
        public string GetCsvFileName() => "items.csv";
        
        public void OnDataLoaded()
        {
            Debug.Log($"[AdvancedItem] {Name} loaded ({Rarity}) - {Stats}");
        }
        
        public bool ValidateData() => ID > 0 && !string.IsNullOrEmpty(Name);
    }
}