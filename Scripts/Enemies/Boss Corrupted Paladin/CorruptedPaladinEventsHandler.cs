using UnityEngine;

namespace OctoberStudio.Enemy
{
    public class CorruptedPaladinEventsHandler : MonoBehaviour
    {
        [SerializeField] CorruptedPaladinBehavior paladin;

        public void OnDashImpact()
        {
            if (paladin != null)
            {
                paladin.OnDashImpact();
            }
        }

        public void OnDeathAnimationComplete()
        {
            // Called at the end of death animation to trigger any final effects
            if (paladin != null)
            {
                // Additional death effects can be added here
                Debug.Log("Corrupted Paladin death animation complete");
            }
        }

        public void OnSwordSummonComplete()
        {
            // Called when sword summon animation completes
            if (paladin != null)
            {
                Debug.Log("Sword summon animation complete");
            }
        }

        // Audio events for animations
        public void PlaySwordDashSound()
        {
            GameController.AudioManager.PlaySound("PaladinSwordDash".GetHashCode());
        }

        public void PlayDivineSummonSound()
        {
            GameController.AudioManager.PlaySound("DivineSwordSummon".GetHashCode());
        }

        public void PlayWeaponDropSound()
        {
            GameController.AudioManager.PlaySound("WeaponClatter".GetHashCode());
        }

        public void PlayKneelDownSound()
        {
            GameController.AudioManager.PlaySound("ArmorKneel".GetHashCode());
        }
    }
}