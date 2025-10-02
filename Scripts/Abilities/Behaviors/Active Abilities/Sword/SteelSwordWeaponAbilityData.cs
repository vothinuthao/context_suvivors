using UnityEngine;

namespace OctoberStudio.Abilities
{
    [CreateAssetMenu(fileName = "Steel Sword Data", menuName = "October/Abilities/Active/Steel Sword")]
    public class SteelSwordWeaponAbilityData : GenericAbilityData<SteelSwordWeaponAbilityLevel>
    {
        private void Awake()
        {
            type = AbilityType.SteelSword;
            isWeaponAbility = true;
        }

        private void OnValidate()
        {
            type = AbilityType.SteelSword;
            isWeaponAbility = true;
        }
    }

    [System.Serializable]
    public class SteelSwordWeaponAbilityLevel : AbilityLevel
    {
        [Tooltip("Amount of time between attacks")]
        [SerializeField] float abilityCooldown;
        public float AbilityCooldown => abilityCooldown;

        [Tooltip("Amount of slashes in the attack")]
        [SerializeField] int slashesCount;
        public int SlashesCount => slashesCount;

        [Tooltip("Damage of slashes calculates like this: Player.Damage * Damage")]
        [SerializeField] float damage;
        public float Damage => damage;

        [Tooltip("Size of slash visual")]
        [SerializeField] float slashSize;
        public float SlashSize => slashSize;

        [Tooltip("Range/size of slash collider (affects hit detection)")]
        [SerializeField] float slashRange;
        public float SlashRange => slashRange;

        [Tooltip("Speed of slash movement")]
        [SerializeField] float slashSpeed;
        public float SlashSpeed => slashSpeed;

        [Tooltip("Delay Before each slash")]
        [SerializeField] float timeBetweenSlashes;
        public float TimeBetweenSlashes => timeBetweenSlashes;

        [Header("Slash Animation")]
        [Tooltip("Duration of slash animation (cycling through sprites)")]
        [SerializeField] float slashAnimationDuration = 0.3f;
        public float SlashAnimationDuration => slashAnimationDuration;

        [Tooltip("Duration of slash movement after animation")]
        [SerializeField] float slashMoveDuration = 0.5f;
        public float SlashMoveDuration => slashMoveDuration;

        [Header("Surrounding Slashes")]
        [Tooltip("Number of slashes spawned around the player")]
        [SerializeField] int surroundingSlashesCount;
        public int SurroundingSlashesCount => surroundingSlashesCount;

        [Tooltip("Angle coverage of each slash in degrees (based on slash size)")]
        [SerializeField] float slashAngleCoverage = 30f;
        public float SlashAngleCoverage => slashAngleCoverage;

        [Tooltip("Additional spacing angle between slashes in degrees")]
        [SerializeField] float slashSpacing = 15f;
        public float SlashSpacing => slashSpacing;
    }
}