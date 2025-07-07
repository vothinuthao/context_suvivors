using System;

namespace TwoSleepyCats.CSVReader.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CsvColumnAttribute : Attribute
    {
        public string ColumnName { get; }
        public int ColumnIndex { get; }
        public bool IsOptional { get; }
        public bool AutoConvert { get; }
        public bool UseIndex { get; }
        
        /// <summary>
        /// Map by column name (recommended)
        /// </summary>
        public CsvColumnAttribute(string columnName, bool isOptional = false, bool autoConvert = false)
        {
            ColumnName = columnName;
            ColumnIndex = -1;
            IsOptional = isOptional;
            AutoConvert = autoConvert;
            UseIndex = false;
        }
        
        /// <summary>
        /// Map by column index (fallback)
        /// </summary>
        public CsvColumnAttribute(int columnIndex, bool isOptional = false, bool autoConvert = false)
        {
            ColumnName = null;
            ColumnIndex = columnIndex;
            IsOptional = isOptional;
            AutoConvert = autoConvert;
            UseIndex = true;
        }
    }
}