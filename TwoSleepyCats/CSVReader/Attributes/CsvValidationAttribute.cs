using System;

namespace TwoSleepyCats.CSVReader.Attributes
{
    /// <summary>
    /// Mark property for validation rules
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CsvValidationAttribute : Attribute
    {
        public object MinValue { get; set; }
        public object MaxValue { get; set; }
        public string RegexPattern { get; set; }
        public bool Required { get; set; } = true;
        public string ErrorMessage { get; set; }
    }
}