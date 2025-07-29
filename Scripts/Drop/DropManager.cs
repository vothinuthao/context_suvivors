using OctoberStudio.Drop;
using OctoberStudio.Easing;
using OctoberStudio.Pool;
using System.Collections;
using System.Collections.Generic;
using OctoberStudio.Equipment;
using OctoberStudio.Equipment.Drop;
using OctoberStudio.User;
using UnityEngine;

namespace OctoberStudio
{
    public class DropManager : MonoBehaviour
    {
        
        [SerializeField] DropDatabase database;
        [Header("Equipment Drop System")]
        [SerializeField] private EquipmentDropConfig equipmentDropConfig;
        [SerializeField] private bool enableEquipmentDrops = true;
        
        
        public Dictionary<DropType, PoolComponent<DropBehavior>> dropPools = new Dictionary<DropType, PoolComponent<DropBehavior>>();
        public Dictionary<DropType, float> lastTimeDropped = new Dictionary<DropType, float>();
        
        private Dictionary<EquipmentRarity, PoolComponent<EquipmentDropBehavior>> equipmentDropPools;
        private float lastEquipmentDropTime = -999f;
        private int equipmentDropsThisStage = 0;
        public List<DropBehavior> dropList = new List<DropBehavior>();

        private int startIndex;

        public void Init()
        {
            for (int i = 0; i < database.GemsCount; i++)
            {
                var data = database.GetGemData(i);

                var pool = new PoolComponent<DropBehavior>($"Drop_{data.DropType}", data.Prefab, 100);

                dropPools.Add(data.DropType, pool);
                lastTimeDropped.Add(data.DropType, 0);
            }
            InitializeEquipmentDropPools();
        }

        private void Update()
        {
            // Evaluating only a third drops every frame. Optimization techick.
            startIndex++;
            startIndex %= 3;
            for (int i = startIndex; i < dropList.Count; i += 3)
            {
                if(PlayerBehavior.Player.IsInsideMagnetRadius(dropList[i].transform))
                {
                    var drop = dropList[i];
                    drop.transform.DoPosition(PlayerBehavior.CenterTransform, 0.25f).SetEasing(EasingType.BackIn).SetOnFinish(() =>
                    {
                        drop.OnPickedUp();
                    });

                    dropList.RemoveAt(i);

                    i--;
                }
            }
        }

        public void PickUpAllDrop()
        {
            StartCoroutine(PickUpAllDropCoroutine());
        }

        private IEnumerator PickUpAllDropCoroutine()
        {
            var mgnetizedDropList = new List<DropBehavior>();

            for(int i = 0; i < dropList.Count; i++)
            {
                if (dropList[i].DropData.AffectedByMagnet)
                {
                    mgnetizedDropList.Add(dropList[i]);
                    dropList.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < mgnetizedDropList.Count; i ++)
            {
                // Preventing lags by picking up 10 drops each frame
                if (i % 10 == 0) yield return null;

                var drop = mgnetizedDropList[i];
                drop.transform.DoPosition(PlayerBehavior.CenterTransform, 0.4f).SetEasing(EasingType.BackIn).SetOnFinish(() =>
                {
                    drop.OnPickedUp();
                });
            }

            mgnetizedDropList.Clear();
        }

        public bool CheckDropCooldown(DropType dropType)
        {
            return Time.time - lastTimeDropped[dropType] >= database.GetGemData(dropType).DropCooldown;
        }

        public void Drop(DropType dropType, Vector3 position)
        {
            var drop = dropPools[dropType].GetEntity();

            drop.Init(database.GetGemData(dropType));
            drop.transform.position = position;

            dropList.Add(drop);

            lastTimeDropped[dropType] = Time.time;
        }
        private void InitializeEquipmentDropPools()
        {
            if (!enableEquipmentDrops || equipmentDropConfig == null)
            {
                Debug.LogWarning("[DropManager] Equipment drops disabled or config missing");
                return;
            }

            equipmentDropPools = new Dictionary<EquipmentRarity, PoolComponent<EquipmentDropBehavior>>();
    
            foreach (EquipmentRarity rarity in System.Enum.GetValues(typeof(EquipmentRarity)))
            {
                var equipmentDropData = database.GetEquipmentDropData(rarity);
                if (equipmentDropData?.Prefab != null)
                {
                    var pool = new PoolComponent<EquipmentDropBehavior>($"EquipmentDrop_{rarity}", equipmentDropData.Prefab, 3);
                    equipmentDropPools[rarity] = pool;
                    Debug.Log($"[DropManager] Created equipment drop pool for {rarity}");
                }
            }
        }
        /// <summary>
        /// Try to drop equipment from enemy death
        /// </summary>
        public void TryDropEquipment(Vector3 position, bool isBoss, int stageLevel)
        {
            if (!enableEquipmentDrops || !CanDropEquipment())
                return;
            
            // Get eligible equipment for this enemy type and stage
            var eligibleEquipment = EquipmentDropHelper.GetEligibleEquipment(isBoss, stageLevel);
            if (eligibleEquipment.Count == 0)
                return;
            
            // Calculate drop multiplier
            float dropMultiplier = isBoss ? equipmentDropConfig.BossDropMultiplier : equipmentDropConfig.SubEnemyDropMultiplier;
            
            // Add stage progress bonus
            dropMultiplier += (stageLevel - 1) * equipmentDropConfig.StageProgressMultiplier;
            
            // Select equipment to drop
            var selectedEquipment = EquipmentDropHelper.SelectEquipmentToDrop(eligibleEquipment, isBoss, dropMultiplier);
            
            if (selectedEquipment != null)
            {
                SpawnEquipmentDrop(selectedEquipment, position);
            }
        }

        private bool CanDropEquipment()
        {
            // Check if user is eligible for equipment drops
            if (!EquipmentDropHelper.IsUserEligibleForDrops(equipmentDropConfig.MaxUserLevelForDrops))
                return false;
            
            // Check drop cooldown
            if (Time.time - lastEquipmentDropTime < equipmentDropConfig.DropCooldown)
                return false;
            
            // Check stage drop limit
            if (equipmentDropsThisStage >= equipmentDropConfig.MaxDropsPerStage)
                return false;
            
            return true;
        }

        private void SpawnEquipmentDrop(EquipmentModel equipment, Vector3 position)
        {
            if (!equipmentDropPools.TryGetValue(equipment.Rarity, out var pool))
            {
                Debug.LogWarning($"[DropManager] No pool found for equipment rarity: {equipment.Rarity}");
                return;
            }
            
            var dropBehavior = pool.GetEntity();
            if (dropBehavior != null)
            {
                // Calculate equipment level for this drop
                int equipmentLevel = EquipmentDropHelper.CalculateDropLevel(equipment, equipmentDropConfig);
                
                // Setup the equipment drop
                dropBehavior.Setup(equipment.ID, equipmentLevel, equipment.Rarity);
                dropBehavior.transform.position = position;
                
                // Create DropData for consistency with existing system
                var dropData = CreateEquipmentDropData(equipment);
                dropBehavior.Init(dropData);
                
                // Add to drop list for magnet functionality
                dropList.Add(dropBehavior);
                
                // Track the drop
                lastEquipmentDropTime = Time.time;
                equipmentDropsThisStage++;
                
                Debug.Log($"[DropManager] Equipment dropped: {equipment.Name} (Level {equipmentLevel}) at {position}");
            }
        }

        private DropData CreateEquipmentDropData(EquipmentModel equipment)
        {
            var equipmentDropData = database.GetEquipmentDropData(equipment.Rarity);
            DropData newDropData = new DropData(DropType.Equipment, equipmentDropData?.Prefab, equipmentDropData?.AffectedByMagnet ?? equipmentDropConfig.AffectedByMagnet);
            return newDropData;
        }

        /// <summary>
        /// Reset equipment drops when starting new stage
        /// </summary>
        public void ResetEquipmentDrops()
        {
            equipmentDropsThisStage = 0;
            Debug.Log("[DropManager] Equipment drops reset for new stage");
        }

        /// <summary>
        /// Get equipment drop statistics for debugging
        /// </summary>
        public string GetEquipmentDropStats()
        {
            return $"Equipment Drops This Stage: {equipmentDropsThisStage}/{equipmentDropConfig?.MaxDropsPerStage ?? 0}";
        }
        
        // ADD these debug methods to DropManager class

        [Header("Debug Testing")]
        [SerializeField] private bool enableDebugMode = true;

        [ContextMenu("🧪 Test Common Equipment Drop")]
        public void TestCommonDrop()
        {
            if (!Application.isPlaying) return;
            
            Vector3 testPos = PlayerBehavior.Player.transform.position + Vector3.right * 2f;
            ForceDropSpecificRarity(EquipmentRarity.Common, testPos);
        }

        [ContextMenu("🧪 Test Rare Equipment Drop")]
        public void TestRareDrop()
        {
            if (!Application.isPlaying) return;
            
            Vector3 testPos = PlayerBehavior.Player.transform.position + Vector3.right * 2f;
            ForceDropSpecificRarity(EquipmentRarity.Rare, testPos);
        }

        [ContextMenu("🧪 Test Epic Equipment Drop")]
        public void TestEpicDrop()
        {
            if (!Application.isPlaying) return;
            
            Vector3 testPos = PlayerBehavior.Player.transform.position + Vector3.right * 2f;
            ForceDropSpecificRarity(EquipmentRarity.Epic, testPos);
        }

        [ContextMenu("🧪 Force Boss Drop Test")]
        public void TestBossDrop()
        {
            if (!Application.isPlaying) return;
            
            Vector3 testPos = PlayerBehavior.Player.transform.position + Vector3.up * 1f;
            TryDropEquipment(testPos, isBoss: true, stageLevel: 3);
            Debug.Log("🔥 BOSS DROP TEST - Check for equipment drop!");
        }

        [ContextMenu("🧪 Force Sub-Enemy Drop Test")]
        public void TestSubEnemyDrop()
        {
            if (!Application.isPlaying) return;
            
            Vector3 testPos = PlayerBehavior.Player.transform.position + Vector3.down * 1f;
            TryDropEquipment(testPos, isBoss: false, stageLevel: 2);
            Debug.Log("⚔️ SUB-ENEMY DROP TEST - Check for equipment drop!");
        }

        public void ForceDropSpecificRarity(EquipmentRarity targetRarity, Vector3 position)
        {
            if (!EquipmentDatabase.Instance.IsDataLoaded) return;
            
            // Get equipment of specific rarity
            var equipmentOfRarity = EquipmentDatabase.Instance.GetEquipmentByRarity(targetRarity);
            if (equipmentOfRarity.Length == 0)
            {
                Debug.LogWarning($"No equipment found for rarity: {targetRarity}");
                return;
            }
            
            // Pick random equipment of this rarity
            var selectedEquipment = equipmentOfRarity[Random.Range(0, equipmentOfRarity.Length)];
            
            // Force spawn it
            SpawnEquipmentDrop(selectedEquipment, position);
            
            Debug.Log($"🎁 FORCED DROP: {selectedEquipment.Name} ({targetRarity}) at {position}");
        }

        [ContextMenu("📊 Log Equipment Drop Status")]
        public void LogDropStatus()
        {
            Debug.Log("=== EQUIPMENT DROP STATUS ===");
            Debug.Log($"User Level: {UserProfileManager.Instance?.ProfileSave?.UserLevel ?? 0}");
            Debug.Log($"Drops This Stage: {equipmentDropsThisStage}/{equipmentDropConfig?.MaxDropsPerStage ?? 0}");
            Debug.Log($"Last Drop Time: {Time.time - lastEquipmentDropTime:F1}s ago");
            Debug.Log($"Drop Cooldown: {equipmentDropConfig?.DropCooldown ?? 0}s");
            Debug.Log($"Equipment Drops Enabled: {enableEquipmentDrops}");
            
            // Log eligible equipment
            EquipmentDropHelper.LogEligibleEquipment(true, 1);
        }
    }
}