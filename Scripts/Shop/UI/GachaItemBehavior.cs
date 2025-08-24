using System.Collections;
using DG.Tweening;
using OctoberStudio.Easing;
using OctoberStudio.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shop.UI
{
    public class GachaItemBehavior : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private Image backgroundFlash;
        [SerializeField] private ParticleSystem itemParticles;
        [SerializeField] private TMP_Text itemNameText;

        private RewardData rewardData;

        /// <summary>
        /// Setup gacha item
        /// </summary>
        public void Setup(RewardData reward)
        {
            rewardData = reward;

            // Set icon
            if (itemIcon != null)
            {
                itemIcon.sprite = GetRewardIcon(reward);
            }

            // Set rarity border
            if (rarityBorder != null)
            {
                rarityBorder.color = GetRarityColor(reward.Rarity);
            }

            // Set name
            if (itemNameText != null)
            {
                itemNameText.text = reward.DisplayName;
                itemNameText.gameObject.SetActive(false); // Start hidden
            }

            // Setup particles
            if (itemParticles != null)
            {
                var main = itemParticles.main;
                main.startColor = GetRarityColor(reward.Rarity);
            }
        }

        /// <summary>
        /// Play reveal animation with enhanced DOTween effects
        /// </summary>
        public IEnumerator PlayRevealAnimation(float duration, Ease easing)
        {
            float scaleMultiplier = GetRarityScaleMultiplier(rewardData.Rarity);
            
            // ✨ ENHANCED SEQUENCE
            var revealSequence = DOTween.Sequence();
            
            // 1. Scale up with overshoot
            revealSequence.Append(transform.DOScale(Vector3.one * (scaleMultiplier * 1.15f), duration * 0.7f)
                .SetEase(easing));
            
            // 2. Settle to final scale
            revealSequence.Append(transform.DOScale(Vector3.one * scaleMultiplier, duration * 0.3f)
                .SetEase(Ease.OutSine));
            
            // 3. Add special effects for rare items
            if (rewardData.Rarity >= EquipmentRarity.Epic)
            {
                // Rotation effect
                revealSequence.Insert(0, transform.DORotate(new Vector3(0, 0, 180), duration, RotateMode.FastBeyond360));
                
                // Pulse effect
                revealSequence.Insert(duration * 0.5f, transform.DOScale(Vector3.one * (scaleMultiplier * 1.05f), 0.2f)
                    .SetLoops(3, LoopType.Yoyo));
            }
            
            revealSequence.Play();

            // Flash background for epic+ items
            if (rewardData.Rarity >= EquipmentRarity.Epic && backgroundFlash != null)
            {
                backgroundFlash.color = GetRarityColor(rewardData.Rarity);
                backgroundFlash.gameObject.SetActive(true);
                backgroundFlash.DoAlpha(0f, duration);
            }

            // Start particles
            if (itemParticles != null)
            {
                itemParticles.Play();
            }

            // Show name after half duration
            yield return new WaitForSecondsRealtime(duration * 0.5f);
            
            if (itemNameText != null)
            {
                itemNameText.gameObject.SetActive(true);
                
                // Text animation
                var textCanvasGroup = itemNameText.GetComponent<CanvasGroup>();
                if (textCanvasGroup != null)
                {
                    textCanvasGroup.DoAlpha(1f, duration * 0.5f);
                }
                
                // Bounce text
                itemNameText.transform.DOScale(1.1f, 0.2f)
                    .SetLoops(2, LoopType.Yoyo);
            }

            yield return new WaitForSecondsRealtime(duration * 0.5f);
        }

        /// <summary>
        /// Instantly reveal item (for skip)
        /// </summary>
        public void InstantReveal()
        {
            float scaleMultiplier = GetRarityScaleMultiplier(rewardData.Rarity);
            transform.localScale = Vector3.one * scaleMultiplier;

            if (itemNameText != null)
            {
                itemNameText.gameObject.SetActive(true);
                var canvasGroup = itemNameText.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }
            }

            if (itemParticles != null)
            {
                itemParticles.Play();
            }
        }

        /// <summary>
        /// Get reward data
        /// </summary>
        public RewardData GetReward()
        {
            return rewardData;
        }

        // Helper methods (same as RewardItemBehavior)
        private Sprite GetRewardIcon(RewardData reward)
        {
            if (reward.Type == RewardType.Equipment && reward.EquipmentData != null)
            {
                return reward.EquipmentData.GetIcon();
            }
            return null;
        }

        private Color GetRarityColor(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => Color.white,
                EquipmentRarity.Uncommon => Color.green,
                EquipmentRarity.Rare => Color.blue,
                EquipmentRarity.Epic => Color.magenta,
                EquipmentRarity.Legendary => Color.yellow,
                _ => Color.white
            };
        }

        private float GetRarityScaleMultiplier(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Epic => 1.1f,
                EquipmentRarity.Legendary => 1.2f,
                _ => 1f
            };
        }
    }
}