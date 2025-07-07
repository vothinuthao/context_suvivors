using System;
using System.Collections.Generic;
using System.Linq;
using TwoSleepyCats.CSVReader.Models;
using UnityEngine;

namespace TwoSleepyCats.CSVReader.Utils
{
    /// <summary>
    /// Enhanced type converter with custom converter support
    /// </summary>
    public static class AdvancedTypeConverter
    {
        private static readonly Dictionary<Type, ICsvConverter> _customConverters = new Dictionary<Type, ICsvConverter>();
        private static readonly Dictionary<Type, Func<string, object>> _builtInConverters = new Dictionary<Type, Func<string, object>>();
        
        static AdvancedTypeConverter()
        {
            RegisterBuiltInConverters();
        }
        
        private static void RegisterBuiltInConverters()
        {
            // Basic types
            _builtInConverters[typeof(string)] = value => value;
            _builtInConverters[typeof(int)] = value => int.Parse(value);
            _builtInConverters[typeof(float)] = value => float.Parse(value);
            _builtInConverters[typeof(double)] = value => double.Parse(value);
            _builtInConverters[typeof(bool)] = ParseBool;
            _builtInConverters[typeof(DateTime)] = value => DateTime.Parse(value);
            
            _builtInConverters[typeof(Vector2)] = ParseVector2;
            _builtInConverters[typeof(Vector3)] = ParseVector3;
            _builtInConverters[typeof(Vector4)] = ParseVector4;
            _builtInConverters[typeof(Color)] = ParseColor;
            _builtInConverters[typeof(Color32)] = ParseColor32;
            _builtInConverters[typeof(Quaternion)] = ParseQuaternion;
            
            _builtInConverters[typeof(string[])] = value => value.Split(',').Select(s => s.Trim()).ToArray();
            _builtInConverters[typeof(int[])] = value => value.Split(',').Select(s => int.Parse(s.Trim())).ToArray();
            _builtInConverters[typeof(float[])] = value => value.Split(',').Select(s => float.Parse(s.Trim())).ToArray();
        }
        
        public static void RegisterConverter<T>(ICsvConverter converter)
        {
            _customConverters[typeof(T)] = converter;
        }
        
        public static void RegisterConverter<T>(Func<string, T> converter)
        {
            _builtInConverters[typeof(T)] = value => converter(value);
        }
        
        public static object ConvertValue(string value, Type targetType, bool autoConvert = false, ICsvConverter customConverter = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return GetDefaultValue(targetType);
            }

            try
            {
                if (customConverter != null && customConverter.CanConvert(targetType))
                {
                    return customConverter.Convert(value, targetType);
                }
                
                if (_customConverters.ContainsKey(targetType))
                {
                    return _customConverters[targetType].Convert(value, targetType);
                }
                
                // Handle nullable types
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var underlyingType = Nullable.GetUnderlyingType(targetType);
                    var convertedValue = ConvertValue(value, underlyingType, autoConvert, customConverter);
                    return convertedValue;
                }

                if (_builtInConverters.TryGetValue(targetType, out var converter))
                {
                    return converter(value);
                }
                
                if (targetType.IsEnum)
                {
                    return Enum.Parse(targetType, value, true);
                }
                
                // Handle arrays
                if (targetType.IsArray)
                {
                    return ConvertArray(value, targetType);
                }
                
                // Handle generic collections
                if (targetType.IsGenericType)
                {
                    return ConvertGenericCollection(value, targetType);
                }

                // Fallback: try Convert.ChangeType
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AdvancedTypeConverter] Failed to convert '{value}' to {targetType.Name}: {ex.Message}");
                return GetDefaultValue(targetType);
            }
        }
        
        private static object ConvertArray(string value, Type arrayType)
        {
            var elementType = arrayType.GetElementType();
            var parts = value.Split(',').Select(s => s.Trim()).ToArray();
            
            var array = Array.CreateInstance(elementType, parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                var convertedElement = ConvertValue(parts[i], elementType);
                array.SetValue(convertedElement, i);
            }
            
            return array;
        }
        
        private static object ConvertGenericCollection(string value, Type collectionType)
        {
            if (collectionType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = collectionType.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = Activator.CreateInstance(listType);
                var addMethod = listType.GetMethod("Add");
                
                var parts = value.Split(',').Select(s => s.Trim()).ToArray();
                foreach (var part in parts)
                {
                    var convertedElement = ConvertValue(part, elementType);
                    addMethod.Invoke(list, new[] { convertedElement });
                }
                
                return list;
            }
            
            return GetDefaultValue(collectionType);
        }
        
        private static object ParseBool(string value)
        {
            value = value.ToLowerInvariant();
            return value == "true" || value == "1" || value == "yes" || value == "on" || value == "enabled";
        }
        
        private static object ParseVector2(string value)
        {
            var parts = value.Split(',');
            if (parts.Length >= 2)
            {
                return new Vector2(float.Parse(parts[0].Trim()), float.Parse(parts[1].Trim()));
            }
            return Vector2.zero;
        }
        
        private static object ParseVector3(string value)
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
        
        private static object ParseVector4(string value)
        {
            var parts = value.Split(',');
            if (parts.Length >= 4)
            {
                return new Vector4(
                    float.Parse(parts[0].Trim()),
                    float.Parse(parts[1].Trim()),
                    float.Parse(parts[2].Trim()),
                    float.Parse(parts[3].Trim())
                );
            }
            return Vector4.zero;
        }
        
        private static object ParseColor(string value)
        {
            // Try parsing as hex color
            if (value.StartsWith("#"))
            {
                if (ColorUtility.TryParseHtmlString(value, out Color color))
                    return color;
            }
            
            // Try RGBA format: "1,0,0,1"
            var parts = value.Split(',');
            if (parts.Length >= 3)
            {
                var r = float.Parse(parts[0].Trim());
                var g = float.Parse(parts[1].Trim());
                var b = float.Parse(parts[2].Trim());
                var a = parts.Length >= 4 ? float.Parse(parts[3].Trim()) : 1f;
                return new Color(r, g, b, a);
            }
            
            // Try parsing as color name
            return ParseColorByName(value);
        }
        
        private static object ParseColor32(string value)
        {
            var color = (Color)ParseColor(value);
            return (Color32)color;
        }
        
        private static object ParseQuaternion(string value)
        {
            var parts = value.Split(',');
            if (parts.Length >= 4)
            {
                return new Quaternion(
                    float.Parse(parts[0].Trim()),
                    float.Parse(parts[1].Trim()),
                    float.Parse(parts[2].Trim()),
                    float.Parse(parts[3].Trim())
                );
            }
            else if (parts.Length >= 3)
            {
                // Euler angles
                var euler = new Vector3(
                    float.Parse(parts[0].Trim()),
                    float.Parse(parts[1].Trim()),
                    float.Parse(parts[2].Trim())
                );
                return Quaternion.Euler(euler);
            }
            return Quaternion.identity;
        }
        
        private static Color ParseColorByName(string value)
        {
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
                case "gray": case "grey": return Color.gray;
                case "clear": return Color.clear;
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
}