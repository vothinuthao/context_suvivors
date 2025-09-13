using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace OctoberStudio.Abilities
{
    public class BowShotProjectileBehavior : SimplePlayerProjectileBehavior
    {
        private static readonly int BOW_SHOT_HASH = "Bow Shot".GetHashCode();
        private static readonly int ARROW_EXPLOSION_HASH = "Arrow Explosion".GetHashCode();

        public UnityAction<BowShotProjectileBehavior> onArrowFinished;

        public float ExplosionRadius { get; set; }
        public float ExplosionDamage { get; set; }
        public float Size { get; set; }

        private bool hasExploded = false;

        public void Spawn(Vector3 direction)
        {
            Init(transform.position, direction);

            transform.localScale = Vector3.one * Size * PlayerBehavior.Player.SizeMultiplier;
            
            hasExploded = false;
            selfDestructOnHit = true;

            KickBack = true;

            GameController.AudioManager.PlaySound(BOW_SHOT_HASH);
        }

        public void Disable()
        {
            onArrowFinished = null;
            gameObject.SetActive(false);
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<EnemyBehavior>(out var enemy))
            {
                // Create explosion effect on impact
                CreateExplosion();
            }
            
            base.OnTriggerEnter2D(other);
            
            if (!gameObject.activeInHierarchy) // If we were destroyed
            {
                onArrowFinished?.Invoke(this);
            }
        }

        public override void Clear()
        {
            // Create explosion when lifetime ends
            if (!hasExploded)
            {
                CreateExplosion();
            }
            
            base.Clear();
            onArrowFinished?.Invoke(this);
        }

        private void CreateExplosion()
        {
            if (hasExploded) return;
            
            hasExploded = true;
            
            GameController.AudioManager.PlaySound(ARROW_EXPLOSION_HASH);
            
            // Find all enemies in explosion radius
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, ExplosionRadius * PlayerBehavior.Player.SizeMultiplier);
            
            foreach (var collider in hitEnemies)
            {
                if (collider.TryGetComponent<EnemyBehavior>(out var enemy))
                {
                    // Apply explosion damage (separate from arrow damage)
                    float explosionDamageAmount = ExplosionDamage * PlayerBehavior.Player.Damage;
                    // Note: You may need to implement a separate damage method for explosion damage
                    // For now, we'll just note that explosion damage should be applied
                }
            }
            
            // TODO: Add visual explosion effect here
            // You could instantiate an explosion particle system or animation
        }

        // For debugging - visualize explosion radius in scene view
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, ExplosionRadius);
        }
    }
}