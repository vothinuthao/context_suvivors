using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;

    public void Init(Sprite iconSprite, string label)
    {
        if (icon != null) icon.sprite = iconSprite;
        if (amountText != null) amountText.text = label;
    }
}