using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Common.Scripts.Equipment.UI
{
    /// <summary>
    /// Component for DescriptionCharacterStats prefab (200x50 size)
    /// Shows star tier and description for character upgrade system
    /// </summary>
    public class DescriptionCharacterStatsBehavior : MonoBehaviour
    {
        [Header("Star Display (Left Side)")]
        [SerializeField] private Transform starContainer;
        [SerializeField] private Image[] starImages = new Image[3]; // Max 3 stars

        [Header("Description Display (Right Side)")]
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Visual States")]
        [SerializeField] private Color unlockedTextColor = Color.white;
        [SerializeField] private Color lockedTextColor = Color.gray;

        private bool isUnlocked = false;

        /// <summary>
        /// Initialize the stat description for a specific tier
        /// </summary>
        /// <param name="tier">Tier number (1-6)</param>
        /// <param name="starCount">Number of stars to show (1-3)</param>
        /// <param name="starSprite">Sprite for the stars</param>
        /// <param name="description">Description text</param>
        /// <param name="unlocked">Whether this tier is unlocked</param>
        public void Initialize(int tier, int starCount, Sprite starSprite, string description, bool unlocked)
        {
            isUnlocked = unlocked;

            // Setup stars
            SetupStars(starCount, starSprite);

            // Setup description
            SetupDescription(description);

            // Setup visual state
            SetVisualState(unlocked);
        }

        /// <summary>
        /// Setup star display on the left side
        /// </summary>
        private void SetupStars(int starCount, Sprite starSprite)
        {
            // Clamp star count to max 3
            starCount = Mathf.Clamp(starCount, 0, 3);

            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    // Show star if within star count, hide otherwise
                    starImages[i].gameObject.SetActive(i < starCount);

                    if (i < starCount && starSprite != null)
                    {
                        starImages[i].sprite = starSprite;
                    }
                }
            }
        }

        /// <summary>
        /// Setup description text on the right side
        /// </summary>
        private void SetupDescription(string description)
        {
            if (descriptionText != null)
            {
                descriptionText.text = description;
            }
        }

        /// <summary>
        /// Set visual state based on unlock status
        /// </summary>
        private void SetVisualState(bool unlocked)
        {
            // Update text color
            if (descriptionText != null)
            {
                descriptionText.color = unlocked ? unlockedTextColor : lockedTextColor;
            }

            // Update star alpha
            foreach (var starImage in starImages)
            {
                if (starImage != null)
                {
                    var color = starImage.color;
                    color.a = unlocked ? 1f : 0.5f;
                    starImage.color = color;
                }
            }

            // Update overall alpha
            if (canvasGroup != null)
            {
                canvasGroup.alpha = unlocked ? 1f : 0.7f;
            }
        }

        /// <summary>
        /// Update unlock state dynamically
        /// </summary>
        public void SetUnlocked(bool unlocked)
        {
            if (isUnlocked != unlocked)
            {
                isUnlocked = unlocked;
                SetVisualState(unlocked);
            }
        }

        /// <summary>
        /// Get current unlock state
        /// </summary>
        public bool IsUnlocked => isUnlocked;
    }
}