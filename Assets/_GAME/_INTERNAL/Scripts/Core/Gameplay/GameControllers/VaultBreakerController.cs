using Core.Common;
using Core.Data;
using Core.Services;
using Core.Services.Analytics;
using Core.Services.Audio;
using Cysharp.Threading.Tasks;
using System.Collections;
using TMPro;
using UI.Other;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Gameplay.GameControllers
{
    public class VaultBreakerController : GameController
    {
        [Header("Vault UI")]
        [SerializeField] private TextMeshProUGUI _scoreLabel;
        [SerializeField] private TextMeshProUGUI _timerLabel;
        [SerializeField] private ActionButton _claimButton;
        [SerializeField] private ActionButton _startButton;
        [SerializeField] private GameObject _alarmPanel;
        [SerializeField] private Image _alarmFlashImage;

        [Space(5), Header("SFX Setup")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioClip _openVaultClip;
        [SerializeField] private AudioClip _alarmClip;

        [Space(5), Header("Settings")]
        [SerializeField] private float _gameDuration = 10f;
        [SerializeField] private float _minClaimTime = 3f;
        [SerializeField] private float _maxClaimTime = 8f;
        [SerializeField] private int _baseReward = 500;
        [SerializeField] private float _pointsPerSecond = 100f;
        [SerializeField] private float _bet = 250f;

        [Space(5), Header("Other Panels")]
        [SerializeField] private WarningMessageView _warningMessageView;

        private float _currentScore;
        private float _elapsedTime;
        private float _claimThresholdTime;
        private bool _isPlaying;
        private bool _canClaim;
        private Coroutine _alarmCoroutine;

        public override void Enter()
        {
            AnalyticsService.Instance.ReportGameStart(GameConstants.GAME_VAULT);

            if (_alarmPanel != null && _alarmPanel.activeSelf)
                _alarmPanel.SetActive(false);

            if (_sfxSource != null)
                _sfxSource.volume = AudioService.Instance.GetSfxVolume();

            ResetGameState();
            UpdateUI();
        }

        public override void Initialize()
        {
            if (_startButton != null)
                _startButton.OnButtonClick += HandleStartButtonClick;

            if (_claimButton != null)
                _claimButton.OnButtonClick += HandleClaimButtonClick;

            UpdateUI();
        }

        public override void Exit()
        {
            if (_startButton != null)
                _startButton.OnButtonClick -= HandleStartButtonClick;

            if (_claimButton != null)
                _claimButton.OnButtonClick -= HandleClaimButtonClick;

            if (_alarmCoroutine != null)
                StopCoroutine(_alarmCoroutine);
        }

        private void ResetGameState()
        {
            _currentScore = 0f;
            _elapsedTime = 0f;
            _isPlaying = false;
            _canClaim = false;
            _claimThresholdTime = Random.Range(_minClaimTime, _maxClaimTime);

            _claimButton.gameObject.SetActive(true);

            if (_alarmPanel != null)
                _alarmPanel.SetActive(false);
        }

        private async UniTask StartGame()
        {
            _isPlaying = true;
            _elapsedTime = 0f;
            _canClaim = false;
            SetInteractable(false);

            // Запускаем сигналку в случайный момент
            float alarmTime = Random.Range(_minClaimTime, _maxClaimTime);
            _claimThresholdTime = alarmTime;

            Debug.Log($"[Vault] Game started. Alarm will trigger at {alarmTime:N2}s");

            // Таймер игры
            while (_elapsedTime < _gameDuration && _isPlaying)
            {
                _elapsedTime += Time.deltaTime;
                _currentScore = _elapsedTime * _pointsPerSecond;

                // Проверяем, можно ли забирать приз
                if (!_canClaim && _elapsedTime >= _claimThresholdTime)
                {
                    _canClaim = true;

                    Debug.Log("[Vault] Can claim now!");
                }

                // Проверяем, не сработала ли сигналка
                if (_elapsedTime > _claimThresholdTime + 1.5f) // 1.5 секунды на реакцию
                {
                    TriggerAlarm();
                    break;
                }

                UpdateUI();
                await UniTask.Yield();
            }

            if (_isPlaying && !_canClaim)
            {
                // Время вышло, но игрок не успел
                EndGame(false, 0);
            }

            _isPlaying = false;
        }

        private void TriggerAlarm()
        {
            Debug.Log("[Vault] ALARM TRIGGERED!");
            if (_sfxSource != null && _alarmClip != null)
                _sfxSource.PlayOneShot(_alarmClip);

            if (_alarmPanel != null)
                _alarmPanel.SetActive(true);

            if (_alarmFlashImage != null)
                _alarmCoroutine = StartCoroutine(AlarmFlashEffect());

            _canClaim = false;
            SetInteractable(false);

            // Небольшая задержка перед окончанием игры
            Invoke(nameof(EndAfterAlarm), 1.5f);
        }

        private IEnumerator AlarmFlashEffect()
        {
            Color originalColor = _alarmFlashImage.color;
            Color flashColor = new(1f, 0f, 0f, 0.5f);

            for (int i = 0; i < 6; i++)
            {
                _alarmFlashImage.color = flashColor;
                yield return new WaitForSeconds(0.15f);
                _alarmFlashImage.color = originalColor;
                yield return new WaitForSeconds(0.15f);
            }
        }

        private void EndAfterAlarm() => EndGame(false, 0);

        private void EndGame(bool isWin, float reward)
        {
            _isPlaying = false;

            string questTag = isWin ? GameConstants.TAG_OPEN_THE_VAULT : null;
            bool isAlreadyPlayed = GameServices.PlayedAcradesService.IsArcadePlayed(GameConstants.GAME_VAULT);

            GameResult result = new(
                isWin: isWin,
                rewardCoins: reward,
                rewardXP: isWin ? 30f : 10f,
                questTag: questTag,
                gameId: GameConstants.GAME_VAULT,
                arcadePlayed: isAlreadyPlayed
            );

            GameServices.GameCompletionHandler.HandleGameResult(result);
            RecordArcadePlay(GameConstants.GAME_VAULT);

            ShowResult(isWin, reward);
            SetInteractable(true);
        }

        private void ShowResult(bool isWin, float winAmount)
        {
            if (_alarmPanel != null)
                _alarmPanel.SetActive(!isWin && _alarmPanel.activeSelf);

            Debug.Log($"[Vault] Result: Win={isWin}, Amount={winAmount}, Score={_currentScore:N0}");
        }

        private void UpdateUI()
        {
            if (_scoreLabel != null)
                _scoreLabel.text = $"{_currentScore:N0}";

            if (_timerLabel != null)
            {
                float remainingTime = Mathf.Max(0, _gameDuration - _elapsedTime);
                _timerLabel.text = $"{remainingTime:N1}s";
            }

            if (_claimButton != null)
                _claimButton.Interactable = _canClaim && _isPlaying;

            if (_startButton != null)
                _startButton.gameObject.SetActive(!_isPlaying);
        }

        private void SetInteractable(bool interactable)
        {
            if (_startButton != null)
                _startButton.gameObject.SetActive(interactable && !_isPlaying);

            if (_claimButton != null && !_isPlaying)
                _claimButton.Interactable = false;
        }

        private void HandleStartButtonClick()
        {
            if (_isPlaying)
                return;

            if (!base.SpendEnergy())
            {
                _warningMessageView.SetWarningMessage("Not enough energy!", $"You don't have enough energy ({GameConstants.ENERGY_FOR_GAME}) for this game.");
                _warningMessageView.Show();
                return;
            }

            if (!GameServices.EconomyService.HasEnoughBalance(_bet))
            {
                _warningMessageView.SetWarningMessage("Not enough coins!", $"You don't have enough coins ({_bet}) for this bet.");
                _warningMessageView.Show();
                return;
            }

            ResetGameState();
            StartGame().Forget();
            GameServices.EconomyService.SpendCoins(_bet);
            
        }

        private void HandleClaimButtonClick()
        {
            if (!_canClaim || !_isPlaying)
                return;

            if (_sfxSource != null && _openVaultClip != null)
                _sfxSource.PlayOneShot(_openVaultClip);

            // Бонус за быструю реакцию
            float timeBonus = Mathf.Max(0f, (_claimThresholdTime + 1.5f - _elapsedTime) * 50f);
            float totalReward = _baseReward + _currentScore + timeBonus;

            Debug.Log($"[Vault] Claimed! Base={_baseReward}, Score={_currentScore:F0}, TimeBonus={timeBonus:F0}, Total={totalReward:F0}");

            EndGame(true, totalReward);
        }
    }
}