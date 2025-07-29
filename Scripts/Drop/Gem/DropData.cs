using UnityEngine;

namespace OctoberStudio
{
    [System.Serializable]
    public class DropData
    {
        [SerializeField] DropType dropType;
        [SerializeField] GameObject prefab;
        [SerializeField] bool affectedByMagnet;
        [SerializeField, Min(0)] float dropCooldown;

        public DropType DropType => dropType;
        public GameObject Prefab => prefab;
        public bool AffectedByMagnet => affectedByMagnet;
        public float DropCooldown => dropCooldown;
        public DropData(DropType type, GameObject prefab, bool affectedByMagnet = true, float dropCooldown = 0f)
        {
            this.dropType = type;
            this.prefab = prefab;
            this.affectedByMagnet = affectedByMagnet;
            this.dropCooldown = dropCooldown;
        }
    }
}