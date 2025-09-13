using UnityEngine;

namespace OctoberStudio.Abilities
{
    [CreateAssetMenu(fileName = "Bow Shot Data", menuName = "October/Abilities/Active/Bow Shot")]
    public class BowShotAbilityData : GenericAbilityData<BowShotAbilityLevel>
    {
        private void Awake() 
        { 
            type = AbilityType.BowShot; 
            isWeaponAbility = true; 
        }
        
        private void OnValidate() 
        { 
            type = AbilityType.BowShot; 
            isWeaponAbility = true; 
        }
    }

    [System.Serializable]
    public class BowShotAbilityLevel : BaseProjectileLevel
    {
        [Header("Multi-Shot")]
        [SerializeField] private int arrowCount = 3;
        public int ArrowCount => arrowCount;
        
        [SerializeField] private float spreadAngle = 30f;
        public float SpreadAngle => spreadAngle;
        
        [Header("Explosion")]
        [SerializeField] private float explosionRadius = 1.5f;
        public float ExplosionRadius => explosionRadius;
        
        [SerializeField] private float explosionDamage = 0.5f;
        public float ExplosionDamage => explosionDamage;
    }
}