using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

namespace TwoSleepyCats.CSVReader.Core
{
    public static partial class CsvReader<T> where T : ICsvModel, new()
    {
        public static async Task<(List<T> data, Models.CsvErrorCollection errors)> LoadAsync()
        {
            var model = new T();
            var fileName = model.GetCsvFileName();
            var csvPath = $"CSV/{Path.GetFileNameWithoutExtension(fileName)}";
            
            return await LoadFromResourceAsync(csvPath);
        }

        public static async Task<(List<T> data, Models.CsvErrorCollection errors)> LoadFromResourceAsync(string resourcePath)
        {
            var errors = new Models.CsvErrorCollection();
            var data = new List<T>();

            try
            {
                var textAsset = Resources.Load<TextAsset>(resourcePath);
                if (textAsset == null)
                {
                    errors.AddError(new Models.CsvError
                    {
                        Row = 0,
                        Column = "File",
                        Value = resourcePath,
                        ErrorMessage = $"CSV file not found at Resources/{resourcePath}",
                        Severity = Models.ErrorSeverity.Critical
                    });
                    return (data, errors);
                }
                return await ParseCsvContentAsync(textAsset.text, errors);
            }
            catch (Exception ex)
            {
                errors.AddError(new Models.CsvError
                {
                    Row = 0,
                    Column = "System",
                    ErrorMessage = $"Failed to load CSV: {ex.Message}",
                    Severity = Models.ErrorSeverity.Critical
                });
                return (data, errors);
            }
        }

        private static async Task<(List<T> data, Models.CsvErrorCollection errors)> ParseCsvContentAsync(
            string csvContent, Models.CsvErrorCollection errors)
        {
            var data = new List<T>();
            await Task.Run(() =>
            {
                ParseCsvContent(csvContent, data, errors);
            });

            return (data, errors);
        }

        private static void ParseCsvContent(string csvContent, List<T> data, Models.CsvErrorCollection errors)
        {
            var lines = csvContent.Split('\n');
            if (lines.Length == 0)
            {
                errors.AddError(new Models.CsvError
                {
                    Row = 0,
                    Column = "File",
                    ErrorMessage = "CSV file is empty",
                    Severity = Models.ErrorSeverity.Critical
                });
                return;
            }

            // Parse header
            string[] headers = null;
            int startRow = 0;
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (Utils.CsvParser.IsLineEmpty(line) || Utils.CsvParser.IsLineComment(line))
                    continue;
                    
                headers = Utils.CsvParser.ParseHeader(line);
                startRow = i + 1;
                break;
            }

            if (headers == null)
            {
                errors.AddError(new Models.CsvError
                {
                    Row = 0,
                    Column = "Header",
                    ErrorMessage = "No valid header found in CSV",
                    Severity = Models.ErrorSeverity.Critical
                });
                return;
            }

            // Get property mappings
            var propertyMappings = GetPropertyMappings<T>(headers, errors);

            // Parse data rows
            for (int i = startRow; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (Utils.CsvParser.IsLineEmpty(line) || Utils.CsvParser.IsLineComment(line))
                    continue;

                var rowData = Utils.CsvParser.ParseLine(line);
                var model = ParseRow<T>(rowData, headers, propertyMappings, i + 1, errors);
                
                if (model != null)
                {
                    try
                    {
                        model.OnDataLoaded();
                        if (model.ValidateData())
                        {
                            data.Add(model);
                        }
                        else
                        {
                            errors.AddError(new Models.CsvError
                            {
                                Row = i + 1,
                                Column = "Validation",
                                ErrorMessage = "Model validation failed",
                                Severity = Models.ErrorSeverity.Error
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.AddError(new Models.CsvError
                        {
                            Row = i + 1,
                            Column = "Processing",
                            ErrorMessage = $"Error in OnDataLoaded or ValidateData: {ex.Message}",
                            Severity = Models.ErrorSeverity.Error
                        });
                    }
                }
            }
        }

        private static Dictionary<PropertyInfo, (int index, Attributes.CsvColumnAttribute attribute)> GetPropertyMappings<TModel>(
            string[] headers, Models.CsvErrorCollection errors)
        {
            var mappings = new Dictionary<PropertyInfo, (int, Attributes.CsvColumnAttribute)>();
            var properties = typeof(TModel).GetProperties()
                .Where(p => p.CanWrite && !p.HasAttribute<Attributes.CsvIgnoreAttribute>())
                .ToArray();

            foreach (var property in properties)
            {
                var csvAttr = property.GetCustomAttribute<Attributes.CsvColumnAttribute>();
                int columnIndex = -1;

                if (csvAttr != null)
                {
                    if (csvAttr.UseIndex)
                    {
                        // Map by index
                        if (csvAttr.ColumnIndex < headers.Length)
                        {
                            columnIndex = csvAttr.ColumnIndex;
                        }
                    }
                    else
                    {
                        // Map by name
                        columnIndex = Array.FindIndex(headers, h => 
                            string.Equals(h, csvAttr.ColumnName, StringComparison.OrdinalIgnoreCase));
                    }
                }
                else
                {
                    // Auto-map by property name
                    columnIndex = Array.FindIndex(headers, h => 
                        string.Equals(h, property.Name, StringComparison.OrdinalIgnoreCase));
                }

                if (columnIndex >= 0)
                {
                    mappings[property] = (columnIndex, csvAttr);
                }
                else if (csvAttr == null || !csvAttr.IsOptional)
                {
                    // Required column not found
                    errors.AddError(new Models.CsvError
                    {
                        Row = 0,
                        Column = csvAttr?.ColumnName ?? property.Name,
                        ErrorMessage = $"Required column not found for property '{property.Name}'",
                        Severity = Models.ErrorSeverity.Error
                    });
                }
            }

            return mappings;
        }

        private static TModel ParseRow<TModel>(string[] rowData, string[] headers, 
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

                    var convertedValue = Utils.BasicTypeConverter.ConvertValue(
                        value, property.PropertyType, attribute?.AutoConvert ?? false);
                    
                    property.SetValue(model, convertedValue);
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
                        var defaultValue = Utils.BasicTypeConverter.ConvertValue("", property.PropertyType);
                        property.SetValue(model, defaultValue);
                    }
                }
            }

            return model;
        }
    }
}