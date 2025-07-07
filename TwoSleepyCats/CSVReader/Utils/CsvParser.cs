using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TwoSleepyCats.CSVReader.Utils
{
    public class CsvParser
    {
        public static string[] ParseLine(string line, char delimiter = ',')
        {
            if (string.IsNullOrEmpty(line))
                return Array.Empty<string>();

            List<string> result = new List<string>();
            bool inQuotes = false;
            StringBuilder currentField = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == delimiter && !inQuotes)
                {
                    result.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            result.Add(currentField.ToString().Trim());
            return result.ToArray();
        }

        public static string[] ParseHeader(string headerLine, char delimiter = ',')
        {
            var headers = ParseLine(headerLine, delimiter);
            for (int i = 0; i < headers.Length; i++)
            {
                headers[i] = headers[i].Trim(' ', '"', '\t');
                if (string.IsNullOrEmpty(headers[i]))
                {
                    headers[i] = $"Column_{i}";
                }
            }
            
            return headers;
        }

        public static bool IsLineEmpty(string line)
        {
            return string.IsNullOrWhiteSpace(line);
        }

        public static bool IsLineComment(string line, string commentPrefix = "#")
        {
            return line.TrimStart().StartsWith(commentPrefix);
        }
    }

    public static class BasicTypeConverter
    {
        public static object ConvertValue(string value, Type targetType, bool autoConvert = false)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return GetDefaultValue(targetType);
            }

            try
            {
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var underlyingType = Nullable.GetUnderlyingType(targetType);
                    var convertedValue = ConvertValue(value, underlyingType, autoConvert);
                    return convertedValue;
                }

                if (targetType == typeof(string))
                    return value;
                else if (targetType == typeof(int))
                    return int.Parse(value);
                else if (targetType == typeof(float))
                    return float.Parse(value);
                else if (targetType == typeof(double))
                    return double.Parse(value);
                else if (targetType == typeof(bool))
                    return ParseBool(value);
                else if (targetType.IsEnum)
                    return Enum.Parse(targetType, value, true);
                else if (targetType == typeof(DateTime))
                    return DateTime.Parse(value);

                else if (targetType == typeof(Vector3))
                    return ParseVector3(value);
                else if (targetType == typeof(Vector2))
                    return ParseVector2(value);
                else if (targetType == typeof(Color))
                    return ParseColor(value);

                return Convert.ChangeType(value, targetType);
            }
            catch (Exception)
            {
                return GetDefaultValue(targetType);
            }
        }

        private static bool ParseBool(string value)
        {
            value = value.ToLowerInvariant();
            return value == "true" || value == "1" || value == "yes" || value == "on";
        }

        private static Vector3 ParseVector3(string value)
        {
            var parts = value.Split(',');
            if (parts.Length >= 3)
            {
                return new Vector3(
                    float.Parse(parts[0].Trim()),
                    float.Parse(parts[1].Trim()),
                    float.Parse(parts[2].Trim())
                );
            }
            return Vector3.zero;
        }

        private static Vector2 ParseVector2(string value)
        {
            var parts = value.Split(',');
            if (parts.Length >= 2)
            {
                return new Vector2(
                    float.Parse(parts[0].Trim()),
                    float.Parse(parts[1].Trim())
                );
            }
            return Vector2.zero;
        }

        private static Color ParseColor(string value)
        {
            if (value.StartsWith("#"))
            {
                if (ColorUtility.TryParseHtmlString(value, out Color color))
                    return color;
            }
            
            switch (value.ToLowerInvariant())
            {
                case "red": return Color.red;
                case "green": return Color.green;
                case "blue": return Color.blue;
                case "white": return Color.white;
                case "black": return Color.black;
                case "yellow": return Color.yellow;
                case "cyan": return Color.cyan;
                case "magenta": return Color.magenta;
                default: return Color.white;
            }
        }

        private static object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }
    }

    public static class ReflectionExtensions
    {
        public static bool HasAttribute<T>(this PropertyInfo property) where T : Attribute
        {
            return property.GetCustomAttribute<T>() != null;
        }
    }
}