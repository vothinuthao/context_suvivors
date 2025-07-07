using System;
using TwoSleepyCats.CSVReader.Models;

namespace TwoSleepyCats.Samples.AdvancedSample
{
    /// <summary>
    /// Custom converter for ItemStats: "100,50,75,0.15" → ItemStats
    /// </summary>
    public class ItemStatsConverter : ICsvConverter
    {
        public bool CanConvert(Type targetType)
        {
            return targetType == typeof(ItemStats);
        }
        
        public object Convert(string value, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new ItemStats();
            
            var parts = value.Split(',');
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
            
            return new ItemStats();
        }
    }
}