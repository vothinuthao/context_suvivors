namespace OctoberStudio.Equipment
{
    [System.Serializable]
    public struct EquipmentStats
    {
        public float bonusHP;
        public float bonusDamage;
        public float bonusSpeed;
        public float bonusMagnetRadius;
        public float bonusXPMultiplier;
        public float bonusCooldownReduction;
        public float bonusDamageReduction;
    
        public static EquipmentStats operator +(EquipmentStats a, EquipmentStats b)
        {
            return new EquipmentStats
            {
                bonusHP = a.bonusHP + b.bonusHP,
                bonusDamage = a.bonusDamage + b.bonusDamage,
                bonusSpeed = a.bonusSpeed + b.bonusSpeed,
                bonusMagnetRadius = a.bonusMagnetRadius + b.bonusMagnetRadius,
                bonusXPMultiplier = a.bonusXPMultiplier + b.bonusXPMultiplier,
                bonusCooldownReduction = a.bonusCooldownReduction + b.bonusCooldownReduction,
                bonusDamageReduction = a.bonusDamageReduction + b.bonusDamageReduction
            };
        }
    
        public static EquipmentStats operator *(EquipmentStats stats, float multiplier)
        {
            return new EquipmentStats
            {
                bonusHP = stats.bonusHP * multiplier,
                bonusDamage = stats.bonusDamage * multiplier,
                bonusSpeed = stats.bonusSpeed * multiplier,
                bonusMagnetRadius = stats.bonusMagnetRadius * multiplier,
                bonusXPMultiplier = stats.bonusXPMultiplier * multiplier,
                bonusCooldownReduction = stats.bonusCooldownReduction * multiplier,
                bonusDamageReduction = stats.bonusDamageReduction * multiplier
            };
        }
    
        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string>();
        
            if (bonusHP > 0) parts.Add($"HP+{bonusHP:F0}");
            if (bonusDamage > 0) parts.Add($"DMG+{bonusDamage:F0}");
            if (bonusSpeed > 0) parts.Add($"SPD+{bonusSpeed*100:F0}%");
            if (bonusMagnetRadius > 0) parts.Add($"MAG+{bonusMagnetRadius:F1}");
            if (bonusXPMultiplier > 0) parts.Add($"XP+{bonusXPMultiplier*100:F0}%");
            if (bonusCooldownReduction > 0) parts.Add($"CD-{bonusCooldownReduction*100:F0}%");
            if (bonusDamageReduction > 0) parts.Add($"DEF+{bonusDamageReduction:F0}");
        
            return string.Join(", ", parts);
        }
    }
}