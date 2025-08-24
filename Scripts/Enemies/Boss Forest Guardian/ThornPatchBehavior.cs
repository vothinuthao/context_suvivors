using OctoberStudio.Easing;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.Enemy
{
    public class ThornPatchBehavior : MonoBehaviour
    {
        private static readonly int SPAWN_TRIGGER = Animator.StringToHash("Spawn");
        private static readonly int IDLE_POISON_HASH = Animator.StringToHash("IdlePoison");
        private static readonly int HIDE_TRIGGER = Animator.StringToHash("Hide");

        [Header("Components")]
        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer thornSprite;
        [SerializeField] SpriteRenderer poisonAura;
        [SerializeField] Collider2D triggerCollider;
        [SerializeField] ParticleSystem spawnEffect;
        [SerializeField] ParticleSystem poisonEffect;

        [Header("Settings")]
        [SerializeField] float warningDuration = 1.3f;
        [SerializeField] float poisonTickRate = 1f;

        public float ContactDamage { get; set; }
        public float PoisonDamage { get; set; }
        public event UnityAction<ThornPatchBehavior> onFinished;

        private WarningCircleBehavior warningCircle;
        private bool isActive = false;
        private bool isHiding = false;
        private Coroutine poisonCoroutine;
        private PlayerBehavior playerInTrigger;

        public void Spawn(float lifetime)
        {
            if (thornSprite != null)
                thornSprite.enabled = false;
            if (poisonAura != null)
                poisonAura.enabled = false;
            if (triggerCollider != null)
                triggerCollider.enabled = false;
            
            isActive = false;
            isHiding = false;

            if (StageController.PoolsManager != null)
            {
                var warningPool = StageController.PoolsManager.GetPool("Warning Circle");
                if (warningPool != null)
                {
                    warningCircle = warningPool.GetEntity<WarningCircleBehavior>();
                    if (warningCircle != null)
                    {
                        warningCircle.transform.position = transform.position;
                        warningCircle.Play(2f, 0.5f, warningDuration - 0.5f, ShowThornPatch);
                    }
                    else
                    {
                        EasingManager.DoAfter(warningDuration, ShowThornPatch);
                    }
                }
                else
                {
                    EasingManager.DoAfter(warningDuration, ShowThornPatch);
                }
            }
            else
            {
                EasingManager.DoAfter(warningDuration, ShowThornPatch);
            }

            if (lifetime > 0)
            {
                EasingManager.DoAfter(lifetime, Hide);
            }
        }

        private void ShowThornPatch()
        {
            if (thornSprite != null)
                thornSprite.enabled = true;
            if (poisonAura != null)
                poisonAura.enabled = true;
            if (triggerCollider != null)
                triggerCollider.enabled = true;
            
            isActive = true;

            if (animator != null)
            {
                animator.SetTrigger(SPAWN_TRIGGER);
                animator.SetBool(IDLE_POISON_HASH, true);
            }
            
            if (spawnEffect != null)
                spawnEffect.Play();
            if (poisonEffect != null)
                poisonEffect.Play();
        }

        public void Hide()
        {
            if (!isHiding && isActive)
            {
                isHiding = true;
                isActive = false;
                
                if (poisonCoroutine != null)
                {
                    StopCoroutine(poisonCoroutine);
                    poisonCoroutine = null;
                }

                if (animator != null)
                {
                    animator.SetBool(IDLE_POISON_HASH, false);
                    animator.SetTrigger(HIDE_TRIGGER);
                }
                
                if (poisonEffect != null)
                    poisonEffect.Stop();

                EasingManager.DoAfter(0.5f, () => {
                    OnHidden();
                });
            }
        }

        public void OnHidden()
        {
            if (isHiding)
            {
                Clear();
                onFinished?.Invoke(this);
            }
        }

        public void Clear()
        {
            if (warningCircle != null)
            {
                warningCircle.gameObject.SetActive(false);
                warningCircle = null;
            }

            if (poisonCoroutine != null)
            {
                StopCoroutine(poisonCoroutine);
                poisonCoroutine = null;
            }

            playerInTrigger = null;
            gameObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!isActive) return;

            var playerCollisionHelper = collision.GetComponent<PlayerEnemyCollisionHelper>();
            if (playerCollisionHelper != null && PlayerBehavior.Player != null)
            {
                playerInTrigger = PlayerBehavior.Player;
                
                PlayerBehavior.Player.TakeDamage(ContactDamage);

                if (poisonCoroutine == null)
                {
                    poisonCoroutine = StartCoroutine(PoisonDamageCoroutine());
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!isActive) return;

            var playerCollisionHelper = collision.GetComponent<PlayerEnemyCollisionHelper>();
            if (playerCollisionHelper != null && PlayerBehavior.Player != null)
            {
                playerInTrigger = null;
                
                if (poisonCoroutine != null)
                {
                    StopCoroutine(poisonCoroutine);
                    poisonCoroutine = null;
                }
            }
        }

        private IEnumerator PoisonDamageCoroutine()
        {
            while (isActive && playerInTrigger != null)
            {
                yield return new WaitForSeconds(poisonTickRate);
                
                if (isActive && playerInTrigger != null)
                {
                    playerInTrigger.TakeDamage(PoisonDamage);
                }
            }
        }
    }
}