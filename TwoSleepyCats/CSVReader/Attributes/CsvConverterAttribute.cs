using System;
using TwoSleepyCats.CSVReader.Models;

namespace TwoSleepyCats.CSVReader.Attributes
{
    /// <summary>
    /// Custom type converter for complex data types
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CsvConverterAttribute : Attribute
    {
        public Type ConverterType { get; }
        
        public CsvConverterAttribute(Type converterType)
        {
            if (!typeof(ICsvConverter).IsAssignableFrom(converterType))
            {
                throw new ArgumentException($"Converter must implement ICsvConverter: {converterType.Name}");
            }
            ConverterType = converterType;
        }
    }
}