using Core.Data.Cyber21;
using Core.Services.Audio;
using DG.Tweening;
using System;
using TMPro;
using UI.Other;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CyberMaster
{
    public class CyberMasterView : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField] private RectTransform _rightSlot;
        [SerializeField] private RectTransform _leftSlot;
        [SerializeField] private RectTransform _deckTransform;

        [Space(5), Header("Buttons Setup")]
        [SerializeField] private ActionButton _hitButton;
        [SerializeField] private ActionButton _standButton;
        [SerializeField] private ActionButton _startButton;

        [Space(5), Header("Card Setup")]
        [SerializeField] private GameObject _cardPrefab;
        [SerializeField] private Sprite _cardBackSprite;

        [Space(5), Header("SFX Settings")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioClip _addCardToHandClip;

        [Space(5), Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _betText;

        [Space(5), Header("Animations Setup")]
        [SerializeField] private float _flyDuration = 0.35f;
        [SerializeField] private float _flipDuration = 0.12f;
        [SerializeField] private float _flyStartScale = 0.6f;
        [Tooltip("Если true — новая карта заменяет предыдущую в слоте")]
        [SerializeField] private bool _replacePreviousCard = false;

        [Space(5), Header("Result Panel")]
        [SerializeField] private ResultPanelView _resultPanelView;
        [SerializeField] private WarningMessageView _warningPanelView;

        private bool _nextCardToLeft = true;

        public event Action OnHitButtonClicked;
        public event Action OnStandButtonClicked;
        public event Action OnRestartButtonClicked;
        public event Action OnStartButtonClicked;

        private void InitButtons()
        {
            if (_hitButton != null)
                _hitButton.OnButtonClick += HandleHitButtomClick;

            if (_standButton != null)
                _standButton.OnButtonClick += HandleStandButtonClick;

            if(_startButton != null)
                _startButton.OnButtonClick += HandleStartButtonClick;

            if (_resultPanelView != null)
            {
                _resultPanelView.OnRestartGameButtonClick += HandleRestartButtonClick;
                _resultPanelView.SetAudioSource(_sfxSource);
            }
        }

        public void Init(int deckSize)
        {
            if (_sfxSource != null)
                _sfxSource.volume = AudioService.Instance.GetSfxVolume();

            InitButtons();
            Debug.Log($"[CyberMasterView] Initialized with deck size: {deckSize}");
        }

        public void Dispose()
        {
            if(_hitButton != null)
                _hitButton.OnButtonClick -= HandleHitButtomClick;

            if(_standButton != null)
                _standButton.OnButtonClick -= HandleStandButtonClick;

            if (_startButton != null)
                _startButton.OnButtonClick -= HandleStartButtonClick;

            if (_resultPanelView != null)
                _resultPanelView.OnRestartGameButtonClick -= HandleRestartButtonClick;

            KillCardTweens(_leftSlot);
            KillCardTweens(_rightSlot);
        }

        public void ClearHand()
        {
            _nextCardToLeft = true;
            ClearSlot(_leftSlot);
            ClearSlot(_rightSlot);
        }

        public void AddCardToHand(CardData cardData, float delay = 0f)
        {
            if (_deckTransform == null || _cardPrefab == null)
            {
                Debug.LogError("[CyberMasterView] Deck transform or card prefab is not assigned!");
                return;
            }

            // Выбираем слот по кругу: лево -> право -> лево...
            Transform targetSlot = _nextCardToLeft ? _leftSlot : _rightSlot;
            _nextCardToLeft = !_nextCardToLeft;

            if (targetSlot == null)
            {
                Debug.LogError("[CyberMasterView] Left/Right slot is not assigned!");
                return;
            }

            if (_sfxSource != null && _addCardToHandClip != null)
                _sfxSource.PlayOneShot(_addCardToHandClip);

            // Режим замены: старая карта схлопывается, новая занимает её место
            if (_replacePreviousCard)
            {
                foreach (Transform child in targetSlot)
                {
                    child.DOKill();
                    child.DOScale(0f, 0.15f).OnComplete(() => Destroy(child.gameObject));
                }
            }

            int indexInSlot = targetSlot.childCount;

            GameObject cardObj = Instantiate(_cardPrefab, targetSlot);
            RectTransform rect = cardObj.GetComponent<RectTransform>();

            // Стартуем рубашкой вверх из колоды
            SetCardFace(cardObj, false, cardData);
            rect.position = _deckTransform.position;
            rect.localScale = Vector3.one * (delay > 0f ? 0f : _flyStartScale);

            // Целевая точка внутри слота
            Vector3 targetLocal = Vector3.zero;

            Sequence seq = DOTween.Sequence();

            if (delay > 0f)
            {
                seq.AppendInterval(delay);
                seq.AppendCallback(() => rect.localScale = Vector3.one * _flyStartScale);
            }

            // 1) Перелёт из колоды в слот + рост масштаба + лёгкий наклон
            seq.Append(rect.DOMove(targetSlot.TransformPoint(targetLocal), _flyDuration).SetEase(Ease.OutCubic));
            seq.Join(rect.DOScale(1f, _flyDuration).SetEase(Ease.OutCubic));
            seq.Join(rect.DORotate(new Vector3(0f, 0f, UnityEngine.Random.Range(-15f, 15f)), _flyDuration).SetEase(Ease.OutCubic));

            // 2) Флип: сжатие по X -> подмена рубашки на лицо -> раскрытие
            seq.Append(rect.DOScaleX(0f, _flipDuration).SetEase(Ease.InQuad));
            seq.AppendCallback(() => SetCardFace(cardObj, true, cardData));
            seq.Append(rect.DOScaleX(1f, _flipDuration).SetEase(Ease.OutBack));
            seq.Join(rect.DORotate(Vector3.zero, _flipDuration * 2f).SetEase(Ease.OutQuad));

            // Финальная доводка, чтобы карта встала ровно в слот
            seq.OnComplete(() => rect.localPosition = targetLocal);
        }

        public void ShowWarningMessage(string title, string message)
        {
            if (_warningPanelView != null)
            {
                _warningPanelView.SetWarningMessage(title, message);
                _warningPanelView.Show();
            }
        }

        public void UpdateScore(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"{score}";

                // Подсветка, если близко к перебору
                if (score > 18)
                    _scoreText.color = Color.yellow;
                else if (score > 15)
                    _scoreText.color = Color.white;
                else
                    _scoreText.color = Color.green;
            }
        }

        public void UpdateBet(int bet)
        {
            if (_betText != null)
                _betText.text = $"Bet: {bet}";
        }

        public void SetStartButtonState(bool value) => _startButton.Interactable = value;

        public void SetButtonsState(bool isGameActive)
        {
            if (_startButton != null)
                _startButton.gameObject.SetActive(!isGameActive);

            if (_hitButton != null)
                _hitButton.gameObject.SetActive(isGameActive);

            if (_standButton != null)
                _standButton.gameObject.SetActive(isGameActive);
        }

        public void ShowResult(bool isWin, float reward, int finalScore, bool isBlackjack)
        {
            if (_resultPanelView == null)
                return;

            _resultPanelView.ShowResultPanel(isWin, isBlackjack, Mathf.RoundToInt(reward), finalScore);
        }

        private void SetCardFace(GameObject cardObj, bool showFace, CardData data)
        {
            if (cardObj.TryGetComponent<Image>(out var image))
            {
                int randomIndex = UnityEngine.Random.Range(0, data.CardSprites.Count);
                image.sprite = showFace ? data.CardSprites[randomIndex] : _cardBackSprite;
            }

            TextMeshProUGUI label = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.gameObject.SetActive(true);
                if (showFace)
                    label.text = data.IsAce ? "A" : data.CardValue.ToString();
            }
        }

        private void ClearSlot(RectTransform slot)
        {
            if (slot == null) 
                return;

            foreach (Transform child in slot)
            {
                child.DOKill();
                child.DOScale(0f, 0.25f).SetEase(Ease.InOutQuad).OnComplete(() =>
                {
                    child.DOKill();
                    Destroy(child.gameObject);
                });
            }
        }

        private void KillCardTweens(Transform slot)
        {
            if (slot == null) return;

            foreach (Transform child in slot)
                child.DOKill();
        }

        private void HandleRestartButtonClick() => OnRestartButtonClicked?.Invoke();
        private void HandleStandButtonClick() => OnStandButtonClicked?.Invoke();
        private void HandleHitButtomClick() => OnHitButtonClicked?.Invoke();
        private void HandleStartButtonClick() => OnStartButtonClicked?.Invoke();
    }
}