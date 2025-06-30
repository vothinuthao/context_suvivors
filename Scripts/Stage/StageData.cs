using UnityEngine;
using UnityEngine.Timeline;

namespace OctoberStudio
{
    [CreateAssetMenu(fileName = "Stage Data", menuName = "October/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("Display Data")]
        [SerializeField] Sprite icon;
        public Sprite Icon => icon;

        [SerializeField] string displayName;
        public string DisplayName => displayName;

        [Header("Timeline Data")]
        [SerializeField] TimelineAsset timeline;
        public TimelineAsset Timeline => timeline;

        [Header("Stage Settings")]
        [SerializeField] StageType stageType;
        public StageType StageType => stageType;

        [SerializeField] StageFieldData stageFieldData;
        public StageFieldData StageFieldData => stageFieldData;

        [SerializeField] bool spawnProp;
        public bool SpawnProp => spawnProp;

        [SerializeField] bool removePropFromBossfight;
        public bool RemovePropFromBossfight => removePropFromBossfight;

        [Space]
        [SerializeField] Color spotlightColor;
        public Color SpotlightColor => spotlightColor;

        [SerializeField] Color spotlightShadowColor;
        public Color SpotlightShadowColor => spotlightShadowColor;

        [Space]
        [SerializeField] float enemyDamage;
        public float EnemyDamage => enemyDamage;

        [SerializeField] float enemyHP;
        public float EnemyHP => enemyHP;

        [Space]
        [SerializeField] bool useCustomMusic;
        public bool UseCustomMusic => useCustomMusic;

        [SerializeField] string musicName;
        public string MusicName => musicName;
    }

    public enum StageType
    {
        Endless,
        VerticalEndless,
        HorizontalEndless,
        Rect
    }
}