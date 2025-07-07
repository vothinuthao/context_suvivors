using System;

namespace TwoSleepyCats.CSVReader.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CsvIgnoreAttribute : Attribute
    {
        // Mark property to ignore during CSV mapping
    }
}