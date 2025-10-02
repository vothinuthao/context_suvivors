using OctoberStudio.Abilities;
using OctoberStudio.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    public class CharacterItemBehavior : MonoBehaviour
    {
        [SerializeField] RectTransform rect;
        public RectTransform Rect => rect;

        [Header("Info")]
        [SerializeField] Image iconImage;
        [SerializeField] TextMeshProUGUI titleLabel;
        // [SerializeField] GameObject startingAbilityObject;
        // [SerializeField] Image startingAbilityImage;

        [Header("Star Display")]
        [SerializeField] private Image[] starImages = new Image[3]; // Display current star level

        [Header("Lock System")]
        [SerializeField] GameObject lockMask;
        [SerializeField] Image lockIcon;

        [Header("Button")]
        [SerializeField] Button selectButtonCharacter;
        [SerializeField] Sprite enabledButtonSprite;
        [SerializeField] Sprite disabledButtonSprite;
        [SerializeField] Sprite selectedButtonSprite;

        [Header("Star Sprites")]
        [SerializeField] private Sprite greyStarSprite;     // For tiers 1-3
        [SerializeField] private Sprite goldStarSprite;     // For tier 4
        [SerializeField] private Sprite orangeStarSprite;   // For tier 5
        [SerializeField] private Sprite purpleStarSprite;   // For tier 6
        [SerializeField] private Sprite emptyStarSprite;    // For locked tiers
        

        public CurrencySave GoldCurrency { get; private set; }
        private CharactersSave charactersSave;

        public Selectable Selectable => selectButtonCharacter;
        public Button OnclickButton => selectButtonCharacter;

        public CharacterData Data { get; private set; }
        public int CharacterId { get; private set; }

        public bool IsSelected { get; private set; }

        public UnityAction<CharacterItemBehavior> onNavigationSelected;

        private void Start()
        {
            selectButtonCharacter.onClick.AddListener(SelectButtonClick);
        }

        public void Init(int id, CharacterData characterData, AbilitiesDatabase database)
        {
            if(charactersSave == null)
            {
                charactersSave = GameController.SaveManager.GetSave<CharactersSave>("Characters");
                charactersSave.onSelectedCharacterChanged += RedrawVisuals;
                charactersSave.onCharacterUpgraded += OnCharacterUpgraded;
            }

            if (GoldCurrency == null)
            {
                GoldCurrency = GameController.SaveManager.GetSave<CurrencySave>("gold");
                GoldCurrency.onGoldAmountChanged += OnGoldAmountChanged;
            }

            // startingAbilityObject.SetActive(characterData.HasStartingAbility);
            //
            // if(characterData.HasStartingAbility)
            // {
            //     var abilityData = database.GetAbility(characterData.StartingAbility);
            //     startingAbilityImage.sprite = abilityData.Icon;
            // }

            Data = characterData;
            CharacterId = id;

            RedrawVisuals();
        }

        private void RedrawVisuals()
        {
            titleLabel.text = Data.Name;
            iconImage.sprite = Data.Icon;

            UpdateStarDisplay();
            RedrawButton();
        }

        /// <summary>
        /// Update star display to show current character star level
        /// </summary>
        private void UpdateStarDisplay()
        {
            if (starImages == null || charactersSave == null) return;

            // Get current star level for this character
            int currentStarLevel = charactersSave.GetCharacterStarLevel(CharacterId);

            // Always ensure all 3 star images are active
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    starImages[i].gameObject.SetActive(true);
                    UpdateSingleStarImage(i, currentStarLevel);
                }
            }
        }

        /// <summary>
        /// Update a single star image based on character progress
        /// Uses milestone system: Level 1-3 grey, 4-6 orange, 7-9 gold, 10-12 purple
        /// </summary>
        private void UpdateSingleStarImage(int starIndex, int currentStarLevel)
        {
            if (starImages[starIndex] == null) return;

            Image starImage = starImages[starIndex];

            // Get character data to access upgrade config
            if (Data?.UpgradeConfig == null)
            {
                starImage.sprite = emptyStarSprite;
                starImage.color = Color.gray;
                return;
            }

            // Get how many stars should be shown for current level
            int starsToShow = Data.UpgradeConfig.GetStarsInMilestone(currentStarLevel);

            if (starIndex < starsToShow)
            {
                // This star should be shown - get appropriate sprite for current level
                Sprite starSprite = GetSpriteForStarType(Data.UpgradeConfig.GetStarTypeForLevel(currentStarLevel));
                starImage.sprite = starSprite;
                starImage.color = Color.white;
            }
            else
            {
                // This star should be empty
                starImage.sprite = emptyStarSprite;
                starImage.color = Color.gray;
            }
        }

        /// <summary>
        /// Get sprite for star type
        /// </summary>
        private Sprite GetSpriteForStarType(StarType starType)
        {
            return starType switch
            {
                StarType.Grey => greyStarSprite,
                StarType.Orange => orangeStarSprite,
                StarType.Gold => goldStarSprite,
                StarType.Purple => purpleStarSprite,
                _ => greyStarSprite
            };
        }

        private void RedrawButton()
        {
            // Check if character is actually owned through save system
            bool isOwned = charactersSave != null && charactersSave.HasCharacterBeenBought(CharacterId);
            bool isCurrentlySelected = charactersSave != null && charactersSave.SelectedCharacterId == CharacterId;

            Debug.Log($"[CharacterItem] RedrawButton for Character {CharacterId}: isOwned={isOwned}");

            // Show lock mask for characters that are NOT owned
            UpdateLockMask(!isOwned);

            // All characters are clickable for viewing, but only owned ones can be selected
            selectButtonCharacter.interactable = true;

            if (isOwned)
            {
                if (isCurrentlySelected)
                {
                    selectButtonCharacter.image.sprite = selectedButtonSprite;
                    Debug.Log($"[CharacterItem] Character {CharacterId}: SELECTED state");
                }
                else
                {
                    selectButtonCharacter.image.sprite = enabledButtonSprite;
                    Debug.Log($"[CharacterItem] Character {CharacterId}: OWNED state");
                }
            }
            else
            {
                // Locked characters: Show enabled button but with lock mask
                selectButtonCharacter.image.sprite = enabledButtonSprite;
                Debug.Log($"[CharacterItem] Character {CharacterId}: LOCKED state (clickable for viewing)");
            }
        }


        private void UpdateLockMask(bool showLock)
        {
            if (lockMask != null)
            {
                lockMask.SetActive(showLock);
                Debug.Log($"[CharacterItem] Character {CharacterId}: Setting lockMask to {showLock}");
            }
        }

        private void SelectButtonClick()
        {
            bool isOwned = charactersSave != null && charactersSave.HasCharacterBeenBought(CharacterId);

            if (isOwned)
            {
                // Owned characters: Can be selected normally
                charactersSave.SetSelectedCharacterId(CharacterId);
                GameController.SaveManager.Save();

                if (GameController.AudioManager != null)
                    GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

                Debug.Log($"[CharacterItem] Selected owned character {CharacterId}: {Data.Name}");
            }
            else
            {
                // Locked characters: Just view stats, don't select
                if (GameController.AudioManager != null)
                    GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

                Debug.Log($"[CharacterItem] Viewing locked character {CharacterId}: {Data.Name} (not selectable)");
            }

            // Always notify parent window about navigation (for stats display and sprite change)
            onNavigationSelected?.Invoke(this);
        }

        private void OnGoldAmountChanged(int amount)
        {
            RedrawButton();
        }

        /// <summary>
        /// Called when any character is upgraded to update star display
        /// </summary>
        private void OnCharacterUpgraded(int upgradedCharacterId)
        {
            // Only update if this is the character that was upgraded
            if (upgradedCharacterId == CharacterId)
            {
                UpdateStarDisplay();
            }
        }

        /// <summary>
        /// Force refresh all visuals - call this to fix any display issues
        /// </summary>
        public void ForceRefresh()
        {
            Debug.Log($"[CharacterItem] ForceRefresh called for Character {CharacterId}");
            RedrawVisuals();
        }

        public void Select()
        {
            EventSystem.current.SetSelectedGameObject(selectButtonCharacter.gameObject);
        }

        public void Unselect()
        {
            IsSelected = false;
        }

        private void Update()
        {
            if(!IsSelected && EventSystem.current.currentSelectedGameObject == selectButtonCharacter.gameObject)
            {
                IsSelected = true;
                onNavigationSelected?.Invoke(this);
            }
            else if(IsSelected && EventSystem.current.currentSelectedGameObject != selectButtonCharacter.gameObject)
            {
                IsSelected = false;
            }
        }

        public void Clear()
        {
            if (GoldCurrency != null)
            {
                GoldCurrency.onGoldAmountChanged -= OnGoldAmountChanged;
            }

            if (charactersSave != null)
            {
                charactersSave.onSelectedCharacterChanged -= RedrawVisuals;
                charactersSave.onCharacterUpgraded -= OnCharacterUpgraded;
            }
        }
    }
}