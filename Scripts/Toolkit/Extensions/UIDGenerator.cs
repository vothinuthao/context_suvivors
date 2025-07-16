using System;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio.Equipment
{
    /// <summary>
    /// Professional UID Generator for Equipment System
    /// Ensures unique IDs across the entire game session and provides collision detection
    /// </summary>
    public static class UIDGenerator
    {
        // Cache to track generated UIDs and prevent collisions
        private static readonly HashSet<string> GeneratedUIDs = new HashSet<string>();
        
        // Different UID formats for different purposes
        public enum UIDFormat
        {
            Short,      // 8 characters (for UI display)
            Standard,   // 16 characters (default)
            Long,       // 32 characters (maximum uniqueness)
            Timestamped // Includes timestamp for chronological sorting
        }

        /// <summary>
        /// Generate a unique ID for equipment items
        /// </summary>
        /// <param name="format">The format of UID to generate</param>
        /// <param name="prefix">Optional prefix for categorization (e.g., "EQ_")</param>
        /// <returns>Unique identifier string</returns>
        public static string GenerateUID(UIDFormat format = UIDFormat.Standard, string prefix = "")
        {
            string uid;
            int maxAttempts = 1000; // Prevent infinite loops in edge cases
            int attempts = 0;

            do
            {
                uid = GenerateUIDInternal(format, prefix);
                attempts++;
                
                if (attempts >= maxAttempts)
                {
                    // Fallback to long format if we can't generate unique ID
                    Debug.LogWarning($"[UIDGenerator] Could not generate unique {format} UID after {maxAttempts} attempts. Using long format.");
                    uid = GenerateUIDInternal(UIDFormat.Long, prefix);
                    break;
                }
            }
            while (GeneratedUIDs.Contains(uid));

            // Cache the generated UID
            GeneratedUIDs.Add(uid);
            
            return uid;
        }

        /// <summary>
        /// Internal UID generation based on format
        /// </summary>
        private static string GenerateUIDInternal(UIDFormat format, string prefix)
        {
            var guid = Guid.NewGuid();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            string baseUID = format switch
            {
                UIDFormat.Short => guid.ToString("N")[..8],
                UIDFormat.Standard => guid.ToString("N")[..16],
                UIDFormat.Long => guid.ToString("N"),
                UIDFormat.Timestamped => $"{timestamp:X8}{guid.ToString("N")[..8]}",
                _ => guid.ToString("N")[..16]
            };

            return string.IsNullOrEmpty(prefix) ? baseUID : $"{prefix}{baseUID}";
        }

        /// <summary>
        /// Generate UID specifically for equipment items
        /// </summary>
        public static string GenerateEquipmentUID()
        {
            return GenerateUID(UIDFormat.Standard, "EQ_");
        }

        /// <summary>
        /// Generate UID for inventory items with timestamp
        /// </summary>
        public static string GenerateInventoryItemUID()
        {
            return GenerateUID(UIDFormat.Timestamped, "INV_");
        }

        /// <summary>
        /// Validate if a UID follows the expected format
        /// </summary>
        public static bool IsValidUID(string uid)
        {
            if (string.IsNullOrEmpty(uid))
                return false;

            // Remove any prefix for validation
            string cleanUID = uid;
            if (uid.Contains("_"))
            {
                var parts = uid.Split('_');
                if (parts.Length == 2)
                    cleanUID = parts[1];
            }

            // Check if it's a valid hex string of appropriate length
            return cleanUID.Length >= 8 && 
                   cleanUID.Length <= 32 && 
                   IsHexString(cleanUID);
        }

        /// <summary>
        /// Check if string contains only hexadecimal characters
        /// </summary>
        private static bool IsHexString(string input)
        {
            foreach (char c in input)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Extract timestamp from timestamped UID
        /// </summary>
        public static DateTime? GetTimestampFromUID(string uid)
        {
            if (string.IsNullOrEmpty(uid))
                return null;

            try
            {
                // Remove prefix if exists
                string cleanUID = uid;
                if (uid.Contains("_"))
                {
                    var parts = uid.Split('_');
                    if (parts.Length == 2)
                        cleanUID = parts[1];
                }

                // Check if it's a timestamped UID (first 8 characters should be hex timestamp)
                if (cleanUID.Length >= 16)
                {
                    string timestampHex = cleanUID[..8];
                    if (long.TryParse(timestampHex, System.Globalization.NumberStyles.HexNumber, null, out long timestamp))
                    {
                        return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIDGenerator] Failed to extract timestamp from UID {uid}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Get statistics about generated UIDs
        /// </summary>
        public static UIDStatistics GetStatistics()
        {
            return new UIDStatistics
            {
                TotalGenerated = GeneratedUIDs.Count,
                CacheSize = GeneratedUIDs.Count,
                LastGeneratedAt = DateTime.Now // This would need to be tracked separately for accuracy
            };
        }

        /// <summary>
        /// Clear the UID cache (use with caution, only for testing/debugging)
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void ClearCache()
        {
            GeneratedUIDs.Clear();
            Debug.Log("[UIDGenerator] UID cache cleared");
        }

        /// <summary>
        /// Statistics about UID generation
        /// </summary>
        public struct UIDStatistics
        {
            public int TotalGenerated;
            public int CacheSize;
            public DateTime LastGeneratedAt;

            public override string ToString()
            {
                return $"UIDs Generated: {TotalGenerated}, Cache Size: {CacheSize}, Last Generated: {LastGeneratedAt:yyyy-MM-dd HH:mm:ss}";
            }
        }

        /// <summary>
        /// Generate batch of UIDs for bulk operations
        /// </summary>
        public static List<string> GenerateBatch(int count, UIDFormat format = UIDFormat.Standard, string prefix = "")
        {
            var uids = new List<string>(count);
            
            for (int i = 0; i < count; i++)
            {
                uids.Add(GenerateUID(format, prefix));
            }

            return uids;
        }

        /// <summary>
        /// Check for potential UID collision without generating
        /// </summary>
        public static bool WouldCollide(string uid)
        {
            return GeneratedUIDs.Contains(uid);
        }

        /// <summary>
        /// Manually register an existing UID to prevent future collisions
        /// Used when loading UIDs from save files
        /// </summary>
        public static void RegisterExistingUID(string uid)
        {
            if (IsValidUID(uid))
            {
                GeneratedUIDs.Add(uid);
            }
            else
            {
                Debug.LogWarning($"[UIDGenerator] Attempted to register invalid UID: {uid}");
            }
        }

        /// <summary>
        /// Register multiple existing UIDs from save data
        /// </summary>
        public static void RegisterExistingUIDs(IEnumerable<string> uids)
        {
            foreach (string uid in uids)
            {
                RegisterExistingUID(uid);
            }
        }
    }
}