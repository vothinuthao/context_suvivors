using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio
{
    [CreateAssetMenu(menuName = "October/Enemies Database", fileName = "Enemies Database")]
    public class EnemiesDatabase : ScriptableObject
    {
        [SerializeField] List<EnemyData> enemies;

        public int EnemiesCount => enemies.Count;

        public EnemyData GetEnemyData(int index)
        {
            return enemies[index];
        }

        public EnemyData GetEnemyData(EnemyType type)
        {
            for(int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].Type == type) return enemies[i];
            }

            return null;
        }
    }

    [System.Serializable]
    public class EnemyData
    {
        [SerializeField] EnemyType type;
        [SerializeField] GameObject prefab;
        [SerializeField] Sprite icon;
        [SerializeField] List<EnemyDropData> enemyDrop;
        
        [Space]
        [Header("Element System")]
        [SerializeField] ElementType elementType = ElementType.None;
        public ElementType ElementType => elementType;
        public EnemyType Type => type;
        public GameObject Prefab => prefab;
        public Sprite Icon => icon;
        public List<EnemyDropData> EnemyDrop => enemyDrop;
    }

    [System.Serializable]
    public class EnemyDropData
    {
        [SerializeField] DropType dropType;
        [SerializeField, Range(0, 100)] float chance;

        public DropType DropType => dropType;
        public float Chance => chance;
    }

    public enum EnemyType
    {
        Bat = 0,
        Orc = 1,
        Slime = 2,
        Crab = 3,
        Vampire = 4,
        Wolf = 5,
        Goblin = 8,
        Spider = 9,
        Ghost = 10,
        Hand = 11,
        Eye = 12,
        FireSlime = 13,
        PurpleJellyfish = 14,
        StagBeetle = 15,
        Shade = 16,
        ShadeJellyfish = 17,
        ShadeBat = 18,
        ShadeVampire = 19,
    }
}