using UnityEngine;
using OctoberStudio.Abilities;

namespace OctoberStudio.Abilities
{
    [System.Serializable] 
    public abstract class BaseAOELevel : AbilityLevel
    {
        [Header("Damage")]
        [SerializeField, Min(0.1f)] protected float damage = 1f;
        public float Damage => damage;
        
        [SerializeField] protected float damageInterval = 0.5f;
        public float DamageInterval => damageInterval;
        
        [Header("Area")]
        [SerializeField] protected float areaRadius = 2f;
        public float AreaRadius => areaRadius;
        
        [Header("Effects")]
        [SerializeField] protected float effectDuration = 5f;
        public float EffectDuration => effectDuration;
    }
}