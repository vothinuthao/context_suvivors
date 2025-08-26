using System.Collections;
using UnityEngine;

namespace OctoberStudio.Enemy
{
    public class SkeletonMinionBehavior : EnemyBehavior
    {
        private static readonly int SPAWN_TRIGGER = Animator.StringToHash("Spawn");
        private static readonly int ATTACK_TRIGGER = Animator.StringToHash("Attack");
        private static readonly int DEATH_TRIGGER = Animator.StringToHash("Death");

        [SerializeField] Animator animator;
        [SerializeField] ParticleSystem spawnEffect;
        [SerializeField] ParticleSystem deathEffect;

        [Header("Minion Settings")]
        [Tooltip("Attack range for the skeleton minion")]
        [SerializeField] float attackRange = 2f;
        [Tooltip("Attack cooldown for the skeleton minion")]
        [SerializeField] float attackCooldown = 2f;
        [Tooltip("Damage multiplier for minion attacks")]
        [SerializeField] float minionDamageMultiplier = 0.8f;

        private bool isSpawning = true;
        private float lastAttackTime;
        private Coroutine attackCoroutine;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void Play()
        {
            base.Play();

            // Start with spawn animation and brief invulnerability
            isSpawning = true;
            
            if (spawnEffect != null)
                spawnEffect.Play();
                
            if (animator != null)
                animator.SetTrigger(SPAWN_TRIGGER);

            StartCoroutine(SpawnSequence());
        }

        private IEnumerator SpawnSequence()
        {
            // Brief spawn protection and animation time
            yield return new WaitForSeconds(0.5f);
            
            isSpawning = false;
            
            if (spawnEffect != null)
                spawnEffect.Stop();

            // Start minion AI behavior
            StartCoroutine(MinionAI());
        }

        private IEnumerator MinionAI()
        {
            while (gameObject.activeInHierarchy && !isSpawning)
            {
                if (PlayerBehavior.Player != null)
                {
                    float distanceToPlayer = Vector2.Distance(transform.position, PlayerBehavior.Player.transform.position);

                    // Attack if player is within range and cooldown has passed
                    if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
                    {
                        yield return StartCoroutine(AttackSequence());
                    }
                    else
                    {
                        // Move toward player if not in attack range
                        if (distanceToPlayer > attackRange)
                        {
                            IsMoving = true;
                        }
                        else
                        {
                            IsMoving = false;
                        }
                    }
                }

                yield return new WaitForSeconds(0.1f); // Update frequency
            }
        }

        private IEnumerator AttackSequence()
        {
            IsMoving = false;
            lastAttackTime = Time.time;

            if (animator != null)
                animator.SetTrigger(ATTACK_TRIGGER);

            yield return new WaitForSeconds(0.3f);
            if (PlayerBehavior.Player != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, PlayerBehavior.Player.transform.position);
                if (distanceToPlayer <= attackRange)
                {
                    float damage = StageController.Stage.EnemyDamage * minionDamageMultiplier;
                }
            }

            yield return new WaitForSeconds(0.2f); // Attack recovery time
        }

        protected override void Die(bool flash)
        {
            if (deathEffect != null)
                deathEffect.Play();
                
            if (animator != null)
                animator.SetTrigger(DEATH_TRIGGER);

            StopAllCoroutines();
            
            base.Die(flash);
        }


        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}