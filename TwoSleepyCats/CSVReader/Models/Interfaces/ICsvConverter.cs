using System;

namespace TwoSleepyCats.CSVReader.Models
{
    public interface ICsvConverter
    {
        object Convert(string value, Type targetType);
        bool CanConvert(Type targetType);
    }
}