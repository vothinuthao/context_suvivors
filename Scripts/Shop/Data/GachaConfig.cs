using System.Linq;
using UnityEngine;
using OctoberStudio.Equipment;

namespace OctoberStudio.Shop
{
    [CreateAssetMenu(fileName = "Gacha Config", menuName = "October/Shop/Gacha Config")]
    public class GachaConfig : ScriptableObject
    {
        [Header("Pity System")]
        [SerializeField] private bool enablePitySystem = true;
        [SerializeField] private int pityCountForEpic = 20;
        [SerializeField] private int pityCountForLegendary = 50;
        [SerializeField] private bool resetPityOnEpic = true;
        [SerializeField] private bool resetPityOnLegendary = true;

        [Header("Guaranteed Rates")]
        [SerializeField] private bool guaranteedRareIn10Pulls = true;
        [SerializeField] private bool guaranteedEpicIn50Pulls = true;

        [Header("Rate Up Events")]
        [SerializeField] private bool enableRateUpEvents = true;
        [SerializeField] private float rateUpMultiplier = 2f;
        [SerializeField] private EquipmentType[] rateUpTypes;
        [SerializeField] private EquipmentRarity[] rateUpRarities;

        [Header("Animation Settings")]
        [SerializeField] private float singleGachaAnimationDuration = 2f;
        [SerializeField] private float multiGachaAnimationDuration = 3f;
        [SerializeField] private float itemRevealDelay = 0.1f;
        [SerializeField] private float epicItemScaleMultiplier = 1.1f;
        [SerializeField] private float legendaryItemScaleMultiplier = 1.2f;

        [Header("Audio")]
        [SerializeField] private bool playGachaSound = true;
        [SerializeField] private string gachaStartSoundName = "Gacha_Start";
        [SerializeField] private string gachaRevealSoundName = "Gacha_Reveal";
        [SerializeField] private string epicItemSoundName = "Epic_Item";
        [SerializeField] private string legendaryItemSoundName = "Legendary_Item";

        [Header("Visual Effects")]
        [SerializeField] private Color commonItemColor = Color.white;
        [SerializeField] private Color uncommonItemColor = Color.green;
        [SerializeField] private Color rareItemColor = Color.blue;
        [SerializeField] private Color epicItemColor = Color.magenta;
        [SerializeField] private Color legendaryItemColor = Color.yellow;

        // Properties
        public bool EnablePitySystem => enablePitySystem;
        public int PityCountForEpic => pityCountForEpic;
        public int PityCountForLegendary => pityCountForLegendary;
        public bool ResetPityOnEpic => resetPityOnEpic;
        public bool ResetPityOnLegendary => resetPityOnLegendary;
        public bool GuaranteedRareIn10Pulls => guaranteedRareIn10Pulls;
        public bool GuaranteedEpicIn50Pulls => guaranteedEpicIn50Pulls;
        public bool EnableRateUpEvents => enableRateUpEvents;
        public float RateUpMultiplier => rateUpMultiplier;
        public EquipmentType[] RateUpTypes => rateUpTypes;
        public EquipmentRarity[] RateUpRarities => rateUpRarities;
        public float SingleGachaAnimationDuration => singleGachaAnimationDuration;
        public float MultiGachaAnimationDuration => multiGachaAnimationDuration;
        public float ItemRevealDelay => itemRevealDelay;
        public float EpicItemScaleMultiplier => epicItemScaleMultiplier;
        public float LegendaryItemScaleMultiplier => legendaryItemScaleMultiplier;
        public bool PlayGachaSound => playGachaSound;
        public string GachaStartSoundName => gachaStartSoundName;
        public string GachaRevealSoundName => gachaRevealSoundName;
        public string EpicItemSoundName => epicItemSoundName;
        public string LegendaryItemSoundName => legendaryItemSoundName;

        /// <summary>
        /// Get color for rarity
        /// </summary>
        public Color GetRarityColor(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => commonItemColor,
                EquipmentRarity.Uncommon => uncommonItemColor,
                EquipmentRarity.Rare => rareItemColor,
                EquipmentRarity.Epic => epicItemColor,
                EquipmentRarity.Legendary => legendaryItemColor,
                _ => commonItemColor
            };
        }

        /// <summary>
        /// Get scale multiplier for rarity
        /// </summary>
        public float GetRarityScaleMultiplier(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Epic => epicItemScaleMultiplier,
                EquipmentRarity.Legendary => legendaryItemScaleMultiplier,
                _ => 1f
            };
        }

        /// <summary>
        /// Check if equipment type is in rate up
        /// </summary>
        public bool IsTypeRateUp(EquipmentType type)
        {
            if (!enableRateUpEvents || rateUpTypes == null)
                return false;

            return rateUpTypes.Contains(type);
        }

        /// <summary>
        /// Check if rarity is in rate up
        /// </summary>
        public bool IsRarityRateUp(EquipmentRarity rarity)
        {
            if (!enableRateUpEvents || rateUpRarities == null)
                return false;

            return rateUpRarities.Contains(rarity);
        }

        /// <summary>
        /// Calculate final gacha rate with bonuses
        /// </summary>
        public float CalculateFinalRate(float baseRate, EquipmentType type, EquipmentRarity rarity)
        {
            float finalRate = baseRate;

            // Apply rate up if applicable
            if (IsTypeRateUp(type) || IsRarityRateUp(rarity))
            {
                finalRate *= rateUpMultiplier;
            }

            return Mathf.Clamp01(finalRate);
        }

        /// <summary>
        /// Get sound name for rarity
        /// </summary>
        public string GetRaritySoundName(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Epic => epicItemSoundName,
                EquipmentRarity.Legendary => legendaryItemSoundName,
                _ => gachaRevealSoundName
            };
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        public bool ValidateConfig()
        {
            bool isValid = true;

            if (pityCountForEpic <= 0)
            {
                Debug.LogError("[GachaConfig] Pity count for epic must be greater than 0");
                isValid = false;
            }

            if (pityCountForLegendary <= pityCountForEpic)
            {
                Debug.LogError("[GachaConfig] Pity count for legendary must be greater than epic");
                isValid = false;
            }

            if (rateUpMultiplier <= 1f)
            {
                Debug.LogWarning("[GachaConfig] Rate up multiplier should be greater than 1");
            }

            if (singleGachaAnimationDuration <= 0f || multiGachaAnimationDuration <= 0f)
            {
                Debug.LogError("[GachaConfig] Animation durations must be positive");
                isValid = false;
            }

            return isValid;
        }

        private void OnValidate()
        {
            // Ensure values are within reasonable ranges
            pityCountForEpic = Mathf.Max(1, pityCountForEpic);
            pityCountForLegendary = Mathf.Max(pityCountForEpic + 1, pityCountForLegendary);
            rateUpMultiplier = Mathf.Max(1f, rateUpMultiplier);
            singleGachaAnimationDuration = Mathf.Max(0.1f, singleGachaAnimationDuration);
            multiGachaAnimationDuration = Mathf.Max(0.1f, multiGachaAnimationDuration);
            itemRevealDelay = Mathf.Max(0.01f, itemRevealDelay);
            epicItemScaleMultiplier = Mathf.Max(1f, epicItemScaleMultiplier);
            legendaryItemScaleMultiplier = Mathf.Max(epicItemScaleMultiplier, legendaryItemScaleMultiplier);
        }

        [ContextMenu("Reset to Default Values")]
        private void ResetToDefaults()
        {
            enablePitySystem = true;
            pityCountForEpic = 20;
            pityCountForLegendary = 50;
            resetPityOnEpic = true;
            resetPityOnLegendary = true;
            guaranteedRareIn10Pulls = true;
            guaranteedEpicIn50Pulls = true;
            enableRateUpEvents = true;
            rateUpMultiplier = 2f;
            singleGachaAnimationDuration = 2f;
            multiGachaAnimationDuration = 3f;
            itemRevealDelay = 0.1f;
            epicItemScaleMultiplier = 1.1f;
            legendaryItemScaleMultiplier = 1.2f;
            playGachaSound = true;
            
            // Reset colors
            commonItemColor = Color.white;
            uncommonItemColor = Color.green;
            rareItemColor = Color.blue;
            epicItemColor = Color.magenta;
            legendaryItemColor = Color.yellow;
        }
    }
}