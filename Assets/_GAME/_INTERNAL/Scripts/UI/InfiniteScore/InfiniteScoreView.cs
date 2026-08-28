using Core.Data.Cyber21;
using Core.Services.Audio;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UI.Other;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InfiniteScore
{
    public class InfiniteScoreView : MonoBehaviour
    {
        [Header("Teams Setup")]
        [SerializeField] private RectTransform _orangeTeamZone;
        [SerializeField] private RectTransform _redTeamZone;
        [SerializeField] private Image _orangeTeamGlow;
        [SerializeField] private Image _redTeamGlow;

        [Space(5), Header("Card Prefab")]
        [SerializeField] private RectTransform _cardPrefab;
        [SerializeField] private float _dealStagger = 0.25f;
        [SerializeField] private float _cardOffsetY = 150f;

        [Space(5), Header("SFX Setup")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioClip _delayCardsClip;

        [Space(5), Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _orangeScoreLabel;
        [SerializeField] private TextMeshProUGUI _redScoreLabel;
        [SerializeField] private ActionButton _orangeBetButton;
        [SerializeField] private ActionButton _redBetButton;

        [Space(5), Header("Panels")]
        [SerializeField] private ResultPanelView _resultPanelView;
        [SerializeField] private WarningMessageView _warningMessageView;

        [Space(5), Header("Other")]
        [SerializeField] private float _showResultsDelay = 1.5f;

        public event Action OnOrangeBetClicked;
        public event Action OnRedBetClicked;
        public event Action OnRestartGameClicked;
        public event Action OnResultsPanelOpened;

        private void Awake()
        {
            if (_orangeBetButton != null)
                _orangeBetButton.OnButtonClick += HandleBlueBetButtonClick;

            if (_redBetButton != null)
                _redBetButton.OnButtonClick += HandleRedBetButtonClick;

            if (_resultPanelView != null)
            {
                _resultPanelView.OnRestartGameButtonClick += HandleRestartGameButtonClick;
                _resultPanelView.OnPanelOpened += HandleOpenedResultsPanel;
                _resultPanelView.SetAudioSource(_sfxSource);
            }

            if (_sfxSource != null)
                _sfxSource.volume = AudioService.Instance.GetSfxVolume();
        }

        private void OnDestroy()
        {
            if(_redBetButton != null)
                _redBetButton.OnButtonClick -= HandleRedBetButtonClick;

            if(_orangeBetButton != null)
                _orangeBetButton.OnButtonClick -= HandleBlueBetButtonClick;

            if(_resultPanelView != null)
            {
                _resultPanelView.OnRestartGameButtonClick -= HandleRestartGameButtonClick;
                _resultPanelView.OnPanelOpened -= HandleOpenedResultsPanel;
            }

            _orangeTeamGlow.transform.DOKill();
            _redTeamGlow.transform.DOKill();
        }

        public void Init()
        {
            ClearZone(_orangeTeamZone);
            ClearZone(_redTeamZone);

            UpdateScoreText(_orangeScoreLabel, 0);
            UpdateScoreText(_redScoreLabel, 0);
        }

        public async UniTask DealCardsAsync(List<CardData> orangeCards, List<CardData> redCards)
        {
            _orangeTeamGlow.transform.DORewind();
            _redTeamGlow.transform.DORewind();

            ClearZone(_orangeTeamZone);
            ClearZone(_redTeamZone);

            int maxCards = Mathf.Max(orangeCards.Count, redCards.Count);
            int currentScoreOrange = 0;
            int currentScoreRed = 0;

            for (int i = 0; i < maxCards; i++)
            {
                if (i < orangeCards.Count)
                {
                    SpawnCard(_orangeTeamZone, orangeCards[i], i);
                    currentScoreOrange += orangeCards[i].CardValue;
                    UpdateScoreText(_orangeScoreLabel, currentScoreOrange);

                    if (_sfxSource != null && _delayCardsClip != null)
                        _sfxSource.PlayOneShot(_delayCardsClip);
                }

                if (i < redCards.Count)
                {
                    SpawnCard(_redTeamZone, redCards[i], i);
                    currentScoreRed += redCards[i].CardValue;
                    UpdateScoreText(_redScoreLabel, currentScoreRed);

                    if (_sfxSource != null && _delayCardsClip != null)
                        _sfxSource.PlayOneShot(_delayCardsClip);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(_dealStagger), ignoreTimeScale: false);
            }
        }

        private void SpawnCard(RectTransform parent, CardData cardData, int cardIndex)
        {
            RectTransform card = Instantiate(_cardPrefab, parent);
            card.SetAsLastSibling();

            float targetY = cardIndex * _cardOffsetY;
            Vector2 targetPosition = new(0f, -targetY);

            // Начальная анимация вылета
            card.anchoredPosition = new Vector2(0, targetY - card.sizeDelta.y);
            card.localScale = Vector3.zero;

            card.DOAnchorPos(targetPosition, 0.3f).SetEase(Ease.OutBack);
            card.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

            // Установка спрайта и значения
            var cardImage = card.GetComponentInChildren<Image>();
            var valueText = card.GetComponentInChildren<TextMeshProUGUI>();
            int randomCardIndex = UnityEngine.Random.Range(0, cardData.CardSprites.Count);

            if (cardImage != null && cardData.CardSprites != null)
                cardImage.sprite = cardData.CardSprites[randomCardIndex];

            if (valueText != null)
                valueText.text = cardData.CardValue.ToString();
        }

        public void ShowWinner(int winningTeam) // 0 - Orange, 1 - Red, -1 - Tie
        {
            if (winningTeam == 0 && _orangeTeamGlow != null)
                PulseGlow(_orangeTeamGlow.gameObject);
            else if (winningTeam == 1 && _redTeamGlow != null)
                PulseGlow(_redTeamGlow.gameObject);
        }

        private void PulseGlow(GameObject glow)
        {
            glow.transform.DOKill();
            glow.transform.localScale = new(0.95f, 0.95f);
            glow.SetActive(true);
            glow.transform
                .DOScale(Vector3.one, 1f)
                .SetLoops(4, LoopType.Yoyo)
                .OnComplete(() =>
                {
                    glow.SetActive(false);
                    glow.transform.localScale = new(0.95f, 0.95f);
                });
        }

        private void ClearZone(RectTransform zone)
        {
            for (int i = 0; i < zone.childCount; i++)
                Destroy(zone.GetChild(i).gameObject);
        }

        private void UpdateScoreText(TextMeshProUGUI label, int score)
        {
            if (label != null)
                label.text = score.ToString();
        }

        public void SetButtonsInteractable(bool interactable)
        {
            if (_orangeBetButton != null)
                _orangeBetButton.Interactable = interactable;

            if (_redBetButton != null)
                _redBetButton.Interactable = interactable;
        }

        public async UniTask ShowResultPanel(bool isWin, float reward, int score, bool isDraw = false)
        {
            SetButtonsInteractable(false);
            await UniTask.Delay(TimeSpan.FromSeconds(_showResultsDelay), ignoreTimeScale: false);
            _resultPanelView.ShowResultPanel(isWin, false, Mathf.RoundToInt(reward), score, isDraw);
        }

        public void ShowWarningMessage(string title, string message)
        {
            _warningMessageView.SetWarningMessage(title, message);
            _warningMessageView.Show();
        }

        private void HandleRedBetButtonClick()
        {
            OnRedBetClicked?.Invoke();
        }

        private void HandleBlueBetButtonClick()
        {
            OnOrangeBetClicked?.Invoke();
        }

        private void HandleRestartGameButtonClick()
        {
            OnRestartGameClicked?.Invoke();
        }

        private void HandleOpenedResultsPanel()
        {
            OnResultsPanelOpened?.Invoke();
        }
    }
}