using UnityEngine;

namespace OctoberStudio.Enemy
{
    public class ForestGuardianEventsHandler : MonoBehaviour
    {
        [SerializeField] ForestGuardianLordBehavior guardianBehavior;

        private void Awake()
        {
            if (guardianBehavior == null)
                guardianBehavior = GetComponentInParent<ForestGuardianLordBehavior>();
        }

        public void OnThornCastingStart()
        {
            if (guardianBehavior != null)
                guardianBehavior.OnThornCastingAnimationEvent();
        }

        public void OnSeedCastingStart()
        {
            if (guardianBehavior != null)
                guardianBehavior.OnSeedCastingAnimationEvent();
        }

        public void OnDeathExplosion()
        {
            if (guardianBehavior != null)
                guardianBehavior.OnDeathExplosionAnimationEvent();
        }
    }
}