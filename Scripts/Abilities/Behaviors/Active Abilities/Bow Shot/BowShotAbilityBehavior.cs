using OctoberStudio.Extensions;
using OctoberStudio.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    public class BowShotAbilityBehavior : AbilityBehavior<BowShotAbilityData, BowShotAbilityLevel>
    {
        [SerializeField] GameObject arrowPrefab;
        public GameObject ArrowPrefab => arrowPrefab;

        private PoolComponent<BowShotProjectileBehavior> projectilePool;

        private Coroutine abilityCoroutine;
        private List<BowShotProjectileBehavior> projectiles = new List<BowShotProjectileBehavior>();

        private void Awake()
        {
            projectilePool = new PoolComponent<BowShotProjectileBehavior>("Bow Shot Ability Projectile", ArrowPrefab, 10);
        }

        protected override void SetAbilityLevel(int stageId)
        {
            base.SetAbilityLevel(stageId);

            Disable();

            abilityCoroutine = StartCoroutine(AbilityCoroutine());
        }

        private IEnumerator AbilityCoroutine()
        {
            while (true)
            {
                // Find target for main arrow direction
                var enemy = StageController.EnemiesSpawner.GetClosestEnemy(PlayerBehavior.CenterPosition);

                Vector2 baseDirection;
                if(enemy == null)
                {
                    baseDirection = Random.insideUnitCircle.normalized;
                } 
                else
                {
                    baseDirection = (enemy.Center - PlayerBehavior.CenterPosition).normalized;
                }

                // Calculate spread angles for multiple arrows
                float angleStep = AbilityLevel.SpreadAngle / (AbilityLevel.ArrowCount - 1);
                float startAngle = -AbilityLevel.SpreadAngle / 2f;

                for (int i = 0; i < AbilityLevel.ArrowCount; i++)
                {
                    var projectile = projectilePool.GetEntity();
                    projectile.transform.position = PlayerBehavior.CenterPosition;

                    // Calculate direction for this arrow
                    float currentAngle = startAngle + (angleStep * i);
                    Vector2 direction = RotateVector(baseDirection, currentAngle);

                    projectile.DamageMultiplier = AbilityLevel.Damage;
                    projectile.ExplosionRadius = AbilityLevel.ExplosionRadius;
                    projectile.ExplosionDamage = AbilityLevel.ExplosionDamage;
                    projectile.Size = AbilityLevel.ProjectileSize;
                    projectile.Speed = AbilityLevel.ProjectileSpeed;
                    projectile.LifeTime = AbilityLevel.ProjectileLifetime;

                    projectile.onArrowFinished += OnArrowFinished;

                    projectile.Spawn(direction);

                    projectiles.Add(projectile);
                }

                yield return new WaitForSeconds(AbilityLevel.AbilityCooldown / PlayerBehavior.Player.CooldownMultiplier);
            }
        }

        private Vector2 RotateVector(Vector2 vector, float angle)
        {
            float rad = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos
            );
        }

        private void OnArrowFinished(BowShotProjectileBehavior projectile)
        {
            projectile.onArrowFinished -= OnArrowFinished;
            projectiles.Remove(projectile);
        }

        private void Disable()
        {
            if(abilityCoroutine != null)
            {
                StopCoroutine(abilityCoroutine);
                abilityCoroutine = null;
            }

            for(int i = 0; i < projectiles.Count; i++)
            {
                projectiles[i].Disable();
            }

            projectiles.Clear();
        }

        private void OnDestroy()
        {
            Disable();
        }

        private void OnDisable()
        {
            Disable();
        }
    }
}