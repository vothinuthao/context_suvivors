using UnityEngine;

namespace OctoberStudio
{
    public class ElementSystemTest : MonoBehaviour
    {
        [Header("Test Damage Calculation")]
        [SerializeField] ElementType attackerElement = ElementType.Fire;
        [SerializeField] ElementType defenderElement = ElementType.Wood;
        [SerializeField] float baseDamage = 100f;

        [ContextMenu("Test Damage")]
        public void TestDamage()
        {
            float multiplier = ElementSystem.Instance.CalculateElementalDamageMultiplier(attackerElement, defenderElement);
            float finalDamage = baseDamage * multiplier;
            
            Debug.Log($"Base Damage: {baseDamage}");
            Debug.Log($"{attackerElement} vs {defenderElement}");
            Debug.Log($"Multiplier: {multiplier}x");
            Debug.Log($"Final Damage: {finalDamage}");
        }

        [ContextMenu("Test All Elements")]
        public void TestAllElements()
        {
            ElementType[] elements = { ElementType.Fire, ElementType.Water, ElementType.Wood, ElementType.Earth, ElementType.Lightning };
            
            Debug.Log("=== ELEMENT DAMAGE MATRIX ===");
            
            foreach (var attacker in elements)
            {
                foreach (var defender in elements)
                {
                    float multiplier = ElementSystem.Instance.CalculateElementalDamageMultiplier(attacker, defender);
                    string result = multiplier > 1f ? "STRONG" : multiplier < 1f ? "WEAK" : "NEUTRAL";
                    Debug.Log($"{attacker} vs {defender}: {multiplier:F1}x ({result})");
                }
            }
        }
    }
}