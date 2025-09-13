using System.Collections;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    public class StaffEarthMeteorBehavior : MonoBehaviour
    {
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private ParticleSystem impactEffect;
        
        private Vector3 targetPosition;
        private float fallSpeed;
        private float impactRadius;
        private float damage;
        
        private static readonly int METEOR_IMPACT_HASH = "Meteor Impact".GetHashCode();

        public void Initialize(Vector3 target, float speed, float radius, float dmg)
        {
            targetPosition = target;
            fallSpeed = speed;
            impactRadius = radius;
            damage = dmg;
            
            StartCoroutine(FallCoroutine());
        }

        private IEnumerator FallCoroutine()
        {
            Vector3 startPosition = transform.position;
            float journey = 0f;
            float totalDistance = Vector3.Distance(startPosition, targetPosition);
            
            while (journey <= totalDistance)
            {
                journey += fallSpeed * Time.deltaTime;
                float fractionOfJourney = journey / totalDistance;
                transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);
                
                yield return null;
            }
            
            // Impact
            OnImpact();
        }

        private void OnImpact()
        {
            // Play impact sound
            GameController.AudioManager.PlaySound(METEOR_IMPACT_HASH);
            
            // Create impact effect
            if (impactEffect != null)
            {
                impactEffect.transform.position = targetPosition;
                impactEffect.Play();
            }
            
            // Deal damage to enemies in radius
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(targetPosition, impactRadius);
            
            foreach (var collider in hitEnemies)
            {
                if (collider.TryGetComponent<EnemyBehavior>(out var enemy))
                {
                    // Apply damage
                    // Note: You may need to implement enemy damage method
                    // enemy.TakeDamage(damage);
                    
                    // Apply knockback
                    Vector2 knockbackDirection = (enemy.transform.position - targetPosition).normalized;
                    if (enemy.GetComponent<Rigidbody2D>() != null)
                    {
                        enemy.GetComponent<Rigidbody2D>().AddForce(knockbackDirection * 10f, ForceMode2D.Impulse);
                    }
                }
            }
            
            // Destroy meteor after impact
            Destroy(gameObject, impactEffect != null ? impactEffect.main.duration : 0.5f);
        }

        // For debugging - visualize impact radius in scene view
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, impactRadius);
        }
    }
}