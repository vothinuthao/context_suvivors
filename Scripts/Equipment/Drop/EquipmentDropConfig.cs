using UnityEngine;

namespace OctoberStudio.Equipment
{
    [CreateAssetMenu(fileName = "Equipment Drop Config", menuName = "October/Equipment Drop Config")]
    public class EquipmentDropConfig : ScriptableObject
    {
        [Header("User Level Restrictions")]
        [SerializeField] private int maxUserLevelForDrops = 5;
        public int MaxUserLevelForDrops => maxUserLevelForDrops;
        
        [Header("Drop Limits")]
        [SerializeField] private float dropCooldown = 30f;
        [SerializeField] private int maxDropsPerStage = 3;
        public float DropCooldown => dropCooldown;
        public int MaxDropsPerStage => maxDropsPerStage;
        
        [Header("Drop Multipliers")]
        [SerializeField] private float bossDropMultiplier = 2f;
        [SerializeField] private float subEnemyDropMultiplier = 1f;
        [SerializeField] private float stageProgressMultiplier = 0.1f;
        public float BossDropMultiplier => bossDropMultiplier;
        public float SubEnemyDropMultiplier => subEnemyDropMultiplier;
        public float StageProgressMultiplier => stageProgressMultiplier;
        
        [Header("Drop Behavior")]
        [SerializeField] private bool affectedByMagnet = true;
        [SerializeField] private float magnetDelay = 2f;
        public bool AffectedByMagnet => affectedByMagnet;
        public float MagnetDelay => magnetDelay;
        
        [Header("Visual & Audio")]
        [SerializeField] private bool showDropNotification = true;
        [SerializeField] private float notificationDuration = 3f;
        [SerializeField] private bool playDropSound = true;
        public bool ShowDropNotification => showDropNotification;
        public float NotificationDuration => notificationDuration;
        public bool PlayDropSound => playDropSound;
        
        [Header("Level Scaling")]
        [SerializeField] private bool enableLevelScaling = true;
        [SerializeField] private int maxEquipmentLevel = 5;
        public bool EnableLevelScaling => enableLevelScaling;
        public int MaxEquipmentLevel => maxEquipmentLevel;
    }
}