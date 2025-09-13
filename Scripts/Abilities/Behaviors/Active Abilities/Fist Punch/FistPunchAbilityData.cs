using UnityEngine;

namespace OctoberStudio.Abilities
{
    [CreateAssetMenu(fileName = "Fist Punch Data", menuName = "October/Abilities/Active/Fist Punch")]
    public class FistPunchAbilityData : GenericAbilityData<FistPunchAbilityLevel>
    {
        private void Awake() 
        { 
            type = AbilityType.FistPunch; 
            isWeaponAbility = true; 
        }
        
        private void OnValidate() 
        { 
            type = AbilityType.FistPunch; 
            isWeaponAbility = true; 
        }
    }

    [System.Serializable]
    public class FistPunchAbilityLevel : BaseProjectileLevel
    {
        [Header("Punch Specific")]
        [SerializeField] private float punchRange = 2f;
        public float PunchRange => punchRange;
        
        [SerializeField] private float knockbackForce = 5f;
        public float KnockbackForce => knockbackForce;
    }
}