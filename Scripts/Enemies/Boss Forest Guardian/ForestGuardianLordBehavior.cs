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

        [Header("Guardian Settings")]
        [SerializeField] Animator animator;
        [SerializeField] Transform staffTransform;
        [SerializeField] float floatingHeight = 1.5f;
        [SerializeField] float floatingBobSpeed = 2f;
        [SerializeField] float floatingBobAmount = 0.3f;

        [Header("Thorn Patches (Skill 1)")]
        [SerializeField] GameObject thornPatchPrefab;
        [SerializeField] int patchesPerWave = 6;
        [SerializeField] int thornWaves = 2;
        [SerializeField] float timeBetweenPatches = 0.4f;
        [SerializeField] float thornContactDamage = 8f;
        [SerializeField] float thornPoisonDamage = 3f;
        [SerializeField] float thornDuration = 8f;

        [Header("Seed Barrage (Skill 2)")]
        [SerializeField] GameObject seedProjectilePrefab;
        [SerializeField] int seedsPerBarrage = 18;
        [SerializeField] int seedBarrages = 3;
        [SerializeField] float seedSpeed = 8f;
        [SerializeField] float seedDamage = 6f;
        [SerializeField] float timeBetweenSeeds = 0.1f;

        [Header("General Behavior")]
        [SerializeField] float movementDuration = 4f;
        [SerializeField] float attackCooldown = 2f;

        [Header("Effects")]
        [SerializeField] ParticleSystem thornCastingEffect;
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
                seedProjectilePool = new PoolComponent<SeedProjectileBehavior>(seedProjectilePrefab, seedsPerBarrage * seedBarrages);
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

        private IEnumerator FloatingMovement()
        {
            while (isFloating && IsAlive)
            {
                float bobOffset = Mathf.Sin(Time.time * floatingBobSpeed) * floatingBobAmount;
                Vector3 targetPosition = new Vector3(transform.position.x, originalPosition.y + floatingHeight + bobOffset, transform.position.z);
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 2f);

                if (staffTransform != null && IsMoving)
                {
                    Vector2 direction = ((Vector2)PlayerBehavior.Player.transform.position - (Vector2)transform.position).normalized;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    staffTransform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                }

                yield return null;
            }
        }

        private IEnumerator FloatToPosition()
        {
            IsMoving = true;
            yield return new WaitForSeconds(movementDuration);
            IsMoving = false;
        }

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

        private IEnumerator SeedBarrageAttack()
        {
            if (animator != null)
                animator.SetBool(IS_CASTING_SEEDS_HASH, true);
            
            if (seedCastingEffect != null)
                seedCastingEffect.Play();

            for (int barrage = 0; barrage < seedBarrages; barrage++)
            {
                for (int seed = 0; seed < seedsPerBarrage; seed++)
                {
                    LaunchSeed();
                    yield return new WaitForSeconds(timeBetweenSeeds);
                }

                if (barrage < seedBarrages - 1)
                    yield return new WaitForSeconds(0.5f);
            }

            if (animator != null)
                animator.SetBool(IS_CASTING_SEEDS_HASH, false);
        }

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
            thornPatch.Spawn(thornDuration);
        }

        private void LaunchSeed()
        {
            if (seedProjectilePool == null) return;

            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector2 launchPosition = (Vector2)transform.position + randomDirection * 0.5f;

            var seed = seedProjectilePool.GetEntity();
            seed.transform.position = launchPosition;
            seed.Damage = seedDamage * StageController.Stage.EnemyDamage;
            seed.Speed = seedSpeed;
            seed.onFinished += OnSeedFinished;
            
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
        }

        public void OnSeedCastingAnimationEvent()
        {
        }

        public void OnDeathExplosionAnimationEvent()
        {
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
            leaf.transform.position = transform.position;
            leaf.Damage = seedDamage * 0.5f * StageController.Stage.EnemyDamage;
            leaf.Speed = seedSpeed * 1.5f;
            leaf.onFinished += OnSeedFinished;
            
            leaf.Launch(direction);
        }
    }
}