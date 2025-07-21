using OctoberStudio.Save;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OctoberStudio.Equipment
{
    [System.Serializable]
    public class EquipmentSave : ISave
    {
        [System.Serializable]
        public class EquippedItem
        {
            [SerializeField] public EquipmentType equipmentType;
            [SerializeField] public int equipmentId = -1;
            [SerializeField] public int level = 1;
            [SerializeField] public string uid = ""; // UID of the equipped item

            public EquippedItem() { }

            public EquippedItem(EquipmentType type, int id, int lvl, string itemUid = "")
            {
                equipmentType = type;
                equipmentId = id;
                level = lvl;
                uid = itemUid;
            }
        }

        [System.Serializable]
        public class InventoryItem
        {
            [SerializeField] public string uid; // Unique identifier for this specific item
            [SerializeField] public EquipmentType equipmentType;
            [SerializeField] public int equipmentId;
            [SerializeField] public int level = 1;
            [SerializeField] public DateTime createdAt; // When this item was created
            [SerializeField] public string createdAtString; // Serializable version of DateTime

            public InventoryItem() 
            {
                uid = UIDGenerator.GenerateInventoryItemUID();
                createdAt = DateTime.Now;
                createdAtString = createdAt.ToBinary().ToString();
            }

            public InventoryItem(EquipmentType type, int id, int lvl)
            {
                uid = UIDGenerator.GenerateInventoryItemUID();
                equipmentType = type;
                equipmentId = id;
                level = lvl;
                createdAt = DateTime.Now;
                createdAtString = createdAt.ToBinary().ToString();
            }

            // Restore DateTime from string on deserialization
            public void RestoreDateTime()
            {
                if (!string.IsNullOrEmpty(createdAtString))
                {
                    try
                    {
                        long binary = Convert.ToInt64(createdAtString);
                        createdAt = DateTime.FromBinary(binary);
                    }
                    catch
                    {
                        createdAt = DateTime.Now;
                        createdAtString = createdAt.ToBinary().ToString();
                    }
                }
                else
                {
                    createdAt = DateTime.Now;
                    createdAtString = createdAt.ToBinary().ToString();
                }
            }
        }

        [SerializeField] public EquippedItem[] equippedItems = new EquippedItem[6];
        [SerializeField] public InventoryItem[] inventoryItems = new InventoryItem[0];

        // Cached inventory list for performance
        private List<InventoryItem> _inventoryList;

        // Property để dễ sử dụng như List nhưng sync với array
        public List<InventoryItem> inventory
        {
            get
            {
                if (_inventoryList == null)
                {
                    _inventoryList = new List<InventoryItem>(inventoryItems);
                    foreach (var item in _inventoryList)
                    {
                        item.RestoreDateTime();
                    }
                }
                return _inventoryList;
            }
        }

        // Method để sync List về Array khi cần save
        private void SyncInventoryToArray()
        {
            if (_inventoryList != null)
            {
                inventoryItems = _inventoryList.ToArray();
            }
        }

        // Public method để force sync nếu cần
        public void ForceSync()
        {
            SyncInventoryToArray();
        }

        public void Init()
        {
            if (equippedItems == null || equippedItems.Length != 6)
            {
                equippedItems = new EquippedItem[6];
                for (int i = 0; i < 6; i++)
                {
                    equippedItems[i] = new EquippedItem((EquipmentType)i, -1, 1);
                }
            }

            // Initialize inventory if null
            if (inventoryItems == null)
            {
                inventoryItems = new InventoryItem[0];
            }

            // Ensure all equipped items exist
            for (int i = 0; i < 6; i++)
            {
                if (equippedItems[i] == null)
                {
                    equippedItems[i] = new EquippedItem((EquipmentType)i, -1, 1);
                }
            }
            _inventoryList = new List<InventoryItem>(inventoryItems);
            var existingUIDs = new List<string>();
            
            foreach (var item in _inventoryList)
            {
                item.RestoreDateTime();
                if (string.IsNullOrEmpty(item.uid))
                {
                    item.uid = UIDGenerator.GenerateInventoryItemUID();
                }
                else
                {
                    existingUIDs.Add(item.uid);
                }
            }
            if (existingUIDs.Count > 0)
            {
                UIDGenerator.RegisterExistingUIDs(existingUIDs);
            }
            
            // Also register equipped item UIDs
            var equippedUIDs = new List<string>();
            foreach (var equipped in equippedItems)
            {
                if (!string.IsNullOrEmpty(equipped.uid))
                {
                    equippedUIDs.Add(equipped.uid);
                }
            }
            
            if (equippedUIDs.Count > 0)
            {
                UIDGenerator.RegisterExistingUIDs(equippedUIDs);
            }
        }

        public void Flush()
        {
            // Update createdAtString for all items before saving
            foreach (var item in inventory)
            {
                item.createdAtString = item.createdAt.ToBinary().ToString();
            }
            
            // Sync inventory list to array before save
            SyncInventoryToArray();
        }

        public void Clear()
        {
            // Clear all equipped items
            for (int i = 0; i < 6; i++)
            {
                if (equippedItems[i] != null)
                {
                    equippedItems[i].equipmentId = -1;
                    equippedItems[i].level = 1;
                    equippedItems[i].uid = "";
                }
            }

            // Clear inventory list and array
            if (_inventoryList != null)
            {
                _inventoryList.Clear();
            }
            inventoryItems = new InventoryItem[0];
        }

        // Get equipped item for specific slot
        public EquippedItem GetEquippedItem(EquipmentType type)
        {
            int index = (int)type;
            if (index >= 0 && index < equippedItems.Length)
            {
                return equippedItems[index];
            }
            return new EquippedItem(type, -1, 1);
        }

        // Set equipped item for specific slot using UID
        public void SetEquippedItem(EquipmentType type, int equipmentId, int level = 1, string itemUid = "")
        {
            int index = (int)type;
            if (index >= 0 && index < equippedItems.Length)
            {
                equippedItems[index].equipmentId = equipmentId;
                equippedItems[index].level = level;
                equippedItems[index].uid = itemUid;
            }
        }

        // Remove equipped item from slot
        public void UnequipItem(EquipmentType type)
        {
            SetEquippedItem(type, -1, 1, "");
        }

        // Add item to inventory (creates new unique item)
        public InventoryItem AddToInventory(EquipmentType type, int equipmentId, int level = 1)
        {
            var newItem = new InventoryItem(type, equipmentId, level);
            inventory.Add(newItem);
            SyncInventoryToArray();
            return newItem;
        }

        // Add multiple items to inventory
        public List<InventoryItem> AddMultipleToInventory(EquipmentType type, int equipmentId, int level = 1, int count = 1)
        {
            var addedItems = new List<InventoryItem>();
            for (int i = 0; i < count; i++)
            {
                var newItem = AddToInventory(type, equipmentId, level);
                addedItems.Add(newItem);
            }
            return addedItems;
        }

        // Remove specific item by UID
        public bool RemoveFromInventory(string itemUid)
        {
            var item = inventory.FirstOrDefault(i => i.uid == itemUid);
            if (item != null)
            {
                inventory.Remove(item);
                SyncInventoryToArray();
                return true;
            }
            return false;
        }

        // Remove item by type and ID (removes first matching item)
        public bool RemoveFromInventory(EquipmentType type, int equipmentId, int level = 1)
        {
            var item = inventory.FirstOrDefault(i => 
                i.equipmentType == type && 
                i.equipmentId == equipmentId && 
                i.level == level);

            if (item != null)
            {
                inventory.Remove(item);
                SyncInventoryToArray();
                return true;
            }
            return false;
        }

        // Get item by UID
        public InventoryItem GetItemByUID(string uid)
        {
            return inventory.FirstOrDefault(i => i.uid == uid);
        }

        // Get all items of specific type and ID
        public List<InventoryItem> GetItemsByTypeAndId(EquipmentType type, int equipmentId, int level = -1)
        {
            if (level == -1)
            {
                return inventory.Where(i => i.equipmentType == type && i.equipmentId == equipmentId).ToList();
            }
            else
            {
                return inventory.Where(i => i.equipmentType == type && i.equipmentId == equipmentId && i.level == level).ToList();
            }
        }

        // Get count of specific equipment
        public int GetItemCount(EquipmentType type, int equipmentId, int level = -1)
        {
            return GetItemsByTypeAndId(type, equipmentId, level).Count;
        }

        // Get equipped item UID
        public string GetEquippedItemUID(EquipmentType type)
        {
            var equippedItem = GetEquippedItem(type);
            return equippedItem?.uid ?? "";
        }

        // Check if item is currently equipped
        public bool IsItemEquipped(string itemUid)
        {
            if (string.IsNullOrEmpty(itemUid)) return false;
            
            return equippedItems.Any(equipped => equipped.uid == itemUid);
        }

        // Get inventory statistics
        public Dictionary<EquipmentType, int> GetInventoryStats()
        {
            var stats = new Dictionary<EquipmentType, int>();
            
            foreach (EquipmentType type in Enum.GetValues(typeof(EquipmentType)))
            {
                stats[type] = inventory.Count(i => i.equipmentType == type);
            }
            
            return stats;
        }

        // Sort inventory by creation date (newest first)
        public void SortInventoryByDate(bool newestFirst = true)
        {
            if (newestFirst)
            {
                inventory.Sort((a, b) => b.createdAt.CompareTo(a.createdAt));
            }
            else
            {
                inventory.Sort((a, b) => a.createdAt.CompareTo(b.createdAt));
            }
            SyncInventoryToArray();
        }

        // Sort inventory by equipment type and ID
        public void SortInventoryByType()
        {
            inventory.Sort((a, b) => 
            {
                int typeCompare = a.equipmentType.CompareTo(b.equipmentType);
                if (typeCompare != 0) return typeCompare;
                
                int idCompare = a.equipmentId.CompareTo(b.equipmentId);
                if (idCompare != 0) return idCompare;
                
                return b.level.CompareTo(a.level); // Higher level first
            });
            SyncInventoryToArray();
        }
    }
}