using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio
{
    [CreateAssetMenu(fileName = "Element System Config", menuName = "October/Element System Config")]
    public class ElementSystemConfig : ScriptableObject
    {
        [Header("Damage Multipliers")]
        [SerializeField, Range(0f, 2f)] float counterDamageMultiplier = 1.3f;
        [SerializeField, Range(0f, 2f)] float weaknessMultiplier = 0.7f;
        [SerializeField, Range(0f, 2f)] float neutralMultiplier = 1f;

        [Header("Element Relationships")]
        [SerializeField] List<ElementRelationship> relationships = new List<ElementRelationship>();

        public float CounterDamageMultiplier => counterDamageMultiplier;
        public float WeaknessMultiplier => weaknessMultiplier;
        public float NeutralMultiplier => neutralMultiplier;

        public float GetDamageMultiplier(ElementType attackerElement, ElementType defenderElement)
        {
            if (attackerElement == ElementType.None || defenderElement == ElementType.None)
                return neutralMultiplier;

            foreach (var relationship in relationships)
            {
                if (relationship.attacker == attackerElement && relationship.defender == defenderElement)
                {
                    return relationship.isCounter ? counterDamageMultiplier : 
                           relationship.isWeak ? weaknessMultiplier : neutralMultiplier;
                }
            }

            return neutralMultiplier;
        }
        [ContextMenu("Setup Default Relationships")]
        public void SetupDefaultRelationships()
        {
            relationships.Clear();
            AddRelationship(ElementType.Fire, ElementType.Wood, isCounter: true);
            AddRelationship(ElementType.Wood, ElementType.Earth, isCounter: true);
            AddRelationship(ElementType.Earth, ElementType.Lightning, isCounter: true);
            AddRelationship(ElementType.Lightning, ElementType.Water, isCounter: true);
            AddRelationship(ElementType.Water, ElementType.Fire, isCounter: true);

            AddRelationship(ElementType.Wood, ElementType.Fire, isWeak: true);
            AddRelationship(ElementType.Fire, ElementType.Earth, isWeak: true);
            AddRelationship(ElementType.Earth, ElementType.Water, isWeak: true);
            AddRelationship(ElementType.Water, ElementType.Lightning, isWeak: true);
            AddRelationship(ElementType.Lightning, ElementType.Wood, isWeak: true);
        }

        private void AddRelationship(ElementType attacker, ElementType defender, bool isCounter = false, bool isWeak = false)
        {
            relationships.Add(new ElementRelationship
            {
                attacker = attacker,
                defender = defender,
                isCounter = isCounter,
                isWeak = isWeak
            });
        }
    }

    [System.Serializable]
    public class ElementRelationship
    {
        public ElementType attacker;
        public ElementType defender;
        public bool isCounter;
        public bool isWeak;
    }
}