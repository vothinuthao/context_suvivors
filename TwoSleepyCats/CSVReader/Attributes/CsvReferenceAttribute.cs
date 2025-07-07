using System;

namespace TwoSleepyCats.CSVReader.Attributes
{
    /// <summary>
    /// Define relationships between CSV files (Foreign Key relationships)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CsvReferenceAttribute : Attribute
    {
        public string TargetCsv { get; }           // "items.csv"
        public string ForeignKey { get; }          // "character_id" in target CSV
        public string PrimaryKey { get; }          // "id" in current model (optional)
        public bool IsCollection { get; }          // List<T> vs single T
        public bool IsLazy { get; }                // Load on-demand vs eager loading
        
        /// <summary>
        /// One-to-One or One-to-Many relationship
        /// </summary>
        /// <param name="targetCsv">Target CSV filename (items.csv)</param>
        /// <param name="foreignKey">Foreign key column in target CSV</param>
        /// <param name="primaryKey">Primary key in current model (default: "id")</param>
        /// <param name="isCollection">True for One-to-Many, False for One-to-One</param>
        /// <param name="isLazy">True for lazy loading, False for eager loading</param>
        public CsvReferenceAttribute(string targetCsv, string foreignKey, 
            string primaryKey = "id", bool isCollection = false, bool isLazy = false)
        {
            TargetCsv = targetCsv;
            ForeignKey = foreignKey;
            PrimaryKey = primaryKey;
            IsCollection = isCollection;
            IsLazy = isLazy;
        }
    }
}