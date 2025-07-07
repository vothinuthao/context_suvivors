using System.Collections.Generic;
using System.Linq;
using TwoSleepyCats.CSVReader.Attributes;
using TwoSleepyCats.CSVReader.Core;
using UnityEngine;

namespace TwoSleepyCats.Samples.AdvancedSample
{
    /// <summary>
    /// Character class with basic CSV mapping (relationships temporarily disabled)
    /// CSV: character_id,name,level,guild_id,position,equipment_ids
    /// </summary>
    public class AdvancedCharacterModel : ICsvModel
    {
        [CsvColumn("character_id")]
        public int ID { get; set; }
    
        [CsvColumn("name")]
        public string Name { get; set; }
    
        [CsvColumn("level")]
        public int Level { get; set; }
    
        [CsvColumn("guild_id")]
        public int GuildID { get; set; }
    
        [CsvColumn("position", autoConvert: true)]
        public Vector3 Position { get; set; }
    
        [CsvColumn("equipment_ids", isOptional: true)]
        public string EquipmentIDsString { get; set; } 
        
        [CsvIgnore]
        public GuildModel Guild { get; set; }
    
        [CsvIgnore]
        public List<AdvancedItemModel> Inventory { get; set; }
        
        // Helper property to parse equipment IDs
        [CsvIgnore]
        public int[] EquipmentIDs 
        { 
            get 
            {
                if (string.IsNullOrEmpty(EquipmentIDsString))
                    return new int[0];
                    
                try
                {
                    return EquipmentIDsString.Split(',')
                        .Select(s => int.Parse(s.Trim()))
                        .ToArray();
                }
                catch
                {
                    return new int[0];
                }
            }
        }
    
        public string GetCsvFileName() => "characters.csv";

        public void OnDataLoaded()
        {
            // Initialize inventory if null
            if (Inventory == null)
                Inventory = new List<AdvancedItemModel>();
        
            Debug.Log($"[AdvancedCharacter] {Name} loaded (Level {Level}, Guild ID: {GuildID})");
        }
    
        public bool ValidateData()
        {
            return ID > 0 && !string.IsNullOrEmpty(Name) && Level >= 1 && Level <= 100;
        }
    }
}