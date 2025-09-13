using OctoberStudio.Easing;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.Abilities
{
    public class FistPunchProjectileBehavior : SimplePlayerProjectileBehavior
    {
        private static readonly int FIST_PUNCH_HASH = "Fist Punch".GetHashCode();

        public UnityAction<FistPunchProjectileBehavior> onPunchFinished;

        private IEasingCoroutine movementCoroutine;

        public float PunchRange { get; set; }
        public float KnockbackForce { get; set; }
        public float ProjectileLifetime { get; set; }
        public float Size { get; set; }

        public void Spawn(Vector3 direction)
        {
            Init(transform.position, direction);

            transform.localScale = Vector3.one * Size * PlayerBehavior.Player.SizeMultiplier;

            var targetPosition = transform.position + direction * PunchRange * PlayerBehavior.Player.SizeMultiplier;
            movementCoroutine = transform.DoPosition(targetPosition, ProjectileLifetime / PlayerBehavior.Player.ProjectileSpeedMultiplier).SetOnFinish(() =>
            {
                gameObject.SetActive(false);
                onPunchFinished?.Invoke(this);
            });

            KickBack = true;

            GameController.AudioManager.PlaySound(FIST_PUNCH_HASH);
        }

        public void Disable()
        {
            movementCoroutine?.Stop();
            onPunchFinished = null;
            gameObject.SetActive(false);
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            base.OnTriggerEnter2D(other);
            
            if (other.TryGetComponent<EnemyBehavior>(out var enemy))
            {
                Vector2 knockbackDirection = (enemy.transform.position - PlayerBehavior.Player.transform.position).normalized;
                // Apply knockback if the enemy has this method
                if (enemy.GetComponent<Rigidbody2D>() != null)
                {
                    enemy.GetComponent<Rigidbody2D>().AddForce(knockbackDirection * KnockbackForce, ForceMode2D.Impulse);
                }
            }
        }
    }
}