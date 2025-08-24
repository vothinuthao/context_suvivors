using OctoberStudio.Easing;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.Enemy
{
    public class DivineSwordBehavior : MonoBehaviour
    {
        private static readonly int SPAWN_TRIGGER = Animator.StringToHash("Spawn");
        private static readonly int HIDE_TRIGGER = Animator.StringToHash("Hide");

        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Collider2D swordCollider;
        [SerializeField] ParticleSystem spawnEffect;
        [SerializeField] ParticleSystem impactEffect;

        public float Damage { get; set; }
        public event UnityAction<DivineSwordBehavior> onFinished;

        private WarningCircleBehavior warningCircle;
        private bool isHiding = false;

        public void Spawn(float lifetime)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;
            if (swordCollider != null)
                swordCollider.enabled = false;
            
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
                        warningCircle.Play(1f, 0.5f, 0.8f, ShowSword);
                    }
                    else
                    {
                        EasingManager.DoAfter(1.3f, ShowSword);
                    }
                }
                else
                {
                    EasingManager.DoAfter(1.3f, ShowSword);
                }
            }
            else
            {
                EasingManager.DoAfter(1.3f, ShowSword);
            }

            if (lifetime > 0)
            {
                EasingManager.DoAfter(lifetime, Hide);
            }
        }

        private void ShowSword()
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
            if (swordCollider != null)
                swordCollider.enabled = true;
            
            if (animator != null)
                animator.SetTrigger(SPAWN_TRIGGER);
            if (spawnEffect != null)
                spawnEffect.Play();
        }

        public void Hide()
        {
            if (!isHiding)
            {
                if (animator != null)
                    animator.SetTrigger(HIDE_TRIGGER);
                isHiding = true;
                
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
            gameObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            var playerCollisionHelper = collision.GetComponent<PlayerEnemyCollisionHelper>();
            if (playerCollisionHelper != null && PlayerBehavior.Player != null)
            {
                PlayerBehavior.Player.TakeDamage(Damage);
                if (impactEffect != null)
                    impactEffect.Play();
                Hide();
            }
        }
    }
}