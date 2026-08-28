using Core.Common;
using Core.Data;
using Core.Gameplay.GameControllers.CryptoVibe;
using Core.Services;
using Core.Services.Analytics;
using Cysharp.Threading.Tasks;
using UI.CryptoVibe;
using UnityEngine;

namespace Core.Gameplay.GameControllers
{
    public class CryptoVibeController : GameController
    {
        [Header("Setup")]
        [SerializeField] private CryptoVibeView _view;

        [Header("Settings")]
        [SerializeField] private float _minBet = 250f;
        [SerializeField] private float _maxMultiplier = 35f;
        [SerializeField] private float _growthRate = 0.5f;

        private GameState _state;

        public enum GameState
        {
            Idle,
            Flying,
            Crashed,
            CashedOut
        }

        private CrashResultGenerator _resultGenerator;

        private float _currentBet;
        private float _currentMultiplier;
        private float _crashMultiplier;

        private bool _hasCrashed;

        public override void Initialize()
        {
            _resultGenerator = new CrashResultGenerator(_maxMultiplier);

            _view.OnStartClicked += HandleStartClick;
            _view.OnEjectClicked += HandleEjectClick;
            _view.OnBetChanged += HandleBetChanged;
            _view.OnRestartButtonClicked += HandleRestartButtonClick;

            HandleRestartButtonClick();
        }

        public override void Enter()
        {
            AnalyticsService.Instance.ReportGameStart(
                GameConstants.GAME_CRYPTO_VIBE
            );

            _currentBet = _minBet;
            _state = GameState.Idle;

            _view.UpdateBetText(_currentBet);
        }

        public override void Exit()
        {
            _view.OnStartClicked -= HandleStartClick;
            _view.OnEjectClicked -= HandleEjectClick;
            _view.OnBetChanged -= HandleBetChanged;
            _view.OnRestartButtonClicked -= HandleRestartButtonClick;
        }

        private async UniTask StartGame()
        {
            _state = GameState.Flying;

            _currentMultiplier = 1f;

            _crashMultiplier = _resultGenerator.Generate();

            _view.SetInteractable(true);

            Debug.Log($"[CryptoVibe] Crash: {_crashMultiplier:F2}x");

            _view.PlayFlyAnimation(_crashMultiplier, _growthRate);

            while (_state == GameState.Flying && !_hasCrashed)
            {
                _currentMultiplier += _growthRate * Time.deltaTime;

                if (_currentMultiplier >= _crashMultiplier)
                {
                    _currentMultiplier = _crashMultiplier;

                    _view.UpdateMultiplierText(_currentMultiplier);

                    TriggerCrash();
                    break;
                }

                _view.UpdateMultiplierText(_currentMultiplier);

                await UniTask.Yield();
            }
        }

        private void TriggerCrash()
        {
            if (_state != GameState.Flying)
                return;

            _state = GameState.Crashed;
            _hasCrashed = true;

            _view.Crash(OnCrashAnimationComplete);
        }

        private void OnCrashAnimationComplete()
        {
            _view.SetInteractable(false);

            EndGame(
                isWin: false,
                reward: 0,
                questTag: null
            );
        }

        // ------------------------------------------------------------------
        // EJECT
        // ------------------------------------------------------------------

        private void EjectRocket()
        {
            if (_state != GameState.Flying || _hasCrashed)
                return;

            _state = GameState.CashedOut;

            float reward = _currentBet * _currentMultiplier;

            string questTag = _currentMultiplier >= 10f
                ? GameConstants.TAG_REACH_10X_MULTIPLIER
                : string.Empty;

            EndGame(
                isWin: true,
                reward: Mathf.RoundToInt(reward),
                questTag: questTag
            );

            _view.ResetView();
        }

        // ------------------------------------------------------------------
        // END
        // ------------------------------------------------------------------

        private void EndGame(
            bool isWin,
            int reward,
            string questTag)
        {
            _state = isWin ? GameState.CashedOut : GameState.Crashed;
            bool isAlreadyPlayed = GameServices.PlayedAcradesService.IsArcadePlayed(GameConstants.GAME_CRYPTO_VIBE);

            GameResult result = new(
                isWin: isWin,
                rewardCoins: reward,
                rewardXP: isWin ? 30f : 10f,
                questTag: questTag,
                gameId: GameConstants.GAME_CRYPTO_VIBE,
                arcadePlayed: isAlreadyPlayed
            );

            GameServices.GameCompletionHandler.HandleGameResult(result);
            RecordArcadePlay(GameConstants.GAME_CRYPTO_VIBE);

            _view.ShowResult(isWin, reward);
        }

        // ------------------------------------------------------------------
        // INPUT
        // ------------------------------------------------------------------

        private void HandleStartClick()
        {
            if (_state != GameState.Idle)
                return;

            if(!base.SpendEnergy())
            {
                _view.ShowWarningMessage("Not enough energy!", $"You don't have enough energy ({GameConstants.ENERGY_FOR_GAME}) for this game.");
                return;
            }

            if (!GameServices.EconomyService.HasEnoughBalance(_currentBet))
            {
                _view.ShowWarningMessage("Not enough coins!", $"You don't have enough coins ({_currentBet}) for this bet.");
                return;
            }

            GameServices.EconomyService.SpendCoins(_currentBet);
            GameServices.Quests.ProgressQuest(GameConstants.TAG_LAUNCH_3_ROCKETS);

            _hasCrashed = false;
            StartGame().Forget();
        }

        private void HandleEjectClick()
        {
            EjectRocket();
        }

        private void HandleBetChanged(float newBet)
        {
            _currentBet = Mathf.Clamp(newBet, _minBet, float.MaxValue);

            _view.UpdateBetText(_currentBet);
        }

        private void HandleRestartButtonClick()
        {
            _state = GameState.Idle;
            _hasCrashed = false;

            _view.ResetView();
        }
    }
}