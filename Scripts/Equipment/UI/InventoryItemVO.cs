using OctoberStudio.Equipment;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Common.Scripts.Equipment.UI
{
    public class InventoryItemVO : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text uidText; // Optional: Show short UID for debugging
        [SerializeField] private Button itemButton;
        [SerializeField] private GameObject equippedIndicator; // Visual indicator for equipped items

        public EquipmentModel EquipmentModel { get; private set; }
        public EquipmentSave.InventoryItem InventoryItem { get; private set; }

        public UnityEvent<InventoryItemVO> OnItemClicked;

        private void Start()
        {
            // Setup button click listener
            if (itemButton != null)
            {
                itemButton.onClick.AddListener(() => OnItemClicked?.Invoke(this));
            }
        }

        public void Init(EquipmentSave.InventoryItem inventoryItem, EquipmentModel equipmentModel)
        {
            InventoryItem = inventoryItem;
            EquipmentModel = equipmentModel;
            UpdateDisplay();
        }
    
        private void UpdateDisplay()
        {
            if (EquipmentModel == null || InventoryItem == null)
                return;

            // Set icon and rarity border
            iconImage.sprite = EquipmentModel.GetIcon();
            rarityBorder.sprite = EquipmentModel.GetRarityIcon();

            // Show level if more than 1
            if (levelText != null)
            {
                levelText.text = InventoryItem.level > 1 ? $"Lv.{InventoryItem.level}" : "";
                levelText.gameObject.SetActive(InventoryItem.level > 1);
            }

            // Show short UID for debugging (optional)
            if (uidText != null)
            {
                // Show first 8 characters of UID
                string shortUID = InventoryItem.uid.Length > 8 ? InventoryItem.uid.Substring(0, 8) : InventoryItem.uid;
                uidText.text = shortUID;
                uidText.gameObject.SetActive(Application.isEditor); // Only show in editor
            }

            // Update equipped indicator
            UpdateEquippedIndicator();
        }

        private void UpdateEquippedIndicator()
        {
            if (equippedIndicator == null) return;

            // Check if this item is currently equipped
            bool isEquipped = false;
            if (EquipmentManager.Instance != null)
            {
                isEquipped = EquipmentManager.Instance.IsItemEquipped(InventoryItem.uid);
            }

            equippedIndicator.SetActive(isEquipped);
        }

        /// <summary>
        /// Refresh the equipped indicator (call when equipment changes)
        /// </summary>
        public void RefreshEquippedStatus()
        {
            UpdateEquippedIndicator();
        }

        /// <summary>
        /// Get the unique identifier of this item
        /// </summary>
        public string GetUID()
        {
            return InventoryItem?.uid ?? "";
        }

        /// <summary>
        /// Check if this item can be equipped
        /// </summary>
        public bool CanBeEquipped()
        {
            if (InventoryItem == null || EquipmentManager.Instance == null)
                return false;

            // Item can be equipped if it's not already equipped
            return !EquipmentManager.Instance.IsItemEquipped(InventoryItem.uid);
        }

        /// <summary>
        /// Equip this item
        /// </summary>
        public bool EquipItem()
        {
            if (InventoryItem == null || EquipmentManager.Instance == null)
                return false;

            bool success = EquipmentManager.Instance.EquipItemByUID(InventoryItem.uid);
            if (success)
            {
                UpdateEquippedIndicator();
            }
            return success;
        }

        /// <summary>
        /// Get formatted item info for tooltips
        /// </summary>
        public string GetItemInfo()
        {
            if (EquipmentModel == null || InventoryItem == null)
                return "Invalid Item";

            var info = $"{EquipmentModel.GetDisplayName()}\n";
            info += $"Type: {EquipmentModel.EquipmentType}\n";
            info += $"Rarity: {EquipmentModel.Rarity}\n";
            
            if (InventoryItem.level > 1)
                info += $"Level: {InventoryItem.level}\n";
                
            info += $"UID: {InventoryItem.uid}\n";
            info += $"Created: {InventoryItem.createdAt:yyyy-MM-dd HH:mm}\n";
            
            var stats = EquipmentModel.GetStatsText();
            if (!string.IsNullOrEmpty(stats))
            {
                info += $"\nStats:\n{stats}";
            }

            return info;
        }

        /// <summary>
        /// Compare two inventory items for sorting
        /// </summary>
        public static int Compare(InventoryItemVO a, InventoryItemVO b)
        {
            if (a?.EquipmentModel == null || b?.EquipmentModel == null)
                return 0;

            // First sort by equipment type
            int typeCompare = a.EquipmentModel.EquipmentType.CompareTo(b.EquipmentModel.EquipmentType);
            if (typeCompare != 0) return typeCompare;

            // Then by rarity (higher rarity first)
            int rarityCompare = b.EquipmentModel.Rarity.CompareTo(a.EquipmentModel.Rarity);
            if (rarityCompare != 0) return rarityCompare;

            // Then by level (higher level first)
            int levelCompare = b.InventoryItem.level.CompareTo(a.InventoryItem.level);
            if (levelCompare != 0) return levelCompare;

            // Finally by creation date (newer first)
            return b.InventoryItem.createdAt.CompareTo(a.InventoryItem.createdAt);
        }

        private void OnDestroy()
        {
            // Clean up button listener
            if (itemButton != null)
            {
                itemButton.onClick.RemoveAllListeners();
            }
        }
    }
}