using OctoberStudio.Easing;
using OctoberStudio.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardItemBehavior : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private Image rarityBorder;
    [SerializeField] private Image backgroundFlash;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject newBadge;

    /// <summary>
    /// Setup reward item display
    /// </summary>
    public void Setup(RewardData reward)
    {
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

        // Set quantity
        if (quantityText != null)
        {
            if (reward.Quantity > 1 || reward.Type == RewardType.Gold || reward.Type == RewardType.Gems)
            {
                quantityText.text = reward.Quantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }

        // Set name
        if (nameText != null)
        {
            nameText.text = reward.DisplayName;
        }

        // Show new badge for equipment
        if (newBadge != null)
        {
            newBadge.SetActive(reward.Type == RewardType.Equipment);
        }

        // Setup background flash
        if (backgroundFlash != null)
        {
            backgroundFlash.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Get icon for reward
    /// </summary>
    private Sprite GetRewardIcon(RewardData reward)
    {
        if (DataLoadingManager.Instance == null) return null;

        switch (reward.Type)
        {
            case RewardType.Gold:
                return DataLoadingManager.Instance.LoadSprite("Currency", "icon_gold");
            case RewardType.Gems:
                return DataLoadingManager.Instance.LoadSprite("Currency", "icon_gem");
            case RewardType.Equipment:
                return reward.EquipmentData?.GetIcon();
            case RewardType.Character:
                return DataLoadingManager.Instance.LoadSprite("Characters", "icon_character");
            default:
                return null;
        }
    }

    /// <summary>
    /// Get color for rarity
    /// </summary>
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

    /// <summary>
    /// Flash background with color
    /// </summary>
    public void FlashBackground(Color flashColor)
    {
        if (backgroundFlash == null) return;

        backgroundFlash.color = flashColor;
        backgroundFlash.gameObject.SetActive(true);
        
        // Fade out flash
        backgroundFlash.DoAlpha(0f, 1f).SetOnFinish(() => {
            backgroundFlash.gameObject.SetActive(false);
        });
    }
}