using OctoberStudio.Audio;
using UnityEngine;

namespace OctoberStudio.Enemy
{
    public class SkeletonKingEventsHandler : MonoBehaviour
    {
        [SerializeField] SkeletonKingBehavior skeletonKingBehavior;

        [Header("Audio")]
        [SerializeField] string boneClatterSFX = "BoneClatter";
        [SerializeField] string necroSummonSFX = "NecroSummon";
        [SerializeField] string royalCommandSFX = "RoyalCommand";
        [SerializeField] string swordSlashSFX = "SwordSlash";
        [SerializeField] string darkMagicSFX = "DarkMagic";
        [SerializeField] string crownFallSFX = "CrownFall";
        [SerializeField] string skeletonRiseSFX = "SkeletonRise";

        [Header("Effects")]
        [SerializeField] ParticleSystem ambientAura;
        [SerializeField] ParticleSystem crownGlow;
        [SerializeField] ParticleSystem eyeGlow;
        [SerializeField] GameObject crownObject;

        private void Awake()
        {
            if (skeletonKingBehavior == null)
            {
                skeletonKingBehavior = GetComponentInParent<SkeletonKingBehavior>();
            }
        }

        private void Start()
        {
            // Start ambient effects
            if (ambientAura != null)
                ambientAura.Play();
                
            if (crownGlow != null)
                crownGlow.Play();
                
            if (eyeGlow != null)
                eyeGlow.Play();
        }

        #region Slash Animation Events


        public void OnSlashComplete()
        {
            // Called when slash animation completes
        }

        #endregion

        #region Movement Animation Events

        public void OnRegalPause()
        {
            // Called during regal idle animation pauses
            // Can be used for cape flow effects or royal presence
        }

        #endregion

        #region Death Animation Events
        

        public void OnEyesExtinguish()
        {
            // Called when eye glow fades during death
            if (eyeGlow != null)
                eyeGlow.Stop();
        }


        #endregion

        #region Special Effect Events


        public void OnDarkAuraIntensify()
        {
            // Called to intensify dark aura effects
            if (ambientAura != null)
            {
                var emission = ambientAura.emission;
                emission.rateOverTime = emission.rateOverTime.constant * 1.5f;
            }
        }

        public void OnDarkAuraNormal()
        {
            // Called to return dark aura to normal
            if (ambientAura != null)
            {
                var emission = ambientAura.emission;
                emission.rateOverTime = emission.rateOverTime.constant / 1.5f;
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Cleanup any remaining effects
            if (crownObject != null && crownObject.transform.parent == null)
            {
                Destroy(crownObject);
            }
        }
    }
}