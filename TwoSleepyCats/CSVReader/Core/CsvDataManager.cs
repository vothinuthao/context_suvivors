// =============================================================================
// Two Sleepy Cats Studio - Professional CSV Reader Module
// 
// Author: Two Sleepy Cats Development Team
// Version: 1.0.0 - Core Foundation + Advanced Features
// Created: 2025
// 
// A lightweight, generic CSV reading system for Unity 6.
// Designed for performance, flexibility, and developer happiness.
// 
// Features: Generic loading, smart type conversion, rich error handling,
//          relationship mapping, and intelligent caching system.
//
// Usage: One-line loading -> CSVDataManager.Instance.Get<YourModel>()
// 
// Sweet dreams and happy coding! 😸💤
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwoSleepyCats.CSVReader.Models;
using TwoSleepyCats.CSVReader.Utils;
using TwoSleepyCats.Patterns.Singleton;
using UnityEngine;

namespace TwoSleepyCats.CSVReader.Core
{
    /// <summary>
    /// Unified CSV Data Manager with both basic and advanced features
    /// </summary>
    public class CsvDataManager : Singleton<CsvDataManager>
    {
        // =================================================================
        // BASIC CACHE (PHASE 1)
        // =================================================================
        private readonly Dictionary<Type, object> _dataCache = new Dictionary<Type, object>();
        private readonly Dictionary<Type, CsvErrorCollection> _errorCache = new Dictionary<Type, CsvErrorCollection>();
        private readonly object _cacheLock = new object();
        
        // =================================================================
        // ADVANCED CACHE (PHASE 2)
        // =================================================================
        private readonly AdvancedCacheManager _advancedCache = new AdvancedCacheManager();
        private readonly Dictionary<Type, CsvLoadingProgress> _loadingProgress = new Dictionary<Type, CsvLoadingProgress>();
        
        // Events for progress tracking
        public event Action<CsvLoadingProgress> OnLoadingProgress;
        public event Action<Type, List<object>, CsvErrorCollection> OnDataLoaded;

        // =================================================================
        // INITIALIZATION
        // =================================================================
        protected override void Initialize()
        {
        }

        /// <summary>
        /// Load CSV data asynchronously and cache it (Phase 1 method)
        /// </summary>
        public async Task<List<T>> LoadAsync<T>() where T : ICsvModel, new()
        {
            var type = typeof(T);
            
            lock (_cacheLock)
            {
                // Return cached data if available
                if (_dataCache.TryGetValue(type, out var value))
                {
                    return value as List<T>;
                }
            }

            // Load data
            var (data, errors) = await CsvReader<T>.LoadAsync();

            lock (_cacheLock)
            {
                _dataCache[type] = data;
                _errorCache[type] = errors;
            }

            // Log errors
            if (errors.Errors.Count > 0)
            {
                Debug.LogWarning($"[CSVDataManager] Loaded {typeof(T).Name} with {errors.Errors.Count} issues:");
                errors.LogToConsole();
            }
            else
            {
                Debug.Log($"[CSVDataManager] Successfully loaded {data.Count} {typeof(T).Name} records");
            }

            return data;
        }

        /// <summary>
        /// Get cached data (load first if not cached)
        /// </summary>
        public List<T> Get<T>() where T : ICsvModel, new()
        {
            var type = typeof(T);
            
            lock (_cacheLock)
            {
                if (_dataCache.TryGetValue(type, out var value))
                {
                    return value as List<T>;
                }
            }
            var task = LoadAsync<T>();
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Check if data type is already loaded
        /// </summary>
        public bool IsLoaded<T>() where T : ICsvModel
        {
            lock (_cacheLock)
            {
                return _dataCache.ContainsKey(typeof(T));
            }
        }

        /// <summary>
        /// Get errors from last load operation
        /// </summary>
        public CsvError[] GetErrors<T>() where T : ICsvModel
        {
            lock (_cacheLock)
            {
                var type = typeof(T);
                if (_errorCache.TryGetValue(type, out var value))
                {
                    return value.Errors.ToArray();
                }
                return Array.Empty<CsvError>();
            }
        }

        /// <summary>
        /// Preload all known CSV model types (Phase 1 method)
        /// </summary>
        public async Task PreloadAllAsync()
        {
            var csvModelTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(ICsvModel).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                .ToArray();

            Debug.Log($"[CSVDataManager] Preloading {csvModelTypes.Length} CSV model types...");

            var tasks = new List<Task>();
            foreach (var type in csvModelTypes)
            {
                var method = typeof(CsvDataManager).GetMethod(nameof(LoadAsync))?.MakeGenericMethod(type);
                if (method == null) continue;
                var task = (Task)method.Invoke(this, null);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
            Debug.Log("[CSVDataManager] Preloading completed");
        }

        /// <summary>
        /// Load CSV data with relationships and progress tracking (Phase 2 method)
        /// </summary>
        public async Task<List<T>> LoadWithRelationshipsAsync<T>(IProgress<CsvLoadingProgress> progress = null) 
            where T : ICsvModel, new()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var type = typeof(T);
            
            // Check advanced cache first
            if (_advancedCache.Contains(type.Name))
            {
                stopwatch.Stop();
                return _advancedCache.Get<List<T>>(type.Name);
            }
            
            try
            {
                // Load main data using Phase 1 method
                var data = await LoadAsync<T>();
                
                // Create progress tracker
                var progressInfo = new CsvLoadingProgress
                {
                    FileName = new T().GetCsvFileName(),
                    TotalRows = data.Count,
                    ProcessedRows = 0,
                    Status = "Resolving relationships..."
                };
                
                progress?.Report(progressInfo);
                OnLoadingProgress?.Invoke(progressInfo);
                
                // Resolve relationships
                var context = new CsvRelationshipContext();
                await CsvRelationshipResolver.ResolveRelationshipsAsync(data, context);
                
                progressInfo.ProcessedRows = data.Count;
                progressInfo.Status = "Complete";
                progressInfo.ElapsedTime = stopwatch.Elapsed;
                
                progress?.Report(progressInfo);
                OnLoadingProgress?.Invoke(progressInfo);
                
                // Cache with load time in advanced cache
                stopwatch.Stop();
                _advancedCache.Set(type.Name, data, stopwatch.Elapsed);
                
                // Fire event
                OnDataLoaded?.Invoke(type, data.Cast<object>().ToList(), GetErrorCollection<T>());
                
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CSVDataManager] Failed to load {typeof(T).Name} with relationships: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Preload multiple types with progress tracking (Phase 2 method)
        /// </summary>
        public async Task PreloadWithProgressAsync(IProgress<string> progress = null, params Type[] types)
        {
            if (types == null || types.Length == 0)
            {
                // Auto-discover all CSV model types
                types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(type => typeof(ICsvModel).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    .ToArray();
            }
            
            progress?.Report($"Preloading {types.Length} CSV types...");
            
            var tasks = new List<Task>();
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                progress?.Report($"Loading {type.Name} ({i + 1}/{types.Length})...");
                
                var method = typeof(CsvDataManager).GetMethod(nameof(LoadWithRelationshipsAsync))?.MakeGenericMethod(type);
                if (method != null)
                {
                    var task = (Task)method.Invoke(this, new object[] { null });
                    tasks.Add(task);
                }
                
                // Add small delay to prevent overwhelming
                await Task.Delay(10);
            }
            
            await Task.WhenAll(tasks);
            progress?.Report("Preloading completed!");
        }
        
        /// <summary>
        /// Get advanced cache statistics (Phase 2 method)
        /// </summary>
        public CsvCacheStats GetCacheStats()
        {
            return _advancedCache.GetStats();
        }
        
        /// <summary>
        /// Get loading progress for specific type (Phase 2 method)
        /// </summary>
        public CsvLoadingProgress GetLoadingProgress<T>() where T : ICsvModel
        {
            var type = typeof(T);
            return _loadingProgress.TryGetValue(type, out var value) ? value : null;
        }
        
        /// <summary>
        /// Helper method to convert errors to error collection
        /// </summary>
        private CsvErrorCollection GetErrorCollection<T>() where T : ICsvModel
        {
            var errors = GetErrors<T>();
            var collection = new CsvErrorCollection();
            foreach (var error in errors)
            {
                collection.AddError(error);
            }
            return collection;
        }

        // =================================================================
        // UNIFIED METHODS (BOTH PHASES)
        // =================================================================

        /// <summary>
        /// Clear all cached data (unified method for both basic and advanced cache)
        /// </summary>
        public void ClearCache()
        {
            // Clear basic cache (Phase 1)
            lock (_cacheLock)
            {
                _dataCache.Clear();
                _errorCache.Clear();
            }
            
            // Clear advanced cache (Phase 2)
            _advancedCache.Clear();
            _loadingProgress.Clear();
            
            Debug.Log("[CSVDataManager] All caches cleared (basic + advanced)");
        }

        /// <summary>
        /// Dispose pattern implementation
        /// </summary>
        public override void Dispose()
        {
            ClearCache();
            base.Dispose();
        }

        // =================================================================
        // DEBUG & UTILITY METHODS
        // =================================================================

        /// <summary>
        /// Get comprehensive cache information for debugging
        /// </summary>
        public string GetCacheInfo()
        {
            var info = new System.Text.StringBuilder();
            
            lock (_cacheLock)
            {
                info.AppendLine($"Basic Cache Entries: {_dataCache.Count}");
                info.AppendLine($"Error Cache Entries: {_errorCache.Count}");
            }
            
            var advancedStats = _advancedCache.GetStats();
            info.AppendLine($"Advanced Cache Entries: {advancedStats.TotalEntries}");
            info.AppendLine($"Cache Hit Rate: {advancedStats.HitRate:P1}");
            info.AppendLine($"Memory Usage: {advancedStats.MemoryUsageBytes / 1024 / 1024:F2} MB");
            
            return info.ToString();
        }

        /// <summary>
        /// Check if any data is loaded
        /// </summary>
        public bool HasAnyData()
        {
            lock (_cacheLock)
            {
                return _dataCache.Count > 0;
            }
        }

        /// <summary>
        /// Get all loaded type names
        /// </summary>
        public string[] GetLoadedTypeNames()
        {
            lock (_cacheLock)
            {
                return _dataCache.Keys.Select(t => t.Name).ToArray();
            }
        }

        /// <summary>
        /// Force reload specific type (clears cache and reloads)
        /// </summary>
        public async Task<List<T>> ForceReloadAsync<T>() where T : ICsvModel, new()
        {
            var type = typeof(T);
            
            // Clear from all caches
            lock (_cacheLock)
            {
                _dataCache.Remove(type);
                _errorCache.Remove(type);
            }
            _advancedCache.Remove(type.Name);
            _loadingProgress.Remove(type);
            
            // Reload
            return await LoadAsync<T>();
        }
    }
}