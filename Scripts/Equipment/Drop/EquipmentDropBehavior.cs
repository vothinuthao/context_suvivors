using OctoberStudio.Drop;
using OctoberStudio.Easing;
using UnityEngine;
using TMPro;
using System.Linq;

namespace OctoberStudio.Equipment.Drop
{
    public class EquipmentDropBehavior : DropBehavior
    {
        [Header("Equipment Drop Settings")]
        [SerializeField] private int equipmentGlobalId;
        [SerializeField] private int equipmentLevel = 1;
        [SerializeField] private EquipmentRarity rarity;
        
        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer equipmentIcon;
        [SerializeField] private SpriteRenderer rarityBorder;
        [SerializeField] private ParticleSystem rarityGlowEffect;
        [SerializeField] private GameObject levelIndicator;
        [SerializeField] private TMP_Text levelText;
        
        [Header("Animation")]
        [SerializeField] private float spawnAnimationDuration = 0.5f;
        [SerializeField] private AnimationCurve spawnCurve;
        
        private EquipmentModel equipmentData;
        private Vector3 originalScale;
        private bool isSetup;

        private void Awake()
        {
            originalScale = transform.localScale;
            
            if (spawnCurve == null || spawnCurve.keys.Length == 0)
            {
                spawnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
        }

        /// <summary>
        /// Setup equipment drop with specific equipment data
        /// </summary>
        public void Setup(int globalId, int level, EquipmentRarity itemRarity)
        {
            equipmentGlobalId = globalId;
            equipmentLevel = level;
            rarity = itemRarity;
            
            // Get equipment data from database
            equipmentData = EquipmentDatabase.Instance?.GetEquipmentByGlobalId(globalId);
            
            if (equipmentData != null)
            {
                SetupVisuals();
                PlaySpawnAnimation();
                isSetup = true;
            }
            else
            {
                Debug.LogError($"[EquipmentDropBehavior] Equipment with ID {globalId} not found in database!");
            }
        }

        private void SetupVisuals()
        {
            // Set equipment icon
            if (equipmentIcon != null && equipmentData != null)
            {
                equipmentIcon.sprite = equipmentData.GetIcon();
                equipmentIcon.color = Color.white;
            }

            // Set rarity border
            if (rarityBorder != null)
            {
                rarityBorder.color = equipmentData.GetRarityColor();
            }

            // Set up glow effect
            if (rarityGlowEffect != null)
            {
                var main = rarityGlowEffect.main;
                main.startColor = equipmentData.GetRarityColor();
                rarityGlowEffect.Play();
            }

            // Set level indicator
            if (levelIndicator != null && levelText != null)
            {
                if (equipmentLevel > 1)
                {
                    levelIndicator.SetActive(true);
                    levelText.text = $"+{equipmentLevel}";
                }
                else
                {
                    levelIndicator.SetActive(false);
                }
            }
        }

        private void PlaySpawnAnimation()
        {
            // Start with scale 0
            transform.localScale = Vector3.zero;
            
            // Animate to original scale
            transform.DoLocalScale(originalScale, spawnAnimationDuration)
                .SetEasingCurve(spawnCurve);
        }

        public override void OnPickedUp()
        {
            if (!isSetup || equipmentData == null)
            {
                Debug.LogError("[EquipmentDropBehavior] Trying to pick up equipment drop that wasn't properly setup!");
                return;
            }

            // Play base pickup effects
            base.OnPickedUp();

            // Add equipment to inventory
            if (EquipmentManager.Instance != null)
            {
                var addedItem = EquipmentManager.Instance.AddEquipmentToInventory(equipmentGlobalId, equipmentLevel);
                
                if (addedItem != null)
                {
                    // Show special notification
                    ShowEquipmentNotification();
                    
                    // Play rarity-based sound
                    PlayRarityBasedSound();
                    
                    // Log for debugging
                     Debug.Log($"[EquipmentDropBehavior] Picked up: {equipmentData.GetDisplayName()} (UID: {addedItem.uid})");
                }
                else
                {
                    Debug.LogError($"[EquipmentDropBehavior] Failed to add equipment to inventory: {equipmentData.Name}");
                }
            }

            // Deactivate the drop
            gameObject.SetActive(false);
        }

        private void ShowEquipmentNotification()
        {
            string message = GetNotificationMessage();
            
            // Show world space text if available
            if (StageController.WorldSpaceTextManager != null)
            {
                // StageController.WorldSpaceTextManager.ShowText(
                //     transform.position + Vector3.up * 0.5f, 
                //     message, 
                //     equipmentData.GetRarityColor()
                // );
            }
            Debug.Log($"Equipment Notification: {message}");
        }

        private string GetNotificationMessage()
        {
            string levelSuffix = equipmentLevel > 1 ? $" +{equipmentLevel}" : "";
            string rarityIcon = GetRarityIcon(rarity);
            
            return $"{rarityIcon} {equipmentData.Name}{levelSuffix} Found!";
        }

        private string GetRarityIcon(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => "⚪",
                EquipmentRarity.Uncommon => "🟢", 
                EquipmentRarity.Rare => "🔵",
                EquipmentRarity.Epic => "🟣",
                EquipmentRarity.Legendary => "🟡",
                _ => "⚪"
            };
        }

        private void PlayRarityBasedSound()
        {
            if (GameController.AudioManager != null)
            {
                string soundName = $"EquipmentDrop_{rarity}";
                int soundHash = soundName.GetHashCode();
                GameController.AudioManager.PlaySound(soundHash);
            }
        }

        /// <summary>
        /// Get equipment info for debugging
        /// </summary>
        public string GetEquipmentInfo()
        {
            if (equipmentData == null)
                return "No equipment data";
                
            return $"{equipmentData.GetDisplayName()} (ID: {equipmentGlobalId}, Level: {equipmentLevel}, Rarity: {rarity})";
        }

        /// <summary>
        /// Validate that this drop is properly setup
        /// </summary>
        public bool IsValidDrop()
        {
            return isSetup && equipmentData != null && equipmentGlobalId > 0;
        }

        // Reset for object pooling
        private void OnDisable()
        {
            // Reset state for pooling
            isSetup = false;
            equipmentData = null;
            equipmentGlobalId = 0;
            equipmentLevel = 1;
            
            // Reset transform
            transform.localScale = originalScale;
            
            // Stop particle effects
            if (rarityGlowEffect != null && rarityGlowEffect.isPlaying)
            {
                rarityGlowEffect.Stop();
            }
        }

        // Debug method
        [ContextMenu("Test Equipment Drop")]
        private void TestEquipmentDrop()
        {
            if (Application.isPlaying && EquipmentDatabase.Instance.IsDataLoaded)
            {
                var randomEquipment = EquipmentDatabase.Instance.GetAllEquipment().FirstOrDefault();
                if (randomEquipment != null)
                {
                    Setup(randomEquipment.ID, 1, randomEquipment.Rarity);
                }
            }
        }
    }
}