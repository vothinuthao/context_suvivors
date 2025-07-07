using System;
using System.Collections.Generic;

namespace TwoSleepyCats.CSVReader.Models
{
    /// <summary>
    /// Relationship loading context
    /// </summary>
    public class CsvRelationshipContext
    {
        public Dictionary<Type, object> LoadedData { get; } = new Dictionary<Type, object>();
        public HashSet<string> LoadingFiles { get; } = new HashSet<string>();
        public int MaxDepth { get; set; } = 5;
        public int CurrentDepth { get; set; } = 0;
        
        public bool IsCircularReference(string fileName)
        {
            return LoadingFiles.Contains(fileName) || CurrentDepth >= MaxDepth;
        }
        
        public void PushFile(string fileName)
        {
            LoadingFiles.Add(fileName);
            CurrentDepth++;
        }
        
        public void PopFile(string fileName)
        {
            LoadingFiles.Remove(fileName);
            CurrentDepth--;
        }
    }
}