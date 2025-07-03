using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Common.Scripts.Equipment.UI
{
    public class InventoryItemBehavior : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Button itemButton;

        public EquipmentData EquipmentData { get; private set; }
        public EquipmentSave.InventoryItem InventoryItem { get; private set; }

        public UnityEvent<InventoryItemBehavior> OnItemClicked;

        private void Start()
        {
            // Setup button click listener
            if (itemButton != null)
            {
                itemButton.onClick.AddListener(() => OnItemClicked?.Invoke(this));
            }
        }

        public void Init(EquipmentSave.InventoryItem inventoryItem, EquipmentData equipmentData)
        {
            InventoryItem = inventoryItem;
            EquipmentData = equipmentData;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (EquipmentData == null)
                return;

            iconImage.sprite = EquipmentData.Icon;
            rarityBorder.color = EquipmentData.GetRarityColor();

            // Show quantity if more than 1
            quantityText.text = InventoryItem.quantity > 1 ? InventoryItem.quantity.ToString() : "";
            quantityText.gameObject.SetActive(InventoryItem.quantity > 1);

            // Show level if more than 1
            levelText.text = InventoryItem.level > 1 ? $"Lv.{InventoryItem.level}" : "";
            levelText.gameObject.SetActive(InventoryItem.level > 1);
        }
    }
}
