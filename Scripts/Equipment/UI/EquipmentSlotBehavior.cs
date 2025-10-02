using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace OctoberStudio.Equipment.UI
{
    public class EquipmentSlotBehavior : MonoBehaviour
    {
        [Header("UI References")] [SerializeField]
        private Image iconImage;

        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private Button slotButton;

        [Header("Empty Slot")] [SerializeField]
        private Sprite emptySlotSprite;

        [SerializeField] private Color emptySlotColor = Color.gray;

        [Header("Equipment Type")] [SerializeField]
        private EquipmentType equipmentType;

        public EquipmentType EquipmentType => equipmentType;
        public EquipmentModel CurrentEquipment { get; private set; }

        public UnityEvent<EquipmentSlotBehavior> OnSlotClicked;

        private void Start()
        {
            // Subscribe to equipment changes
            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.OnEquipmentChanged.AddListener(OnEquipmentChanged);
            }

            // Setup button click listener
            if (slotButton != null)
            {
                slotButton.onClick.AddListener(() => OnSlotClicked?.Invoke(this));
            }

            // Initialize slot
            UpdateSlotDisplay();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.OnEquipmentChanged.RemoveListener(OnEquipmentChanged);
            }
        }

        private void OnEquipmentChanged(EquipmentType changedType)
        {
            if (changedType == equipmentType)
            {
                UpdateSlotDisplay();
            }
        }

        private void UpdateSlotDisplay()
        {
            if (EquipmentManager.Instance == null)
                return;

            CurrentEquipment = EquipmentManager.Instance.GetEquippedItem(equipmentType);

            if (CurrentEquipment != null)
            {
                // Show equipped item
                iconImage.sprite = CurrentEquipment.GetIcon();
                iconImage.color = Color.white;
                rarityBorder.sprite = CurrentEquipment.GetRarityIcon();
                rarityBorder.gameObject.SetActive(true);
            }
            else
            {
                // Show empty slot
                iconImage.sprite = emptySlotSprite;
                iconImage.color = emptySlotColor;
                rarityBorder.gameObject.SetActive(false);
            }
        }
    }
}