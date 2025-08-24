using OctoberStudio.Easing;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.Enemy
{
    public class SeedProjectileBehavior : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] Rigidbody2D rb;
        [SerializeField] SpriteRenderer seedSprite;
        [SerializeField] Collider2D projectileCollider;
        [SerializeField] ParticleSystem trailEffect;
        [SerializeField] ParticleSystem impactEffect;
        [SerializeField] Animator animator;

        [Header("Settings")]
        [SerializeField] float lifetime = 5f;
        [SerializeField] float rotationSpeed = 360f;

        public float Damage { get; set; }
        public float Speed { get; set; } = 8f;
        public event UnityAction<SeedProjectileBehavior> onFinished;

        private bool isLaunched = false;
        private Vector2 direction;
        private IEasingCoroutine lifetimeCoroutine;

        private void Awake()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody2D>();
        }

        public void Launch(Vector2 launchDirection)
        {
            direction = launchDirection.normalized;
            isLaunched = true;
            
            if (seedSprite != null)
                seedSprite.enabled = true;
            if (projectileCollider != null)
                projectileCollider.enabled = true;
                
            if (trailEffect != null)
                trailEffect.Play();

            if (rb != null)
            {
                rb.linearVelocity = direction * Speed;
            }

            lifetimeCoroutine = EasingManager.DoAfter(lifetime, () => {
                OnLifetimeExpired();
            });
        }

        private void Update()
        {
            if (isLaunched)
            {
                if (seedSprite != null)
                {
                    seedSprite.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!isLaunched) return;

            var playerCollisionHelper = collision.GetComponent<PlayerEnemyCollisionHelper>();
            if (playerCollisionHelper != null && PlayerBehavior.Player != null)
            {
                PlayerBehavior.Player.TakeDamage(Damage);
                OnImpact();
                return;
            }

            if (collision.gameObject.layer == LayerMask.NameToLayer("Wall") ||
                collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
            {
                OnImpact();
            }
        }

        private void OnImpact()
        {
            if (!isLaunched) return;

            isLaunched = false;
            
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
                
            if (trailEffect != null)
                trailEffect.Stop();
                
            if (impactEffect != null)
                impactEffect.Play();

            lifetimeCoroutine.StopIfExists();

            EasingManager.DoAfter(0.3f, () => {
                Clear();
                onFinished?.Invoke(this);
            });
        }

        private void OnLifetimeExpired()
        {
            OnImpact();
        }

        public void Clear()
        {
            isLaunched = false;
            
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
                
            if (trailEffect != null)
                trailEffect.Stop();
                
            if (seedSprite != null)
                seedSprite.enabled = false;
            if (projectileCollider != null)
                projectileCollider.enabled = false;

            lifetimeCoroutine.StopIfExists();
            
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            lifetimeCoroutine.StopIfExists();
        }
    }
}