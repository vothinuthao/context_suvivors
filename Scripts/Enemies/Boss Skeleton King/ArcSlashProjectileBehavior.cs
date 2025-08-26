using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.Enemy
{
    public class ArcSlashProjectileBehavior : SimpleEnemyProjectileBehavior
    {
        [SerializeField] Collider2D projectileCollider;
        [SerializeField] SpriteRenderer slashSprite;
        [SerializeField] ParticleSystem trailEffect;
        [SerializeField] ParticleSystem impactEffect;
        [SerializeField] Animator animator;

        [Header("Arc Movement Settings")]
        [Tooltip("The curve height multiplier for arc trajectory")]
        [SerializeField] float arcHeightMultiplier = 2f;
        [Tooltip("The lifetime of the projectile")]
        [SerializeField] float projectileLifetime = 3f;
        [Tooltip("Size multiplier compared to normal projectiles")]
        [SerializeField] float sizeMultiplier = 1.5f;

        private Vector2 startPosition;
        private Vector2 targetDirection;
        private float speed;
        private float currentLifetime;
        private bool isActive;

        public new UnityAction<ArcSlashProjectileBehavior> onFinished;

        protected  void Awake()
        {
            transform.localScale = Vector3.one * sizeMultiplier;
        }

        public void Init(Vector2 position, Vector2 direction, float projectileSpeed)
        {
            transform.position = position;
            startPosition = position;
            targetDirection = direction.normalized;
            speed = projectileSpeed;
            currentLifetime = 0f;
            isActive = true;

            // Enable components
            projectileCollider.enabled = true;
            slashSprite.enabled = true;

            // Start trail effect
            if (trailEffect != null)
                trailEffect.Play();

            // Rotate sprite to face movement direction
            float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            gameObject.SetActive(true);
        }

        protected override void Update()
        {
            if (!isActive) return;

            currentLifetime += Time.deltaTime;

            // Check lifetime
            if (currentLifetime >= projectileLifetime)
            {
                Clear();
                return;
            }

            // Move projectile with arc trajectory
            MoveWithArc();

            // Check bounds
            if (IsOutOfBounds())
            {
                Clear();
            }
        }

        private void MoveWithArc()
        {
            // Calculate progress (0 to 1)
            float progress = currentLifetime / projectileLifetime;
            
            // Linear movement in target direction
            Vector2 linearPosition = startPosition + targetDirection * speed * currentLifetime;
            
            // Add arc height using sine wave
            float arcHeight = Mathf.Sin(progress * Mathf.PI) * arcHeightMultiplier;
            Vector2 perpendicular = Vector2.Perpendicular(targetDirection).normalized;
            
            // Apply arc offset
            Vector2 finalPosition = linearPosition + perpendicular * arcHeight;
            
            transform.position = finalPosition;

            // Rotate sprite to follow arc trajectory
            Vector2 velocity = targetDirection * speed + perpendicular * (Mathf.Cos(progress * Mathf.PI) * Mathf.PI * arcHeightMultiplier / projectileLifetime);
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        private bool IsOutOfBounds()
        {
            Vector3 position = transform.position;
            
            // Check if projectile is outside camera bounds with some margin
            float margin = 5f;
            return position.x < CameraManager.LeftBound - margin ||
                   position.x > CameraManager.RightBound + margin ||
                   position.y < CameraManager.BottomBound - margin ||
                   position.y > CameraManager.TopBound + margin;
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive) return;

            // Check if hit player
            if (other.CompareTag("Player"))
            {
                // Play impact effect
                if (impactEffect != null)
                {
                    impactEffect.transform.position = transform.position;
                    impactEffect.Play();
                }

                // Deal damage (base class handles this)
                base.OnTriggerEnter2D(other);

                // Don't clear immediately, allow for potential multi-hit
                // Clear after a short delay instead
                StartCoroutine(ClearAfterHit());
            }
        }

        private IEnumerator ClearAfterHit()
        {
            // Disable collider to prevent multiple hits
            projectileCollider.enabled = false;
            
            // Brief delay before clearing
            yield return new WaitForSeconds(0.1f);
            
            Clear();
        }

        public void Clear()
        {
            if (!gameObject.activeInHierarchy) return;

            isActive = false;

            // Stop effects
            if (trailEffect != null)
                trailEffect.Stop();

            // Disable components
            projectileCollider.enabled = false;
            slashSprite.enabled = false;

            // Stop any running coroutines
            StopAllCoroutines();

            // Deactivate and notify
            gameObject.SetActive(false);
            onFinished?.Invoke(this);
        }

        // Animation event handlers (if using animator)
        public void OnImpactAnimationEvent()
        {
            // Called during impact animation frames
        }

        public void OnProjectileAnimationComplete()
        {
            // Called when projectile animation completes
            Clear();
        }

        private void OnDrawGizmos()
        {
            if (!isActive) return;

            // Draw trajectory preview in editor
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            // Draw movement direction
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(targetDirection * 2f));
        }
    }
}