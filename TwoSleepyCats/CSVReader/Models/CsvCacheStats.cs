using System;
using System.Collections.Generic;
using System.Text;

namespace TwoSleepyCats.CSVReader.Models
{
    /// <summary>
    /// Enhanced cache statistics
    /// </summary>
    public class CsvCacheStats
    {
        public int TotalEntries { get; set; }
        public long MemoryUsageBytes { get; set; }
        public float HitRate { get; set; }
        public int TotalHits { get; set; }
        public int TotalMisses { get; set; }
        public TimeSpan AverageLoadTime { get; set; }
        public Dictionary<string, int> TypeCounts { get; set; } = new Dictionary<string, int>();
        
        public string GetSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Cache Entries: {TotalEntries}");
            sb.AppendLine($"Memory Usage: {MemoryUsageBytes / 1024 / 1024:F2} MB");
            sb.AppendLine($"Hit Rate: {HitRate:P1} ({TotalHits} hits, {TotalMisses} misses)");
            sb.AppendLine($"Average Load Time: {AverageLoadTime.TotalMilliseconds:F1}ms");
            
            if (TypeCounts.Count > 0)
            {
                sb.AppendLine("Type Distribution:");
                foreach (var kvp in TypeCounts)
                {
                    sb.AppendLine($"  • {kvp.Key}: {kvp.Value}");
                }
            }
            
            return sb.ToString();
        }
    }
}