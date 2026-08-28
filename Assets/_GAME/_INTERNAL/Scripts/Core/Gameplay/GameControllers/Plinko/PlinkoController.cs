using Core.Common;
using Core.Data;
using Core.Services;
using Core.SO;
using System.Collections.Generic;
using System.Linq;
using UI.Other;
using UI.Plinko;
using UnityEngine;

namespace Core.Gameplay.GameControllers.Plinko
{
    public class PlinkoController : GameController
    {
        private const string KEY_ARCADE_ALREADY_PLAYED = "Plinko_Vibe_Arcade";

        [Header("View Setup")]
        [SerializeField] private PlinkoView _view;
        [SerializeField] private PlinkoBallKillZone _ballKillZone;

        [Space(5), Header("Config")]
        [SerializeField] private PlinkoConfig _config;
        [SerializeField] private PlayerBallView _ballPrefab;
        [SerializeField] private Transform _boardContainer;
        [SerializeField] private Transform _bucketsRoot;
        [SerializeField] private Transform _pegsRoot;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _resultsSFXSource;

        [Space(5), Header("Economy Settings")]
        [SerializeField] private int _minBet = 10;
        [SerializeField] private int _betStep = 10;

        [Space(5), Header("Other")]
        [SerializeField] private ResultPanelView _resultPanelView;
        [SerializeField] private GameObject _gridBuilder;

        // Настройки для математического режима
        [Space(5), Header("Math Mode Settings")]
        [SerializeField] private bool _useMathMode = true; // Переключатель: физика vs математика
        [SerializeField] private int _pathSeed = -1; // Сид для генерации пути (-1 = случайный)

        private readonly List<BucketView> _buckets = new();
        private readonly List<PegView> _pegs = new();

        private PlayerBallView _playerBall;
        private PlinkoPathGenerator _pathGenerator;

        private int _maxBet;
        private int _currentBet;
        private bool _isPlaying;

        public override void Enter()
        {
#if !UNITY_EDITOR
            Destroy(_gridBuilder);
#endif
            CacheBuckets();
            CachePegs();

            _pathGenerator = new PlinkoPathGenerator(_config, _pathSeed);

            GameServices.EconomyService.OnCoinsBalanceChanged += HandleCoinsBalanceChanged;

            _view.OnBetChanged += HandleBetChanged;
            _view.OnBetChangedFallback += HandleBetChangedFallback;
            _view.OnBetUpClick += HandleBetUpClick;
            _view.OnBetDownClick += HandleBetDownClick;
            _view.OnDropButtonClick += HandleDropButtonClick;

            _resultPanelView.OnRestartGameButtonClick += HandleRestartGameButtonClick;
            _resultPanelView.SetAudioSource(_resultsSFXSource);

            _ballKillZone.OnBallDropToKillZone += HandleFinish;

            if (_buckets.Count > 0)
                foreach (var bucket in _buckets)
                    bucket.OnBallEntered += HandleFinish;
        }

        public override void Initialize()
        {
            _maxBet = Mathf.RoundToInt(GameServices.EconomyService.GetCoinsBalance() * 0.9f);
            _currentBet = _minBet;

            _view.Init();
            _view.UpdateUI(_currentBet.ToString("N0"));
        }

        public override void Exit()
        {
            GameServices.EconomyService.OnCoinsBalanceChanged -= HandleCoinsBalanceChanged;

            _view.OnBetChanged -= HandleBetChanged;
            _view.OnBetChangedFallback -= HandleBetChangedFallback;
            _view.OnBetUpClick -= HandleBetUpClick;
            _view.OnBetDownClick -= HandleBetDownClick;
            _view.OnDropButtonClick -= HandleDropButtonClick;

            _resultPanelView.OnRestartGameButtonClick -= HandleRestartGameButtonClick;

            _ballKillZone.OnBallDropToKillZone -= HandleFinish;

            _view.Dispose();

            if (_buckets.Count > 0)
                foreach (var bucket in _buckets)
                    bucket.OnBallEntered -= HandleFinish;
        }

        private void CacheBuckets()
        {
            List<BucketView> buckets = _bucketsRoot.GetComponentsInChildren<BucketView>().ToList();
            _buckets.AddRange(buckets);
        }

        private void CachePegs()
        {
            List<PegView> pegs = _pegsRoot.GetComponentsInChildren<PegView>().ToList();
            _pegs.AddRange(pegs);
        }

        private void DropBall()
        {
            if (_useMathMode)
            {
                // Математический режим: генерируем путь и анимируем мяч
                PlinkoPath path = _pathGenerator.GeneratePath();
                _playerBall = Instantiate(_ballPrefab, _config.SpawnPoint, Quaternion.identity);
                _playerBall.InitForMathMovement(path, _config, _pegs, _buckets, _config.SpawnPoint, _sfxSource);
            }
            else
                _playerBall = Instantiate(_ballPrefab, _config.SpawnPoint, Quaternion.identity, _boardContainer);
        }

        private void HandleDropButtonClick()
        {
            if (_isPlaying)
                return;

            if (!base.SpendEnergy())
            {
                _view.ShowWarningMessage("Not enough energy!", $"You don't have enough energy ({GameConstants.ENERGY_FOR_GAME}) for this game.");
                return;
            }

            if (!GameServices.EconomyService.HasEnoughBalance(_currentBet))
            {
                _view.ShowWarningMessage("Not enough coins!", $"You don't have enough coins ({_currentBet}) for this bet.");
                return;
            }

            _isPlaying = true;
            DropBall();
            _view.ToggleButtonsInteractable(false);
            GameServices.EconomyService.SpendCoins(_currentBet);
        }

        private void HandleFinish(float multiplier)
        {
            bool isWin = multiplier > 0f;
            bool isAlreadyPlayed = GameServices.PlayedAcradesService.IsArcadePlayed(GameConstants.GAME_PLINKO_VIBE);

            var rewardCoins = Mathf.RoundToInt(_currentBet * multiplier);
            if (rewardCoins > 0)
                GameServices.EconomyService.AddCoins(rewardCoins);

            Debug.Log($"[Plinko Controller] Is win: {isWin}, Multiplier: {multiplier}");

            if (isWin)
                _resultPanelView.ShowResultPanel(isWin, false, rewardCoins);
            else
                _resultPanelView.ShowResultPanel(isWin, false, rewardCoins);

            var result = new GameResult(
                isWin: isWin,
                rewardCoins: rewardCoins,
                rewardXP: 5 * (int)Mathf.Max(1, multiplier),
                questTag: GameConstants.TAG_DROP_10_PLINKO_BALLS,
                gameId: GameConstants.GAME_PLINKO_VIBE,
                arcadePlayed: isAlreadyPlayed);

            GameServices.GameCompletionHandler.HandleGameResult(result);
            RecordArcadePlay(GameConstants.GAME_PLINKO_VIBE);
            _isPlaying = false;
        }

        private void HandleBetDownClick()
        {
            if (_isPlaying || _currentBet <= _minBet) 
                return;
            _currentBet = Mathf.Max(_currentBet - _betStep, _minBet);
            _view.UpdateUI(_currentBet.ToString("N0"));
        }

        private void HandleBetUpClick()
        {
            if (_isPlaying || _currentBet >= _maxBet) 
                return;
            _currentBet = Mathf.Min(_currentBet + _betStep, _maxBet);
            _view.UpdateUI(_currentBet.ToString("N0"));
        }

        private void HandleBetChanged(int bet)
        {
            if (_isPlaying) 
                return;

            _currentBet = Mathf.Clamp(bet, _minBet, _maxBet);
            _view.UpdateUI(_currentBet.ToString("N0"));
        }

        private void HandleBetChangedFallback() => _view.RefreshInput(_currentBet.ToString("N0"));

        private void HandleCoinsBalanceChanged(float coins)
        {
            _maxBet = Mathf.RoundToInt(coins * 0.9f);
            if (_currentBet > _maxBet)
            {
                _currentBet = Mathf.Max(_minBet, _maxBet);
                _view.UpdateUI(_currentBet.ToString("N0"));
            }
        }

        private void HandleRestartGameButtonClick() => _view.ToggleButtonsInteractable(true);
    }
}