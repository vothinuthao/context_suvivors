using OctoberStudio.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio.Enemy
{
    public class SkeletonKingBehavior : EnemyBehavior
    {
        private static readonly int SUMMON_TRIGGER = Animator.StringToHash("SummonTrigger");
        private static readonly int SLASH_TRIGGER = Animator.StringToHash("SlashTrigger");

        [SerializeField] Animator animator;

        [Header("Arc Slash Projectiles")]
        [Tooltip("The prefab of the arc slash projectile")]
        [SerializeField] GameObject arcSlashProjectilePrefab;
        [Tooltip("The number of arc slash projectiles per attack")]
        [SerializeField] int slashesPerAttack = 3;
        [Tooltip("The spread angle for arc slashes (total spread)")]
        [SerializeField] float arcSpreadAngle = 120f;
        [Tooltip("The damage of each arc slash")]
        [SerializeField] float slashDamage = 20f;
        [Tooltip("The speed of arc slash projectiles")]
        [SerializeField] float projectileSpeed = 12f;

        [Header("Movement")]
        [Tooltip("Time the king moves between attacks")]
        [SerializeField] float movingDuration = 4f;
        [Tooltip("Time between different attack phases")]
        [SerializeField] float attackCooldown = 3f;
        [Tooltip("Minimum distance to maintain from player")]
        [SerializeField] float minDistanceFromPlayer = 3f;

        [Header("Skeleton Summoning")]
        [Tooltip("The type of skeleton minion that will be spawned")]
        [SerializeField] EnemyType skeletonMinionType = EnemyType.Bat; // Placeholder, will need SkeletonMinion type
        [Tooltip("The number of skeletons summoned per wave")]
        [SerializeField] int skeletonsPerSummon = 4;
        [Tooltip("The time the warning circle is active before skeleton spawns")]
        [SerializeField] float warningDuration = 1.5f;
        [Tooltip("Lifetime of summoned skeleton minions")]
        [SerializeField] float minionLifetime = 15f;
        [Tooltip("Cooldown between summon attacks")]
        [SerializeField] float summonCooldown = 8f;

        [Space]
        [SerializeField] ParticleSystem summoningParticle;
        [SerializeField] ParticleSystem slashingParticle;

        List<WarningCircleBehavior> warningCircles = new List<WarningCircleBehavior>();
        List<EnemyBehavior> summonedMinions = new List<EnemyBehavior>();

        private PoolComponent<ArcSlashProjectileBehavior> slashPool;
        private List<ArcSlashProjectileBehavior> activeSlashes = new List<ArcSlashProjectileBehavior>();

        protected override void Awake()
        {
            base.Awake();

            slashPool = new PoolComponent<ArcSlashProjectileBehavior>(arcSlashProjectilePrefab, 6);
        }

        public override void Play()
        {
            base.Play();

            StartCoroutine(BehaviorCoroutine());
        }

        private IEnumerator BehaviorCoroutine()
        {
            while(true)
            {
                // Phase 1: Move toward player (with proper distance control)
                yield return StartCoroutine(MoveTowardsPlayer());

                // Phase 2: Skeleton Summoning (like MegaSlime spawning)
                IsMoving = false;
                yield return StartCoroutine(SkeletonSummonCoroutine());
                yield return new WaitForSeconds(attackCooldown);

                // Phase 3: Move to new position (avoid sticking)
                yield return StartCoroutine(MoveTowardsPlayer());

                // Phase 4: Arc Slash Attack
                IsMoving = false;
                yield return StartCoroutine(ArcSlashAttackCoroutine());
                yield return new WaitForSeconds(attackCooldown);
            }
        }

        // FIXED: Smart movement that avoids sticking to player
        private IEnumerator MoveTowardsPlayer()
        {
            IsMoving = true;
            
            float elapsedTime = 0f;
            
            while (elapsedTime < movingDuration)
            {
                if (PlayerBehavior.Player != null)
                {
                    float distanceToPlayer = Vector2.Distance(transform.position, PlayerBehavior.Player.transform.position);
                    
                    // FIXED: Stop moving if too close to player
                    if (distanceToPlayer <= minDistanceFromPlayer)
                    {
                        IsMoving = false;
                        break; // Stop movement phase early if close enough
                    }
                    
                    // Continue normal movement toward player if far away
                    IsMoving = true;
                }
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            IsMoving = false;
        }

        private IEnumerator SkeletonSummonCoroutine()
        {
            // Start summoning animation and particle effect
            summoningParticle.Play();
            animator.SetTrigger(SUMMON_TRIGGER);

            // Create warning circles at 4 random positions (like MegaSlime)
            for (int i = 0; i < skeletonsPerSummon; i++)
            {
                var spawnPosition = StageController.FieldManager.Fence.GetRandomPointInside(0.5f);
                
                var warningCircle = StageController.PoolsManager.GetEntity<WarningCircleBehavior>("Warning Circle");
                warningCircle.transform.position = spawnPosition;
                warningCircle.Play(1f, 0.3f, 100, null);
                
                warningCircles.Add(warningCircle);
            }

            // Wait for warning duration
            yield return new WaitForSeconds(warningDuration);

            // Spawn skeletons at warning positions
            for(int i = 0; i < skeletonsPerSummon; i++)
            {
                if (i < warningCircles.Count)
                {
                    var warningCircle = warningCircles[i];
                    
                    // Spawn skeleton minion at warning position
                    var spawnedEnemy = StageController.EnemiesSpawner.Spawn(skeletonMinionType, warningCircle.transform.position);
                    if (spawnedEnemy != null)
                    {
                        summonedMinions.Add(spawnedEnemy);
                        
                        // Set minion to auto-despawn after lifetime
                        StartCoroutine(MinionLifetimeCoroutine(spawnedEnemy));
                    }
                    
                    warningCircle.gameObject.SetActive(false);
                }
            }

            warningCircles.Clear();
            summoningParticle.Stop();
        }

        private IEnumerator MinionLifetimeCoroutine(EnemyBehavior minion)
        {
            yield return new WaitForSeconds(minionLifetime);
            
            if (minion != null && summonedMinions.Contains(minion))
            {
                summonedMinions.Remove(minion);
                if (minion.gameObject.activeInHierarchy)
                {
                    minion.gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator ArcSlashAttackCoroutine()
        {
            // Start slash animation and particle effect
            slashingParticle.Play();
            animator.SetTrigger(SLASH_TRIGGER);

            yield return new WaitForSeconds(0.5f); // Animation buildup time

            // Calculate direction to player
            Vector2 playerDirection = (PlayerBehavior.Player.transform.position - transform.position).normalized;
            
            // Create 3 arc slash projectiles with 120° spread
            float angleStep = arcSpreadAngle / (slashesPerAttack - 1);
            float startAngle = -arcSpreadAngle / 2f;

            for (int i = 0; i < slashesPerAttack; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                Vector2 slashDirection = Quaternion.Euler(0, 0, currentAngle) * playerDirection;

                var slash = slashPool.GetEntity();
                slash.Init(transform.position, slashDirection, projectileSpeed);
                slash.Damage = StageController.Stage.EnemyDamage * slashDamage;
                slash.onFinished += OnSlashFinished;

                activeSlashes.Add(slash);

                yield return new WaitForSeconds(0.1f); // Small delay between slashes
            }

            slashingParticle.Stop();
        }

        // FIXED: Override Update to prevent base movement when close to player
        protected override void Update()
        {
            if (!IsAlive || !IsMoving || PlayerBehavior.Player == null) return;

            float distanceToPlayer = Vector2.Distance(transform.position, PlayerBehavior.Player.transform.position);
            
            // FIXED: Stop base movement if too close to player
            if (distanceToPlayer <= minDistanceFromPlayer)
            {
                IsMoving = false;
                return;
            }
            
            // Call base movement only if not too close
            base.Update();
        }

        private void OnSlashFinished(ArcSlashProjectileBehavior slash)
        {
            slash.onFinished -= OnSlashFinished;
            activeSlashes.Remove(slash);
        }

        protected override void Die(bool flash)
        {
            base.Die(flash);

            // Clean up all active slashes
            for(int i = 0; i < activeSlashes.Count; i++)
            {
                var slash = activeSlashes[i];
                slash.onFinished -= OnSlashFinished;
                slash.Clear();
            }
            activeSlashes.Clear();

            // Clean up warning circles
            for(int i = 0; i < warningCircles.Count; i++)
            {
                warningCircles[i].gameObject.SetActive(false);
            }
            warningCircles.Clear();

            // Clean up summoned minions
            for(int i = 0; i < summonedMinions.Count; i++)
            {
                if (summonedMinions[i] != null && summonedMinions[i].gameObject.activeInHierarchy)
                {
                    summonedMinions[i].gameObject.SetActive(false);
                }
            }
            summonedMinions.Clear();

            StopAllCoroutines();
        }

        // Animation event handlers (called by SkeletonKingEventsHandler)
        public void OnSummonAnimationEvent()
        {
            // Additional effects during summoning animation if needed
        }

        public void OnSlashAnimationEvent()
        {
            // Additional effects during slash animation if needed
        }
    }
}