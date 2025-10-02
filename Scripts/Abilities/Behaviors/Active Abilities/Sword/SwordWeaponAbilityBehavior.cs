using OctoberStudio.Easing;
using OctoberStudio.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    public class SwordWeaponAbilityBehavior : AbilityBehavior<SteelSwordWeaponAbilityData, SteelSwordWeaponAbilityLevel>
    {
        public static readonly int STEEL_SWORD_ATTACK_HASH = "Steel Sword Attack".GetHashCode();

        [SerializeField] GameObject slashPrefab;
        public GameObject SlashPrefab => slashPrefab;

        private PoolComponent<SwordSlashBehavior> slashPool;
        private List<SwordSlashBehavior> slashes = new List<SwordSlashBehavior>();

        [SerializeField] List<Transform> shashDirections;

        IEasingCoroutine projectileCoroutine;
        Coroutine abilityCoroutine;

        private float AbilityCooldown => AbilityLevel.AbilityCooldown * PlayerBehavior.Player.CooldownMultiplier;

        private void Awake()
        {
            slashPool = new PoolComponent<SwordSlashBehavior>("Sword Slash", SlashPrefab, 50);
        }

        protected override void SetAbilityLevel(int stageId)
        {
            base.SetAbilityLevel(stageId);

            if (abilityCoroutine != null) Disable();

            abilityCoroutine = StartCoroutine(AbilityCoroutine());
        }

        private IEnumerator AbilityCoroutine()
        {
            var lastTimeSpawned = Time.time - AbilityCooldown;

            while (true)
            {
                // Get closest enemy direction for the first slash
                var closestEnemy = StageController.EnemiesSpawner.GetClosestEnemy(PlayerBehavior.CenterPosition);
                Vector2 attackDirection = Vector2.up;
                if (closestEnemy != null)
                {
                    attackDirection = closestEnemy.Center - PlayerBehavior.CenterPosition;
                    attackDirection.Normalize();
                }

                if (AbilityLevel.SurroundingSlashesCount > 0)
                {
                    // Calculate angle step based on slash size and spacing
                    float angleStep = AbilityLevel.SlashAngleCoverage + AbilityLevel.SlashSpacing;
                    
                    // Calculate total angle coverage
                    float totalAngle = (AbilityLevel.SurroundingSlashesCount - 1) * angleStep;
                    
                    // Center angle on enemy direction
                    float enemyAngle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
                    float startAngle = enemyAngle - (totalAngle / 2f);

                    // Spawn all slashes simultaneously, spread evenly with enemy at center
                    for (int i = 0; i < AbilityLevel.SurroundingSlashesCount; i++)
                    {
                        float angle = startAngle + (i * angleStep);
                        Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

                        SpawnSlash(direction);
                    }

                    // Play sound once for all slashes
                    GameController.AudioManager.PlaySound(STEEL_SWORD_ATTACK_HASH);
                }
                else
                {
                    // Original behavior - use predefined directions from shashDirections with delay
                    for(int i = 0; i < AbilityLevel.SlashesCount; i++)
                    {
                        Vector2 direction = Quaternion.FromToRotation(Vector2.right, attackDirection) * shashDirections[i].localRotation * Vector2.right;
                        SpawnSlash(direction);
                        GameController.AudioManager.PlaySound(STEEL_SWORD_ATTACK_HASH);

                        if (i < AbilityLevel.SlashesCount - 1)
                        {
                            yield return new WaitForSeconds(AbilityLevel.TimeBetweenSlashes * PlayerBehavior.Player.CooldownMultiplier);
                        }
                    }
                }

                yield return new WaitForSeconds(AbilityLevel.AbilityCooldown * PlayerBehavior.Player.CooldownMultiplier);
            }
        }

        private void SpawnSlash(Vector2 direction)
        {
            var slash = slashPool.GetEntity();

            slash.transform.position = PlayerBehavior.CenterPosition;
            slash.transform.rotation = Quaternion.FromToRotation(Vector2.right, direction);
            slash.DamageMultiplier = AbilityLevel.Damage;
            slash.KickBack = false;
            slash.Size = AbilityLevel.SlashSize;
            slash.Range = AbilityLevel.SlashRange;
            slash.Speed = AbilityLevel.SlashSpeed;
            slash.Direction = direction;
            slash.SlashAnimationDuration = AbilityLevel.SlashAnimationDuration;
            slash.MoveDuration = AbilityLevel.SlashMoveDuration;
            slash.Init();
            slash.onFinished += OnProjectileFinished;
            slashes.Add(slash);
        }

        private void OnProjectileFinished(SwordSlashBehavior slash)
        {
            slash.onFinished -= OnProjectileFinished;

            slashes.Remove(slash);
        }

        private void Disable()
        {
            projectileCoroutine.StopIfExists();

            for (int i = 0; i < slashes.Count; i++)
            {
                slashes[i].Disable();
            }

            slashes.Clear();

            StopCoroutine(abilityCoroutine);
        }

        public override void Clear()
        {
            Disable();

            base.Clear();
        }
    }
}