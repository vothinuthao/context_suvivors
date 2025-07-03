using System.Collections.Generic;
using OctoberStudio.Easing;
using OctoberStudio.Equipment;
using OctoberStudio.Equipment.UI;
using OctoberStudio.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Common.Scripts.Equipment.UI
{
    // Main equipment window behavior
    public class EquipmentWindowBehavior : MonoBehaviour
    {
        [Header("Equipment Slots")]
        [SerializeField] private EquipmentSlotBehavior[] leftEquipmentSlots = new EquipmentSlotBehavior[3];  // Hat, Armor, Ring
        [SerializeField] private EquipmentSlotBehavior[] rightEquipmentSlots = new EquipmentSlotBehavior[3]; // Necklace, Belt, Shoes

        [Header("Inventory")]
        [SerializeField] private GameObject inventoryItemPrefab;
        [SerializeField] private RectTransform inventoryContent;
        [SerializeField] private ScrollRect inventoryScrollRect;

        [Header("Item Details")]
        [SerializeField] private GameObject itemDetailsPanel;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemDescriptionText;
        [SerializeField] private TMP_Text itemStatsText;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button unequipButton;

        [Header("Controls")]
        [SerializeField] private Button backButton;

        private List<InventoryItemBehavior> inventoryItems = new List<InventoryItemBehavior>();
        private EquipmentSlotBehavior selectedSlot;
        private InventoryItemBehavior selectedInventoryItem;

        public void Init(UnityAction onBackButtonClicked)
        {
            backButton.onClick.AddListener(onBackButtonClicked);

            SetupEquipmentSlots();
            RefreshInventory();
            equipButton.onClick.AddListener(OnEquipButtonClicked);
            unequipButton.onClick.AddListener(OnUnequipButtonClicked);

            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.OnInventoryChanged.AddListener(RefreshInventory);
                EquipmentManager.Instance.OnEquipmentChanged.AddListener(OnEquipmentChanged);
            }
            itemDetailsPanel.SetActive(false);
        }

        private void SetupEquipmentSlots()
        {
            // Setup left slots (Hat, Armor, Ring)
            for (int i = 0; i < leftEquipmentSlots.Length; i++)
            {
                leftEquipmentSlots[i].OnSlotClicked.AddListener(OnEquipmentSlotClicked);
            }

            // Setup right slots (Necklace, Belt, Shoes)
            for (int i = 0; i < rightEquipmentSlots.Length; i++)
            {
                rightEquipmentSlots[i].OnSlotClicked.AddListener(OnEquipmentSlotClicked);
            }
        }

        private void RefreshInventory()
        {
            foreach (var item in inventoryItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            inventoryItems.Clear();

            if (EquipmentManager.Instance == null)
                return;
            var inventoryData = EquipmentManager.Instance.GetInventoryItems();
            foreach (var inventoryItem in inventoryData)
            {
                var equipmentData = EquipmentManager.Instance.Database.GetEquipmentById(
                    inventoryItem.equipmentType, inventoryItem.equipmentId);

                if (equipmentData != null)
                {
                    var itemObject = Instantiate(inventoryItemPrefab, inventoryContent);
                    var itemBehavior = itemObject.GetComponent<InventoryItemBehavior>();
                    
                    itemBehavior.Init(inventoryItem, equipmentData);
                    itemBehavior.OnItemClicked.AddListener(OnInventoryItemClicked);
                    itemBehavior.transform.ResetLocal();

                    inventoryItems.Add(itemBehavior);
                }
            }
        }

        private void OnEquipmentSlotClicked(EquipmentSlotBehavior slot)
        {
            selectedSlot = slot;
            selectedInventoryItem = null;

            if (slot.CurrentEquipment != null)
            {
                ShowItemDetails(slot.CurrentEquipment, true);
            }
            else
            {
                itemDetailsPanel.SetActive(false);
            }
        }

        private void OnInventoryItemClicked(InventoryItemBehavior item)
        {
            selectedInventoryItem = item;
            selectedSlot = null;
            ShowItemDetails(item.EquipmentData, false);
        }

        private void ShowItemDetails(EquipmentData equipment, bool isEquipped)
        {
            itemDetailsPanel.SetActive(true);

            itemNameText.text = equipment.Name;
            itemDescriptionText.text = equipment.Description;

            // Build stats text
            var statsText = "";
            if (equipment.BonusHP > 0) statsText += $"HP: +{equipment.BonusHP}\n";
            if (equipment.BonusDamage > 0) statsText += $"Damage: +{equipment.BonusDamage}\n";
            if (equipment.BonusSpeed > 0) statsText += $"Speed: +{equipment.BonusSpeed}\n";
            if (equipment.BonusMagnetRadius > 0) statsText += $"Magnet: +{equipment.BonusMagnetRadius}\n";
            if (equipment.BonusXPMultiplier > 0) statsText += $"XP: +{equipment.BonusXPMultiplier * 100}%\n";
            if (equipment.BonusCooldownReduction > 0) statsText += $"Cooldown: -{equipment.BonusCooldownReduction * 100}%\n";
            if (equipment.BonusDamageReduction > 0) statsText += $"Defense: +{equipment.BonusDamageReduction * 100}%\n";

            itemStatsText.text = statsText.TrimEnd('\n');

            // Show appropriate buttons
            equipButton.gameObject.SetActive(!isEquipped && selectedInventoryItem != null);
            unequipButton.gameObject.SetActive(isEquipped && selectedSlot != null);
        }

        private void OnEquipButtonClicked()
        {
            if (selectedInventoryItem != null && EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.EquipItem(
                    selectedInventoryItem.InventoryItem.equipmentType,
                    selectedInventoryItem.InventoryItem.equipmentId,
                    selectedInventoryItem.InventoryItem.level);
            }
        }

        private void OnUnequipButtonClicked()
        {
            if (selectedSlot != null && EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.UnequipItem(selectedSlot.EquipmentType);
            }
        }

        private void OnEquipmentChanged(EquipmentType equipmentType)
        {
            // Update item details if currently showing an equipped item
            if (selectedSlot != null && selectedSlot.EquipmentType == equipmentType)
            {
                if (selectedSlot.CurrentEquipment != null)
                {
                    ShowItemDetails(selectedSlot.CurrentEquipment, true);
                }
                else
                {
                    itemDetailsPanel.SetActive(false);
                }
            }
        }

        public void Open()
        {
            gameObject.SetActive(true);
            EasingManager.DoNextFrame(() => RefreshInventory());
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.OnInventoryChanged.RemoveListener(RefreshInventory);
                EquipmentManager.Instance.OnEquipmentChanged.RemoveListener(OnEquipmentChanged);
            }
        }
    }
}