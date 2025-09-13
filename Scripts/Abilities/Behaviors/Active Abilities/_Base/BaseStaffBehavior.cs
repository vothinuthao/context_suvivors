using System.Collections;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    public abstract class BaseStaffBehavior<TData, TLevel> : AbilityBehavior<TData, TLevel>
        where TData : GenericAbilityData<TLevel>
        where TLevel : BaseStaffLevel
    {
        protected Coroutine abilityCoroutine;
        protected Coroutine castingCoroutine;

        protected abstract void ExecuteMagic();
        protected abstract int AudioHash { get; }

        protected override void SetAbilityLevel(int stageId)
        {
            base.SetAbilityLevel(stageId);

            Disable();

            abilityCoroutine = StartCoroutine(AbilityCoroutine());
        }

        private IEnumerator AbilityCoroutine()
        {
            while (true)
            {
                // Start casting
                castingCoroutine = StartCoroutine(CastingCoroutine());
                
                yield return castingCoroutine;

                yield return new WaitForSeconds(AbilityLevel.AbilityCooldown / PlayerBehavior.Player.CooldownMultiplier);
            }
        }

        private IEnumerator CastingCoroutine()
        {
            // Cast time with potential visual effects
            yield return new WaitForSeconds(AbilityLevel.CastTime);
            
            // Channel time
            yield return new WaitForSeconds(AbilityLevel.ChannelTime);

            // Execute the magic effect
            ExecuteMagic();
            
            GameController.AudioManager.PlaySound(AudioHash);
        }

        protected virtual void Disable()
        {
            if (abilityCoroutine != null)
            {
                StopCoroutine(abilityCoroutine);
                abilityCoroutine = null;
            }

            if (castingCoroutine != null)
            {
                StopCoroutine(castingCoroutine);
                castingCoroutine = null;
            }
        }

        protected virtual void OnDestroy()
        {
            Disable();
        }

        protected virtual void OnDisable()
        {
            Disable();
        }
    }
}