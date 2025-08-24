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
                yield return MoveToPlayer();
                yield return DashAttackSequence();
                yield return new WaitForSeconds(attackCooldown);

                yield return MoveToPlayer();
                yield return DivineSwordsAttack();
                yield return new WaitForSeconds(attackCooldown);
            }
        }

        private IEnumerator MoveToPlayer()
        {
            IsMoving = true;
            yield return new WaitForSeconds(movementDuration);
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
            if (animator != null)
                animator.SetBool(IS_CHARGING_HASH, true);
            
            if (dashWarningSprite != null)
                dashWarningSprite.gameObject.SetActive(true);
            
            if (chargeEffect != null)
                chargeEffect.Play();

            Vector2 targetDirection = (PlayerBehavior.Player.transform.position - transform.position).normalized;
            
            if (dashWarningSprite != null)
                dashWarningSprite.transform.rotation = Quaternion.FromToRotation(Vector2.up, targetDirection);

            float warningTimer = 0f;
            while (warningTimer < dashWarningDuration)
            {
                warningTimer += Time.deltaTime;
                if (dashWarningSprite != null)
                {
                    dashWarningSprite.size = new Vector2(1f, warningTimer / dashWarningDuration * dashDistance);
                }
                yield return null;
            }

            if (animator != null)
            {
                animator.SetBool(IS_CHARGING_HASH, false);
                animator.SetTrigger(DASH_TRIGGER);
            }
            
            if (dashWarningSprite != null)
                dashWarningSprite.gameObject.SetActive(false);

            isDashing = true;
            if (dashTrail != null)
                dashTrail.Play();

            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition + (Vector3)targetDirection * dashDistance;

            yield return transform.DoPosition(endPosition, dashDistance / dashSpeed)
                .SetEasing(EasingType.ExpoOut)
                .SetOnFinish(() => {
                    isDashing = false;
                    if (dashTrail != null)
                        dashTrail.Stop();
                });

            OnDashImpact();
        }

        private IEnumerator DivineSwordsAttack()
        {
            if (animator != null)
                animator.SetTrigger(SUMMON_SWORDS_TRIGGER);
            
            if (swordSummonEffect != null)
                swordSummonEffect.Play();

            for (int wave = 0; wave < swordWaves; wave++)
            {
                for (int sword = 0; sword < swordsPerWave; sword++)
                {
                    SpawnDivineSword();
                    yield return new WaitForSeconds(timeBetweenSwords);
                }

                if (wave < swordWaves - 1)
                    yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        private void SpawnDivineSword()
        {
            if (swordsPool == null) return;

            Vector2 spawnPosition;
            if (StageController.FieldManager != null && StageController.FieldManager.Fence != null)
            {
                spawnPosition = StageController.FieldManager.Fence.GetRandomPointInside(1f);
            }
            else
            {
                spawnPosition = transform.position + (Vector3)Random.insideUnitCircle * 5f;
            }
            
            var sword = swordsPool.GetEntity();
            sword.transform.position = spawnPosition;
            sword.Damage = swordDamage * StageController.Stage.EnemyDamage;
            sword.onFinished += OnSwordFinished;
            
            activeSwords.Add(sword);
            sword.Spawn(2f);
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

        public void OnDashImpact()
        {
            float damageRadius = 2f;
            if (PlayerBehavior.Player != null && 
                Vector2.Distance(transform.position, PlayerBehavior.Player.transform.position) <= damageRadius)
            {
                PlayerBehavior.Player.TakeDamage(dashDamage * StageController.Stage.EnemyDamage);
            }
        }
    }
}