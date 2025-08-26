using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using OctoberStudio.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio.Enemy
{
    public class ForestGuardianLordBehavior : EnemyBehavior
    {
        private static readonly int IS_CASTING_THORNS_HASH = Animator.StringToHash("IsCastingThorns");
        private static readonly int IS_CASTING_SEEDS_HASH = Animator.StringToHash("IsCastingSeeds");
        private static readonly int IS_FLOATING_HASH = Animator.StringToHash("IsFloating");
        private static readonly int DEATH_EXPLOSION_TRIGGER = Animator.StringToHash("DeathExplosion");

        [Header("Guardian Settings")] [SerializeField]
        Animator animator;

        [SerializeField] Transform staffTransform;
        [SerializeField] float floatingHeight = 1.5f;
        [SerializeField] float floatingBobSpeed = 2f;
        [SerializeField] float floatingBobAmount = 0.3f;

        [Header("Thorn Patches (Skill 1) - FIXED")] [SerializeField]
        GameObject thornPatchPrefab;

        [SerializeField] int patchesPerWave = 6;
        [SerializeField] int thornWaves = 2;
        [SerializeField] float timeBetweenPatches = 0.4f;
        [SerializeField] float thornContactDamage = 8f;
        [SerializeField] float thornPoisonDamage = 3f;
        [SerializeField] float thornDuration = 8f; // FIXED: Lâu hơn nhiều so với Crab spike

        [Header("Seed Barrage (Skill 2) - FIXED")] [SerializeField]
        GameObject seedProjectilePrefab;

        [SerializeField] private int seedsPerBarrage = 12;
        [SerializeField] int seedBarrages = 3;
        [SerializeField] float seedSpeed = 8f; 
        [SerializeField] float seedDamage = 6f; 
        [SerializeField] float timeBetweenBarrages = 0.8f; 
        [SerializeField] float timeBetweenSeeds = 0.5f;

        [Header("General Behavior")] [SerializeField]
        float movementDuration = 4f;

        [SerializeField] float attackCooldown = 2f;

        [Header("Effects")] [SerializeField] ParticleSystem thornCastingEffect;
        [SerializeField] ParticleSystem seedCastingEffect;
        [SerializeField] ParticleSystem deathExplosionEffect;
        [SerializeField] ParticleSystem ambientNatureEffect;
        [SerializeField] ParticleSystem staffGlowEffect;

        private PoolComponent<ThornPatchBehavior> thornPatchPool;
        private PoolComponent<SeedProjectileBehavior> seedProjectilePool;
        private List<ThornPatchBehavior> activeThornPatches = new List<ThornPatchBehavior>();
        private Coroutine behaviorCoroutine;
        private Vector3 originalPosition;
        private bool isFloating = true;

        protected override void Awake()
        {
            base.Awake();
            if (thornPatchPrefab != null)
            {
                thornPatchPool = new PoolComponent<ThornPatchBehavior>(thornPatchPrefab, patchesPerWave * thornWaves);
            }

            if (seedProjectilePrefab != null)
            {
                seedProjectilePool =
                    new PoolComponent<SeedProjectileBehavior>(seedProjectilePrefab, seedsPerBarrage * seedBarrages);
            }
        }

        public override void Play()
        {
            base.Play();
            originalPosition = transform.position;
            isFloating = true;

            if (animator != null)
                animator.SetBool(IS_FLOATING_HASH, true);

            if (ambientNatureEffect != null)
                ambientNatureEffect.Play();
            if (staffGlowEffect != null)
                staffGlowEffect.Play();

            behaviorCoroutine = StartCoroutine(BehaviorCoroutine());
            StartCoroutine(FloatingMovement());
        }

        private IEnumerator BehaviorCoroutine()
        {
            while (IsAlive)
            {
                yield return FloatToPosition();
                yield return ThornPatchesAttack();
                yield return new WaitForSeconds(attackCooldown);

                yield return FloatToPosition();
                yield return SeedBarrageAttack();
                yield return new WaitForSeconds(attackCooldown);
            }
        }

        // FIXED: Override base Update to disable ground movement
        protected override void Update()
        {
            // Don't call base.Update() - we handle our own floating movement
            if (!IsAlive || !isFloating) return;
        }

        private IEnumerator FloatingMovement()
        {
            while (isFloating && IsAlive)
            {
                // FIXED: Only handle Y-axis floating bob, not actual movement
                float bobOffset = Mathf.Sin(Time.time * floatingBobSpeed) * floatingBobAmount;
                Vector3 currentPos = transform.position;
                Vector3 targetBobPosition = new Vector3(currentPos.x, originalPosition.y + floatingHeight + bobOffset,
                    currentPos.z);

                // FIXED: Only lerp Y position for floating bob
                transform.position = new Vector3(currentPos.x,
                    Mathf.Lerp(currentPos.y, targetBobPosition.y, Time.deltaTime * 3f),
                    currentPos.z);

                // FIXED: Staff direction always points to player when visible
                if (staffTransform != null && PlayerBehavior.Player != null)
                {
                    Vector2 direction =
                        ((Vector2)PlayerBehavior.Player.transform.position - (Vector2)transform.position).normalized;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    staffTransform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                }

                yield return null;
            }
        }

        private IEnumerator FloatToPosition()
        {
            // FIXED: Actually move to random position like other bosses
            Vector2 targetPosition = StageController.FieldManager.Fence.GetRandomPointInside(1.5f);

            // FIXED: Ensure reasonable distance from player
            int attempts = 0;
            while (Vector2.Distance(targetPosition, PlayerBehavior.Player.transform.position) < 2f && attempts < 10)
            {
                targetPosition = StageController.FieldManager.Fence.GetRandomPointInside(1.5f);
                attempts++;
            }

            IsMoving = true;

            // FIXED: Smoothly move to target position over time
            Vector2 startPosition = transform.position;
            float elapsed = 0f;

            while (elapsed < movementDuration && IsAlive)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / movementDuration;

                // FIXED: Smooth movement with easing
                Vector2 currentXZ = Vector2.Lerp(startPosition, targetPosition, EaseInOutQuad(progress));
                transform.position = new Vector3(currentXZ.x, transform.position.y, 0);

                yield return null;
            }

            IsMoving = false;

            // FIXED: Update original position for floating reference
            originalPosition = new Vector3(transform.position.x, originalPosition.y, transform.position.z);
        }

        // FIXED: Smooth easing function for movement
        private float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        // FIXED: Skill 1 - Thorn Patches như docs mô tả, không phải spike như Crab
        private IEnumerator ThornPatchesAttack()
        {
            if (animator != null)
                animator.SetBool(IS_CASTING_THORNS_HASH, true);

            if (thornCastingEffect != null)
                thornCastingEffect.Play();

            for (int wave = 0; wave < thornWaves; wave++)
            {
                for (int patch = 0; patch < patchesPerWave; patch++)
                {
                    SpawnThornPatch();
                    yield return new WaitForSeconds(timeBetweenPatches);
                }

                if (wave < thornWaves - 1)
                    yield return new WaitForSeconds(1f);
            }

            if (animator != null)
                animator.SetBool(IS_CASTING_THORNS_HASH, false);
        }

        // FIXED: Skill 2 - Seeds bay từ boss ra ngoài, không giống Mega Slime
        private IEnumerator SeedBarrageAttack()
        {
            if (animator != null)
                animator.SetBool(IS_CASTING_SEEDS_HASH, true);

            if (seedCastingEffect != null)
                seedCastingEffect.Play();

            for (int barrage = 0; barrage < seedBarrages; barrage++)
            {
                // FIXED: Launch seeds từ boss position ra random directions
                for (int seed = 0; seed < seedsPerBarrage; seed++)
                {
                    LaunchSeedFromBoss();
                    yield return new WaitForSeconds(timeBetweenSeeds);
                }

                if (barrage < seedBarrages - 1)
                    yield return new WaitForSeconds(timeBetweenBarrages);
            }

            if (animator != null)
                animator.SetBool(IS_CASTING_SEEDS_HASH, false);
        }

        // FIXED: Spawn thorn patch như poison zone, tồn tại lâu
        private void SpawnThornPatch()
        {
            if (thornPatchPool == null) return;

            Vector2 spawnPosition;
            if (StageController.FieldManager != null && StageController.FieldManager.Fence != null)
            {
                spawnPosition = StageController.FieldManager.Fence.GetRandomPointInside(1f);
            }
            else
            {
                spawnPosition = (Vector2)PlayerBehavior.Player.transform.position + Random.insideUnitCircle * 3f;
            }

            var thornPatch = thornPatchPool.GetEntity();
            thornPatch.transform.position = spawnPosition;
            thornPatch.ContactDamage = thornContactDamage * StageController.Stage.EnemyDamage;
            thornPatch.PoisonDamage = thornPoisonDamage * StageController.Stage.EnemyDamage;
            thornPatch.onFinished += OnThornPatchFinished;

            activeThornPatches.Add(thornPatch);

            // FIXED: Thorn patches tồn tại lâu để tạo area denial
            thornPatch.Spawn(thornDuration);
        }

        // FIXED: Launch seed từ boss ra random direction (không phải spawn từ trên trời)
        private void LaunchSeedFromBoss()
        {
            if (seedProjectilePool == null) return;

            // FIXED: Seeds bay từ boss position, không phải spawn random
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector2 launchPosition = (Vector2)transform.position + randomDirection * 0.5f;

            var seed = seedProjectilePool.GetEntity();
            seed.transform.position = launchPosition;
            seed.Damage = seedDamage * StageController.Stage.EnemyDamage;
            seed.Speed = seedSpeed;
            seed.onFinished += OnSeedFinished;

            // FIXED: Launch từ boss ra ngoài
            seed.Launch(randomDirection);
        }

        private void OnThornPatchFinished(ThornPatchBehavior thornPatch)
        {
            thornPatch.onFinished -= OnThornPatchFinished;
            activeThornPatches.Remove(thornPatch);
        }

        private void OnSeedFinished(SeedProjectileBehavior seed)
        {
            seed.onFinished -= OnSeedFinished;
        }

        protected override void Die(bool flash)
        {
            isFloating = false;

            if (behaviorCoroutine != null)
                StopCoroutine(behaviorCoroutine);

            StopAllEffects();
            ClearActiveObjects();

            if (animator != null)
                animator.SetTrigger(DEATH_EXPLOSION_TRIGGER);

            if (deathExplosionEffect != null)
                deathExplosionEffect.Play();

            base.Die(flash);
        }

        private void StopAllEffects()
        {
            if (thornCastingEffect != null)
                thornCastingEffect.Stop();
            if (seedCastingEffect != null)
                seedCastingEffect.Stop();
            if (ambientNatureEffect != null)
                ambientNatureEffect.Stop();
            if (staffGlowEffect != null)
                staffGlowEffect.Stop();
        }

        private void ClearActiveObjects()
        {
            for (int i = 0; i < activeThornPatches.Count; i++)
            {
                if (activeThornPatches[i] != null)
                {
                    activeThornPatches[i].onFinished -= OnThornPatchFinished;
                    activeThornPatches[i].Clear();
                }
            }

            activeThornPatches.Clear();
        }

        public void OnThornCastingAnimationEvent()
        {
            // Animation event callback
        }

        public void OnSeedCastingAnimationEvent()
        {
            // Animation event callback
        }

        public void OnDeathExplosionAnimationEvent()
        {
            // FIXED: Death explosion bắn seeds ra mọi hướng từ boss
            for (int i = 0; i < 20; i++)
            {
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                LaunchDeathLeaf(randomDirection);
            }
        }

        private void LaunchDeathLeaf(Vector2 direction)
        {
            if (seedProjectilePool == null) return;

            var leaf = seedProjectilePool.GetEntity();
            leaf.Init(transform.position, direction);
            leaf.Damage = seedDamage * 0.5f * StageController.Stage.EnemyDamage;
            leaf.Speed = seedSpeed * 1.5f; // Faster death leaves
            leaf.onFinished += OnSeedFinished;
        }
    }
}