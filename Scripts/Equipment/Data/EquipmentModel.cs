using TwoSleepyCats.CSVReader.Attributes;
using TwoSleepyCats.CSVReader.Core;
using UnityEngine;

namespace OctoberStudio.Equipment
{
    [System.Serializable]
    public class EquipmentModel : ICsvModel
    {
        [CsvColumn("id")] public int ID { get; set; }
        [CsvColumn("name")] public string Name { get; set; }
        [CsvColumn("description")] public string Description { get; set; }
        [CsvColumn("equipment_type")] public string EquipmentTypeString { get; set; }
        [CsvColumn("rarity")] public string RarityString { get; set; }
        [CsvColumn("icon_name")] public string IconName { get; set; }
        [CsvColumn("level")] public int Level { get; set; } = 1;
    
        // Equipment stats
        [CsvColumn("bonus_hp")] public int BonusHP { get; set; }
        [CsvColumn("bonus_damage")] public int BonusDamage { get; set; }
        [CsvColumn("bonus_speed")] public float BonusSpeed { get; set; }
        [CsvColumn("bonus_magnet_radius")] public float BonusMagnetRadius { get; set; }
        [CsvColumn("bonus_xp_multiplier")] public float BonusXPMultiplier { get; set; }
        [CsvColumn("bonus_cooldown_reduction")] public float BonusCooldownReduction { get; set; }
        [CsvColumn("bonus_damage_reduction")] public float BonusDamageReduction { get; set; }
    
        // Parsed enum properties
        [CsvIgnore] 
        public EquipmentType EquipmentType 
        { 
            get 
            {
                if (System.Enum.TryParse<EquipmentType>(EquipmentTypeString, true, out var result))
                    return result;
                return EquipmentType.Armor;
            }
        }
    
        [CsvIgnore] 
        public EquipmentRarity Rarity 
        { 
            get 
            {
                if (System.Enum.TryParse<EquipmentRarity>(RarityString, true, out var result))
                    return result;
                return EquipmentRarity.Common;
            }
        }
    
        // Cached sprites
        [CsvIgnore] public Sprite Icon { get; private set; }
        [CsvIgnore] public Sprite RarityGem { get; private set; }
    
        public string GetCsvFileName() => "equipment.csv";
    
        public void OnDataLoaded()
        {
            LoadIcons();
        }
    
        private void LoadIcons()
        {
            if (!DataLoadingManager.Instance) 
            {
                Debug.LogWarning("[EquipmentModel] DataLoadingManager not found");
                return;
            }
        
            if (string.IsNullOrEmpty(IconName))
            {
                Debug.LogWarning($"[EquipmentModel] Empty IconName for equipment: {Name}");
                return;
            }
        
            // Load main icon using DataLoadingManager
            string category = GetIconCategory();
            Icon = DataLoadingManager.Instance.LoadSprite(category, IconName);
            // Load rarity gem
            string gemIcon = GetRarityGemIcon();
            RarityGem = DataLoadingManager.Instance.LoadSprite("Gems", gemIcon);
        
            if (RarityGem == null)
            {
                Debug.LogWarning($"[EquipmentModel] Rarity gem not found: {gemIcon}");
            }
        }
    
        private string GetIconCategory()
        {
            // Determine category based on icon name patterns
            if (IconName.Contains("accessories"))
                return "Accessories";
            else if (IconName.Contains("crown") || IconName.Contains("chest") || IconName.Contains("special"))
                return "Special";
            else if (IconName.Contains("gem"))
                return "Gems";
            else
                return "Equipment"; // Default for most equipment
        }
    
        private string GetRarityGemIcon()
        {
            switch (Rarity)
            {
                case EquipmentRarity.Common: return "icon_gem_0_blue";
                case EquipmentRarity.Uncommon: return "icon_gem_1_green";
                case EquipmentRarity.Rare: return "icon_gem_1_blue";
                case EquipmentRarity.Epic: return "icon_gem_1_purple";
                case EquipmentRarity.Legendary: return "icon_gem_2_blue";
                default: return "icon_gem_0_blue";
            }
        }
    
        public Color GetRarityColor()
        {
            switch (Rarity)
            {
                case EquipmentRarity.Common: return new Color(0.8f, 0.8f, 0.8f, 1f);      // Light Gray
                case EquipmentRarity.Uncommon: return new Color(0.2f, 0.8f, 0.2f, 1f);   // Green  
                case EquipmentRarity.Rare: return new Color(0.2f, 0.4f, 1f, 1f);         // Blue
                case EquipmentRarity.Epic: return new Color(0.6f, 0.2f, 0.8f, 1f);       // Purple
                case EquipmentRarity.Legendary: return new Color(1f, 0.8f, 0.2f, 1f);    // Gold
                default: return Color.white;
            }
        }
    
        /// <summary>
        /// Force reload icon if needed (for UI refresh)
        /// </summary>
        public void ReloadIcon()
        {
            LoadIcons();
        }
    
        public bool ValidateData()
        {
            bool isValid = ID >= 0 && !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(IconName);
        
            if (!isValid)
            {
                Debug.LogError($"[EquipmentModel] Invalid data for equipment ID: {ID}, Name: {Name}, IconName: {IconName}");
            }
        
            return isValid;
        }
    
        public override string ToString()
        {
            return $"{Name} ({EquipmentType}, {Rarity}) - Icon: {IconName}";
        }
    }
}