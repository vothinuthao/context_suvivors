using UnityEngine;

namespace OctoberStudio.Abilities
{
    [CreateAssetMenu(fileName = "Fire Aura Data", menuName = "October/Abilities/Active/Fire Aura")]
    public class FireAuraAbilityData : GenericAbilityData<FireAuraAbilityLevel>
    {
        private void Awake() 
        { 
            type = AbilityType.FireAura; 
            isActiveAbility = true; 
        }
        
        private void OnValidate() 
        { 
            type = AbilityType.FireAura; 
            isActiveAbility = true; 
        }
    }

    [System.Serializable]
    public class FireAuraAbilityLevel : BaseAOELevel
    {
        [Header("Burn Effect")]
        [SerializeField] private float burnDamagePerSecond = 0.5f;
        public float BurnDamagePerSecond => burnDamagePerSecond;
        
        [SerializeField] private float burnDuration = 3f;
        public float BurnDuration => burnDuration;
    }
}