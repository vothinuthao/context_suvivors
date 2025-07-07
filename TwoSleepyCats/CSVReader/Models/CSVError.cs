using System;

namespace TwoSleepyCats.CSVReader.Models
{
    public enum ErrorSeverity
    {
        Warning,    // Data loaded with default value
        Error,      // Data skipped
        Critical    // Loading failed completely
    }

    public class CsvError
    {
        public int Row { get; set; }
        public string Column { get; set; }
        public string Value { get; set; }
        public string ExpectedType { get; set; }
        public string ErrorMessage { get; set; }
        public ErrorSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return $"[{Severity}] Row {Row}, Column '{Column}': {ErrorMessage} (Value: '{Value}')";
        }
    }
}