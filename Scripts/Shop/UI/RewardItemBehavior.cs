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
            rarityBorder.sprite = GetRarityIcon(reward.Rarity);
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
    /// Get rarity icon sprite based on rarity int value
    /// </summary>
    private Sprite GetRarityIcon(EquipmentRarity rarity)
    {
        if (DataLoadingManager.Instance == null)
        {
            return null;
        }

        // Use the int value of EquipmentRarity as icon name
        string iconName = ((int)rarity).ToString();
        return DataLoadingManager.Instance.LoadSprite("", iconName);
    }

    /// <summary>
    /// Set rarity icon for the item
    /// </summary>
    public void SetRarityIcon(Sprite rarityIcon)
    {
        if (rarityBorder != null && rarityIcon != null)
        {
            rarityBorder.sprite = rarityIcon;
        }
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