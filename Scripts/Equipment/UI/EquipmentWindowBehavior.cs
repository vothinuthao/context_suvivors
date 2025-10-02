using System.Collections.Generic;
using System.Linq;
using OctoberStudio;
using OctoberStudio.Abilities;
using OctoberStudio.Audio;
using OctoberStudio.Easing;
using OctoberStudio.Equipment;
using OctoberStudio.Equipment.UI;
using OctoberStudio.Extensions;
using OctoberStudio.Input;
using OctoberStudio.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Common.Scripts.Equipment.UI
{
    // Main equipment window behavior - updated for UID system
    public class EquipmentWindowBehavior : MonoBehaviour
    {
        
        [Header("Current Character Display")]
        [SerializeField] private Image currentCharacterSprite;
        [SerializeField] private TMP_Text currentCharacterNameText;
        [SerializeField] private TMP_Text currentCharacterHPText;
        [SerializeField] private TMP_Text currentCharacterDamageText;
        [SerializeField] private CharactersDatabase charactersDatabase;
        
        [Header("Equipment Slots")]
        [SerializeField] private EquipmentSlotBehavior[] leftEquipmentSlots = new EquipmentSlotBehavior[3];  // Hat, Armor, Ring
        [SerializeField] private EquipmentSlotBehavior[] rightEquipmentSlots = new EquipmentSlotBehavior[3]; // Glovy, Belt, Shoes

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

        [Header("Tab System")]
        [SerializeField] private Button toggleTabButton;
        [SerializeField] private TMP_Text toggleTabButtonText;
        [SerializeField] private GameObject equipmentTabContent;
        [SerializeField] private GameObject characterTabContent;

        [Header("Character Tab")]
        [SerializeField] private AbilitiesDatabase abilitiesDatabase;
        [SerializeField] private GameObject characterItemPrefab;
        [SerializeField] private RectTransform characterItemsParent;
        [SerializeField] private ScrollRect characterScrollView;
        [SerializeField] private GameObject characterStatsPanel;
        [SerializeField] private TMP_Text selectedCharacterNameText;
        [SerializeField] private TMP_Text selectedCharacterHPText;
        [SerializeField] private TMP_Text selectedCharacterDamageText;
        [SerializeField] private Button selectCharacterButton;
        [SerializeField] private TMP_Text selectCharacterButtonText;

        [Header("Character Upgrade System - 6 Tier Star System")]
        [SerializeField] private Transform upgradeStatsContainer;
        [SerializeField] private GameObject descriptionCharacterStatsPrefab; // New unified prefab 200x50

        [Header("Star Sprites for 6-Tier System")]
        [SerializeField] private Sprite greyStarSprite;     // For tiers 1-3
        [SerializeField] private Sprite goldStarSprite;     // For tier 4
        [SerializeField] private Sprite orangeStarSprite;   // For tier 5
        [SerializeField] private Sprite purpleStarSprite;   // For tier 6
        [SerializeField] private Sprite emptyStarSprite;    // For locked tiers

        [Header("Upgrade Tab")]
        [SerializeField] private Button upgradeButton; // Only for level upgrade
        [SerializeField] private TMP_Text upgradeButtonText;
        [SerializeField] private TMP_Text upgradeCostText;
        [SerializeField] private TMP_Text characterLevelText; // Current/Max level display
        [SerializeField] private TMP_Text characterStarLevelText; // Current star level display
        [SerializeField] private Sprite enabledButtonSprite;
        [SerializeField] private Sprite disabledButtonSprite;

        [Header("Pieces Progress Display")]
        [SerializeField] private Slider piecesProgressSlider; // Progress bar for pieces
        [SerializeField] private TMP_Text piecesProgressText; // "X / Y pieces"
        [SerializeField] private Image[] starImages = new Image[3]; // Pre-existing star images (only 3 stars)

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

        private List<CharacterItemBehavior> characterItems = new List<CharacterItemBehavior>();
        private bool isEquipmentTabActive = true; // Start with Equipment tab (Character button visible)
        private CurrencySave goldCurrency;
        private CharacterData currentDisplayedCharacter;
        private int currentDisplayedCharacterIndex = -1;
        private CharacterUpgradeManager upgradeManager;

        private void Awake()
        {
            // Ensure Equipment tab is always the default state
            InitializeTabState();
        }

        private void InitializeTabState()
        {
            // Force Equipment tab to be active
            isEquipmentTabActive = true;

            // Set UI states immediately (even before Init is called)
            if (equipmentTabContent != null)
                equipmentTabContent.SetActive(true);

            if (characterTabContent != null)
                characterTabContent.SetActive(false);

            // Set button text to show "Character" (next tab)
            if (toggleTabButtonText != null)
                toggleTabButtonText.text = "Character";
        }


        public void Init()
        {
            // backButton.onClick.AddListener(onBackButtonClicked);

            // Initialize upgrade manager and subscribe to events
            upgradeManager = FindObjectOfType<CharacterUpgradeManager>();
            if (charactersSave != null)
            {
                charactersSave.onCharacterUpgraded += OnCharacterUpgraded;
            }
            if (goldCurrency != null)
            {
                goldCurrency.onGoldAmountChanged += OnGoldAmountChanged;
            }

            SetupTabs();
            SetupEquipmentSlots();
            SetupSortingButtons();
            SetupCharacterTab();
            RefreshInventory();
            InitializeCharacterData();
            equipButton.onClick.AddListener(OnEquipButtonClicked);
            unequipButton.onClick.AddListener(OnUnequipButtonClicked);

            if (selectCharacterButton != null)
                selectCharacterButton.onClick.AddListener(OnSelectCharacterButtonClicked);

            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);

            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.OnInventoryChanged.AddListener(RefreshInventory);
                EquipmentManager.Instance.OnEquipmentChanged.AddListener(OnEquipmentChanged);
            }

            itemDetailsPanel.SetActive(false);

            // Reset to current selected character during init
            ResetToCurrentCharacter();
            UpdateCurrentCharacterDisplay();

            // Start with equipment tab active (Character button visible)
            SwitchToEquipmentTab();
        }
        private void SetupTabs()
        {
            if (toggleTabButton != null)
                toggleTabButton.onClick.AddListener(ToggleTab);
        }

        private void ToggleTab()
        {
            if (isEquipmentTabActive)
            {
                SwitchToCharacterTab();
            }
            else
            {
                SwitchToEquipmentTab();
            }
        }

        private void SwitchToEquipmentTab()
        {
            isEquipmentTabActive = true;

            if (equipmentTabContent != null)
                equipmentTabContent.SetActive(true);

            if (characterTabContent != null)
                characterTabContent.SetActive(false);

            // Update button text to show "Character" (next tab)
            if (toggleTabButtonText != null)
                toggleTabButtonText.text = "Character";
        }

        private void SwitchToCharacterTab()
        {
            isEquipmentTabActive = false;

            if (equipmentTabContent != null)
                equipmentTabContent.SetActive(false);

            if (characterTabContent != null)
                characterTabContent.SetActive(true);

            // Refresh character display
            RefreshCharacterTab();

            // Display current selected character when opening character tab
            if (charactersSave != null && charactersDatabase != null)
            {
                // Get current selected character
                int selectedCharacterId = charactersSave.SelectedCharacterId;
                var selectedCharacterData = charactersDatabase.GetCharacterData(selectedCharacterId);

                if (selectedCharacterData != null)
                {
                    currentDisplayedCharacter = selectedCharacterData;
                    currentDisplayedCharacterIndex = selectedCharacterId;
                    DisplayCharacterStats(selectedCharacterData, selectedCharacterId);

                    Debug.Log($"[EquipmentWindow] Displaying current selected character: {selectedCharacterData.Name} (index {selectedCharacterId})");
                }
                else
                {
                    Debug.LogWarning($"[EquipmentWindow] Selected character data not found for ID: {selectedCharacterId}");

                    // Fallback to first character
                    var firstCharacterData = charactersDatabase.GetCharacterData(0);
                    if (firstCharacterData != null)
                    {
                        currentDisplayedCharacter = firstCharacterData;
                        currentDisplayedCharacterIndex = 0;
                        DisplayCharacterStats(firstCharacterData, 0);
                    }
                }
            }

            // Update button text to show "Equipment" (next tab)
            if (toggleTabButtonText != null)
                toggleTabButtonText.text = "Equipment";
        }

        // Removed UpdateTabButtonVisuals method - using single toggle button now

        private void SetupCharacterTab()
        {
            if (charactersDatabase == null) return;

            goldCurrency = GameController.SaveManager.GetSave<CurrencySave>("gold");

            for (int i = 0; i < charactersDatabase.CharactersCount; i++)
            {
                var item = Instantiate(characterItemPrefab, characterItemsParent).GetComponent<CharacterItemBehavior>();
                item.transform.ResetLocal();

                item.Init(i, charactersDatabase.GetCharacterData(i), abilitiesDatabase);
                item.onNavigationSelected += OnCharacterItemSelected;

                // Also listen to button clicks directly for immediate character stats update
                var characterData = charactersDatabase.GetCharacterData(i);
                int characterIndex = i; // Capture for closure
                item.OnclickButton.onClick.AddListener(() => OnCharacterButtonClicked(characterIndex, characterData));

                characterItems.Add(item);
            }

            ResetCharacterNavigation();
        }

        private void RefreshCharacterTab()
        {
            // Refresh all character items to update their visual states
            foreach (var item in characterItems)
            {
                if (item != null)
                {
                    // Force refresh of character item visuals to fix mask lock issues
                    item.ForceRefresh();
                }
            }

            Debug.Log("[EquipmentWindow] RefreshCharacterTab completed - all items refreshed");
        }

        private void ResetCharacterNavigation()
        {
            for (int i = 0; i < characterItems.Count; i++)
            {
                var item = characterItems[i];
                var navigation = new Navigation();
                navigation.mode = Navigation.Mode.Explicit;

                int leftIndex = i - 1;
                // if (leftIndex >= 0) navigation.selectOnLeft = characterItems[leftIndex].Selectable;

                int rightIndex = i + 1;
                if (rightIndex < characterItems.Count)
                {
                    // navigation.selectOnRight = characterItems[rightIndex].Selectable;
                }
                else
                {
                    navigation.selectOnRight = backButton;
                }

                // For vertical navigation if needed (assuming horizontal layout for now)
                navigation.selectOnUp = toggleTabButton;
                navigation.selectOnDown = backButton;

                // item.Selectable.navigation = navigation;
            }
        }

        private void OnCharacterButtonClicked(int characterIndex, CharacterData characterData)
        {
            // All characters can be viewed (including locked ones)
            // Store current displayed character info
            currentDisplayedCharacter = characterData;
            currentDisplayedCharacterIndex = characterIndex;

            // IMMEDIATELY update currentCharacterSprite when any character is clicked
            if (currentCharacterSprite != null && characterData != null)
            {
                currentCharacterSprite.sprite = characterData.Icon;
                Debug.Log($"[EquipmentWindow] Updated currentCharacterSprite to: {characterData.Name}");
            }

            // Update character name display
            if (currentCharacterNameText != null && characterData != null)
            {
                currentCharacterNameText.text = characterData.Name;
            }

            // Update HP and Damage in current character display
            UpdateCurrentCharacterStats(characterData, characterIndex);

            // Display character stats immediately when button is clicked
            DisplayCharacterStats(characterData, characterIndex);

            Debug.Log($"[EquipmentWindow] Displaying character {characterIndex}: {characterData.Name}");
        }

        private void OnCharacterItemSelected(CharacterItemBehavior selectedItem)
        {
            // Handle scroll view positioning similar to original character window
            if (characterScrollView != null)
            {
                var objPosition = (Vector2)characterScrollView.transform.InverseTransformPoint(selectedItem.Rect.position);
                var scrollWidth = characterScrollView.GetComponent<RectTransform>().rect.width;
                var objWidth = selectedItem.Rect.rect.width;

                if (objPosition.x > scrollWidth / 2)
                {
                    characterScrollView.content.localPosition = new Vector2(
                        characterScrollView.content.localPosition.x - objWidth - 37,
                        characterScrollView.content.localPosition.y);
                }

                if (objPosition.x < -scrollWidth / 2)
                {
                    characterScrollView.content.localPosition = new Vector2(
                        characterScrollView.content.localPosition.x + objWidth + 37,
                        characterScrollView.content.localPosition.y);
                }
            }

            // Update character stats display
            UpdateSelectedCharacterStats();
        }

        private void DisplayCharacterStats(CharacterData characterData, int characterIndex = -1)
        {
            if (characterData != null && characterStatsPanel != null)
            {
                characterStatsPanel.SetActive(true);

                if (selectedCharacterNameText != null)
                    selectedCharacterNameText.text = characterData.Name;
                // Use upgraded stats if available
                float currentHP = upgradeManager != null ?
                    upgradeManager.GetCharacterHP(characterIndex) : characterData.BaseHP;
                float currentDamage = upgradeManager != null ?
                    upgradeManager.GetCharacterDamage(characterIndex) : characterData.BaseDamage;

                if (selectedCharacterHPText != null)
                    selectedCharacterHPText.text = currentHP.ToString("F0");

                if (selectedCharacterDamageText != null)
                    selectedCharacterDamageText.text = currentDamage.ToString("F0");

                // Update stars display
                UpdateStarsDisplay(characterData, characterIndex);

                // Update upgrade button and auto star check
                CheckAndPerformAutoStarUpgrade(characterData, characterIndex);
                UpdateUpgradeButton(characterData, characterIndex);
                UpdatePiecesProgressDisplay(characterData, characterIndex);
                UpdateAllStarsDisplay(characterData, characterIndex);

                // Handle select button visibility and state
                UpdateSelectCharacterButton(characterData, characterIndex);
            }
            else if (characterStatsPanel != null)
            {
                characterStatsPanel.SetActive(false);
            }
        }

        private void UpdateSelectCharacterButton(CharacterData characterData, int characterIndex)
        {
            if (selectCharacterButton == null) return;

            // Only owned characters can be selected
            bool isOwned = charactersSave != null && charactersSave.HasCharacterBeenBought(characterIndex);
            bool isCurrentlySelected = charactersSave != null && charactersSave.SelectedCharacterId == characterIndex;

            selectCharacterButton.gameObject.SetActive(isOwned);

            if (isOwned)
            {
                selectCharacterButton.interactable = !isCurrentlySelected;

                if (selectCharacterButtonText != null)
                {
                    selectCharacterButtonText.text = isCurrentlySelected ? "SELECTED" : "SELECT";
                }
            }
        }

        private void OnSelectCharacterButtonClicked()
        {
            if (currentDisplayedCharacter != null && currentDisplayedCharacterIndex >= 0 && charactersSave != null)
            {
                // Select the character
                charactersSave.SetSelectedCharacterId(currentDisplayedCharacterIndex);

                // Update the equipment window's current character display
                UpdateCurrentCharacterDisplay();

                // Refresh the select button state
                UpdateSelectCharacterButton(currentDisplayedCharacter, currentDisplayedCharacterIndex);

                // Refresh all character items to update their visual states
                RefreshCharacterTab();

                // Play audio feedback
                if (GameController.AudioManager != null)
                {
                    GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);
                }
            }
        }

        private void UpdateSelectedCharacterStats()
        {
            if (charactersSave == null || charactersDatabase == null) return;

            var selectedCharacterId = charactersSave.SelectedCharacterId;
            var characterData = charactersDatabase.GetCharacterData(selectedCharacterId);

            DisplayCharacterStats(characterData, selectedCharacterId);
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

            // Always update current character stats in header when selected character changes
            if (charactersSave != null && charactersDatabase != null)
            {
                var selectedCharacterId = charactersSave.SelectedCharacterId;
                var selectedCharacterData = charactersDatabase.GetCharacterData(selectedCharacterId);

                if (selectedCharacterData != null)
                {
                    // Update current character display stats in header
                    UpdateCurrentCharacterStats(selectedCharacterData, selectedCharacterId);
                }
            }

            // If we're in character tab, update the character stats to show current selected character
            if (!isEquipmentTabActive)
            {
                // Show stats for current selected character when selected character changes
                if (charactersSave != null && charactersDatabase != null)
                {
                    var selectedCharacterId = charactersSave.SelectedCharacterId;
                    var selectedCharacterData = charactersDatabase.GetCharacterData(selectedCharacterId);

                    if (selectedCharacterData != null)
                    {
                        currentDisplayedCharacter = selectedCharacterData;
                        currentDisplayedCharacterIndex = selectedCharacterId;
                        DisplayCharacterStats(selectedCharacterData, selectedCharacterId);

                        Debug.Log($"[EquipmentWindow] Selected character changed, now displaying: {selectedCharacterData.Name}");
                    }
                }

                // Also update the select button if we're displaying stats for a different character
                if (currentDisplayedCharacter != null && currentDisplayedCharacterIndex >= 0)
                {
                    UpdateSelectCharacterButton(currentDisplayedCharacter, currentDisplayedCharacterIndex);
                }
            }
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

        [ContextMenu("Refresh Character Ownership")]
        public void RefreshCharacterOwnership()
        {
            if (!isEquipmentTabActive)
            {
                RefreshCharacterTab();
                UpdateSelectedCharacterStats();
            }

            foreach (var item in characterItems)
            {
                if (item != null)
                {
                    // Force refresh of character item visuals
                    item.ForceRefresh();
                }
            }

            Debug.Log("[EquipmentWindow] Character ownership refreshed");
        }

        public void Open()
        {
            gameObject.SetActive(true);

            // Reset to current selected character when opening
            ResetToCurrentCharacter();

            // Always ensure Equipment tab is active when opening
            SwitchToEquipmentTab();

            EasingManager.DoNextFrame(() => RefreshInventory());
        }

        /// <summary>
        /// Reset currentCharacterSprite and display to actual current selected character
        /// </summary>
        private void ResetToCurrentCharacter()
        {
            if (charactersSave != null && charactersDatabase != null)
            {
                var selectedCharacterId = charactersSave.SelectedCharacterId;
                var selectedCharacterData = charactersDatabase.GetCharacterData(selectedCharacterId);

                if (selectedCharacterData != null)
                {
                    // Reset currentCharacterSprite to actual selected character
                    if (currentCharacterSprite != null)
                    {
                        currentCharacterSprite.sprite = selectedCharacterData.Icon;
                    }

                    // Reset character name
                    if (currentCharacterNameText != null)
                    {
                        currentCharacterNameText.text = selectedCharacterData.Name;
                    }

                    // Update HP and Damage for current selected character
                    UpdateCurrentCharacterStats(selectedCharacterData, selectedCharacterId);

                    Debug.Log($"[EquipmentWindow] Reset to current selected character: {selectedCharacterData.Name} (ID: {selectedCharacterId})");
                }
            }
        }

        /// <summary>
        /// Update current character HP and Damage display with upgraded stats
        /// </summary>
        private void UpdateCurrentCharacterStats(CharacterData characterData, int characterIndex)
        {
            if (characterData == null) return;

            // Get upgraded stats if available
            float currentHP = characterData.BaseHP;
            float currentDamage = characterData.BaseDamage;

            if (upgradeManager != null && characterIndex >= 0)
            {
                currentHP = upgradeManager.GetCharacterHP(characterIndex);
                currentDamage = upgradeManager.GetCharacterDamage(characterIndex);
            }

            // Update HP display
            if (currentCharacterHPText != null)
            {
                currentCharacterHPText.text = $"{currentHP:F0}";
            }

            // Update Damage display
            if (currentCharacterDamageText != null)
            {
                currentCharacterDamageText.text = $"{currentDamage:F0}";
            }
        }

        // public void Close()
        // {
        //     gameObject.SetActive(false);
        // }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.OnInventoryChanged.RemoveListener(RefreshInventory);
                EquipmentManager.Instance.OnEquipmentChanged.RemoveListener(OnEquipmentChanged);
            }

            if (charactersSave != null)
            {
                charactersSave.onSelectedCharacterChanged -= OnSelectedCharacterChanged;
                charactersSave.onCharacterUpgraded -= OnCharacterUpgraded;
            }

            if (goldCurrency != null)
            {
                goldCurrency.onGoldAmountChanged -= OnGoldAmountChanged;
            }

            // Clean up character items
            foreach (var item in characterItems)
            {
                if (item != null)
                {
                    item.Clear();
                }
            }

            // Clean up button listeners
            if (sortByTypeButton != null)
                sortByTypeButton.onClick.RemoveAllListeners();
            if (sortByDateButton != null)
                sortByDateButton.onClick.RemoveAllListeners();
            if (sortByRarityButton != null)
                sortByRarityButton.onClick.RemoveAllListeners();
            if (toggleTabButton != null)
                toggleTabButton.onClick.RemoveAllListeners();
            if (selectCharacterButton != null)
                selectCharacterButton.onClick.RemoveAllListeners();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void Clear()
        {
            foreach (var item in characterItems)
            {
                if (item != null)
                {
                    item.Clear();
                }
            }
        }

        #region Enums and Data Types

        private enum SortType
        {
            Type,
            Date,
            Rarity
        }

        #endregion

        #region Debug and Testing Methods

        #region Character Upgrade UI - 6 Tier System

        /// <summary>
        /// Update character upgrade display with FIXED 6-tier system
        /// ALWAYS creates exactly 6 prefabs with FIXED star configuration - NEVER changes:
        /// Index 0: 1 grey star, Index 1: 2 grey stars, Index 2: 3 grey stars,
        /// Index 3: 1 orange star, Index 4: 1 gold star, Index 5: 1 purple star
        /// </summary>
        private void UpdateCharacterUpgradeDisplay(CharacterData characterData, int characterIndex)
        {
            if (upgradeStatsContainer == null || descriptionCharacterStatsPrefab == null)
                return;

            // Clear existing upgrade stats
            for (int i = upgradeStatsContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(upgradeStatsContainer.GetChild(i).gameObject);
            }

            // ALWAYS create exactly 6 prefabs with FIXED configuration - NO EXCEPTIONS
            // This configuration NEVER changes regardless of character or any other logic
            CreateAbsolutelyFixedStarTiers();

            // Update upgrade tab info only if valid character
            if (characterIndex >= 0 && characterData != null)
            {
                UpdateUpgradeTabInfo(characterData, characterIndex);
            }
        }

        /// <summary>
        /// Create ABSOLUTELY FIXED star tiers that NEVER change for ANY character
        /// Always creates exactly these 6 tiers in this exact order:
        /// Index 0: 1 grey star, Index 1: 2 grey stars, Index 2: 3 grey stars,
        /// Index 3: 1 orange star, Index 4: 1 gold star, Index 5: 1 purple star
        /// </summary>
        private void CreateAbsolutelyFixedStarTiers()
        {
            // FIXED TIER 0: 1 Grey Star
            CreateStaticStarTier(0, 1, greyStarSprite, "Tier 1: 1 Grey Star");

            // FIXED TIER 1: 2 Grey Stars
            CreateStaticStarTier(1, 2, greyStarSprite, "Tier 2: 2 Grey Stars");

            // FIXED TIER 2: 3 Grey Stars
            CreateStaticStarTier(2, 3, greyStarSprite, "Tier 3: 3 Grey Stars");

            // FIXED TIER 3: 1 Orange Star
            CreateStaticStarTier(3, 1, orangeStarSprite, "Tier 4: 1 Orange Star");

            // FIXED TIER 4: 1 Gold Star
            CreateStaticStarTier(4, 1, goldStarSprite, "Tier 5: 1 Gold Star");

            // FIXED TIER 5: 1 Purple Star
            CreateStaticStarTier(5, 1, purpleStarSprite, "Tier 6: 1 Purple Star (MAX)");
        }

        /// <summary>
        /// Create a single static star tier that never changes
        /// </summary>
        private void CreateStaticStarTier(int tierIndex, int starCount, Sprite starSprite, string description)
        {
            // Instantiate the description character stats prefab
            GameObject tierObj = Instantiate(descriptionCharacterStatsPrefab, upgradeStatsContainer);
            var tierBehavior = tierObj.GetComponent<DescriptionCharacterStatsBehavior>();

            if (tierBehavior == null)
            {
                Debug.LogError("[EquipmentWindow] DescriptionCharacterStatsBehavior component missing on prefab!");
                return;
            }

            // Initialize with FIXED values - no dynamic logic
            // tierBehavior.Initialize(milestoneId, starsToShow, starSprite, description, isUnlocked)
            tierBehavior.Initialize(tierIndex + 1, starCount, starSprite, description, true);
        }

        /// <summary>
        /// Create fixed tier row with predefined star configurations (LEGACY - being replaced)
        /// Index 0: 1 grey star, Index 1: 2 grey stars, Index 2: 3 grey stars,
        /// Index 3: 1 orange star, Index 4: 1 gold star, Index 5: 1 purple star
        /// </summary>
        private void CreateFixedTierRow(int tierIndex, int currentLevel, CharacterData characterData, int characterIndex)
        {
            // Instantiate the description character stats prefab
            GameObject tierObj = Instantiate(descriptionCharacterStatsPrefab, upgradeStatsContainer);
            var tierBehavior = tierObj.GetComponent<DescriptionCharacterStatsBehavior>();

            if (tierBehavior == null)
            {
                Debug.LogError("[EquipmentWindow] DescriptionCharacterStatsBehavior component missing on prefab!");
                return;
            }

            // Get fixed configuration for this tier index
            var (starCount, starSprite) = GetFixedTierConfig(tierIndex);

            // Determine how many stars to show based on current level vs this tier
            int starsToShow = 0;
            bool isUnlocked = currentLevel > tierIndex; // Tier is unlocked if current level is higher

            if (isUnlocked)
            {
                starsToShow = starCount; // Show all stars for completed tiers
            }
            else if (currentLevel == tierIndex)
            {
                starsToShow = 1; // Show partial progress for current tier
            }
            else
            {
                starsToShow = 0; // Show empty for future tiers
            }

            // Get tier description
            string description = GetFixedTierDescription(tierIndex, characterData);

            // Initialize the tier display with fixed configuration
            tierBehavior.Initialize(tierIndex + 1, starsToShow, starSprite, description, isUnlocked);
        }

        /// <summary>
        /// Create milestone row for a specific milestone
        /// </summary>
        private void CreateMilestoneRow(MilestoneConfig milestone, int currentLevel, CharacterData characterData, int characterIndex)
        {
            // Instantiate the description character stats prefab
            GameObject milestoneObj = Instantiate(descriptionCharacterStatsPrefab, upgradeStatsContainer);
            var milestoneBehavior = milestoneObj.GetComponent<DescriptionCharacterStatsBehavior>();

            if (milestoneBehavior == null)
            {
                Debug.LogError("[EquipmentWindow] DescriptionCharacterStatsBehavior component missing on prefab!");
                return;
            }

            // Determine how many stars to show for this milestone based on current level
            int starsToShow = 0;
            if (currentLevel >= milestone.endLevel)
            {
                starsToShow = 3; // Milestone completed - show all 3 stars
            }
            else if (currentLevel >= milestone.startLevel)
            {
                starsToShow = (currentLevel - milestone.startLevel) + 1; // Show progress within milestone
            }
            else
            {
                starsToShow = 0; // Milestone not started - show empty
            }

            // Get sprite for this milestone's star type
            Sprite starSprite = GetSpriteForStarType(milestone.starType);

            // Get milestone description
            string description = GetMilestoneDescription(milestone, characterData);

            // Check if milestone is unlocked (reached)
            bool isUnlocked = currentLevel >= milestone.startLevel;

            // Initialize the milestone display
            milestoneBehavior.Initialize(milestone.milestoneId, starsToShow, starSprite, description, isUnlocked);
        }

        /// <summary>
        /// Get milestone description
        /// </summary>
        private string GetMilestoneDescription(MilestoneConfig milestone, CharacterData characterData)
        {
            if (characterData?.UpgradeConfig?.milestoneDescriptions != null &&
                milestone.milestoneId <= characterData.UpgradeConfig.milestoneDescriptions.Length)
            {
                return characterData.UpgradeConfig.milestoneDescriptions[milestone.milestoneId - 1];
            }
            return $"+{milestone.hpBonus} HP & +{milestone.damageBonus} DMG";
        }

        /// <summary>
        /// Get fixed tier configuration for upgrade container (index-based)
        /// Index 0: 1 grey star, Index 1: 2 grey stars, Index 2: 3 grey stars,
        /// Index 3: 1 orange star, Index 4: 1 gold star, Index 5: 1 purple star
        /// </summary>
        private (int starCount, Sprite starSprite) GetFixedTierConfig(int tierIndex)
        {
            return tierIndex switch
            {
                0 => (1, greyStarSprite),     // Index 0: 1 grey star
                1 => (2, greyStarSprite),     // Index 1: 2 grey stars
                2 => (3, greyStarSprite),     // Index 2: 3 grey stars
                3 => (1, orangeStarSprite),   // Index 3: 1 orange star
                4 => (1, goldStarSprite),     // Index 4: 1 gold star
                5 => (1, purpleStarSprite),   // Index 5: 1 purple star
                _ => (1, emptyStarSprite)     // Fallback
            };
        }

        /// <summary>
        /// Get description for fixed tier index
        /// </summary>
        private string GetFixedTierDescription(int tierIndex, CharacterData characterData)
        {
            // Use configured descriptions if available
            if (characterData?.UpgradeConfig?.TierDescriptions != null &&
                tierIndex < characterData.UpgradeConfig.TierDescriptions.Length)
            {
                return characterData.UpgradeConfig.TierDescriptions[tierIndex];
            }

            // Fallback descriptions based on tier index
            return tierIndex switch
            {
                0 => $"+{GetTierStatBonus(tierIndex + 1, characterData)} HP/DMG (1 Grey Star)",
                1 => $"+{GetTierStatBonus(tierIndex + 1, characterData)} HP/DMG (2 Grey Stars)",
                2 => $"+{GetTierStatBonus(tierIndex + 1, characterData)} HP/DMG (3 Grey Stars)",
                3 => $"+{GetTierStatBonus(tierIndex + 1, characterData)} HP/DMG (Orange Star)",
                4 => $"+{GetTierStatBonus(tierIndex + 1, characterData)} HP/DMG (Gold Star)",
                5 => $"+{GetTierStatBonus(tierIndex + 1, characterData)} HP/DMG (Purple Star - MAX)",
                _ => "Unknown Upgrade"
            };
        }

        /// <summary>
        /// Get star configuration for specific tier (1-based, legacy method)
        /// </summary>
        private (int starCount, Sprite starSprite) GetTierStarConfig(int tier)
        {
            return tier switch
            {
                1 => (1, greyStarSprite),     // 1 grey star
                2 => (2, greyStarSprite),     // 2 grey stars
                3 => (3, greyStarSprite),     // 3 grey stars
                4 => (1, goldStarSprite),     // 1 gold star
                5 => (1, orangeStarSprite),   // 1 orange star
                6 => (1, purpleStarSprite),   // 1 purple star
                _ => (1, emptyStarSprite)     // Fallback
            };
        }

        /// <summary>
        /// Get description for upgrade tier based on character config
        /// </summary>
        private string GetTierDescription(int tier, CharacterData characterData)
        {
            if (characterData?.UpgradeConfig?.TierDescriptions != null &&
                tier <= characterData.UpgradeConfig.TierDescriptions.Length)
            {
                return characterData.UpgradeConfig.TierDescriptions[tier - 1];
            }

            // Fallback generic descriptions
            return tier switch
            {
                1 => $"+{GetTierStatBonus(tier, characterData)} HP/DMG",
                2 => $"+{GetTierStatBonus(tier, characterData)} HP/DMG",
                3 => $"+{GetTierStatBonus(tier, characterData)} HP/DMG",
                4 => $"+{GetTierStatBonus(tier, characterData)} HP/DMG",
                5 => $"Special Ability: {GetTierSpecialAbility(characterData)}", // Tier 5 special
                6 => $"+{GetTierStatBonus(tier, characterData)} HP/DMG (MAX)",
                _ => "Unknown Upgrade"
            };
        }

        /// <summary>
        /// Get stat bonus for tier (fallback if no config)
        /// </summary>
        private int GetTierStatBonus(int tier, CharacterData characterData)
        {
            if (characterData?.UpgradeConfig?.StatBonusPerTier != null)
            {
                return characterData.UpgradeConfig.StatBonusPerTier;
            }
            return tier * 10; // Fallback: 10 points per tier
        }

        /// <summary>
        /// Get special ability description for tier 5
        /// </summary>
        private string GetTierSpecialAbility(CharacterData characterData)
        {
            if (characterData?.UpgradeConfig?.SpecialAbilityDescription != null)
            {
                return characterData.UpgradeConfig.SpecialAbilityDescription;
            }
            return "Enhanced Combat Ability"; // Fallback
        }

        /// <summary>
        /// Update upgrade tab information (materials, cost, level)
        /// </summary>
        private void UpdateUpgradeTabInfo(CharacterData characterData, int characterIndex)
        {
            if (characterIndex < 0) return;

            int currentLevel = charactersSave.GetCharacterStarLevel(characterIndex);
            int maxLevel = 6; // Fixed 6-tier system

            // Update level display with new format: show current level vs max level for current star
            if (characterLevelText != null)
            {
                int currentStarLevel = charactersSave.GetCharacterStarLevel(characterIndex);
                int currentCharLevel = charactersSave.GetCharacterLevel(characterIndex);
                int maxLevelForStar = characterData.UpgradeConfig.GetMaxLevelForStar(currentStarLevel);

                // characterStarLevelText.text = $"Star: {currentStarLevel}/{maxLevel}";
                characterLevelText.text = $"Lv: {currentCharLevel}/{maxLevelForStar}";
            }

            // Check for auto star upgrade and update level upgrade button
            CheckAndPerformAutoStarUpgrade(characterData, characterIndex);
            UpdateUpgradeButton(characterData, characterIndex);
            UpdatePiecesProgressDisplay(characterData, characterIndex);
            UpdateAllStarsDisplay(characterData, characterIndex);
        }
        
        #endregion

        #region Updated Integration Methods

        /// <summary>
        /// Updated method to replace old UpdateStarsDisplay
        /// </summary>
        private void UpdateStarsDisplay(CharacterData characterData, int characterIndex)
        {
            // Use new 6-tier system instead
            UpdateCharacterUpgradeDisplay(characterData, characterIndex);
        }

        #endregion

        /// <summary>
        /// Check and perform auto star upgrade if player has enough pieces
        /// </summary>
        private void CheckAndPerformAutoStarUpgrade(CharacterData characterData, int characterIndex)
        {
            if (upgradeManager == null || characterIndex < 0 || characterData?.UpgradeConfig == null) return;

            int currentStarLevel = charactersSave.GetCharacterStarLevel(characterIndex);
            int maxStarLevel = 6; // Fixed 6-tier system

            // Keep checking and upgrading until we can't upgrade anymore
            while (currentStarLevel < maxStarLevel)
            {
                if (upgradeManager.CanUpgradeCharacterStar(characterIndex))
                {
                    if (upgradeManager.TryUpgradeCharacterStar(characterIndex))
                    {
                        currentStarLevel = charactersSave.GetCharacterStarLevel(characterIndex);
                        Debug.Log($"[EquipmentWindow] Auto upgraded character {characterIndex} to star level {currentStarLevel}");
                    }
                    else
                    {
                        break; // Failed to upgrade, stop trying
                    }
                }
                else
                {
                    break; // Can't upgrade, stop
                }
            }
        }

        /// <summary>
        /// Update upgrade button for level only (star upgrade is automatic)
        /// </summary>
        private void UpdateUpgradeButton(CharacterData characterData, int characterIndex)
        {
            if (upgradeButton == null || upgradeManager == null || characterIndex < 0) return;

            // Only show upgrade button for owned characters
            bool isOwned = charactersSave.HasCharacterBeenBought(characterIndex);
            upgradeButton.gameObject.SetActive(isOwned);

            if (!isOwned) return;

            int currentLevel = charactersSave.GetCharacterLevel(characterIndex);
            int maxLevelForStar = upgradeManager.GetMaxLevelForCurrentStar(characterIndex);

            bool canUpgrade = upgradeManager.CanUpgradeCharacterLevel(characterIndex) && currentLevel < maxLevelForStar;
            bool isMaxLevel = currentLevel >= maxLevelForStar;

            upgradeButton.interactable = canUpgrade;

            // Update level upgrade button text and cost
            if (isMaxLevel)
            {
                upgradeButton.image.sprite = disabledButtonSprite;
                if (upgradeButtonText != null)
                    upgradeButtonText.text = "MAX LEVEL FOR CURRENT STAR";
                if (upgradeCostText != null)
                    upgradeCostText.text = "0 Gold";
            }
            else if (canUpgrade)
            {
                upgradeButton.image.sprite = enabledButtonSprite;
                if (upgradeButtonText != null)
                {
                    int nextLevel = currentLevel + 1;
                    upgradeButtonText.text = $"UPGRADE TO LEVEL {nextLevel}";
                }

                int goldRequired = upgradeManager.GetLevelUpgradeCost(characterIndex);
                if (upgradeCostText != null)
                    upgradeCostText.text = $"{goldRequired} Gold";
            }
            else
            {
                upgradeButton.image.sprite = disabledButtonSprite;
                if (upgradeButtonText != null)
                    upgradeButtonText.text = "INSUFFICIENT GOLD";

                int goldRequired = upgradeManager.GetLevelUpgradeCost(characterIndex);
                if (upgradeCostText != null)
                    upgradeCostText.text = $"{goldRequired} Gold";
            }
        }

        /// <summary>
        /// Update pieces progress slider and text for character-specific pieces with sub-star system
        /// </summary>
        private void UpdatePiecesProgressDisplay(CharacterData characterData, int characterIndex)
        {
            if (characterIndex < 0 || characterData?.UpgradeConfig == null) return;

            int currentStarLevel = charactersSave.GetCharacterStarLevel(characterIndex);
            int currentSubStarProgress = charactersSave.GetCharacterSubStarProgress(characterIndex);
            int maxStarLevel = 6;
            int currentPieces = charactersSave.GetCharacterPieces(characterIndex);

            // Check if max star reached
            if (currentStarLevel >= maxStarLevel)
            {
                if (piecesProgressSlider != null)
                {
                    piecesProgressSlider.value = 1f;
                }
                if (piecesProgressText != null)
                {
                    piecesProgressText.text = "MAX STAR";
                    piecesProgressText.color = Color.yellow;
                }
                return;
            }

            // Get current tier config
            var currentTierConfig = characterData.UpgradeConfig.GetStarTierConfig(currentStarLevel);
            if (currentTierConfig == null) return;

            int piecesRequired;
            string upgradeText;

            // Determine what we're upgrading to and the cost
            if (currentSubStarProgress < currentTierConfig.subStarCount)
            {
                // Upgrading sub-star within current tier
                piecesRequired = characterData.UpgradeConfig.GetPiecesRequiredForSubStar(currentStarLevel, currentSubStarProgress);
                upgradeText = $"STAR {currentStarLevel} SUB-{currentSubStarProgress + 1}";
            }
            else if (currentStarLevel + 1 <= maxStarLevel)
            {
                // Moving to next tier
                piecesRequired = characterData.UpgradeConfig.GetPiecesRequiredForSubStar(currentStarLevel + 1, 0);
                upgradeText = $"NEXT STAR {currentStarLevel + 1}";
            }
            else
            {
                // This shouldn't happen, but safety check
                if (piecesProgressSlider != null)
                {
                    piecesProgressSlider.value = 1f;
                }
                if (piecesProgressText != null)
                {
                    piecesProgressText.text = "MAX STAR";
                    piecesProgressText.color = Color.yellow;
                }
                return;
            }

            // Update progress bar and text
            float progress = Mathf.Clamp01((float)currentPieces / piecesRequired);

            if (piecesProgressSlider != null)
            {
                piecesProgressSlider.value = progress;
            }

            if (piecesProgressText != null)
            {
                piecesProgressText.text = $"{upgradeText}: {currentPieces} / {piecesRequired} pieces";

                // Color coding based on progress
                if (currentPieces >= piecesRequired)
                {
                    piecesProgressText.color = Color.green; // Can upgrade
                }
                else if (progress >= 0.5f)
                {
                    piecesProgressText.color = Color.yellow; // Getting close
                }
                else
                {
                    piecesProgressText.color = Color.white; // Normal
                }
            }
        }

        /// <summary>
        /// Update all stars display with only 3 star images, star 1 always active
        /// </summary>
        private void UpdateAllStarsDisplay(CharacterData characterData, int characterIndex)
        {
            if (characterIndex < 0 || characterData?.UpgradeConfig == null || starImages == null) return;

            int currentStarLevel = charactersSave.GetCharacterStarLevel(characterIndex);
            int currentSubStarProgress = charactersSave.GetCharacterSubStarProgress(characterIndex);

            // Always ensure all 3 star images are active
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    starImages[i].gameObject.SetActive(true);
                }
            }

            // Update each of the 3 star images based on current progress
            for (int starIndex = 0; starIndex < 3; starIndex++)
            {
                if (starImages[starIndex] != null)
                {
                    UpdateSingleStarImage(starIndex, currentStarLevel, currentSubStarProgress);
                }
            }
        }

        /// <summary>
        /// Determine how a star tier should be displayed
        /// </summary>
        private StarDisplayState GetStarDisplayState(int starTier, int currentStarLevel, int currentSubStarProgress, StarTierConfig tierConfig)
        {
            if (starTier < currentStarLevel)
            {
                // Completed star tier - show all sub-stars as filled
                return new StarDisplayState
                {
                    isCompleted = true,
                    filledSubStars = tierConfig.subStarCount,
                    totalSubStars = tierConfig.subStarCount
                };
            }
            else if (starTier == currentStarLevel)
            {
                // Current star tier - show progress based on sub-star progress
                return new StarDisplayState
                {
                    isCompleted = false,
                    filledSubStars = currentSubStarProgress,
                    totalSubStars = tierConfig.subStarCount
                };
            }
            else
            {
                // Future star tier - show all as empty
                return new StarDisplayState
                {
                    isCompleted = false,
                    filledSubStars = 0,
                    totalSubStars = tierConfig.subStarCount
                };
            }
        }

        /// <summary>
        /// Update a single star image based on character progress
        /// Uses milestone system: Level 1-3 grey, 4-6 orange, 7-9 gold, 10-12 purple
        /// </summary>
        private void UpdateSingleStarImage(int starIndex, int currentStarLevel, int currentSubStarProgress)
        {
            Image starImage = starImages[starIndex];

            // Get character data to access upgrade config
            var characterData = charactersDatabase.GetCharacterData(currentDisplayedCharacterIndex);
            if (characterData?.UpgradeConfig == null)
            {
                starImage.sprite = emptyStarSprite;
                starImage.color = Color.gray;
                return;
            }

            // Get how many stars should be shown for current level
            int starsToShow = characterData.UpgradeConfig.GetStarsInMilestone(currentStarLevel);

            if (starIndex < starsToShow)
            {
                // This star should be shown - get appropriate sprite for current level
                Sprite starSprite = GetSpriteForStarType(characterData.UpgradeConfig.GetStarTypeForLevel(currentStarLevel));
                starImage.sprite = starSprite;
                starImage.color = Color.white;
            }
            else
            {
                // This star should be empty
                starImage.sprite = emptyStarSprite;
                starImage.color = Color.gray;
            }
        }

        /// <summary>
        /// Get sprite for star type
        /// </summary>
        private Sprite GetSpriteForStarType(StarType starType)
        {
            return starType switch
            {
                StarType.Grey => greyStarSprite,
                StarType.Orange => orangeStarSprite,
                StarType.Gold => goldStarSprite,
                StarType.Purple => purpleStarSprite,
                _ => greyStarSprite
            };
        }

        /// <summary>
        /// Get the appropriate sprite for a given star level
        /// Star 1-3: Grey stars, Star 4: Gold, Star 5: Orange, Star 6: Purple
        /// </summary>
        private Sprite GetSpriteForStarLevel(int starLevel)
        {
            return starLevel switch
            {
                1 => greyStarSprite,     // Star 1: Grey
                2 => greyStarSprite,     // Star 2: Grey
                3 => greyStarSprite,     // Star 3: Grey
                4 => goldStarSprite,     // Star 4: Gold
                5 => orangeStarSprite,   // Star 5: Orange
                6 => purpleStarSprite,   // Star 6: Purple
                _ => emptyStarSprite
            };
        }

        /// <summary>
        /// Data structure for star display state
        /// </summary>
        private struct StarDisplayState
        {
            public bool isCompleted;
            public int filledSubStars;
            public int totalSubStars;
        }

        private void OnUpgradeButtonClicked()
        {
            if (upgradeManager != null && currentDisplayedCharacterIndex >= 0)
            {
                if (upgradeManager.TryUpgradeCharacterLevel(currentDisplayedCharacterIndex))
                {
                    // Audio feedback will be handled by the upgrade manager
                    // The display will be updated by the OnCharacterUpgraded event
                    RefreshCurrentTab();
                }
            }
        }

        /// <summary>
        /// Refresh current tab to update display after upgrade
        /// </summary>
        private void RefreshCurrentTab()
        {
            if (!isEquipmentTabActive && currentDisplayedCharacter != null && currentDisplayedCharacterIndex >= 0)
            {
                // Refresh character tab display
                DisplayCharacterStats(currentDisplayedCharacter, currentDisplayedCharacterIndex);
            }
        }

        private void OnCharacterUpgraded(int characterId)
        {
            if (characterId == currentDisplayedCharacterIndex && currentDisplayedCharacter != null)
            {
                DisplayCharacterStats(currentDisplayedCharacter, currentDisplayedCharacterIndex);
            }

            // Always update current character stats in header if upgraded character is the selected one
            if (charactersSave != null && characterId == charactersSave.SelectedCharacterId)
            {
                var characterData = charactersDatabase.GetCharacterData(characterId);
                if (characterData != null)
                {
                    UpdateCurrentCharacterStats(characterData, characterId);
                    Debug.Log($"[EquipmentWindow] Updated current character stats after upgrade: {characterData.Name}");
                }
            }
        }

        private void OnGoldAmountChanged(int amount)
        {
            if (currentDisplayedCharacter != null && currentDisplayedCharacterIndex >= 0)
            {
                UpdateUpgradeButton(currentDisplayedCharacter, currentDisplayedCharacterIndex);
                UpdatePiecesProgressDisplay(currentDisplayedCharacter, currentDisplayedCharacterIndex);
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

        [ContextMenu("Test Character Upgrade UI")]
        public void TestCharacterUpgradeUI()
        {
            if (currentDisplayedCharacter != null && currentDisplayedCharacterIndex >= 0)
            {
                Debug.Log("=== Character Upgrade UI Test ===");
                Debug.Log($"Character: {currentDisplayedCharacter.Name}");
                Debug.Log($"Index: {currentDisplayedCharacterIndex}");
                Debug.Log($"Current Level: {charactersSave.GetCharacterStarLevel(currentDisplayedCharacterIndex)}");

                // Test upgrade display
                UpdateCharacterUpgradeDisplay(currentDisplayedCharacter, currentDisplayedCharacterIndex);
                Debug.Log("Upgrade display updated");
            }
            else
            {
                Debug.LogWarning("No character selected for upgrade test");
            }
        }

        [ContextMenu("Debug Character Upgrade Config")]
        public void DebugCharacterUpgradeConfig()
        {
            if (currentDisplayedCharacter?.UpgradeConfig != null)
            {
                var config = currentDisplayedCharacter.UpgradeConfig;
                Debug.Log("=== Character Upgrade Config ===");
                Debug.Log($"Max Stars: {config.MaxStars}");
                Debug.Log($"Stat Bonus Per Tier: {config.StatBonusPerTier}");
                Debug.Log($"Special Ability: {config.SpecialAbilityDescription}");

                for (int i = 0; i < config.TierDescriptions.Length; i++)
                {
                    Debug.Log($"Tier {i + 1}: {config.TierDescriptions[i]}");
                }

                for (int i = 0; i < config.MaterialRequirements.Length; i++)
                {
                    var req = config.MaterialRequirements[i];
                    Debug.Log($"Tier {i + 1} Materials: {req.materialName} x{req.amount}");
                }
            }
            else
            {
                Debug.LogWarning("No character or upgrade config selected");
            }
        }

        #endregion

    }
}