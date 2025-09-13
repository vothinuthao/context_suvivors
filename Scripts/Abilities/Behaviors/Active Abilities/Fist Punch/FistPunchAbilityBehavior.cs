using OctoberStudio.Extensions;
using OctoberStudio.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    public class FistPunchAbilityBehavior : AbilityBehavior<FistPunchAbilityData, FistPunchAbilityLevel>
    {
        [SerializeField] GameObject fistPunchPrefab;
        public GameObject FistPunchPrefab => fistPunchPrefab;

        private PoolComponent<FistPunchProjectileBehavior> projectilePool;

        private Coroutine abilityCoroutine;
        private List<FistPunchProjectileBehavior> projectiles = new List<FistPunchProjectileBehavior>();

        private void Awake()
        {
            projectilePool = new PoolComponent<FistPunchProjectileBehavior>("Fist Punch Ability Projectile", FistPunchPrefab, 5);
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
                projectile.PunchRange = AbilityLevel.PunchRange;
                projectile.KnockbackForce = AbilityLevel.KnockbackForce;
                projectile.ProjectileLifetime = AbilityLevel.ProjectileLifetime;
                projectile.Size = AbilityLevel.ProjectileSize;

                projectile.onPunchFinished += OnPunchFinished;

                projectile.Spawn(direction);

                projectiles.Add(projectile);

                yield return new WaitForSeconds(AbilityLevel.AbilityCooldown / PlayerBehavior.Player.CooldownMultiplier);
            }
        }

        private void OnPunchFinished(FistPunchProjectileBehavior projectile)
        {
            projectile.onPunchFinished -= OnPunchFinished;

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