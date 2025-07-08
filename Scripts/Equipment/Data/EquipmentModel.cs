using TwoSleepyCats.CSVReader.Attributes;
using TwoSleepyCats.CSVReader.Core;
using UnityEngine;

[System.Serializable]
public class EquipmentModel : ICsvModel
{
    [CsvColumn("id")]
    public int ID { get; set; }
    
    [CsvColumn("name")]
    public string Name { get; set; }
    
    [CsvColumn("description")]
    public string Description { get; set; }
    
    [CsvColumn("equipment_type")]
    public EquipmentType EquipmentType { get; set; }
    
    [CsvColumn("rarity")]
    public EquipmentRarity Rarity { get; set; }
    
    [CsvColumn("icon_path")]
    public string IconPath { get; set; }
    
    [CsvColumn("level")]
    public int Level { get; set; }
    
    // Equipment stats
    [CsvColumn("bonus_hp")]
    public float BonusHP { get; set; }
    
    [CsvColumn("bonus_damage")]
    public float BonusDamage { get; set; }
    
    [CsvColumn("bonus_speed")]
    public float BonusSpeed { get; set; }
    
    [CsvColumn("bonus_magnet_radius")]
    public float BonusMagnetRadius { get; set; }
    
    [CsvColumn("bonus_xp_multiplier")]
    public float BonusXPMultiplier { get; set; }
    
    [CsvColumn("bonus_cooldown_reduction")]
    public float BonusCooldownReduction { get; set; }
    
    [CsvColumn("bonus_damage_reduction")]
    public float BonusDamageReduction { get; set; }
    
    // Cached icon sprite
    [CsvIgnore]
    public Sprite Icon { get; private set; }
    
    // CSV Model interface implementation
    public string GetCsvFileName() => "equipment.csv";
    
    public void OnDataLoaded()
    {
        // Load icon sprite from Resources
        if (!string.IsNullOrEmpty(IconPath))
        {
            Icon = Resources.Load<Sprite>(IconPath);
            if (Icon == null)
            {
                Debug.LogWarning($"[EquipmentData] Icon not found at path: {IconPath} for equipment: {Name}");
            }
        }
    }
    
    public bool ValidateData()
    {
        return ID >= 0 && !string.IsNullOrEmpty(Name);
    }
    
    // Get rarity color for UI display
    public Color GetRarityColor()
    {
        switch (Rarity)
        {
            case EquipmentRarity.Common: return Color.white;
            case EquipmentRarity.Uncommon: return Color.green;
            case EquipmentRarity.Rare: return Color.blue;
            case EquipmentRarity.Epic: return Color.magenta;
            case EquipmentRarity.Legendary: return Color.yellow;
            default: return Color.white;
        }
    }
    
    // Get stats text for UI display
    public string GetStatsText()
    {
        var statsText = "";
        if (BonusHP > 0) statsText += $"HP: +{BonusHP}\n";
        if (BonusDamage > 0) statsText += $"Damage: +{BonusDamage}\n";
        if (BonusSpeed > 0) statsText += $"Speed: +{BonusSpeed * 100:F0}%\n";
        if (BonusMagnetRadius > 0) statsText += $"Magnet: +{BonusMagnetRadius}\n";
        if (BonusXPMultiplier > 0) statsText += $"XP: +{BonusXPMultiplier * 100:F0}%\n";
        if (BonusCooldownReduction > 0) statsText += $"Cooldown: -{BonusCooldownReduction * 100:F0}%\n";
        if (BonusDamageReduction > 0) statsText += $"Defense: +{BonusDamageReduction * 100:F0}%\n";
        
        return statsText.TrimEnd('\n');
    }
    
    // Get formatted name with level
    public string GetDisplayName()
    {
        return Level > 1 ? $"{Name} +{Level}" : Name;
    }
    
    // Check if equipment has any bonuses
    public bool HasBonuses()
    {
        return BonusHP > 0 || BonusDamage > 0 || BonusSpeed > 0 || 
               BonusMagnetRadius > 0 || BonusXPMultiplier > 0 || 
               BonusCooldownReduction > 0 || BonusDamageReduction > 0;
    }
    
    // Calculate equipment value based on stats (useful for sorting)
    public float CalculateValue()
    {
        float value = 0f;
        value += BonusHP * 1f;
        value += BonusDamage * 2f;
        value += BonusSpeed * 50f;
        value += BonusMagnetRadius * 10f;
        value += BonusXPMultiplier * 30f;
        value += BonusCooldownReduction * 40f;
        value += BonusDamageReduction * 60f;
        
        // Multiply by rarity
        switch (Rarity)
        {
            case EquipmentRarity.Common: return value * 1f;
            case EquipmentRarity.Uncommon: return value * 1.5f;
            case EquipmentRarity.Rare: return value * 2f;
            case EquipmentRarity.Epic: return value * 3f;
            case EquipmentRarity.Legendary: return value * 5f;
            default: return value;
        }
    }
    
    public override string ToString()
    {
        return $"{GetDisplayName()} ({EquipmentType}, {Rarity})";
    }
}