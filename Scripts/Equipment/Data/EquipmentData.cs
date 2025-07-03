using UnityEngine;

[System.Serializable]
public class EquipmentData
{
    [SerializeField] protected string name;
    public string Name => name;

    [SerializeField] protected string description;
    public string Description => description;

    [SerializeField] protected EquipmentType equipmentType;
    public EquipmentType EquipmentType => equipmentType;

    [SerializeField] protected EquipmentRarity rarity;
    public EquipmentRarity Rarity => rarity;

    [SerializeField] protected Sprite icon;
    public Sprite Icon => icon;

    [SerializeField] protected int level = 1;
    public int Level => level;

    // Equipment stats - similar to character base stats
    [Header("Equipment Stats")]
    [SerializeField] protected float bonusHP = 0f;
    public float BonusHP => bonusHP;

    [SerializeField] protected float bonusDamage = 0f;
    public float BonusDamage => bonusDamage;

    [SerializeField] protected float bonusSpeed = 0f;
    public float BonusSpeed => bonusSpeed;

    [SerializeField] protected float bonusMagnetRadius = 0f;
    public float BonusMagnetRadius => bonusMagnetRadius;

    [SerializeField] protected float bonusXPMultiplier = 0f;
    public float BonusXPMultiplier => bonusXPMultiplier;

    [SerializeField] protected float bonusCooldownReduction = 0f;
    public float BonusCooldownReduction => bonusCooldownReduction;

    [SerializeField] protected float bonusDamageReduction = 0f;
    public float BonusDamageReduction => bonusDamageReduction;

    // Get rarity color for UI display
    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case EquipmentRarity.Common: return Color.white;
            case EquipmentRarity.Uncommon: return Color.green;
            case EquipmentRarity.Rare: return Color.blue;
            case EquipmentRarity.Epic: return Color.magenta;
            case EquipmentRarity.Legendary: return Color.yellow;
            default: return Color.white;
        }
    }
}