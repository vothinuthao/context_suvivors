using UnityEngine;
using OctoberStudio.Abilities;

namespace OctoberStudio.Abilities
{
    [System.Serializable]
    public abstract class BaseProjectileLevel : AbilityLevel
    {
        [Header("Damage")]
        [SerializeField, Min(0.1f)] protected float damage = 1f;
        public float Damage => damage;
        
        [Header("Timing")]
        [SerializeField] protected float abilityCooldown = 1f;
        public float AbilityCooldown => abilityCooldown;
        
        [Header("Projectile")]
        [SerializeField] protected float projectileSpeed = 5f;
        public float ProjectileSpeed => projectileSpeed;
        
        [SerializeField] protected float projectileLifetime = 3f;
        public float ProjectileLifetime => projectileLifetime;
        
        [SerializeField] protected float projectileSize = 1f;
        public float ProjectileSize => projectileSize;
    }
}