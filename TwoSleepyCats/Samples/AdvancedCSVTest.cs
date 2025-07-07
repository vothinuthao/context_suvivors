using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TwoSleepyCats.CSVReader.Core;
using TwoSleepyCats.CSVReader.Models;
using TwoSleepyCats.Samples.AdvancedSample;
using UnityEngine;

namespace TwoSleepyCats.Samples
{
    public class AdvancedCSVTest : MonoBehaviour
    {
        [Header("Advanced Testing")]
        [SerializeField] private bool loadWithRelationships = true;
        [SerializeField] private bool showProgressUpdates = true;
        [SerializeField] private bool testCustomConverters = true;
    
        [Header("Runtime Data")]
        [SerializeField] private List<AdvancedCharacterModel> characters;
        [SerializeField] private List<GuildModel> guilds;
        [SerializeField] private List<AdvancedItemModel> items;
    
        private void Start()
        {
            StartCoroutine(LoadAllDataCoroutine());
        }
    
        private IEnumerator LoadAllDataCoroutine()
        {
            Debug.Log("=== Advanced CSV Loading Started ===");
        
            // Subscribe to events
            CsvDataManager.Instance.OnLoadingProgress += OnLoadingProgress;
            CsvDataManager.Instance.OnDataLoaded += OnDataLoaded;
        
            if (loadWithRelationships)
            {
                yield return StartCoroutine(LoadWithRelationshipsCoroutine());
            }
            else
            {
                yield return StartCoroutine(LoadBasicDataCoroutine());
            }
        
            // Show final statistics
            ShowCacheStatistics();
            TestDataRelationships();
        
            // Unsubscribe from events
            CsvDataManager.Instance.OnLoadingProgress -= OnLoadingProgress;
            CsvDataManager.Instance.OnDataLoaded -= OnDataLoaded;
        }
    
        private IEnumerator LoadWithRelationshipsCoroutine()
        {
            Debug.Log("--- Loading with Relationships ---");
            var progress = new Progress<CsvLoadingProgress>(OnProgressUpdate);
        
            var characterTask = CsvDataManager.Instance.LoadWithRelationshipsAsync<AdvancedCharacterModel>(progress);
            yield return new WaitUntil(() => characterTask.IsCompleted);
        
            if (!characterTask.IsFaulted)
            {
                characters = characterTask.Result;
                Debug.Log($"✅ Loaded {characters.Count} characters with relationships");
            }
            else
            {
                Debug.LogError($"❌ Failed to load characters: {characterTask.Exception?.GetBaseException().Message}");
            }
        
            // Load remaining data
            yield return StartCoroutine(LoadBasicDataCoroutine());
        }
    
        private IEnumerator LoadBasicDataCoroutine()
        {
            Debug.Log("--- Loading Basic Data ---");
        
            // Load guilds
            var guildTask = CsvDataManager.Instance.LoadAsync<GuildModel>();
            yield return new WaitUntil(() => guildTask.IsCompleted);
        
            if (!guildTask.IsFaulted)
            {
                guilds = guildTask.Result;
                Debug.Log($"✅ Loaded {guilds.Count} guilds");
            }
        
            // Load items
            var itemTask = CsvDataManager.Instance.LoadAsync<AdvancedItemModel>();
            yield return new WaitUntil(() => itemTask.IsCompleted);
        
            if (!itemTask.IsFaulted)
            {
                items = itemTask.Result;
                Debug.Log($"✅ Loaded {items.Count} items");
            }
        }
    
        private void OnLoadingProgress(CsvLoadingProgress progress)
        {
            if (showProgressUpdates)
            {
                Debug.Log($"[Progress] {progress}");
            }
        }
    
        private void OnProgressUpdate(CsvLoadingProgress progress)
        {
            // Update UI progress bar here if needed
            Debug.Log($"[Detailed Progress] {progress.FileName}: {progress.Progress:P1} - {progress.Status}");
        }
    
        private void OnDataLoaded(Type dataType, List<object> data, CsvErrorCollection errors)
        {
            Debug.Log($"[Event] Loaded {dataType.Name}: {data.Count} records");
        
            if (errors.HasWarnings || errors.HasErrors)
            {
                Debug.LogWarning($"[Event] {dataType.Name} has {errors.Errors.Count} issues");
                errors.LogToConsole();
            }
        }
    
        [ContextMenu("Show Cache Statistics")]
        public void ShowCacheStatistics()
        {
            var stats = CsvDataManager.Instance.GetCacheStats();
            Debug.Log("=== Cache Statistics ===");
            Debug.Log(stats.GetSummary());
        }
    
        [ContextMenu("Test Data Relationships")]
        public void TestDataRelationships()
        {
            if (characters == null || characters.Count == 0)
            {
                Debug.LogWarning("No character data to test relationships");
                return;
            }
        
            Debug.Log("=== Testing Data Relationships ===");
        
            foreach (var character in characters.Take(3)) // Test first 3 characters
            {
                Debug.Log($"Character: {character.Name}");
                Debug.Log($"  Guild: {character.Guild?.Name ?? "None"}");
                Debug.Log($"  Inventory: {character.Inventory?.Count ?? 0} items");
            
                if (character.Inventory != null)
                {
                    foreach (var item in character.Inventory.Take(2)) // Show first 2 items
                    {
                        Debug.Log($"    • {item.Name} ({item.Rarity}) - {item.Stats}");
                    }
                }
            }
        }
    
        [ContextMenu("Test Custom Converters")]
        public void TestCustomConverters()
        {
            if (items == null || items.Count == 0)
            {
                Debug.LogWarning("No item data to test converters");
                return;
            }
        
            Debug.Log("=== Testing Custom Converters ===");
        
            foreach (var item in items.Take(5))
            {
                Debug.Log($"Item: {item.Name}");
                Debug.Log($"  Color: {item.ItemColor}");
                Debug.Log($"  Stats: {item.Stats}");
                Debug.Log($"  Tags: [{string.Join(", ", item.Tags ?? new string[0])}]");
            }
        }
    
        [ContextMenu("Preload All With Progress")]
        public async void PreloadAllWithProgress()
        {
            var progress = new Progress<string>(message => Debug.Log($"[Preload] {message}"));
        
            await CsvDataManager.Instance.PreloadWithProgressAsync(progress);
        
            ShowCacheStatistics();
        }
    
        [ContextMenu("Clear All Cache")]
        public void ClearAllCache()
        {
            CsvDataManager.Instance.ClearCache();
            characters = null;
            guilds = null;
            items = null;
            Debug.Log("✅ All cache cleared");
        }
    }
}