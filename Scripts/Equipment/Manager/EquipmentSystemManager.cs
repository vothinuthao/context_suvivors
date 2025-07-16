using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using OctoberStudio.Save;

namespace OctoberStudio.Equipment
{
    /// <summary>
    /// Central coordinator for the entire equipment system
    /// Manages initialization, validation, and system health
    /// </summary>
    [DefaultExecutionOrder(-50)] // Execute before other equipment components
    public class EquipmentSystemManager : MonoBehaviour
    {
        [Header("System Settings")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool performMigrationOnStart = true;
        [SerializeField] private bool validateDataOnStart = true;
        [SerializeField] private bool enableDebugLogs = true;

        [Header("Auto-Fix Settings")]
        [SerializeField] private bool autoFixIssues = true;
        [SerializeField] private bool createBackupBeforeFix = true;

        [Header("System Status")]
        [SerializeField, ReadOnly] private bool systemInitialized = false;
        [SerializeField, ReadOnly] private bool databaseReady = false;
        [SerializeField, ReadOnly] private bool managerReady = false;
        [SerializeField, ReadOnly] private bool saveDataReady = false;
        [SerializeField, ReadOnly] private int totalValidationErrors = 0;

        // Events
        public UnityEvent OnSystemInitialized;
        public UnityEvent OnSystemReady; // All components ready
        public UnityEvent<string> OnSystemError;
        public UnityEvent<int> OnValidationComplete; // error count

        // Static access
        private static EquipmentSystemManager instance;
        public static EquipmentSystemManager Instance => instance;

        // System components
        private EquipmentDatabase database;
        private EquipmentManager manager;
        private EquipmentSave equipmentSave;

        // Initialization tracking
        private bool initializationStarted = false;
        private List<string> initializationErrors = new List<string>();

        private void Awake()
        {
            // Singleton pattern
            if (instance != null)
            {
                LogWarning("Another EquipmentSystemManager already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (autoInitialize)
            {
                StartCoroutine(InitializeSystemCoroutine());
            }
        }

        /// <summary>
        /// Initialize the equipment system
        /// </summary>
        public void InitializeSystem()
        {
            if (initializationStarted)
            {
                LogWarning("System initialization already in progress");
                return;
            }

            StartCoroutine(InitializeSystemCoroutine());
        }

        private IEnumerator InitializeSystemCoroutine()
        {
            initializationStarted = true;
            initializationErrors.Clear();
            
            LogInfo("Starting Equipment System initialization...");

            // Step 1: Wait for GameController and SaveManager
            yield return StartCoroutine(WaitForSaveManager());

            // Step 2: Initialize Database
            yield return StartCoroutine(InitializeDatabase());

            // Step 3: Initialize Manager
            yield return StartCoroutine(InitializeManager());

            // Step 4: Load Save Data
            yield return StartCoroutine(LoadSaveData());

            // Step 5: Perform Migration (if needed)
            if (performMigrationOnStart)
            {
                yield return StartCoroutine(PerformMigration());
            }

            // Step 6: Validate System
            if (validateDataOnStart)
            {
                yield return StartCoroutine(ValidateSystem());
            }

            // Step 7: Auto-fix issues (if enabled)
            if (autoFixIssues && totalValidationErrors > 0)
            {
                yield return StartCoroutine(AutoFixIssues());
            }

            // Step 8: Final status check
            CheckSystemStatus();
        }

        private IEnumerator WaitForSaveManager()
        {
            LogInfo("Waiting for SaveManager...");
            
            float timeout = 10f;
            float elapsed = 0f;

            while (GameController.SaveManager == null && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (GameController.SaveManager == null)
            {
                var error = "SaveManager not found after timeout";
                initializationErrors.Add(error);
                OnSystemError?.Invoke(error);
            }
            else
            {
                LogInfo("SaveManager ready");
            }
        }

        private IEnumerator InitializeDatabase()
        {
            LogInfo("Initializing Equipment Database...");

            // Find or create database
            database = FindObjectOfType<EquipmentDatabase>();
            if (database == null)
            {
                LogWarning("EquipmentDatabase not found in scene. Creating one...");
                var dbGO = new GameObject("EquipmentDatabase (Auto-Created)");
                database = dbGO.AddComponent<EquipmentDatabase>();
            }

            // Wait for database to load
            if (!database.IsDataLoaded)
            {
                database.LoadEquipmentData();
                
                float timeout = 30f;
                float elapsed = 0f;
                
                while (!database.IsDataLoaded && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            if (database.IsDataLoaded)
            {
                databaseReady = true;
                LogInfo($"Database ready with {database.TotalEquipmentCount} equipment items");
            }
            else
            {
                var error = "Failed to load Equipment Database";
                initializationErrors.Add(error);
                OnSystemError?.Invoke(error);
            }
        }

        private IEnumerator InitializeManager()
        {
            LogInfo("Initializing Equipment Manager...");

            // Find or create manager
            manager = FindObjectOfType<EquipmentManager>();
            if (manager == null)
            {
                LogWarning("EquipmentManager not found in scene. Creating one...");
                var managerGO = new GameObject("EquipmentManager (Auto-Created)");
                manager = managerGO.AddComponent<EquipmentManager>();
            }

            // Wait a frame for manager to initialize
            yield return null;

            if (EquipmentManager.Instance != null)
            {
                managerReady = true;
                LogInfo("Equipment Manager ready");
            }
            else
            {
                var error = "Failed to initialize Equipment Manager";
                initializationErrors.Add(error);
                OnSystemError?.Invoke(error);
            }
        }

        private IEnumerator LoadSaveData()
        {
            LogInfo("Loading Equipment Save Data...");

            if (GameController.SaveManager != null)
            {
                equipmentSave = GameController.SaveManager.GetSave<EquipmentSave>("Equipment");
                
                if (equipmentSave != null)
                {
                    equipmentSave.Init();
                    saveDataReady = true;
                    LogInfo($"Save data loaded with {equipmentSave.inventory.Count} inventory items");
                }
                else
                {
                    var error = "Failed to load Equipment Save Data";
                    initializationErrors.Add(error);
                    OnSystemError?.Invoke(error);
                }
            }

            yield return null;
        }

        private IEnumerator PerformMigration()
        {
            LogInfo("Performing data migration...");

            if (equipmentSave != null)
            {
                if (createBackupBeforeFix)
                {
                    EquipmentMigrationHelper.CreateBackup(equipmentSave);
                }

                bool migrationPerformed = EquipmentMigrationHelper.MigrateToUIDSystem(equipmentSave);
                
                if (migrationPerformed)
                {
                    LogInfo("Data migration completed successfully");
                }
                else
                {
                    LogInfo("No migration needed - data already in correct format");
                }
            }

            yield return null;
        }

        private IEnumerator ValidateSystem()
        {
            LogInfo("Validating equipment system...");

            if (equipmentSave != null)
            {
                var validation = EquipmentMigrationHelper.ValidateSaveData(equipmentSave);
                totalValidationErrors = validation.Errors.Count;
                
                if (validation.IsValid)
                {
                    LogInfo("System validation passed");
                }
                else
                {
                    LogWarning($"System validation found {totalValidationErrors} errors");
                    foreach (var error in validation.Errors)
                    {
                        LogError($"Validation Error: {error}");
                    }
                }

                OnValidationComplete?.Invoke(totalValidationErrors);
            }

            yield return null;
        }

        private IEnumerator AutoFixIssues()
        {
            LogInfo("Auto-fixing system issues...");

            int fixedCount = EquipmentDebugUtilities.AutoFixIssues();
            
            if (fixedCount > 0)
            {
                LogInfo($"Auto-fixed {fixedCount} issues");
                
                // Re-validate after fixes
                if (equipmentSave != null)
                {
                    var validation = EquipmentMigrationHelper.ValidateSaveData(equipmentSave);
                    totalValidationErrors = validation.Errors.Count;
                    LogInfo($"After auto-fix: {totalValidationErrors} remaining errors");
                }
            }

            yield return null;
        }

        private void CheckSystemStatus()
        {
            systemInitialized = true;
            
            bool systemHealthy = databaseReady && managerReady && saveDataReady && totalValidationErrors == 0;
            
            if (systemHealthy)
            {
                LogInfo("Equipment System fully initialized and ready!");
                OnSystemReady?.Invoke();
            }
            else
            {
                LogWarning($"Equipment System initialized with issues. Errors: {initializationErrors.Count}, Validation Errors: {totalValidationErrors}");
            }

            OnSystemInitialized?.Invoke();

            // Log final status
            if (enableDebugLogs)
            {
                LogSystemStatus();
            }
        }

        /// <summary>
        /// Get comprehensive system status
        /// </summary>
        public SystemStatus GetSystemStatus()
        {
            return new SystemStatus
            {
                IsInitialized = systemInitialized,
                DatabaseReady = databaseReady,
                ManagerReady = managerReady,
                SaveDataReady = saveDataReady,
                ValidationErrors = totalValidationErrors,
                InitializationErrors = initializationErrors.ToArray(),
                TotalEquipmentInDB = database?.TotalEquipmentCount ?? 0,
                TotalInventoryItems = equipmentSave?.inventory.Count ?? 0,
                UIDStatistics = UIDGenerator.GetStatistics()
            };
        }

        /// <summary>
        /// Log detailed system status
        /// </summary>
        public void LogSystemStatus()
        {
            var status = GetSystemStatus();
            var report = $"=== EQUIPMENT SYSTEM STATUS ===\n";
            report += $"Initialized: {status.IsInitialized}\n";
            report += $"Database Ready: {status.DatabaseReady} ({status.TotalEquipmentInDB} items)\n";
            report += $"Manager Ready: {status.ManagerReady}\n";
            report += $"Save Data Ready: {status.SaveDataReady} ({status.TotalInventoryItems} items)\n";
            report += $"Validation Errors: {status.ValidationErrors}\n";
            report += $"Initialization Errors: {status.InitializationErrors.Length}\n";
            report += $"UID Statistics: {status.UIDStatistics}\n";
            
            if (status.InitializationErrors.Length > 0)
            {
                report += "Initialization Errors:\n";
                foreach (var error in status.InitializationErrors)
                {
                    report += $"- {error}\n";
                }
            }

            LogInfo(report);
        }

        /// <summary>
        /// Force re-initialization of the system
        /// </summary>
        public void ReinitializeSystem()
        {
            LogInfo("Force re-initializing equipment system...");
            
            // Reset status
            systemInitialized = false;
            databaseReady = false;
            managerReady = false;
            saveDataReady = false;
            totalValidationErrors = 0;
            initializationStarted = false;
            initializationErrors.Clear();

            // Start initialization
            StartCoroutine(InitializeSystemCoroutine());
        }

        /// <summary>
        /// Perform emergency system repair
        /// </summary>
        public void EmergencyRepair()
        {
            LogInfo("Performing emergency system repair...");

            if (createBackupBeforeFix && equipmentSave != null)
            {
                EquipmentMigrationHelper.CreateBackup(equipmentSave);
            }

            // Force migration
            if (equipmentSave != null)
            {
                EquipmentMigrationHelper.MigrateToUIDSystem(equipmentSave);
            }

            // Auto-fix all issues
            int fixedCount = EquipmentDebugUtilities.AutoFixIssues();
            LogInfo($"Emergency repair completed. Fixed {fixedCount} issues.");

            // Re-validate
            if (equipmentSave != null)
            {
                var validation = EquipmentMigrationHelper.ValidateSaveData(equipmentSave);
                totalValidationErrors = validation.Errors.Count;
                LogInfo($"After emergency repair: {totalValidationErrors} remaining errors");
            }
        }

        // Logging methods
        private void LogInfo(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[EquipmentSystem] {message}");
        }

        private void LogWarning(string message)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[EquipmentSystem] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[EquipmentSystem] {message}");
        }

        // Context menu methods for debugging
        [ContextMenu("Log System Status")]
        public void LogSystemStatusMenuItem()
        {
            LogSystemStatus();
        }

        [ContextMenu("Generate System Report")]
        public void GenerateSystemReport()
        {
            var report = EquipmentDebugUtilities.GenerateSystemReport();
            Debug.Log(report);
        }

        [ContextMenu("Validate System")]
        public void ValidateSystemMenuItem()
        {
            if (equipmentSave != null)
            {
                var validation = EquipmentMigrationHelper.ValidateSaveData(equipmentSave);
                Debug.Log(validation.GetSummary());
            }
        }

        [ContextMenu("Emergency Repair")]
        public void EmergencyRepairMenuItem()
        {
            EmergencyRepair();
        }

        [ContextMenu("Reinitialize System")]
        public void ReinitializeSystemMenuItem()
        {
            ReinitializeSystem();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// System status data structure
        /// </summary>
        [System.Serializable]
        public struct SystemStatus
        {
            public bool IsInitialized;
            public bool DatabaseReady;
            public bool ManagerReady;
            public bool SaveDataReady;
            public int ValidationErrors;
            public string[] InitializationErrors;
            public int TotalEquipmentInDB;
            public int TotalInventoryItems;
            public UIDGenerator.UIDStatistics UIDStatistics;

            public bool IsHealthy => IsInitialized && DatabaseReady && ManagerReady && SaveDataReady && ValidationErrors == 0;
        }

        /// <summary>
        /// Quick access properties for other systems
        /// </summary>
        public static bool IsSystemReady => Instance != null && Instance.GetSystemStatus().IsHealthy;
        public static bool IsSystemInitialized => Instance != null && Instance.systemInitialized;
        public static EquipmentDatabase Database => Instance?.database;
        public static EquipmentManager Manager => Instance?.manager;
        public static EquipmentSave SaveData => Instance?.equipmentSave;
    }
}