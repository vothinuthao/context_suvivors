using UnityEngine;

namespace OctoberStudio.Abilities
{
    [CreateAssetMenu(fileName = "Staff Earth Data", menuName = "October/Abilities/Active/Staff Magic/Earth")]
    public class StaffEarthAbilityData : GenericAbilityData<StaffEarthAbilityLevel>
    {
        private void Awake() 
        { 
            type = AbilityType.StaffEarth; 
            isWeaponAbility = true; 
        }
        
        private void OnValidate() 
        { 
            type = AbilityType.StaffEarth; 
            isWeaponAbility = true; 
        }
    }

    [System.Serializable]
    public class StaffEarthAbilityLevel : BaseStaffLevel
    {
        [Header("Meteor")]
        [SerializeField] private float meteorFallSpeed = 10f;
        public float MeteorFallSpeed => meteorFallSpeed;
        
        [SerializeField] private float impactRadius = 2f;
        public float ImpactRadius => impactRadius;
    }
}