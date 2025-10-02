using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio
{
    [System.Serializable]
    public class CharacterData
    {
        [SerializeField] protected string name;
        public string Name => name;

        [SerializeField] protected int cost;
        public int Cost => cost;

        [SerializeField] protected Sprite icon;
        public Sprite Icon => icon;

        [SerializeField] protected GameObject prefab;
        public GameObject Prefab => prefab;
        
        [Space]
        [Header("Element System")]
        [SerializeField] protected ElementType elementType = ElementType.None;
        public ElementType ElementType => elementType;

        [Space]
        [SerializeField] protected bool hasStartingAbility = false;
        public bool HasStartingAbility => hasStartingAbility;

        [SerializeField] protected AbilityType startingAbility;
        public AbilityType StartingAbility => startingAbility;

        [Space]
        [Header("Base Stats")]
        [SerializeField, Min(1)] protected float baseHP;
        public float BaseHP => baseHP;

        [SerializeField, Min(1f)] protected float baseDamage;
        public float BaseDamage => baseDamage;

        [Space]
        [Header("Star Upgrade System")]
        [SerializeField] protected CharacterUpgradeConfig upgradeConfig;
        public CharacterUpgradeConfig UpgradeConfig => upgradeConfig;

        public float GetHPAtStarLevel(int starLevel)
        {
            if (upgradeConfig == null || starLevel <= 0) return baseHP;
            return baseHP + (upgradeConfig.hpPerStar * starLevel);
        }

        public float GetDamageAtStarLevel(int starLevel)
        {
            if (upgradeConfig == null || starLevel <= 0) return baseDamage;
            return baseDamage + (upgradeConfig.damagePerStar * starLevel);
        }

        public int GetUpgradeCostForStarLevel(int starLevel)
        {
            if (upgradeConfig == null || starLevel <= 0) return 0;
            return upgradeConfig.baseCost + (upgradeConfig.costPerStar * (starLevel - 1));
        }
    }
}