using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OctoberStudio
{
    [CreateAssetMenu(fileName = "Drop Database", menuName = "October/Drop Database")]
    public class DropDatabase : ScriptableObject
    {
        [SerializeField] List<DropData> gems;
        [Header("Equipment Drops")]
        [SerializeField] List<EquipmentDropData> equipmentDrops = new List<EquipmentDropData>();
        public int GemsCount => gems.Count;

        public DropData GetGemData(int index)
        {
            return gems[index];
        }

        public DropData GetGemData(DropType dropType)
        {
            for(int i = 0; i < gems.Count; i++)
            {
                if (gems[i].DropType == dropType) return gems[i];
            }
            return null;
        }
        public EquipmentDropData GetEquipmentDropData(EquipmentRarity rarity)
        {
            return equipmentDrops.FirstOrDefault(x => x.Rarity == rarity);
        }

        public bool HasEquipmentDropData(EquipmentRarity rarity)
        {
            return equipmentDrops.Any(x => x.Rarity == rarity);
        }

        public int EquipmentDropsCount => equipmentDrops.Count;
    }
    [System.Serializable]
    public class EquipmentDropData
    {
        [SerializeField] EquipmentRarity rarity;
        [SerializeField] GameObject prefab;
        [SerializeField] bool affectedByMagnet = true;
        [SerializeField] float dropCooldown = 0f;
    
        public EquipmentRarity Rarity => rarity;
        public GameObject Prefab => prefab;
        public bool AffectedByMagnet => affectedByMagnet;
        public float DropCooldown => dropCooldown;
    }

}