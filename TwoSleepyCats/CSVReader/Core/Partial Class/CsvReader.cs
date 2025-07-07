using System;
using System.Collections.Generic;
using System.Reflection;

namespace TwoSleepyCats.CSVReader.Core
{
    /// <summary>
    /// Enhanced CSV Reader with advanced type conversion
    /// </summary>
    public static partial class CsvReader<T> where T : ICsvModel, new()
    {
        /// <summary>
        /// Parse row with advanced type conversion and validation
        /// </summary>
        private static TModel ParseRowAdvanced<TModel>(string[] rowData, string[] headers, 
            Dictionary<PropertyInfo, (int index, Attributes.CsvColumnAttribute attribute)> mappings, 
            int rowNumber, Models.CsvErrorCollection errors) where TModel : new()
        {
            var model = new TModel();

            foreach (var mapping in mappings)
            {
                var property = mapping.Key;
                var (columnIndex, attribute) = mapping.Value;

                try
                {
                    string value = "";
                    if (columnIndex < rowData.Length)
                    {
                        value = rowData[columnIndex];
                    }

                    // Check for custom converter
                    Models.ICsvConverter customConverter = null;
                    var converterAttr = property.GetCustomAttribute<Attributes.CsvConverterAttribute>();
                    if (converterAttr != null)
                    {
                        customConverter = (Models.ICsvConverter)Activator.CreateInstance(converterAttr.ConverterType);
                    }

                    // Use advanced type converter
                    var convertedValue = Utils.AdvancedTypeConverter.ConvertValue(
                        value, property.PropertyType, attribute?.AutoConvert ?? false, customConverter);
                    
                    property.SetValue(model, convertedValue);
                    
                    // Perform validation
                    var validationAttr = property.GetCustomAttribute<Attributes.CsvValidationAttribute>();
                    if (validationAttr != null)
                    {
                        ValidateProperty(property, convertedValue, validationAttr, rowNumber, errors);
                    }
                }
                catch (Exception ex)
                {
                    var columnName = attribute?.ColumnName ?? property.Name;
                    var rawValue = columnIndex < rowData.Length ? rowData[columnIndex] : "";
                    
                    errors.AddError(new Models.CsvError
                    {
                        Row = rowNumber,
                        Column = columnName,
                        Value = rawValue,
                        ExpectedType = property.PropertyType.Name,
                        ErrorMessage = $"Failed to convert value: {ex.Message}",
                        Severity = attribute?.IsOptional == true ? Models.ErrorSeverity.Warning : Models.ErrorSeverity.Error
                    });

                    // Set default value for optional properties
                    if (attribute?.IsOptional == true)
                    {
                        var defaultValue = Utils.AdvancedTypeConverter.ConvertValue("", property.PropertyType);
                        property.SetValue(model, defaultValue);
                    }
                }
            }

            return model;
        }
        
        private static void ValidateProperty(PropertyInfo property, object value, Attributes.CsvValidationAttribute validation, 
            int rowNumber, Models.CsvErrorCollection errors)
        {
            // Required validation
            if (validation.Required && value == null)
            {
                errors.AddError(new Models.CsvError
                {
                    Row = rowNumber,
                    Column = property.Name,
                    Value = value?.ToString() ?? "null",
                    ErrorMessage = validation.ErrorMessage ?? $"Required property '{property.Name}' is null",
                    Severity = Models.ErrorSeverity.Error
                });
                return;
            }
            
            if (value == null) return;
            
            // Range validation for comparable types
            if (validation.MinValue != null && value is IComparable comparable)
            {
                if (comparable.CompareTo(validation.MinValue) < 0)
                {
                    errors.AddError(new Models.CsvError
                    {
                        Row = rowNumber,
                        Column = property.Name,
                        Value = value.ToString(),
                        ErrorMessage = validation.ErrorMessage ?? $"Value {value} is less than minimum {validation.MinValue}",
                        Severity = Models.ErrorSeverity.Warning
                    });
                }
            }
            
            if (validation.MaxValue != null && value is IComparable comparable2)
            {
                if (comparable2.CompareTo(validation.MaxValue) > 0)
                {
                    errors.AddError(new Models.CsvError
                    {
                        Row = rowNumber,
                        Column = property.Name,
                        Value = value.ToString(),
                        ErrorMessage = validation.ErrorMessage ?? $"Value {value} is greater than maximum {validation.MaxValue}",
                        Severity = Models.ErrorSeverity.Warning
                    });
                }
            }
            
            // Regex validation for strings
            if (!string.IsNullOrEmpty(validation.RegexPattern) && value is string stringValue)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(stringValue, validation.RegexPattern))
                {
                    errors.AddError(new Models.CsvError
                    {
                        Row = rowNumber,
                        Column = property.Name,
                        Value = stringValue,
                        ErrorMessage = validation.ErrorMessage ?? $"Value '{stringValue}' does not match pattern '{validation.RegexPattern}'",
                        Severity = Models.ErrorSeverity.Warning
                    });
                }
            }
        }
    }
}