using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TwoSleepyCats.CSVReader.Utils
{
    /// <summary>
    /// Advanced cache manager with statistics and performance tracking
    /// </summary>
    public class AdvancedCacheManager
    {
        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();
        private readonly Dictionary<string, DateTime> _cacheTimestamps = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, TimeSpan> _loadTimes = new Dictionary<string, TimeSpan>();
        private int _totalHits = 0;
        private int _totalMisses = 0;
        
        public void Set<T>(string key, T data, TimeSpan loadTime = default)
        {
            _cache[key] = data;
            _cacheTimestamps[key] = DateTime.Now;
            if (loadTime != default)
            {
                _loadTimes[key] = loadTime;
            }
        }
        
        public T Get<T>(string key)
        {
            if (_cache.ContainsKey(key))
            {
                _totalHits++;
                return (T)_cache[key];
            }
            
            _totalMisses++;
            return default(T);
        }
        
        public bool Contains(string key)
        {
            return _cache.ContainsKey(key);
        }
        
        public void Remove(string key)
        {
            _cache.Remove(key);
            _cacheTimestamps.Remove(key);
            _loadTimes.Remove(key);
        }
        
        public void Clear()
        {
            _cache.Clear();
            _cacheTimestamps.Clear();
            _loadTimes.Clear();
            _totalHits = 0;
            _totalMisses = 0;
        }
        
        public Models.CsvCacheStats GetStats()
        {
            var stats = new Models.CsvCacheStats
            {
                TotalEntries = _cache.Count,
                TotalHits = _totalHits,
                TotalMisses = _totalMisses,
                HitRate = _totalHits + _totalMisses > 0 ? (float)_totalHits / (_totalHits + _totalMisses) : 0f
            };
            
            long memoryUsage = 0;
            foreach (var item in _cache.Values)
            {
                if (item is IList list)
                {
                    memoryUsage += list.Count * 100; 
                }
                else
                {
                    memoryUsage += 100;
                }
            }
            stats.MemoryUsageBytes = memoryUsage;
            
            if (_loadTimes.Count > 0)
            {
                var totalTicks = _loadTimes.Values.Sum(t => t.Ticks);
                stats.AverageLoadTime = new TimeSpan(totalTicks / _loadTimes.Count);
            }
            
            foreach (var item in _cache.Values)
            {
                var typeName = item.GetType().Name;
                if (!stats.TypeCounts.TryAdd(typeName, 1))
                    stats.TypeCounts[typeName]++;
            }
            
            return stats;
        }
    }
}