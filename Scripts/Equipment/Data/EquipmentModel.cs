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
    
    [CsvColumn("drop_rate")] public float DropRate { get; set; } = 0f;
    [CsvColumn("min_stage_level")] public int MinStageLevel { get; set; } = 1;
    [CsvColumn("max_stage_level")] public int MaxStageLevel { get; set; } = 5;
    [CsvColumn("drop_from_boss")] public bool DropFromBoss { get; set; } = false;
    [CsvColumn("drop_from_sub_enemy")] public bool DropFromSubEnemy { get; set; } = false;
    
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
    
    public string GetCsvFileName() => "equipment.csv";
    
    public void OnDataLoaded()
    {
    }

    private string GETPATHICONS = "Equipment";
    /// <summary>
    /// Get equipment icon sprite based on IconName
    /// </summary>
    public Sprite GetIcon()
    {
        if (!DataLoadingManager.Instance || string.IsNullOrEmpty(IconName))
        {
            return null;
        }
        return DataLoadingManager.Instance.LoadSprite(GETPATHICONS, IconName);
    }
    
    /// <summary>
    /// Get rarity gem sprite
    /// </summary>
    public Sprite GetRarityGem()
    {
        if (!DataLoadingManager.Instance)
        {
            return null;
        }
        
        string gemIcon = GetRarityGemIcon();
        return DataLoadingManager.Instance.LoadSprite("Gems", gemIcon);
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
        float levelMultiplier = 1f + (Level - 1) * 0.1f; // 10% increase per level
        
        return new EquipmentStats
        {
            bonusHP = BonusHP * levelMultiplier,
            bonusDamage = BonusDamage * levelMultiplier,
            bonusSpeed = BonusSpeed, // Speed doesn't scale with level
            bonusMagnetRadius = BonusMagnetRadius, // Magnet doesn't scale with level
            bonusXPMultiplier = BonusXPMultiplier, // XP multiplier doesn't scale
            bonusCooldownReduction = BonusCooldownReduction, // Cooldown doesn't scale
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
    
    /// <summary>
    /// Get equipment power score (for sorting/comparison)
    /// </summary>
    public float GetPowerScore()
    {
        var stats = GetTotalStats();
        return stats.bonusHP * 0.5f + 
               stats.bonusDamage * 2f + 
               stats.bonusSpeed * 100f + 
               stats.bonusMagnetRadius * 10f + 
               stats.bonusXPMultiplier * 50f + 
               stats.bonusCooldownReduction * 100f + 
               stats.bonusDamageReduction * 1.5f;
    }
    
    /// <summary>
    /// Check if this equipment can drop from specified enemy type and stage level
    /// </summary>
    public bool CanDropFromEnemy(bool isBoss, int stageLevel)
    {
        bool validEnemyType = isBoss ? DropFromBoss : DropFromSubEnemy;
        bool validStageLevel = stageLevel >= MinStageLevel && stageLevel <= MaxStageLevel;
        return validEnemyType && validStageLevel && DropRate > 0f;
    }
    /// <summary>
    /// Roll for drop chance with multiplier
    /// </summary>
    public bool RollForDrop(float multiplier = 1f)
    {
        return Random.Range(0f, 100f) < (DropRate * multiplier);
    }
    /// <summary>
    /// Check if equipment has drop configuration
    /// </summary>
    public bool HasDropConfig()
    {
        return DropRate > 0f && (DropFromBoss || DropFromSubEnemy);
    }
    
    /// <summary>
    /// Get upgrade cost for next level
    /// </summary>
    public int GetUpgradeCost()
    {
        int baseCost = Level * 100;
        float rarityMultiplier = 1f + (int)Rarity * 0.5f;
        return Mathf.RoundToInt(baseCost * rarityMultiplier);
    }
    
    /// <summary>
    /// Check if equipment can be upgraded
    /// </summary>
    public bool CanUpgrade(int maxLevel = 10)
    {
        return Level < maxLevel;
    }
    
    /// <summary>
    /// Upgrade equipment to next level
    /// </summary>
    public void UpgradeLevel()
    {
        Level++;
    }
    
    /// <summary>
    /// Get stat comparison with another equipment
    /// </summary>
    public string GetStatComparison(EquipmentModel other)
    {
        if (other == null || other.EquipmentType != EquipmentType)
            return "";
            
        var myStats = GetTotalStats();
        var otherStats = other.GetTotalStats();
        var comparison = "";
        
        float hpDiff = myStats.bonusHP - otherStats.bonusHP;
        if (hpDiff != 0)
            comparison += $"HP: {(hpDiff > 0 ? "+" : "")}{hpDiff:F0}\n";
            
        float dmgDiff = myStats.bonusDamage - otherStats.bonusDamage;
        if (dmgDiff != 0)
            comparison += $"Damage: {(dmgDiff > 0 ? "+" : "")}{dmgDiff:F0}\n";
            
        float speedDiff = myStats.bonusSpeed - otherStats.bonusSpeed;
        if (speedDiff != 0)
            comparison += $"Speed: {(speedDiff > 0 ? "+" : "")}{speedDiff * 100:F0}%\n";
            
        // Add more stat comparisons as needed...
        
        return comparison.TrimEnd('\n');
    }
    
    public bool ValidateData()
    {
        bool isValid = ID >= 0 && !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(IconName);
        
        if (!isValid)
        {
            Debug.LogError($"[EquipmentModel] Invalid data - ID: {ID}, Name: '{Name}', Icon: '{IconName}'");
        }
        
        return isValid;
    }
    
    
    public override string ToString()
    {
        return $"{GetDisplayName()} ({EquipmentType}, {Rarity}) - Power: {GetPowerScore():F0}";
    }
}