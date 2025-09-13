using OctoberStudio.Extensions;
using OctoberStudio.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    public class KunaiThrowAbilityBehavior : AbilityBehavior<KunaiThrowAbilityData, KunaiThrowAbilityLevel>
    {
        [SerializeField] GameObject kunaiPrefab;
        public GameObject KunaiPrefab => kunaiPrefab;

        private PoolComponent<KunaiThrowProjectileBehavior> projectilePool;

        private Coroutine abilityCoroutine;
        private List<KunaiThrowProjectileBehavior> projectiles = new List<KunaiThrowProjectileBehavior>();

        private void Awake()
        {
            projectilePool = new PoolComponent<KunaiThrowProjectileBehavior>("Kunai Throw Ability Projectile", KunaiPrefab, 5);
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
                var projectile = projectilePool.GetEntity();

                projectile.transform.position = PlayerBehavior.CenterPosition;

                var enemy = StageController.EnemiesSpawner.GetClosestEnemy(projectile.transform.position);

                Vector2 direction;
                if(enemy == null)
                {
                    direction = Random.insideUnitCircle.normalized;
                } 
                else
                {
                    direction = (enemy.Center - projectile.transform.position.XY()).normalized;
                }

                projectile.DamageMultiplier = AbilityLevel.Damage;
                projectile.SpinSpeed = AbilityLevel.SpinSpeed;
                projectile.Piercing = AbilityLevel.Piercing;
                projectile.Size = AbilityLevel.ProjectileSize;
                projectile.Speed = AbilityLevel.ProjectileSpeed;
                projectile.LifeTime = AbilityLevel.ProjectileLifetime;

                projectile.onKunaiFinished += OnKunaiFinished;

                projectile.Spawn(direction);

                projectiles.Add(projectile);

                yield return new WaitForSeconds(AbilityLevel.AbilityCooldown / PlayerBehavior.Player.CooldownMultiplier);
            }
        }

        private void OnKunaiFinished(KunaiThrowProjectileBehavior projectile)
        {
            projectile.onKunaiFinished -= OnKunaiFinished;

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