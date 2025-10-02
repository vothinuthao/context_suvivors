using OctoberStudio.Abilities;
using OctoberStudio.Audio;
using OctoberStudio.Easing;
using OctoberStudio.Input;
using OctoberStudio.UI.Chest;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OctoberStudio.UI
{
    public class ChestWindowBehavior : MonoBehaviour
    {
        private static readonly int OPEN_TRIGGER = Animator.StringToHash("Open");
        private static readonly int FORCE_OPEN_TRIGGER = Animator.StringToHash("Force Open");

        private static readonly int CHEST_WINDOW_POPUP_HASH = "Chest Window Popup".GetHashCode();
        private static readonly int CHEST_IN_PROGRESS_HASH = "Chest In Progress".GetHashCode();

        [SerializeField] RectTransform windowRect;
        [SerializeField] Animator chestAnimator;
        [SerializeField] ChestCoinsParticleBehavior coinsParticle;
        [SerializeField] ScalingLabelBehavior coinsLabel;

        [Space]
        [SerializeField] Vector2 openedPosition;
        [SerializeField] Vector2 closedPosition;

        [Space]
        [SerializeField] List<ChestLineBehavior> lines;

        [Space]
        [SerializeField] Button backgroundButton;
        [SerializeField] Button takeButton;
        [SerializeField] CanvasGroup takeButtonCanvasGroup;

        [Header("Gold Reward")]
        [SerializeField] int baseRewardAmount = 100;
        [SerializeField] int tierRewardAmount = 50;
        [SerializeField] int randomRewardAmount = 50;

        [Header("Timings")]
        [SerializeField] float linesAnimationDuration = 5;
        [SerializeField] float abilityAppearDelay = 0.5f;

        [Space]
        [SerializeField] Color[] colorsTier1;
        [SerializeField] Color[] colorsTier2;
        [SerializeField] Color[] colorsTier3;

        [Space]
        [SerializeField] float[] pitchTier1;
        [SerializeField] float[] pitchTier2;
        [SerializeField] float[] pitchTier3;

        private const int REQUIRED_ABILITIES_COUNT = 3;

        private Coroutine coinsCoroutine;
        private IEasingCoroutine takeButtonCoroutine;

        public bool IsAnimationPlaying { get; private set;}
        private int coinsReward;

        private List<Color[]> colorTiers;
        private List<float[]> pitchTiers;

        private float cacheMusicVolume;
        private AudioSource chestSound;

        public event UnityAction OnClosed;

        private void Awake()
        {
            takeButton.onClick.AddListener(TakeButton);
            backgroundButton.onClick.AddListener(SkipAnimationButton);

            colorTiers = new List<Color[]>() { colorsTier1, colorsTier2, colorsTier3 };
            pitchTiers = new List<float[]>() { pitchTier1, pitchTier2, pitchTier3 };
        }

        private void Start()
        {
            takeButton.interactable = false;
        }

        public void OpenWindow(int tierId, List<AbilityData> abilities, List<AbilityData> selectedAbilities)
        {
            // Đảm bảo luôn có đủ 3 abilities
            List<AbilityData> finalSelectedAbilities = EnsureThreeAbilities(abilities, selectedAbilities);

            cacheMusicVolume = GameController.Music.volume;
            GameController.Music.DoVolume(0, 0.3f).SetUnscaledTime(true);

            backgroundButton.gameObject.SetActive(true);
            EventSystem.current.SetSelectedGameObject(backgroundButton.gameObject);

            windowRect.anchoredPosition = closedPosition;

            windowRect.DoAnchorPosition(openedPosition, 0.3f).SetUnscaledTime(true).SetEasing(EasingType.SineOut).SetOnFinish(() => 
            {
                chestAnimator.SetTrigger(OPEN_TRIGGER);
            });

            // Luôn hiển thị 3 lines
            for(int i = 0; i < REQUIRED_ABILITIES_COUNT; i++)
            {
                var ability = finalSelectedAbilities[i];
                var line = lines[i];
                var color = colorTiers[tierId][i];
                var pitch = pitchTiers[tierId][i];
                line.Launch(abilities, ability, linesAnimationDuration + abilityAppearDelay * i, 0.95f, color, pitch);
            }

            // Ẩn các lines còn lại (nếu có)
            for(int i = REQUIRED_ABILITIES_COUNT; i < lines.Count; i++)
            {
                lines[i].gameObject.SetActive(false);
            }

            coinsReward = baseRewardAmount + tierId * tierRewardAmount + Random.Range(0, randomRewardAmount);
            coinsCoroutine = StartCoroutine(CoinsCoroutine(coinsReward, linesAnimationDuration + abilityAppearDelay * (REQUIRED_ABILITIES_COUNT - 2)));

            IsAnimationPlaying = true;

            takeButton.gameObject.SetActive(false);
            takeButton.interactable = false;

            GameController.InputManager.onInputChanged += OnInputChanged;

            GameController.AudioManager.PlaySound(CHEST_WINDOW_POPUP_HASH);
        }

        /// <summary>
        /// Đảm bảo luôn có đủ 3 abilities. Nếu thiếu, sẽ thêm abilities ngẫu nhiên từ list abilities.
        /// </summary>
        private List<AbilityData> EnsureThreeAbilities(List<AbilityData> availableAbilities, List<AbilityData> selectedAbilities)
        {
            List<AbilityData> result = new List<AbilityData>(selectedAbilities);

            // Nếu đã có đủ 3 hoặc nhiều hơn, trả về 3 phần tử đầu tiên
            if (result.Count >= REQUIRED_ABILITIES_COUNT)
            {
                return result.GetRange(0, REQUIRED_ABILITIES_COUNT);
            }

            // Nếu thiếu, thêm các abilities ngẫu nhiên từ list availableAbilities
            List<AbilityData> remainingAbilities = availableAbilities
                .Where(ability => !result.Contains(ability))
                .ToList();

            // Shuffle để chọn ngẫu nhiên
            for (int i = remainingAbilities.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                var temp = remainingAbilities[i];
                remainingAbilities[i] = remainingAbilities[randomIndex];
                remainingAbilities[randomIndex] = temp;
            }

            // Thêm abilities cho đến khi đủ 3
            int neededCount = REQUIRED_ABILITIES_COUNT - result.Count;
            for (int i = 0; i < neededCount && i < remainingAbilities.Count; i++)
            {
                result.Add(remainingAbilities[i]);
            }

            // Nếu vẫn không đủ 3 (trường hợp availableAbilities có ít hơn 3 phần tử)
            // Thêm lại các abilities đã có để đủ 3
            while (result.Count < REQUIRED_ABILITIES_COUNT)
            {
                result.Add(availableAbilities[result.Count % availableAbilities.Count]);
            }

            return result;
        }

        private IEnumerator CoinsCoroutine(int coinsAmount, float duration)
        {
            coinsLabel.SetAmount(0);

            coinsParticle.Show();

            yield return new WaitForSecondsRealtime(0.8f);

            chestSound = GameController.AudioManager.PlaySound(CHEST_IN_PROGRESS_HASH);

            coinsParticle.PlayParticle();

            float time = 0;

            float soundTime = 0;

            while (time < duration)
            {
                yield return null;
                time += Time.unscaledDeltaTime;

                var t = time / duration;

                coinsLabel.SetAmount(Mathf.CeilToInt(coinsAmount * t));

                if(Time.unscaledTime - soundTime > 0.1f)
                {
                    soundTime = Time.unscaledTime;
                }
            }
            coinsLabel.SetAmount(coinsAmount);

            coinsParticle.StopParticle();

            IsAnimationPlaying = false;

            takeButton.gameObject.SetActive(true);
            takeButtonCanvasGroup.alpha = 0;
            takeButtonCoroutine = takeButtonCanvasGroup.DoAlpha(1, 0.3f).SetUnscaledTime(true).SetOnFinish(() => 
            { 
                takeButton.interactable = true;
                EventSystem.current.SetSelectedGameObject(takeButton.gameObject);
            });
        }

        private void SkipAnimationButton()
        {
            if (IsAnimationPlaying)
            {
                IsAnimationPlaying = false;

                StopCoroutine(coinsCoroutine);

                coinsParticle.StopParticle();

                coinsLabel.SetAmount(coinsReward);

                chestAnimator.SetTrigger(FORCE_OPEN_TRIGGER);

                for(int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].gameObject.activeSelf)
                    {
                        lines[i].ForceFinish();
                    }
                }

                takeButtonCoroutine.StopIfExists();

                takeButton.gameObject.SetActive(true);
                takeButtonCanvasGroup.alpha = 1;
                takeButton.interactable = true;

                EventSystem.current.SetSelectedGameObject(takeButton.gameObject);

                if(chestSound != null) chestSound.DoVolume(0f, 0.2f).SetUnscaledTime(true).SetOnFinish(() => chestSound.Stop());
            }
        }

        private void TakeButton()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            GameController.TempGold.Deposit(coinsReward);

            CloseWindow();
        }

        public void CloseWindow()
        {
            GameController.Music.DoVolume(cacheMusicVolume, 0.3f).SetUnscaledTime(true);

            windowRect.DoAnchorPosition(closedPosition, 0.3f).SetUnscaledTime(true).SetEasing(EasingType.SineIn).SetOnFinish(() =>
            {
                Time.timeScale = 1;
                backgroundButton.gameObject.SetActive(false);

                OnClosed?.Invoke();
            });

            GameController.InputManager.onInputChanged -= OnInputChanged;
        }

        private void OnInputChanged(InputType prevInput, InputType inputType)
        {
            if (prevInput == InputType.UIJoystick)
            {
                if (IsAnimationPlaying)
                {
                    EventSystem.current.SetSelectedGameObject(backgroundButton.gameObject);
                } else
                {
                    EventSystem.current.SetSelectedGameObject(takeButton.gameObject);
                }
                
            }
        }
    }
}