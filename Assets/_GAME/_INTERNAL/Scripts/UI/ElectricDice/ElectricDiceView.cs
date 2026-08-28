using Core.Services.Audio;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UI.Other;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UI.ElectricDice
{
    public class ElectricDiceView : MonoBehaviour
    {
        [Header("Dice")]
        [SerializeField] private RectTransform _die1;
        [SerializeField] private RectTransform _die2;
        [SerializeField] private Image _die1Face;
        [SerializeField] private Image _die2Face;
        [SerializeField] private List<Sprite> _diceFaces; // 6 спрайтов (1-6)

        [Space(5), Header("SFX Setup")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioClip _rollClip;

        [Space(5), Header("Target Number Controls")]
        [SerializeField] private ActionButton _decreaseTargetButton;
        [SerializeField] private ActionButton _increaseTargetButton;
        [SerializeField] private TextMeshProUGUI _targetNumberLabel;

        [Space(5), Header("Condition Slider")]
        [Tooltip("Слайдер с minValue=0, maxValue=2, wholeNumbers=true (0 - Меньше, 1 - Равно, 2 - Больше)")]
        [SerializeField] private Slider _conditionSlider;
        [SerializeField] private TextMeshProUGUI _conditionLabel;

        [Space(5), Header("Action Buttons")]
        [SerializeField] private ActionButton _spinButton;

        [Space(5), Header("Panels")]
        [SerializeField] private ResultPanelView _resultPanelView;
        [SerializeField] private WarningMessageView _warningMessageView;

        [Space(5), Header("Animation Settings")]
        [SerializeField] private float _rollDuration = 1.2f;
        [SerializeField] private float _tumbleHeight = 240f;

        // События для контроллера
        public event Action OnDecreaseTargetClicked;
        public event Action OnIncreaseTargetClicked;
        public event Action<int> OnConditionChanged; // 0, 1, 2
        public event Action OnSpinButtonClicked;
        public event Action OnRestartButtonClicked;

        public int ConditionValue => Mathf.RoundToInt(_conditionSlider.value);

        private void Awake()
        {
            if (_conditionSlider != null)
            {
                _conditionSlider.minValue = 0;
                _conditionSlider.maxValue = 2;
                _conditionSlider.wholeNumbers = true;
                _conditionSlider.value = 1; // По умолчанию "Равно"
                _conditionSlider.onValueChanged.AddListener(val => OnConditionChanged?.Invoke(Mathf.RoundToInt(val)));
            }

            if (_decreaseTargetButton != null)
                _decreaseTargetButton.OnButtonClick += HandleDecreaseTargetButtonClick;

            if (_increaseTargetButton != null)
                _increaseTargetButton.OnButtonClick += HandleIncreaseTargetButtonClick;

            if (_spinButton != null)
                _spinButton.OnButtonClick += HandleSpinButtonClick;

            if(_resultPanelView != null)
            {
                _resultPanelView.OnRestartGameButtonClick += HandleRestartButtonClick;
                _resultPanelView.SetAudioSource(_sfxSource);
            }

            if (_sfxSource != null)
                _sfxSource.volume = AudioService.Instance.GetSfxVolume();
        }

        private void OnDestroy()
        {
            if (_conditionSlider != null)
                _conditionSlider.onValueChanged.RemoveAllListeners();

            if (_decreaseTargetButton != null)
                _decreaseTargetButton.OnButtonClick -= HandleDecreaseTargetButtonClick;
            if (_increaseTargetButton != null)
                _increaseTargetButton.OnButtonClick -= HandleIncreaseTargetButtonClick;
            if (_spinButton != null)
                _spinButton.OnButtonClick -= HandleSpinButtonClick;
            if (_resultPanelView != null)
                _resultPanelView.OnRestartGameButtonClick -= HandleRestartButtonClick;
        }

        public void Init(int initialTarget)
        {
            SetDieFace(_die1Face, 1);
            SetDieFace(_die2Face, 1);
            UpdateTargetNumber(initialTarget);
            UpdateConditionLabel(0);
        }

        public async UniTask RollDiceAsync(int value1, int value2)
        {
            var t1 = AnimateDie(_die1, _die1Face, value1);
            var t2 = AnimateDie(_die2, _die2Face, value2);

            if (_sfxSource != null && _rollClip != null)
                _sfxSource.PlayOneShot(_rollClip);

            await UniTask.WhenAll(t1, t2);
        }

        private async UniTask AnimateDie(RectTransform die, Image face, int finalValue)
        {
            if (_diceFaces == null || _diceFaces.Count < 6) 
                return;

            Vector2 finalPos = die.anchoredPosition;

            // Небольшое случайное смещение по X для динамики
            Vector2 startPos = finalPos + new Vector2(Random.Range(-20f, 20f), _tumbleHeight);

            // Сброс перед броском
            die.anchoredPosition = startPos;
            die.localScale = Vector3.one;
            die.localRotation = Quaternion.identity;

            float targetRotZ = 720f * (Random.value > 0.5f ? 1f : -1f) + Random.Range(-15f, 15f);

            // 1. Кинематографичное падение: Резкое падение -> Небольшой отскок -> Плавная остановка
            Sequence fallSeq = DOTween.Sequence();
            fallSeq.Append(die.DOAnchorPos(finalPos, _rollDuration * 0.6f).SetEase(Ease.InQuart)); // Стремительное ускорение вниз
            fallSeq.Append(die.DOAnchorPos(finalPos + new Vector2(0, 45f), _rollDuration * 0.15f).SetEase(Ease.OutQuad)); // Острый отскок вверх
            fallSeq.Append(die.DOAnchorPos(finalPos, _rollDuration * 0.25f).SetEase(Ease.InQuad)); // Плавное падение обратно

            // 2. Вращение по Z с замедлением
            die.DOLocalRotate(new Vector3(0, 0, targetRotZ), _rollDuration, RotateMode.FastBeyond360).SetEase(Ease.OutCubic);

            // 3. Эффект "Цифрового табло" (Slot machine)
            // Меняем спрайты немного реже (каждые 60мс), чтобы глаз успевал фиксировать цифры.
            float elapsed = 0f;
            float interval = 0.06f;
            while (elapsed < _rollDuration * 0.8f)
            {
                face.sprite = _diceFaces[Random.Range(0, 6)];
                await UniTask.Delay(TimeSpan.FromSeconds(interval), ignoreTimeScale: false);
                elapsed += interval;
            }

            // Ждем конца анимации падения
            await fallSeq.AsyncWaitForCompletion();

            // Устанавливаем финальную грань строго в момент удара
            SetDieFace(face, finalValue);

            // 4. Техно-импульс при приземлении (Без искажений, просто резкий рост и плавное затухание)
            Sequence impactSeq = DOTween.Sequence();
            impactSeq.Append(die.DOScale(1.2f, 0.08f).SetEase(Ease.OutQuad)); // Резкий "пульс"
            impactSeq.Append(die.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutQuart)); // Плавное возвращение

            await impactSeq.AsyncWaitForCompletion();
        }

        public void SetDieFace(Image face, int value)
        {
            if (face != null && _diceFaces != null && _diceFaces.Count >= 6)
                face.sprite = _diceFaces[value - 1];
        }

        public void UpdateTargetNumber(int value)
        {
            if (_targetNumberLabel != null) 
                _targetNumberLabel.text = value.ToString();

            if (_decreaseTargetButton != null) 
                _decreaseTargetButton.Interactable = value > 2;
            if (_increaseTargetButton != null) 
                _increaseTargetButton.Interactable = value < 12;
        }

        public void UpdateConditionLabel(int condition)
        {
            if (_conditionLabel == null) 
                return;
            _conditionLabel.text = condition switch
            {
                0 => "<", // Меньше
                1 => "=", // Равно
                2 => ">", // Больше
                _ => "?"
            };
        }

        public void UpdateSpinButton(bool canSpin)
        {
            if (_spinButton != null) 
                _spinButton.Interactable = canSpin;
        }

        public void SetControlsInteractable(bool interactable)
        {
            if (_spinButton != null) 
                _spinButton.Interactable = interactable;
            if (_decreaseTargetButton != null) 
                _decreaseTargetButton.Interactable = interactable;
            if (_increaseTargetButton != null) 
                _increaseTargetButton.Interactable = interactable;
            if (_conditionSlider != null) 
                _conditionSlider.interactable = interactable;
        }

        public void ShowWarningMessage(string title, string message)
        {
            if(_warningMessageView != null)
            {
                _warningMessageView.SetWarningMessage(title, message);
                _warningMessageView.Show();
            }
        }

        public void ShowResultPanel(bool isWin, int score, int reward)
        {
            if (_resultPanelView == null) 
                return;

            _resultPanelView.ShowResultPanel(isWin, false, reward, score);
            _spinButton.Interactable = false;
        }

        private void HandleSpinButtonClick()
        {
            OnSpinButtonClicked?.Invoke();
        }

        private void HandleIncreaseTargetButtonClick()
        {
            OnIncreaseTargetClicked?.Invoke();
        }

        private void HandleDecreaseTargetButtonClick()
        {
            OnDecreaseTargetClicked?.Invoke();
        }

        private void HandleRestartButtonClick()
        {
            _spinButton.Interactable = true;
            OnRestartButtonClicked?.Invoke();
        }
    }
}