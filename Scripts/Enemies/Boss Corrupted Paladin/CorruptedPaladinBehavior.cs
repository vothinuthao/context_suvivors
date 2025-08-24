using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using OctoberStudio.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio.Enemy
{
    public class CorruptedPaladinBehavior : EnemyBehavior
    {
        private static readonly int IS_CHARGING_HASH = Animator.StringToHash("IsCharging");
        private static readonly int DASH_TRIGGER = Animator.StringToHash("Dash");
        private static readonly int SUMMON_SWORDS_TRIGGER = Animator.StringToHash("SummonSwords");
        private static readonly int DEATH_TRIGGER = Animator.StringToHash("Death");

        [Header("Paladin Settings")]
        [SerializeField] Animator animator;
        [SerializeField] Vector2 fenceOffset = Vector2.one;

        [Header("Dash Attack (Skill 1)")]
        [SerializeField] SpriteRenderer dashWarningSprite;
        [SerializeField] float dashWarningDuration = 2f;
        [SerializeField] float dashSpeed = 15f;
        [SerializeField] float dashDistance = 8f;
        [SerializeField] float dashDamage = 15f;
        [SerializeField] int dashAttacksCount = 2;

        [Header("Divine Swords (Skill 2)")]
        [SerializeField] GameObject divineSwordPrefab;
        [SerializeField] int swordsPerWave = 5;
        [SerializeField] int swordWaves = 3;
        [SerializeField] float timeBetweenSwords = 0.3f;
        [SerializeField] float timeBetweenWaves = 1.5f;
        [SerializeField] float swordDamage = 12f;

        [Header("General Behavior")]
        [SerializeField] float movementDuration = 3f;
        [SerializeField] float attackCooldown = 2f;

        [Header("Effects")]
        [SerializeField] ParticleSystem chargeEffect;
        [SerializeField] ParticleSystem dashTrail;
        [SerializeField] ParticleSystem swordSummonEffect;

        private PoolComponent<DivineSwordBehavior> swordsPool;
        private List<DivineSwordBehavior> activeSwords = new List<DivineSwordBehavior>();
        private Coroutine behaviorCoroutine;
        private bool isDashing = false;

        protected override void Awake()
        {
            base.Awake();
            if (divineSwordPrefab != null)
            {
                swordsPool = new PoolComponent<DivineSwordBehavior>(divineSwordPrefab, swordsPerWave * swordWaves);
            }
        }

        public override void Play()
        {
            base.Play();
            behaviorCoroutine = StartCoroutine(BehaviorCoroutine());
        }

        private IEnumerator BehaviorCoroutine()
        {
            while (IsAlive)
            {
                // Movement phase - move to random position like other bosses
                yield return Movement();
                
                // Skill 1: Dash Attack (like Queen Wasp charge)
                yield return DashAttackSequence();
                yield return new WaitForSeconds(attackCooldown);

                // Movement phase again
                yield return Movement();
                
                // Skill 2: Divine Swords (like Mega Slime sword attack)
                yield return DivineSwordsAttack();
                yield return new WaitForSeconds(attackCooldown);
            }
        }

        private IEnumerator Movement()
        {
            IsMoving = true;

            // Move to random position near player (like other bosses)
            var randomPoint = StageController.FieldManager.Fence.GetRandomPointInside(1);

            // Ensure reasonable distance from player
            while(Vector2.Distance(randomPoint, PlayerBehavior.Player.transform.position) > 4)
            {
                randomPoint = StageController.FieldManager.Fence.GetRandomPointInside(1);
            }

            IsMovingToCustomPoint = true;
            CustomPoint = randomPoint;

            yield return new WaitUntil(() => Vector2.Distance(transform.position, randomPoint) < 0.2f);

            IsMovingToCustomPoint = false;
            IsMoving = false;
        }

        private IEnumerator DashAttackSequence()
        {
            for (int i = 0; i < dashAttacksCount; i++)
            {
                yield return PerformDashAttack();
                if (i < dashAttacksCount - 1)
                    yield return new WaitForSeconds(1f);
            }
        }

        private IEnumerator PerformDashAttack()
        {
            // Stop movement during attack
            IsMoving = false;
            
            if (animator != null)
                animator.SetBool(IS_CHARGING_HASH, true);
            
            if (dashWarningSprite != null)
                dashWarningSprite.gameObject.SetActive(true);
            
            if (chargeEffect != null)
                chargeEffect.Play();

            // Calculate dash direction like Queen Wasp charge
            float time = 0;
            Vector2 movementDirection = Vector2.up;

            // Warning phase - track player like Queen Wasp
            while (time < dashWarningDuration)
            {
                time += Time.deltaTime;

                movementDirection = (PlayerBehavior.Player.transform.position - transform.position).normalized;
                if (dashWarningSprite != null)
                {
                    dashWarningSprite.transform.rotation = Quaternion.FromToRotation(Vector2.up, movementDirection);
                    dashWarningSprite.size = new Vector2(1f, time / dashWarningDuration * dashDistance);
                }

                // Update scale direction like other bosses
                if (!scaleCoroutine.ExistsAndActive())
                {
                    var scale = transform.localScale;
                    if (movementDirection.x > 0 && scale.x < 0 || movementDirection.x < 0 && scale.x > 0)
                    {
                        scale.x *= -1;
                        transform.localScale = scale;
                    }
                }
                yield return null;
            }

            if (dashWarningSprite != null)
                dashWarningSprite.gameObject.SetActive(false);

            if (animator != null)
            {
                animator.SetBool(IS_CHARGING_HASH, false);
                animator.SetTrigger(DASH_TRIGGER);
            }

            isDashing = true;
            if (dashTrail != null)
                dashTrail.Play();

            // Execute dash like Queen Wasp charge
            time = 0;
            while (time < dashDistance / dashSpeed)
            {
                var newPosition = transform.position.XY() + Time.deltaTime * movementDirection * dashSpeed;
                
                // Validate position like Queen Wasp
                if (StageController.FieldManager.ValidatePosition(newPosition, fenceOffset))
                {
                    transform.position = newPosition;
                }

                time += Time.deltaTime;
                yield return null;
            }

            isDashing = false;
            if (dashTrail != null)
                dashTrail.Stop();

            if (animator != null)
                animator.SetBool(IS_CHARGING_HASH, false);

            OnDashImpact();
        }

        private IEnumerator DivineSwordsAttack()
        {
            // Stop movement during attack like Mega Slime
            IsMoving = false;
            
            if (animator != null)
                animator.SetTrigger(SUMMON_SWORDS_TRIGGER);
            
            if (swordSummonEffect != null)
                swordSummonEffect.Play();

            // Spawn swords in waves like Mega Slime sword attack
            for (int wave = 0; wave < swordWaves; wave++)
            {
                for (int sword = 0; sword < swordsPerWave; sword++)
                {
                    StartCoroutine(SpawnDivineSwordCoroutine());
                    yield return new WaitForSeconds(timeBetweenSwords);
                }

                if (wave < swordWaves - 1)
                    yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        private IEnumerator SpawnDivineSwordCoroutine()
        {
            // Get random spawn position like Mega Slime
            var spawnPosition = StageController.FieldManager.Fence.GetRandomPointInside(0.5f);

            // Create warning circle like Mega Slime
            var warningCircle = StageController.PoolsManager.GetEntity<WarningCircleBehavior>("Warning Circle");
            warningCircle.transform.position = spawnPosition;
            warningCircle.Play(1f, 0.3f, 0.8f, null);

            yield return new WaitForSeconds(1.3f);

            // Spawn divine sword
            if (swordsPool != null)
            {
                var sword = swordsPool.GetEntity();
                sword.transform.position = spawnPosition;
                sword.Damage = swordDamage * StageController.Stage.EnemyDamage;
                sword.onFinished += OnSwordFinished;
                
                activeSwords.Add(sword);
                sword.Spawn(2f);
            }

            // Clean up warning circle
            warningCircle.gameObject.SetActive(false);
        }

        public void OnDashImpact()
        {
            // Check damage in dash area
            float damageRadius = 2f;
            if (PlayerBehavior.Player != null && 
                Vector2.Distance(transform.position, PlayerBehavior.Player.transform.position) <= damageRadius)
            {
                PlayerBehavior.Player.TakeDamage(dashDamage * StageController.Stage.EnemyDamage);
            }
        }

        private void OnSwordFinished(DivineSwordBehavior sword)
        {
            sword.onFinished -= OnSwordFinished;
            activeSwords.Remove(sword);
        }

        protected override void Die(bool flash)
        {
            if (behaviorCoroutine != null)
                StopCoroutine(behaviorCoroutine);

            if (chargeEffect != null)
                chargeEffect.Stop();
            if (dashTrail != null)
                dashTrail.Stop();
            if (swordSummonEffect != null)
                swordSummonEffect.Stop();
            if (dashWarningSprite != null)
                dashWarningSprite.gameObject.SetActive(false);

            for (int i = 0; i < activeSwords.Count; i++)
            {
                if (activeSwords[i] != null)
                {
                    activeSwords[i].onFinished -= OnSwordFinished;
                    activeSwords[i].Clear();
                }
            }
            activeSwords.Clear();

            if (animator != null)
                animator.SetTrigger(DEATH_TRIGGER);

            base.Die(flash);
        }
    }
}