using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.Abilities
{
    public class KunaiThrowProjectileBehavior : SimplePlayerProjectileBehavior
    {
        private static readonly int KUNAI_THROW_HASH = "Kunai Throw".GetHashCode();

        public UnityAction<KunaiThrowProjectileBehavior> onKunaiFinished;

        public float SpinSpeed { get; set; }
        public bool Piercing { get; set; }
        public float Size { get; set; }

        private int hitCount = 0;
        private const int MAX_HITS = 3; // Maximum hits for piercing

        public void Spawn(Vector3 direction)
        {
            Init(transform.position, direction);

            transform.localScale = Vector3.one * Size * PlayerBehavior.Player.SizeMultiplier;
            
            hitCount = 0;
            selfDestructOnHit = !Piercing; // Don't self destruct if piercing

            KickBack = true;

            GameController.AudioManager.PlaySound(KUNAI_THROW_HASH);
        }

        protected override void Update()
        {
            base.Update();
            
            // Add spinning animation
            if (rotatingPart != null)
            {
                rotatingPart.Rotate(0, 0, SpinSpeed * Time.deltaTime);
            }
            else
            {
                transform.Rotate(0, 0, SpinSpeed * Time.deltaTime);
            }
        }

        public void Disable()
        {
            onKunaiFinished = null;
            gameObject.SetActive(false);
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<EnemyBehavior>(out var enemy))
            {
                hitCount++;
                
                if (Piercing && hitCount < MAX_HITS)
                {
                    // Don't destroy on hit, keep going
                    return;
                }
            }
            
            base.OnTriggerEnter2D(other);
            
            if (!gameObject.activeInHierarchy) // If we were destroyed
            {
                onKunaiFinished?.Invoke(this);
            }
        }

        public override void Clear()
        {
            base.Clear();
            onKunaiFinished?.Invoke(this);
        }
    }
}