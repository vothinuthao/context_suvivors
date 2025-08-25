using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.Enemy
{
    public class SeedProjectileBehavior : SimpleEnemyProjectileBehavior
    {
        [Header("Settings - FIXED")]
        [SerializeField] float speed = 8f; // FIXED: Tốc độ nhanh
        [SerializeField] float lifetime = 5f; // FIXED: Lifetime phù hợp
        [SerializeField] TrailRenderer trail;

        [Header("Effects")]
        [SerializeField] ParticleSystem impactEffect;
        [SerializeField] GameObject visualsObject; // Container for seed visuals

        public float Damage { get; set; }
        public float Speed { get; set; }
        public event UnityAction<SeedProjectileBehavior> onFinished;

        private Vector2 direction;
        private float endTime;
        private bool isActive = false;

        private void Awake()
        {
            Speed = speed;
        }

        // FIXED: Launch seed từ boss position theo direction cụ thể
        public void Launch(Vector2 launchDirection)
        {
            direction = launchDirection.normalized;
            endTime = Time.time + lifetime;
            isActive = true;

            // FIXED: Enable visuals when launching
            if (visualsObject != null)
                visualsObject.SetActive(true);

            // FIXED: Reset trail
            if (trail != null)
                trail.Clear();
        }

        private void Update()
        {
            if (!isActive) return;

            // FIXED: Timeout check
            if (Time.time > endTime)
            {
                OnImpact();
                return;
            }

            // FIXED: Movement với tốc độ nhanh
            if (Speed > 0)
            {
                transform.position += (Vector3)direction * Time.deltaTime * Speed;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!isActive) return;

            // FIXED: Hit player
            var playerCollisionHelper = collision.GetComponent<PlayerEnemyCollisionHelper>();
            if (playerCollisionHelper != null && PlayerBehavior.Player != null)
            {
                PlayerBehavior.Player.TakeDamage(Damage);
                OnImpact();
                return;
            }

            // FIXED: Hit walls/obstacles
            if (collision.gameObject.layer == LayerMask.NameToLayer("Wall") ||
                collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
            {
                OnImpact();
            }
        }

        private void OnImpact()
        {
            isActive = false;
            
            // FIXED: Play impact effect
            if (impactEffect != null)
                impactEffect.Play();

            // FIXED: Clear và callback
            Clear();
            onFinished?.Invoke(this);
        }

        public void Clear()
        {
            isActive = false;
            Speed = speed; // FIXED: Reset speed
            
            // FIXED: Hide visuals
            if (visualsObject != null)
                visualsObject.SetActive(false);
            
            // FIXED: Clear trail
            if (trail != null) 
                trail.Clear();
                
            gameObject.SetActive(false);
        }
    }
}