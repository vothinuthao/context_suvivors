using UnityEngine;
using TwoSleepyCats.Patterns.Singleton;

namespace OctoberStudio
{
    public class ElementSystem : MonoSingleton<ElementSystem>
    {
        [SerializeField] ElementSystemConfig config;


        public static float CalculateElementalDamageMultiplier(ElementType attackerElement, ElementType defenderElement)
        {
            if (Instance?.config != null)
            {
                return Instance.config.GetDamageMultiplier(attackerElement, defenderElement);
            }
            
            return 1f;
        }

        public static void LogElementalDamage(ElementType attacker, ElementType defender, float baseDamage, float finalDamage)
        {
            if (!HasInstance) return;
            
            float multiplier = finalDamage / baseDamage;
            string relationship = multiplier > 1f ? "STRONG" : multiplier < 1f ? "WEAK" : "NEUTRAL";
            Debug.Log($"[ElementSystem] {attacker} vs {defender}: {baseDamage} -> {finalDamage} ({multiplier:F1}x) [{relationship}]");
        }

        public void SetConfig(ElementSystemConfig newConfig)
        {
            config = newConfig;
        }
    }
}