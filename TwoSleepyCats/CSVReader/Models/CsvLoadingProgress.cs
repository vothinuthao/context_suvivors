using System;

namespace TwoSleepyCats.CSVReader.Models
{
    public class CsvLoadingProgress
    {
        public string FileName { get; set; }
        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; }
        public float Progress => TotalRows > 0 ? (float)ProcessedRows / TotalRows : 0f;
        public string Status { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public int ErrorCount { get; set; }
        
        public override string ToString()
        {
            return $"{FileName}: {ProcessedRows}/{TotalRows} ({Progress:P1}) - {Status}";
        }
    }
}