using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.Enemy
{
    public class SeedProjectileBehavior : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] float speed = 8f;
        [SerializeField] float lifetime = 5f;
        [SerializeField] TrailRenderer trail;

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

        public void Launch(Vector2 launchDirection)
        {
            direction = launchDirection.normalized;
            endTime = Time.time + lifetime;
            isActive = true;
        }

        private void Update()
        {
            if (!isActive) return;

            if (Time.time > endTime)
            {
                OnImpact();
                return;
            }

            if (Speed > 0)
            {
                transform.position += (Vector3)direction * Time.deltaTime * Speed;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!isActive) return;

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
            isActive = false;
            Clear();
            onFinished?.Invoke(this);
        }

        public void Clear()
        {
            isActive = false;
            Speed = speed;
            
            if (trail != null) 
                trail.Clear();
                
            gameObject.SetActive(false);
        }
    }
}