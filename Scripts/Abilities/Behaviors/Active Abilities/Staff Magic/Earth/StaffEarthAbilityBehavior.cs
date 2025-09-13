using OctoberStudio.Extensions;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    public class StaffEarthAbilityBehavior : BaseStaffBehavior<StaffEarthAbilityData, StaffEarthAbilityLevel>
    {
        [SerializeField] GameObject meteorPrefab;
        
        private static readonly int STAFF_EARTH_HASH = "Staff Earth".GetHashCode();
        protected override int AudioHash => STAFF_EARTH_HASH;

        protected override void ExecuteMagic()
        {
            // Find target location
            var enemy = StageController.EnemiesSpawner.GetClosestEnemy(PlayerBehavior.CenterPosition);
            
            Vector3 targetPosition;
            if (enemy == null)
            {
                // Cast at random location around player
                Vector2 randomOffset = Random.insideUnitCircle * AbilityLevel.EffectRadius;
                targetPosition = PlayerBehavior.CenterPosition + randomOffset;
            }
            else
            {
                targetPosition = enemy.Center;
            }

            // Create meteor falling from above
            CreateMeteor(targetPosition);
        }

        private void CreateMeteor(Vector3 targetPosition)
        {
            if (meteorPrefab != null)
            {
                // Spawn meteor high above target
                Vector3 spawnPosition = targetPosition + Vector3.up * 10f;
                GameObject meteor = Instantiate(meteorPrefab, spawnPosition, Quaternion.identity);
                
                // Configure meteor behavior
                if (meteor.TryGetComponent<StaffEarthMeteorBehavior>(out var meteorBehavior))
                {
                    meteorBehavior.Initialize(
                        targetPosition,
                        AbilityLevel.MeteorFallSpeed,
                        AbilityLevel.ImpactRadius,
                        AbilityLevel.Damage * PlayerBehavior.Player.Damage
                    );
                }
            }
            else
            {
                // Fallback: Create simple explosion at target
                CreateImpactExplosion(targetPosition);
            }
        }

        private void CreateImpactExplosion(Vector3 position)
        {
            // Find all enemies in impact radius
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(position, AbilityLevel.ImpactRadius * PlayerBehavior.Player.SizeMultiplier);
            
            foreach (var collider in hitEnemies)
            {
                if (collider.TryGetComponent<EnemyBehavior>(out var enemy))
                {
                    // Apply damage
                    float damage = AbilityLevel.Damage * PlayerBehavior.Player.Damage;
                    // Note: You may need to implement enemy damage method
                    // enemy.TakeDamage(damage);
                }
            }
            
            // TODO: Add visual explosion effect
            // You could instantiate an explosion particle system or animation
        }

        // For debugging - visualize effect radius in scene view
        private void OnDrawGizmosSelected()
        {
            if (AbilityLevel != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(PlayerBehavior.CenterPosition, AbilityLevel.EffectRadius);
            }
        }
    }
}