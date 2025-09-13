using UnityEngine;
using OctoberStudio.Abilities;

namespace OctoberStudio.Abilities
{
    [System.Serializable]
    public abstract class BaseMagicLevel : AbilityLevel
    {
        [Header("Damage")]
        [SerializeField, Min(0.1f)] protected float damage = 1f;
        public float Damage => damage;
        
        [Header("Timing")]
        [SerializeField] protected float abilityCooldown = 2f;
        public float AbilityCooldown => abilityCooldown;
        
        [Header("Magic Properties")]
        [SerializeField] protected float castTime = 0.5f;
        public float CastTime => castTime;
        
        [SerializeField] protected float effectRadius = 3f;
        public float EffectRadius => effectRadius;
    }
}