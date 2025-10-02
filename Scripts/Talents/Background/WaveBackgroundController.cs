using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Talents.Data;
using Talents.Manager;

namespace Talents.Background
{
    /// <summary>
    /// Wave Background System for Talent Tree
    /// Creates visual feedback showing progression with wave effect
    /// Wave height = (Highest Unlocked Level / Max Level) * Screen Height
    /// </summary>
    public class WaveBackgroundController : MonoBehaviour
    {
        [Header("Wave Components")]
        [SerializeField] private RectTransform waveContainer;
        [SerializeField] private Image waveBackground;
        [SerializeField] private Image waveOverlay;

        [Header("Wave Settings")]
        [SerializeField] private Color waveColor = new Color(0.2f, 0.8f, 1f, 0.3f);
        [SerializeField] private Color darkSpaceColor = new Color(0.1f, 0.1f, 0.2f, 0.8f);
        [SerializeField] private float animationDuration = 1.0f;
        [SerializeField] private Ease animationEasing = Ease.OutQuart;

        [Header("Wave Animation")]
        [SerializeField] private bool enableWaveAnimation = true;
        [SerializeField] private float waveSpeed = 1.0f;
        [SerializeField] private float waveAmplitude = 10f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private float currentWaveHeight = 0f;
        [SerializeField] private int highestUnlockedLevel = 0;
        [SerializeField] private int maxLevel = 30;

        // Animation
        private Tween currentWaveTween;
        private Material waveMaterial;

        private void Start()
        {
            InitializeWaveBackground();

            // Subscribe to talent events
            if (TalentManager.Instance != null)
            {
                TalentManager.Instance.OnTalentLearned.AddListener(OnTalentLearned);
            }
        }

        private void OnDestroy()
        {
            // Cleanup
            if (TalentManager.Instance != null)
            {
                TalentManager.Instance.OnTalentLearned.RemoveListener(OnTalentLearned);
            }

            currentWaveTween?.Kill();
        }

        /// <summary>
        /// Initialize wave background system
        /// </summary>
        private void InitializeWaveBackground()
        {
            if (waveContainer == null)
            {
                Debug.LogError("[WaveBackgroundController] Wave container not assigned!");
                return;
            }

            // Setup wave background
            if (waveBackground != null)
            {
                waveBackground.color = waveColor;

                // Create material for wave animation if needed
                if (enableWaveAnimation && waveBackground.material == null)
                {
                    waveMaterial = new Material(Shader.Find("UI/Default"));
                    waveBackground.material = waveMaterial;
                }
            }

            // Setup dark space overlay
            if (waveOverlay != null)
            {
                waveOverlay.color = darkSpaceColor;
            }

            // Get current progress and update wave
            UpdateWaveHeightFromCurrentProgress();
        }

        /// <summary>
        /// Update wave height based on current talent progression
        /// </summary>
        public void UpdateWaveHeightFromCurrentProgress()
        {
            if (TalentManager.Instance == null || !TalentManager.Instance.IsInitialized)
                return;

            // Calculate highest unlocked level
            int calculatedHighestLevel = CalculateHighestUnlockedLevel();
            int calculatedMaxLevel = TalentDatabase.Instance.MaxPlayerLevel;

            UpdateWaveHeight(calculatedHighestLevel, calculatedMaxLevel);
        }

        /// <summary>
        /// Calculate highest unlocked level from talent system
        /// </summary>
        private int CalculateHighestUnlockedLevel()
        {
            int highestLevel = 0;
            var allTalents = TalentDatabase.Instance.GetAllTalents();

            foreach (var talent in allTalents)
            {
                if (TalentManager.Instance.IsTalentLearned(talent.ID))
                {
                    highestLevel = Mathf.Max(highestLevel, talent.RequiredPlayerLevel);
                }
            }

            return highestLevel;
        }

        /// <summary>
        /// Update wave height with animation
        /// </summary>
        public void UpdateWaveHeight(int highestLevel, int maxLevel)
        {
            if (waveContainer == null) return;

            // Store values for debug
            highestUnlockedLevel = highestLevel;
            this.maxLevel = maxLevel;

            // Calculate wave height
            float progress = maxLevel > 0 ? (float)highestLevel / maxLevel : 0f;
            float screenHeight = Screen.height;
            float targetWaveHeight = screenHeight * progress;

            if (showDebugInfo)
            {
                Debug.Log($"[WaveBackground] Updating wave: Level {highestLevel}/{maxLevel} = {progress:F2} progress, Height: {targetWaveHeight:F0}px");
            }

            // Animate wave height
            AnimateWaveHeightChange(targetWaveHeight);
        }

        /// <summary>
        /// Animate wave height change with smooth transition
        /// </summary>
        private void AnimateWaveHeightChange(float targetHeight)
        {
            // Kill existing tween
            currentWaveTween?.Kill();

            // Get current height
            float currentHeight = waveContainer.sizeDelta.y;
            currentWaveHeight = currentHeight;

            // Animate to target height
            currentWaveTween = DOTween.To(
                () => currentHeight,
                height => {
                    var sizeDelta = waveContainer.sizeDelta;
                    sizeDelta.y = height;
                    waveContainer.sizeDelta = sizeDelta;
                    currentWaveHeight = height;
                },
                targetHeight,
                animationDuration
            ).SetEase(animationEasing);

            // Add completion callback
            currentWaveTween.OnComplete(() => {
                if (showDebugInfo)
                {
                    Debug.Log($"[WaveBackground] Wave animation completed at height: {targetHeight:F0}px");
                }

                // Spawn wave particles or effects here if needed
                SpawnWaveParticles(targetHeight);
            });
        }

        /// <summary>
        /// Spawn particle effects at wave line (placeholder for future particle system)
        /// </summary>
        private void SpawnWaveParticles(float waveHeight)
        {
            // TODO: Add particle effects at wave line
            // For now, just log the action
            if (showDebugInfo)
            {
                Debug.Log($"[WaveBackground] Wave particles spawned at height: {waveHeight:F0}px");
            }
        }

        /// <summary>
        /// Event handler for talent learned
        /// </summary>
        private void OnTalentLearned(TalentModel learnedTalent)
        {
            if (learnedTalent == null) return;

            // Update wave height when new talent is learned
            UpdateWaveHeightFromCurrentProgress();

            if (showDebugInfo)
            {
                Debug.Log($"[WaveBackground] Talent learned: {learnedTalent.Name} (Level {learnedTalent.RequiredPlayerLevel})");
            }
        }

        /// <summary>
        /// Manual wave height update (for testing)
        /// </summary>
        [ContextMenu("Test Wave Animation")]
        public void TestWaveAnimation()
        {
            UpdateWaveHeight(15, 30); // Test with 50% progress
        }

        /// <summary>
        /// Reset wave to bottom
        /// </summary>
        [ContextMenu("Reset Wave")]
        public void ResetWave()
        {
            UpdateWaveHeight(0, maxLevel);
        }

        /// <summary>
        /// Set wave to maximum
        /// </summary>
        [ContextMenu("Max Wave")]
        public void MaxWave()
        {
            UpdateWaveHeight(maxLevel, maxLevel);
        }

        private void Update()
        {
            // Animate wave texture if enabled (simple wave effect)
            if (enableWaveAnimation && waveMaterial != null)
            {
                float waveOffset = Mathf.Sin(Time.time * waveSpeed) * waveAmplitude;
                // Apply wave animation to material if using custom shader
                // waveMaterial.SetFloat("_WaveOffset", waveOffset);
            }
        }

        private void OnValidate()
        {
            // Update colors in editor
            if (waveBackground != null)
                waveBackground.color = waveColor;

            if (waveOverlay != null)
                waveOverlay.color = darkSpaceColor;
        }
    }
}