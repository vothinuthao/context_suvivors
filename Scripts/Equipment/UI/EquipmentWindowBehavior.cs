using System.Collections.Generic;
using System.Linq;
using OctoberStudio;
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
    // Main equipment window behavior - updated for UID system
    public class EquipmentWindowBehavior : MonoBehaviour
    {
        
        [Header("Current Character Display")]
        [SerializeField] private Image currentCharacterSprite;
        [SerializeField] private TMP_Text currentCharacterNameText;
        [SerializeField] private CharactersDatabase charactersDatabase;
        
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
        [SerializeField] private TMP_Text itemUIDText; // Show UID for debugging
        [SerializeField] private TMP_Text itemCreatedDateText; // Show creation date
        [SerializeField] private Button equipButton;
        [SerializeField] private Button unequipButton;

        [Header("Controls")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button sortByTypeButton;
        [SerializeField] private Button sortByDateButton;
        [SerializeField] private Button sortByRarityButton;

        [Header("Inventory Info")]
        [SerializeField] private TMP_Text inventoryCountText;
        [SerializeField] private TMP_Text inventoryStatsText;

        private List<InventoryItemVO> inventoryItemVOs = new List<InventoryItemVO>();
        private EquipmentSlotBehavior selectedSlot;
        private InventoryItemVO selectedInventoryItem;
        private CharactersSave charactersSave;


        public void Init()
        {
            // backButton.onClick.AddListener(onBackButtonClicked);

            SetupEquipmentSlots();
            SetupSortingButtons();
            RefreshInventory();
            InitializeCharacterData();
            equipButton.onClick.AddListener(OnEquipButtonClicked);
            unequipButton.onClick.AddListener(OnUnequipButtonClicked);

            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.OnInventoryChanged.AddListener(RefreshInventory);
                EquipmentManager.Instance.OnEquipmentChanged.AddListener(OnEquipmentChanged);
            }
            
            itemDetailsPanel.SetActive(false);
            UpdateCurrentCharacterDisplay();
        }
        private void InitializeCharacterData()
        {
            if (charactersSave == null)
            {
                charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
                if (charactersSave != null)
                {
                    charactersSave.onSelectedCharacterChanged += OnSelectedCharacterChanged;
                }
            }
        }
        private void OnSelectedCharacterChanged()
        {
            UpdateCurrentCharacterDisplay();
        }
        private void UpdateCurrentCharacterDisplay()
        {
            if (charactersSave == null || charactersDatabase == null) return;

            var selectedCharacterId = charactersSave.SelectedCharacterId;
            var characterData = charactersDatabase.GetCharacterData(selectedCharacterId);

            if (characterData != null)
            {
                // Update character sprite
                if (currentCharacterSprite != null)
                {
                    currentCharacterSprite.sprite = characterData.Icon;
                }

                // Update character name
                if (currentCharacterNameText != null)
                {
                    currentCharacterNameText.text = characterData.Name;
                }

                Debug.Log($"[EquipmentWindow] Updated current character display: {characterData.Name}");
            }
            else
            {
                Debug.LogWarning($"[EquipmentWindow] Character data not found for ID: {selectedCharacterId}");
                
                // Set default/empty state
                if (currentCharacterSprite != null)
                {
                    currentCharacterSprite.sprite = null;
                }

                if (currentCharacterNameText != null)
                {
                    currentCharacterNameText.text = "Unknown Character";
                }
            }
        }
        
        private void SetupEquipmentSlots()
        {
            foreach (var t in leftEquipmentSlots)
            {
                t.OnSlotClicked.AddListener(OnEquipmentSlotClicked);
            }

            foreach (var t in rightEquipmentSlots)
            {
                t.OnSlotClicked.AddListener(OnEquipmentSlotClicked);
            }
        }

        private void SetupSortingButtons()
        {
            if (sortByTypeButton != null)
                sortByTypeButton.onClick.AddListener(() => SortInventory(SortType.Type));
                
            if (sortByDateButton != null)
                sortByDateButton.onClick.AddListener(() => SortInventory(SortType.Date));
                
            if (sortByRarityButton != null)
                sortByRarityButton.onClick.AddListener(() => SortInventory(SortType.Rarity));
        }

        private void RefreshInventory()
        {
            inventoryContent.DestroyAllChildren();
            inventoryItemVOs.Clear();
            // Clear item details panel
            if (!EquipmentManager.Instance)
                return;
        
            var inventoryItemsWithData = EquipmentManager.Instance.GetInventoryItemsWithData();
            
            foreach (var (inventoryItem, equipmentData) in inventoryItemsWithData)
            {
                if (equipmentData != null)
                {
                    var itemVO = inventoryItemPrefab.SpawnWithSetup<InventoryItemVO>(
                        inventoryContent, 
                        behavior => {
                            behavior.Init(inventoryItem, equipmentData);
                            behavior.OnItemClicked.AddListener(OnInventoryItemClicked);
                        }
                    );

                    inventoryItemVOs.Add(itemVO);
                }
            }
            itemDetailsPanel.SetActive(false);
            UpdateInventoryInfo();
        }

        private void UpdateInventoryInfo()
        {
            if (inventoryCountText != null)
            {
                inventoryCountText.text = $"Items: {inventoryItemVOs.Count}";
            }

            if (inventoryStatsText != null && EquipmentManager.Instance != null)
            {
                var stats = EquipmentManager.Instance.GetInventoryStatistics();
                var statsText = "Inventory:\n";
                foreach (var kvp in stats)
                {
                    if (kvp.Value > 0)
                        statsText += $"{kvp.Key}: {kvp.Value}\n";
                }
                inventoryStatsText.text = statsText.TrimEnd('\n');
            }
        }

        private void OnEquipmentSlotClicked(EquipmentSlotBehavior slot)
        {
            selectedSlot = slot;
            selectedInventoryItem = null;

            if (slot.CurrentEquipment != null)
            {
                var equippedUID = EquipmentManager.Instance.GetEquippedItemUID(slot.EquipmentType);
                ShowItemDetails(slot.CurrentEquipment, true, equippedUID);
            }
            else
            {
                itemDetailsPanel.SetActive(false);
            }
        }

        private void OnInventoryItemClicked(InventoryItemVO item)
        {
            selectedInventoryItem = item;
            selectedSlot = null;
            ShowItemDetails(item.EquipmentModel, false, item.GetUID());
        }

        private void ShowItemDetails(EquipmentModel equipment, bool isEquipped, string itemUID)
        {
            itemDetailsPanel.SetActive(true);

            itemNameText.text = equipment.GetDisplayName();
            itemDescriptionText.text = equipment.Description;
            itemStatsText.text = equipment.GetStatsText();

            // Show UID and creation date for debugging/info
            if (itemUIDText != null)
            {
                itemUIDText.text = $"UID: {itemUID}";
                itemUIDText.gameObject.SetActive(!string.IsNullOrEmpty(itemUID));
            }

            if (itemCreatedDateText != null && !string.IsNullOrEmpty(itemUID))
            {
                var timestamp = UIDGenerator.GetTimestampFromUID(itemUID);
                if (timestamp.HasValue)
                {
                    itemCreatedDateText.text = $"Created: {timestamp.Value:yyyy-MM-dd HH:mm}";
                    itemCreatedDateText.gameObject.SetActive(true);
                }
                else if (selectedInventoryItem != null)
                {
                    itemCreatedDateText.text = $"Created: {selectedInventoryItem.InventoryItem.createdAt:yyyy-MM-dd HH:mm}";
                    itemCreatedDateText.gameObject.SetActive(true);
                }
                else
                {
                    itemCreatedDateText.gameObject.SetActive(false);
                }
            }

            // Show appropriate buttons
            bool canEquip = !isEquipped && selectedInventoryItem != null && selectedInventoryItem.CanBeEquipped();
            bool canUnequip = isEquipped && selectedSlot != null;

            equipButton.gameObject.SetActive(canEquip);
            unequipButton.gameObject.SetActive(canUnequip);
        }

        private void OnEquipButtonClicked()
        {
            if (selectedInventoryItem != null && EquipmentManager.Instance != null)
            {
                bool success = selectedInventoryItem.EquipItem();
                
                if (success)
                {
                    itemDetailsPanel.SetActive(false);
                    
                    // Refresh equipped indicators
                    RefreshAllEquippedIndicators();
                }
                else
                {
                    Debug.LogWarning($"Failed to equip item: {selectedInventoryItem.GetUID()}");
                }
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
                    var equippedUID = EquipmentManager.Instance.GetEquippedItemUID(selectedSlot.EquipmentType);
                    ShowItemDetails(selectedSlot.CurrentEquipment, true, equippedUID);
                }
                else
                {
                    itemDetailsPanel.SetActive(false);
                }
            }

            // Refresh equipped indicators for all inventory items
            RefreshAllEquippedIndicators();
        }

        private void RefreshAllEquippedIndicators()
        {
            foreach (var itemVO in inventoryItemVOs)
            {
                itemVO.RefreshEquippedStatus();
            }
        }

        private void SortInventory(SortType sortType)
        {
            switch (sortType)
            {
                case SortType.Type:
                    inventoryItemVOs.Sort(InventoryItemVO.Compare);
                    break;
                    
                case SortType.Date:
                    inventoryItemVOs.Sort((a, b) => 
                        b.InventoryItem.createdAt.CompareTo(a.InventoryItem.createdAt));
                    break;
                    
                case SortType.Rarity:
                    inventoryItemVOs.Sort((a, b) => 
                    {
                        int rarityCompare = b.EquipmentModel.Rarity.CompareTo(a.EquipmentModel.Rarity);
                        if (rarityCompare != 0) return rarityCompare;
                        return b.InventoryItem.createdAt.CompareTo(a.InventoryItem.createdAt);
                    });
                    break;
            }

            // Reorder UI elements
            for (int i = 0; i < inventoryItemVOs.Count; i++)
            {
                inventoryItemVOs[i].transform.SetSiblingIndex(i);
            }

            Debug.Log($"Inventory sorted by {sortType}");
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

            // Clean up button listeners
            if (sortByTypeButton != null)
                sortByTypeButton.onClick.RemoveAllListeners();
            if (sortByDateButton != null)
                sortByDateButton.onClick.RemoveAllListeners();
            if (sortByRarityButton != null)
                sortByRarityButton.onClick.RemoveAllListeners();
        }

        private enum SortType
        {
            Type,
            Date,
            Rarity
        }

        // Debug methods
        [ContextMenu("Log All Item UIDs")]
        public void LogAllItemUIDs()
        {
            Debug.Log("=== Current Inventory UIDs ===");
            foreach (var itemVO in inventoryItemVOs)
            {
                var item = itemVO.InventoryItem;
                var equipment = itemVO.EquipmentModel;
                Debug.Log($"{equipment.Name} (Level {item.level}) - UID: {item.uid} - Created: {item.createdAt:yyyy-MM-dd HH:mm:ss}");
            }
        }

        [ContextMenu("Validate All UIDs")]
        public void ValidateAllUIDs()
        {
            int validCount = 0;
            int invalidCount = 0;

            foreach (var itemVO in inventoryItemVOs)
            {
                if (UIDGenerator.IsValidUID(itemVO.GetUID()))
                {
                    validCount++;
                }
                else
                {
                    invalidCount++;
                    Debug.LogError($"Invalid UID found: {itemVO.GetUID()} for item {itemVO.EquipmentModel.Name}");
                }
            }

            Debug.Log($"UID Validation Result: {validCount} valid, {invalidCount} invalid");
        }

        [ContextMenu("Show Duplicate Items")]
        public void ShowDuplicateItems()
        {
            var itemGroups = inventoryItemVOs
                .GroupBy(item => new { 
                    Type = item.InventoryItem.equipmentType, 
                    ID = item.InventoryItem.equipmentId, 
                    Level = item.InventoryItem.level 
                })
                .Where(group => group.Count() > 1)
                .ToList();

            if (itemGroups.Count == 0)
            {
                Debug.Log("No duplicate items found");
                return;
            }

            Debug.Log("=== Duplicate Items Found ===");
            foreach (var group in itemGroups)
            {
                var key = group.Key;
                var items = group.ToList();
                Debug.Log($"{key.Type} ID:{key.ID} Level:{key.Level} - {items.Count} copies:");
                
                foreach (var item in items)
                {
                    Debug.Log($"  UID: {item.GetUID()} - Created: {item.InventoryItem.createdAt:yyyy-MM-dd HH:mm:ss}");
                }
            }
        }
    }
}