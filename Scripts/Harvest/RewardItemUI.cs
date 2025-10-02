using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OctoberStudio.Harvest;
using OctoberStudio.Equipment;

public class RewardItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject rarityGlow;

    [Header("Rarity Colors")]
    [SerializeField] private Color commonColor = Color.white;
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = Color.magenta;
    [SerializeField] private Color legendaryColor = Color.yellow;

    public void Init(Sprite iconSprite, string label)
    {
        if (icon != null) icon.sprite = iconSprite;
        if (amountText != null) amountText.text = label;
    }

    public void Init(HarvestRewardData rewardData)
    {
        if (icon != null) icon.sprite = rewardData.icon;
        if (amountText != null) amountText.text = $"{rewardData.name} x{rewardData.amount}";

        // Set rarity effects for equipment
        if (rewardData.rewardHarvestType == RewardHarvestType.Equipment)
        {
            SetEquipmentRarity(rewardData.equipmentRarity);
        }
        else
        {
            SetRewardTypeColor(rewardData.rewardHarvestType);
        }
    }

    private void SetEquipmentRarity(EquipmentRarity rarity)
    {
        Color rarityColor = GetRarityColor(rarity);

        // Set background color
        if (backgroundImage != null)
        {
            backgroundImage.color = rarityColor;
        }

        // Show glow for rare+ items
        if (rarityGlow != null)
        {
            bool showGlow = rarity >= EquipmentRarity.Rare;
            rarityGlow.SetActive(showGlow);

            if (showGlow)
            {
                var glowImage = rarityGlow.GetComponent<Image>();
                if (glowImage != null)
                {
                    glowImage.color = rarityColor;
                }
            }
        }
    }

    private void SetRewardTypeColor(RewardHarvestType rewardType)
    {
        if (backgroundImage == null) return;

        Color typeColor = rewardType switch
        {
            RewardHarvestType.Gold => Color.yellow,
            RewardHarvestType.Gem => Color.magenta,
            RewardHarvestType.Exp => Color.cyan,
            RewardHarvestType.CharacterPieces => Color.green,
            _ => Color.white
        };

        backgroundImage.color = new Color(typeColor.r, typeColor.g, typeColor.b, 0.3f);
    }

    private Color GetRarityColor(EquipmentRarity rarity)
    {
        return rarity switch
        {
            EquipmentRarity.Common => commonColor,
            EquipmentRarity.Rare => rareColor,
            EquipmentRarity.Epic => epicColor,
            EquipmentRarity.Legendary => legendaryColor,
            _ => commonColor
        };
    }
}