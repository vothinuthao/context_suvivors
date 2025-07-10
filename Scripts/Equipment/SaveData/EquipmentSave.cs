using OctoberStudio.Save;
using System.Collections.Generic;
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

            public EquippedItem() { }

            public EquippedItem(EquipmentType type, int id, int lvl)
            {
                equipmentType = type;
                equipmentId = id;
                level = lvl;
            }
        }

        [System.Serializable]
        public class InventoryItem
        {
            [SerializeField] public EquipmentType equipmentType;
            [SerializeField] public int equipmentId;
            [SerializeField] public int level = 1;
            [SerializeField] public int quantity = 1;

            public InventoryItem() { }

            public InventoryItem(EquipmentType type, int id, int lvl, int qty)
            {
                equipmentType = type;
                equipmentId = id;
                level = lvl;
                quantity = qty;
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
            // Initialize equipped slots if null
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

            // Load inventory from array to cached list
            _inventoryList = new List<InventoryItem>(inventoryItems);
        }

        public void Flush()
        {
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

        // Set equipped item for specific slot
        public void SetEquippedItem(EquipmentType type, int equipmentId, int level = 1)
        {
            int index = (int)type;
            if (index >= 0 && index < equippedItems.Length)
            {
                equippedItems[index].equipmentId = equipmentId;
                equippedItems[index].level = level;
            }
        }

        // Remove equipped item from slot
        public void UnequipItem(EquipmentType type)
        {
            SetEquippedItem(type, -1, 1);
        }

        // Add item to inventory
        public void AddToInventory(EquipmentType type, int equipmentId, int level = 1, int quantity = 1)
        {
            // var existingItem = inventory.Find(item => 
            //     item.equipmentType == type && 
            //     item.equipmentId == equipmentId && 
            //     item.level == level);

            // if (existingItem != null)
            // {
            //     existingItem.quantity += quantity;
            // }
            inventory.Add(new InventoryItem(type, equipmentId, level, quantity));

            SyncInventoryToArray();
        }

        // Remove item from inventory
        public bool RemoveFromInventory(EquipmentType type, int equipmentId, int level = 1, int quantity = 1)
        {
            var item = inventory.Find(i => 
                i.equipmentType == type && 
                i.equipmentId == equipmentId && 
                i.level == level);

            if (item != null && item.quantity >= quantity)
            {
                item.quantity -= quantity;
                if (item.quantity <= 0)
                {
                    inventory.Remove(item);
                }

                // Immediately sync to array
                SyncInventoryToArray();
                return true;
            }
            return false;
        }
    }
}