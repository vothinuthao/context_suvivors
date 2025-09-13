using UnityEngine;

namespace OctoberStudio.Abilities
{
    [CreateAssetMenu(fileName = "Kunai Throw Data", menuName = "October/Abilities/Active/Kunai Throw")]
    public class KunaiThrowAbilityData : GenericAbilityData<KunaiThrowAbilityLevel>
    {
        private void Awake() 
        { 
            type = AbilityType.KunaiThrow; 
            isWeaponAbility = true; 
        }
        
        private void OnValidate() 
        { 
            type = AbilityType.KunaiThrow; 
            isWeaponAbility = true; 
        }
    }

    [System.Serializable]
    public class KunaiThrowAbilityLevel : BaseProjectileLevel
    {
        [Header("Kunai Specific")]
        [SerializeField] private float spinSpeed = 360f;
        public float SpinSpeed => spinSpeed;
        
        [SerializeField] private bool piercing = false;
        public bool Piercing => piercing;
    }
}