using OctoberStudio.Abilities;
using OctoberStudio.Extensions;
using OctoberStudio.Pool;
using OctoberStudio.Timeline.Bossfight;
using OctoberStudio.UI;
using OctoberStudio.User;
using UnityEngine;
using UnityEngine.Playables;

namespace OctoberStudio
{
    public class StageController : MonoBehaviour
    {
        private static StageController instance;

        [SerializeField] StagesDatabase database;
        [SerializeField] PlayableDirector director;
        [SerializeField] EnemiesSpawner spawner;
        [SerializeField] StageFieldManager fieldManager;
        [SerializeField] ExperienceManager experienceManager;
        [SerializeField] DropManager dropManager;
        [SerializeField] AbilityManager abilityManager;
        [SerializeField] PoolsManager poolsManager;
        [SerializeField] WorldSpaceTextManager worldSpaceTextManager;
        [SerializeField] CameraManager cameraManager;

        public static EnemiesSpawner EnemiesSpawner => instance.spawner;
        public static ExperienceManager ExperienceManager => instance.experienceManager;
        public static AbilityManager AbilityManager => instance.abilityManager;
        public static StageFieldManager FieldManager => instance.fieldManager;
        public static PlayableDirector Director => instance.director;
        public static PoolsManager PoolsManager => instance.poolsManager;
        public static WorldSpaceTextManager WorldSpaceTextManager => instance.worldSpaceTextManager;
        public static CameraManager CameraController => instance.cameraManager;
        public static DropManager DropManager => instance.dropManager;

        [Header("UI")]
        [SerializeField] GameScreenBehavior gameScreen;
        [SerializeField] StageFailedScreen stageFailedScreen;
        [SerializeField] StageCompleteScreen stageCompletedScreen;

        [Header("Testing")]
        [SerializeField] PresetData testingPreset;

        public static GameScreenBehavior GameScreen => instance.gameScreen;

        public static StageData Stage { get; private set; }

        private StageSave stageSave;
        private float gameStartTime;
        private int enemiesKilledThisSession = 0;


        private void Awake()
        {
            instance = this;

            stageSave = GameController.SaveManager.GetSave<StageSave>("Stage");
        }

        private void Start()
        {
            Stage = database.GetStage(stageSave.SelectedStageId);

            director.playableAsset = Stage.Timeline;

            spawner.Init(director);
            experienceManager.Init(testingPreset);
            dropManager.Init();
            fieldManager.Init(Stage, director);
            abilityManager.Init(testingPreset, PlayerBehavior.Player.Data);
            cameraManager.Init(Stage);

            PlayerBehavior.Player.onPlayerDied += OnGameFailed;

            director.stopped += TimelineStopped;
            if (testingPreset != null) {
                director.time = testingPreset.StartTime; 
            } else
            {
                var time = stageSave.Time;

                var bossClips = director.GetClips<BossTrack, Boss>();

                for(int i = 0; i < bossClips.Count; i++)
                {
                    var bossClip = bossClips[i];

                    if(time >= bossClip.start && time <= bossClip.end)
                    {
                        time = (float) bossClip.start;
                        break;
                    }
                }

                director.time = time;
            }

            director.Play();

            if (Stage.UseCustomMusic)
            {
                GameController.ChangeMusic(Stage.MusicName);
            }
            gameStartTime = Time.time;
            enemiesKilledThisSession = 0;
        }

        private void TimelineStopped(PlayableDirector director)
        {
            if (gameObject.activeSelf)
            {
                // Calculate survival time
                float survivalTimeSeconds = Time.time - gameStartTime;
                
                // Update stage progress (existing code)
                if (stageSave.MaxReachedStageId < stageSave.SelectedStageId + 1 && stageSave.SelectedStageId + 1 < database.StagesCount)
                {
                    stageSave.SetMaxReachedStageId(stageSave.SelectedStageId + 1);
                }

                stageSave.IsPlaying = false;
                GameController.SaveManager.Save(true);

                // NEW: Grant User Profile XP for stage completion
                GrantUserProfileXP(survivalTimeSeconds, stageSave.SelectedStageId, true);

                gameScreen.Hide();
                stageCompletedScreen.Show();
                Time.timeScale = 0;

                Debug.Log($"Stage {stageSave.SelectedStageId} completed! Survival time: {survivalTimeSeconds:F1}s");
            }
        }

        private void OnGameFailed()
        {
            Time.timeScale = 0;

            // Calculate survival time
            float survivalTimeSeconds = Time.time - gameStartTime;

            stageSave.IsPlaying = false;
            GameController.SaveManager.Save(true);
            GrantUserProfileXP(survivalTimeSeconds, stageSave.SelectedStageId, false);

            gameScreen.Hide();
            stageFailedScreen.Show();

            Debug.Log($"Game failed on stage {stageSave.SelectedStageId}. Survival time: {survivalTimeSeconds:F1}s");
        }
        private void GrantUserProfileXP(float survivalTimeSeconds, int stageNumber, bool isVictory)
        {
            if (UserProfileManager.Instance != null)
            {
                var sessionData = new GameSessionData
                {
                    survivalTimeSeconds = survivalTimeSeconds,
                    stageNumber = stageNumber,
                    isVictory = isVictory,
                    enemiesKilled = enemiesKilledThisSession,
                    finalXPLevel = stageSave.XPLEVEL,
                    finalXP = stageSave.XP
                };

                UserProfileManager.Instance.OnGameSessionCompleted(sessionData);
            }
            else
            {
                Debug.LogWarning("UserProfileManager not found! User Profile XP not granted.");
            }
        }

        // NEW: Track enemy kills (optional - nếu bạn muốn bonus XP for kills)
        private void OnEnemyKilled()
        {
            enemiesKilledThisSession++;
        }

        public static void ResurrectPlayer()
        {
            EnemiesSpawner.DealDamageToAllEnemies(PlayerBehavior.Player.Damage * 1000);

            GameScreen.Show();
            PlayerBehavior.Player.Revive();
            Time.timeScale = 1;
        }

        public static void ReturnToMainMenu()
        {
            GameController.LoadMainMenu();
        }

        private void OnDisable()
        {
            director.stopped -= TimelineStopped;
        }
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugCompleteStage()
        {
            float fakeTime = Random.Range(60f, 300f); // 1-5 minutes
            GrantUserProfileXP(fakeTime, stageSave.SelectedStageId, true);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]  
        public void DebugFailStage()
        {
            float fakeTime = Random.Range(30f, 180f); // 30s-3min
            GrantUserProfileXP(fakeTime, stageSave.SelectedStageId, false);
        }
    }
    [System.Serializable]
    public class GameSessionData
    {
        public float survivalTimeSeconds;
        public int stageNumber;
        public bool isVictory;
        public int enemiesKilled;
        public int finalXPLevel;
        public float finalXP;

        public float GetSurvivalTimeMinutes() => survivalTimeSeconds / 60f;
        
        public override string ToString()
        {
            return $"Stage {stageNumber}: {GetSurvivalTimeMinutes():F1}min, " +
                   $"{(isVictory ? "Victory" : "Defeat")}, {enemiesKilled} kills";
        }
    }
}