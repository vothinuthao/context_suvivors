using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TwoSleepyCats.CSVReader.Core;
using UnityEngine;

namespace TwoSleepyCats.CSVReader.Utils
{
    /// <summary>
    /// Relationship resolver for CSV references
    /// </summary>
    public static class CsvRelationshipResolver
    {
        public static async Task ResolveRelationshipsAsync<T>(List<T> models, Models.CsvRelationshipContext context = null) 
            where T : Core.ICsvModel
        {
            context = context ?? new Models.CsvRelationshipContext();
            
            if (models == null || models.Count == 0) return;
            
            var modelType = typeof(T);
            var properties = modelType.GetProperties()
                .Where(p => p.GetCustomAttribute<Attributes.CsvReferenceAttribute>() != null)
                .ToArray();
            
            foreach (var property in properties)
            {
                var refAttr = property.GetCustomAttribute<Attributes.CsvReferenceAttribute>();
                if (refAttr.IsLazy) continue;
                
                await ResolvePropertyRelationship(models, property, refAttr, context);
            }
        }
        
        private static async Task ResolvePropertyRelationship<T>(List<T> models, PropertyInfo property, 
            Attributes.CsvReferenceAttribute refAttr, Models.CsvRelationshipContext context)
        {
            try
            {
                // Prevent circular references
                if (context.IsCircularReference(refAttr.TargetCsv))
                {
                    Debug.LogWarning($"[CSVRelationshipResolver] Circular reference detected: {refAttr.TargetCsv}");
                    return;
                }
                
                context.PushFile(refAttr.TargetCsv);
                
                // Get target data type
                var targetType = refAttr.IsCollection 
                    ? property.PropertyType.GetGenericArguments()[0] 
                    : property.PropertyType;
                if (!context.LoadedData.ContainsKey(targetType))
                {
                    var loadMethod = typeof(CsvDataManager)
                        .GetMethod(nameof(CsvDataManager.LoadAsync))
                        ?.MakeGenericMethod(targetType);

                    if (loadMethod != null)
                    {
                        var loadTask = (Task)loadMethod.Invoke(CsvDataManager.Instance, null);
                        await loadTask;
                    
                        var resultProperty = loadTask.GetType().GetProperty("Result");
                        var targetData = resultProperty.GetValue(loadTask);
                        context.LoadedData[targetType] = targetData;
                    }
                }
                
                var targetList = (IList)context.LoadedData[targetType];
                
                // Resolve relationships for each model
                foreach (var model in models)
                {
                    ResolveModelRelationship(model, property, refAttr, targetList);
                }
                
                context.PopFile(refAttr.TargetCsv);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CSVRelationshipResolver] Failed to resolve relationship {property.Name}: {ex.Message}");
                context.PopFile(refAttr.TargetCsv);
            }
        }
        
        private static void ResolveModelRelationship(object model, PropertyInfo property, 
            Attributes.CsvReferenceAttribute refAttr, IList targetList)
        {
            // Get primary key value from current model
            var primaryKeyProperty = model.GetType().GetProperty(refAttr.PrimaryKey, 
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            
            if (primaryKeyProperty == null)
            {
                Debug.LogError($"[CSVRelationshipResolver] Primary key property '{refAttr.PrimaryKey}' not found in {model.GetType().Name}");
                return;
            }
            
            var primaryKeyValue = primaryKeyProperty.GetValue(model);
            
            if (refAttr.IsCollection)
            {
                // One-to-Many relationship
                var matchingItems = new List<object>();
                
                foreach (var targetItem in targetList)
                {
                    var foreignKeyProperty = targetItem.GetType().GetProperty(refAttr.ForeignKey,
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    
                    if (foreignKeyProperty != null)
                    {
                        var foreignKeyValue = foreignKeyProperty.GetValue(targetItem);
                        if (Equals(foreignKeyValue, primaryKeyValue))
                        {
                            matchingItems.Add(targetItem);
                        }
                    }
                }
                
                // Create typed list
                var listType = typeof(List<>).MakeGenericType(property.PropertyType.GetGenericArguments()[0]);
                var typedList = Activator.CreateInstance(listType);
                var addMethod = listType.GetMethod("Add");
                
                foreach (var item in matchingItems)
                {
                    addMethod.Invoke(typedList, new[] { item });
                }
                
                property.SetValue(model, typedList);
            }
            else
            {
                // One-to-One relationship
                foreach (var targetItem in targetList)
                {
                    var foreignKeyProperty = targetItem.GetType().GetProperty(refAttr.ForeignKey,
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    
                    if (foreignKeyProperty != null)
                    {
                        var foreignKeyValue = foreignKeyProperty.GetValue(targetItem);
                        if (Equals(foreignKeyValue, primaryKeyValue))
                        {
                            property.SetValue(model, targetItem);
                            break;
                        }
                    }
                }
            }
        }
    }
}