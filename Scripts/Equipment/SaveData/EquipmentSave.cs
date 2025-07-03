using System.Collections.Generic;
using OctoberStudio.Save;
using UnityEngine;

namespace Common.Scripts.Equipment
{
    [System.Serializable]
    public class EquipmentSave : ISave
    {
        [System.Serializable]
        public class EquippedItem
        {
            public EquipmentType equipmentType;
            public int equipmentId = -1;
            public int level = 1;
        }

        [System.Serializable]
        public class InventoryItem
        {
            public EquipmentType equipmentType;
            public int equipmentId;
            public int level = 1;
            public int quantity = 1;
        }
        [SerializeField] public EquippedItem[] equippedItems = new EquippedItem[6];
        [SerializeField] public List<InventoryItem> inventory = new List<InventoryItem>();
        public void Init()
        {
            if (equippedItems == null || equippedItems.Length != 6)
            {
                equippedItems = new EquippedItem[6];
                for (int i = 0; i < 6; i++)
                {
                    equippedItems[i] = new EquippedItem
                    {
                        equipmentType = (EquipmentType)i,
                        equipmentId = -1
                    };
                }
            }
            inventory ??= new List<InventoryItem>();
        }

        public void Flush()
        {
        }

        public void Clear()
        {
            for (int i = 0; i < 6; i++)
            {
                if (equippedItems[i] != null)
                {
                    equippedItems[i].equipmentId = -1;
                    equippedItems[i].level = 1;
                }
            }

            inventory.Clear();
        }

        public EquippedItem GetEquippedItem(EquipmentType type)
        {
            return equippedItems[(int)type];
        }

        // Set equipped item for specific slot
        public void SetEquippedItem(EquipmentType type, int equipmentId, int level = 1)
        {
            equippedItems[(int)type].equipmentId = equipmentId;
            equippedItems[(int)type].level = level;
        }

        // Remove equipped item from slot
        public void UnequipItem(EquipmentType type)
        {
            equippedItems[(int)type].equipmentId = -1;
            equippedItems[(int)type].level = 1;
        }

        // Add item to inventory
        public void AddToInventory(EquipmentType type, int equipmentId, int level = 1, int quantity = 1)
        {
            var existingItem = inventory.Find(item => 
                item.equipmentType == type && 
                item.equipmentId == equipmentId && 
                item.level == level);

            if (existingItem != null)
            {
                existingItem.quantity += quantity;
            }
            else
            {
                inventory.Add(new InventoryItem
                {
                    equipmentType = type,
                    equipmentId = equipmentId,
                    level = level,
                    quantity = quantity
                });
            }
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
                return true;
            }
            return false;
        }
    }
}