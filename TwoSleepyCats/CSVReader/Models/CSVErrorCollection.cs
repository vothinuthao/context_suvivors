using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TwoSleepyCats.CSVReader.Models
{
    public class CsvErrorCollection
    {
        public List<CsvError> Errors { get; private set; } = new();

        public bool HasCriticalErrors => Errors.Any(e => e.Severity == ErrorSeverity.Critical);
        public bool HasErrors => Errors.Any(e => e.Severity == ErrorSeverity.Error);
        public bool HasWarnings => Errors.Any(e => e.Severity == ErrorSeverity.Warning);
        public bool IsSuccess => !HasCriticalErrors && !HasErrors;

        public void AddError(CsvError error)
        {
            Errors.Add(error);
        }

        public void LogToConsole()
        {
            foreach (var error in Errors)
            {
                switch (error.Severity)
                {
                    case ErrorSeverity.Warning:
                        Debug.LogWarning($"CSV Warning: {error}");
                        break;
                    case ErrorSeverity.Error:
                        Debug.LogError($"CSV Error: {error}");
                        break;
                    case ErrorSeverity.Critical:
                        Debug.LogError($"CSV Critical: {error}");
                        break;
                }
            }
        }

        public string GetSummary()
        {
            if (Errors.Count == 0) return "No errors";
            
            var sb = new StringBuilder();
            sb.AppendLine($"Total Errors: {Errors.Count}");
            sb.AppendLine($"Critical: {Errors.Count(e => e.Severity == ErrorSeverity.Critical)}");
            sb.AppendLine($"Errors: {Errors.Count(e => e.Severity == ErrorSeverity.Error)}");
            sb.AppendLine($"Warnings: {Errors.Count(e => e.Severity == ErrorSeverity.Warning)}");
            return sb.ToString();
        }

        public CsvError[] GetErrorsForRow(int row)
        {
            return Errors.Where(e => e.Row == row).ToArray();
        }
    }
}