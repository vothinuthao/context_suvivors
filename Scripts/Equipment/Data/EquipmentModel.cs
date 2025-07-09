using OctoberStudio.Equipment;
using TwoSleepyCats.CSVReader.Attributes;
using TwoSleepyCats.CSVReader.Core;
using UnityEngine;

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
            return EquipmentType.Armor; // Default fallback
        }
    }
    
    [CsvIgnore] 
    public EquipmentRarity Rarity 
    { 
        get 
        {
            if (System.Enum.TryParse<EquipmentRarity>(RarityString, true, out var result))
                return result;
            return EquipmentRarity.Common; // Default fallback
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
    
    // ReSharper disable Unity.PerformanceAnalysis
    private void LoadIcons()
    {
        if (!DataLoadingManager.Instance) 
        {
            return;
        }
        
        string category = GetIconCategory();
        Icon = DataLoadingManager.Instance.LoadSprite(category, IconName);
        
        string gemIcon = GetRarityGemIcon();
        RarityGem = DataLoadingManager.Instance.LoadSprite("Gems", gemIcon);
    }
    
    private string GetIconCategory()
    {
        if (IconName.Contains("accessories"))
            return "Accessories";
        else if (IconName.Contains("crown") || IconName.Contains("special"))
            return "Special";
        else
            return "Equipment";
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
    /// Calculate total stats including level scaling
    /// </summary>
    public EquipmentStats GetTotalStats()
    {
        float levelMultiplier = 1f + (Level - 1) * 0.1f;
        
        return new EquipmentStats
        {
            bonusHP = BonusHP * levelMultiplier,
            bonusDamage = BonusDamage * levelMultiplier,
            bonusSpeed = BonusSpeed,
            bonusMagnetRadius = BonusMagnetRadius,
            bonusXPMultiplier = BonusXPMultiplier,
            bonusCooldownReduction = BonusCooldownReduction,
            bonusDamageReduction = BonusDamageReduction * levelMultiplier
        };
    }
    
    /// <summary>
    /// Get formatted stats text for UI display
    /// </summary>
    public string GetStatsText()
    {
        var stats = GetTotalStats();
        var statsText = "";
        
        if (stats.bonusHP > 0) 
            statsText += $"HP: +{stats.bonusHP:F0}\n";
        if (stats.bonusDamage > 0) 
            statsText += $"Damage: +{stats.bonusDamage:F0}\n";
        if (stats.bonusSpeed > 0) 
            statsText += $"Speed: +{stats.bonusSpeed * 100:F0}%\n";
        if (stats.bonusMagnetRadius > 0) 
            statsText += $"Magnet: +{stats.bonusMagnetRadius:F1}\n";
        if (stats.bonusXPMultiplier > 0) 
            statsText += $"XP: +{stats.bonusXPMultiplier * 100:F0}%\n";
        if (stats.bonusCooldownReduction > 0) 
            statsText += $"Cooldown: -{stats.bonusCooldownReduction * 100:F0}%\n";
        if (stats.bonusDamageReduction > 0) 
            statsText += $"Defense: +{stats.bonusDamageReduction:F0}\n";
        
        return statsText.TrimEnd('\n');
    }
    
    /// <summary>
    /// Get formatted name with level if above 1
    /// </summary>
    public string GetDisplayName()
    {
        return Level > 1 ? $"{Name} +{Level}" : Name;
    }
    
    /// <summary>
    /// Check if equipment has any stat bonuses
    /// </summary>
    public bool HasBonuses()
    {
        return BonusHP > 0 || BonusDamage > 0 || BonusSpeed > 0 || 
               BonusMagnetRadius > 0 || BonusXPMultiplier > 0 || 
               BonusCooldownReduction > 0 || BonusDamageReduction > 0;
    }
    
    public bool ValidateData()
    {
        bool isValid = ID >= 0 && !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(IconName);
        return isValid;
    }
    
    public override string ToString()
    {
        return $"{GetDisplayName()} ({EquipmentType}, {Rarity})";
    }
}