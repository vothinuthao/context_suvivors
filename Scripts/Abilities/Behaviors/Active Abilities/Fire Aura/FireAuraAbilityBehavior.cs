using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    public class FireAuraAbilityBehavior : AbilityBehavior<FireAuraAbilityData, FireAuraAbilityLevel>
    {
        [SerializeField] protected CircleCollider2D areaCollider;
        [SerializeField] protected Transform visualsTransform;
        [SerializeField] protected ParticleSystem fireParticles;

        protected Dictionary<EnemyBehavior, BurnEffect> affectedEnemies = new Dictionary<EnemyBehavior, BurnEffect>();
        protected Coroutine damageCoroutine;
        protected Coroutine effectCoroutine;

        private static readonly int FIRE_AURA_HASH = "Fire Aura".GetHashCode();

        public class BurnEffect
        {
            public float burnEndTime;
            public Coroutine burnCoroutine;
        }

        protected override void SetAbilityLevel(int stageId)
        {
            base.SetAbilityLevel(stageId);

            SetupAura();
            StartEffect();
        }

        private void SetupAura()
        {
            if (areaCollider == null)
            {
                areaCollider = GetComponent<CircleCollider2D>();
                if (areaCollider == null)
                {
                    areaCollider = gameObject.AddComponent<CircleCollider2D>();
                    areaCollider.isTrigger = true;
                }
            }

            areaCollider.radius = AbilityLevel.AreaRadius * PlayerBehavior.Player.SizeMultiplier;
            
            if (visualsTransform != null)
            {
                visualsTransform.localScale = Vector3.one * (areaCollider.radius * 2f);
            }

            if (fireParticles != null)
            {
                var shape = fireParticles.shape;
                shape.radius = areaCollider.radius;
            }

            transform.position = PlayerBehavior.Player.transform.position;
        }

        private void StartEffect()
        {
            StopEffect();
            
            effectCoroutine = StartCoroutine(EffectCoroutine());
            damageCoroutine = StartCoroutine(DamageCoroutine());

            GameController.AudioManager.PlaySound(FIRE_AURA_HASH);
        }

        private IEnumerator EffectCoroutine()
        {
            float endTime = Time.time + AbilityLevel.EffectDuration * PlayerBehavior.Player.DurationMultiplier;
            
            while (Time.time < endTime)
            {
                // Follow the player
                transform.position = PlayerBehavior.Player.transform.position;
                yield return null;
            }
            
            StopEffect();
        }

        private IEnumerator DamageCoroutine()
        {
            while (effectCoroutine != null)
            {
                // Apply damage to all enemies in the aura
                List<EnemyBehavior> enemiesToRemove = new List<EnemyBehavior>();
                
                foreach (var kvp in affectedEnemies)
                {
                    var enemy = kvp.Key;
                    if (enemy == null || !enemy.gameObject.activeInHierarchy)
                    {
                        enemiesToRemove.Add(enemy);
                        continue;
                    }

                    // Apply burn damage
                    float damage = AbilityLevel.Damage * PlayerBehavior.Player.Damage;
                    // Note: You may need to implement enemy damage method
                    // enemy.TakeDamage(damage);
                }

                // Clean up destroyed enemies
                foreach (var enemy in enemiesToRemove)
                {
                    if (affectedEnemies.ContainsKey(enemy))
                    {
                        if (affectedEnemies[enemy].burnCoroutine != null)
                        {
                            StopCoroutine(affectedEnemies[enemy].burnCoroutine);
                        }
                        affectedEnemies.Remove(enemy);
                    }
                }

                yield return new WaitForSeconds(AbilityLevel.DamageInterval);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<EnemyBehavior>(out var enemy))
            {
                if (!affectedEnemies.ContainsKey(enemy))
                {
                    var burnEffect = new BurnEffect
                    {
                        burnEndTime = Time.time + AbilityLevel.BurnDuration,
                        burnCoroutine = StartCoroutine(BurnCoroutine(enemy))
                    };
                    
                    affectedEnemies[enemy] = burnEffect;
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent<EnemyBehavior>(out var enemy))
            {
                // Keep burn effect even after leaving aura for burn duration
                if (affectedEnemies.ContainsKey(enemy))
                {
                    affectedEnemies[enemy].burnEndTime = Time.time + AbilityLevel.BurnDuration;
                }
            }
        }

        private IEnumerator BurnCoroutine(EnemyBehavior enemy)
        {
            while (affectedEnemies.ContainsKey(enemy) && enemy != null && enemy.gameObject.activeInHierarchy)
            {
                if (Time.time >= affectedEnemies[enemy].burnEndTime)
                {
                    break;
                }

                // Apply burn damage over time
                float burnDamage = AbilityLevel.BurnDamagePerSecond * PlayerBehavior.Player.Damage;
                // Note: You may need to implement enemy damage method
                // enemy.TakeDamage(burnDamage);

                yield return new WaitForSeconds(1f);
            }

            // Remove burn effect
            if (affectedEnemies.ContainsKey(enemy))
            {
                affectedEnemies.Remove(enemy);
            }
        }

        private void StopEffect()
        {
            if (effectCoroutine != null)
            {
                StopCoroutine(effectCoroutine);
                effectCoroutine = null;
            }

            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }

            // Stop all burn effects
            foreach (var kvp in affectedEnemies)
            {
                if (kvp.Value.burnCoroutine != null)
                {
                    StopCoroutine(kvp.Value.burnCoroutine);
                }
            }
            
            affectedEnemies.Clear();
        }

        private void OnDestroy()
        {
            StopEffect();
        }

        private void OnDisable()
        {
            StopEffect();
        }

        // For debugging - visualize aura radius in scene view
        private void OnDrawGizmosSelected()
        {
            if (areaCollider != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, areaCollider.radius);
            }
        }
    }
}