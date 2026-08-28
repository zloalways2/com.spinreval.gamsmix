using Core.Common;
using Core.Data;
using Core.Data.Reels;
using Core.Services;
using Core.Services.Analytics;
using Core.Services.Audio;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UI.Animations.Game;
using UI.Other;
using UI.Reels;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Gameplay.GameControllers
{
    public class ReelsController : GameController
    {
        [Header("Reels")]
        [SerializeField] private List<ReelView> _reels = new();

        [Space(5), Header("UI")]
        [SerializeField] private TMP_InputField _betInputField;
        [SerializeField] private TextMeshProUGUI _autoSpinLabel;
        [SerializeField] private TextMeshProUGUI _currentBetLabel;
        [SerializeField] private TextMeshProUGUI _winAmountLabel;
        [SerializeField] private ActionButton _spinButton;
        [SerializeField] private ActionButton _betPlusButton;
        [SerializeField] private ActionButton _betMinusButton;
        [SerializeField] private ActionButton _turboButton;
        [SerializeField] private ActionButton _maxBetButton;
        [SerializeField] private ActionButton _infoButton;
        [SerializeField] private ActionButton _autoButton;
        [SerializeField] private GameObject _winPanel;
        [SerializeField] private GameObject _infoPanel;
        [SerializeField] private Image _winGlowImage;
        [SerializeField] private WarningMessageView _warningMessageView;
        [SerializeField] private LineDisplayController _lineDisplayController;

        [Space(5), Header("Audio Settings")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioClip _slotsSFXClip;
        [SerializeField] private AudioClip _winSFXClip;

        [Space(5), Header("Auto Spin View Setup")]
        [SerializeField] private TMP_ColorGradient _activeColor;
        [SerializeField] private TMP_ColorGradient _inactiveColor;

        [Space(5), Header("Data")]
        [SerializeField] private List<SymbolData> _symbolData = new();

        [Space(5), Header("Settings")]
        [SerializeField] private ReelsType _reelsType = ReelsType.Classic;
        [SerializeField] private int _minBet = 10;
        [SerializeField] private int _betStep = 10;
        [SerializeField] private float _baseSpinDuration = 1f;
        [SerializeField] private float _reelDelay = 0.2f;
        [SerializeField] private float _autoSpinDelay = 1f;
        [SerializeField] private List<Sprite> _symbols = new();
        [SerializeField] private int _diamondReelsMultiplier = 25;

        private Material _winGlowMaterial;

        private float _maxBet;
        private int _currentBet;
        private bool _isSpinning;
        private bool _isTurboMode;
        private float _spinDuration;

        // Автоспин
        private bool _isAutoSpinEnabled = false;
        private UniTask _autoSpinTask;
        private CancellationTokenSource _autoSpinCts;

        public override void Enter()
        {
            AnalyticsService.Instance.ReportGameStart(GameConstants.GAME_REELS);

            _winGlowMaterial = new(_winGlowImage.material);
            _winGlowImage.material = _winGlowMaterial;

            _currentBet = _minBet;
            _isTurboMode = false;
            _spinDuration = _baseSpinDuration;
            _maxBet = Mathf.RoundToInt(GameServices.EconomyService.GetCoinsBalance() * 0.9f);

            for (int i = 0; i < _reels.Count; i++)
                _reels[i].Init(_symbols);

            if (_infoPanel != null && _infoPanel.activeSelf)
                _infoPanel.SetActive(false);

            if (_sfxSource != null)
                _sfxSource.volume = AudioService.Instance.GetSfxVolume();
        }

        public override void Initialize()
        {
            GameServices.EconomyService.OnCoinsBalanceChanged += HandleChangedCoinsBalance;

            if (_spinButton != null) 
                _spinButton.OnButtonClick += HandleSpinButtonClick;
            if (_betPlusButton != null)
            {
                _betPlusButton.IsUseHeldFunc = true;
                _betPlusButton.OnButtonClick += HandleBetUpButtonClick;
            }
            if (_betMinusButton != null)
            {
                _betMinusButton.IsUseHeldFunc = true;
                _betMinusButton.OnButtonClick += HandleBetDownButtonClick;
            }
            if (_turboButton != null) 
                _turboButton.OnButtonClick += HandleTurboModeButtonClick;
            if (_betInputField != null) 
                _betInputField.onEndEdit.AddListener(HandleBetChanged);
            if (_maxBetButton != null) 
                _maxBetButton.OnButtonClick += HandleMaxBetButtonClick;
            if (_infoButton != null) 
                _infoButton.OnButtonClick += HandleInfoButtonClick;
            if(_autoButton != null)
                _autoButton.OnButtonClick += HandleAutoButtonClick;

            if (_autoSpinLabel != null)
                _inactiveColor = _autoSpinLabel.colorGradientPreset;

            UpdateUI();
        }

        public override void Exit()
        {
            GameServices.EconomyService.OnCoinsBalanceChanged -= HandleChangedCoinsBalance;

            if (_spinButton != null) 
                _spinButton.OnButtonClick -= HandleSpinButtonClick;
            if (_betPlusButton != null)
                _betPlusButton.OnButtonClick -= HandleBetUpButtonClick;
            if (_betMinusButton != null)
                _betMinusButton.OnButtonClick -= HandleBetDownButtonClick;
            if (_turboButton != null) 
                _turboButton.OnButtonClick -= HandleTurboModeButtonClick;
            if (_betInputField != null) 
                _betInputField.onEndEdit.RemoveListener(HandleBetChanged);
            if (_maxBetButton != null) 
                _maxBetButton.OnButtonClick -= HandleMaxBetButtonClick;
            if (_infoButton != null) 
                _infoButton.OnButtonClick -= HandleInfoButtonClick;
            if (_autoButton != null)
                _autoButton.OnButtonClick -= HandleAutoButtonClick;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
                SetTurboMode(!_isTurboMode);
        }

        private void UpdateUI()
        {
            if (_betInputField != null) 
                _betInputField.text = $"{_currentBet}";

            if (_spinButton != null) 
                _spinButton.Interactable = !_isSpinning && GameServices.EconomyService.GetCoinsBalance() >= _currentBet;
            if (_betPlusButton != null) 
                _betPlusButton.Interactable = !_isSpinning && _currentBet < _maxBet;
            if (_betMinusButton != null) 
                _betMinusButton.Interactable = !_isSpinning && _currentBet > _minBet;

            if (_turboButton != null)
            {
                if (_turboButton.TryGetComponent<Image>(out var turboImage))
                    turboImage.color = _isTurboMode ? Color.green : Color.white;
            }
        }

        private async UniTask StartSpin()
        {
            if (!base.SpendEnergy())
            {
                _warningMessageView.SetWarningMessage("Not enough energy!", $"You don't have enough energy ({GameConstants.ENERGY_FOR_GAME}) for this game.");
                _warningMessageView.Show();
                if (_isAutoSpinEnabled)
                    StopAutoSpin();
                return;
            }

            if (!GameServices.EconomyService.HasEnoughBalance(_currentBet))
            {
                _warningMessageView.SetWarningMessage("Not enough coins!", $"You don't have enough coins ({_currentBet}) for this bet.");
                _warningMessageView.Show();
                return;
            }

            if (_lineDisplayController != null)
                _lineDisplayController.ClearLines();

            GameServices.EconomyService.SpendCoins(_currentBet);

            if (_sfxSource != null && _slotsSFXClip != null)
                _sfxSource.PlayOneShot(_slotsSFXClip);

            _isSpinning = true;
            SetInteractable(false);

            int reelCount = _reels.Count;
            int[][] results = new int[reelCount][];
            int[] middleRow = new int[reelCount]; // Центральные символы для проверки выигрыша

            // 1. Генерируем результаты (лента из 3 символов для каждого барабана)
            for (int i = 0; i < reelCount; i++)
            {
                results[i] = new int[3];
                int mid = UnityEngine.Random.Range(0, _symbolData.Count);

                results[i][0] = (mid + 1) % _symbolData.Count; // Верхний
                results[i][1] = mid;                           // Средний
                results[i][2] = (mid - 1 + _symbolData.Count) % _symbolData.Count; // Нижний

                middleRow[i] = mid;
            }

            // 2. Запускаем вращение с задержками
            List<UniTask> spinTasks = new();
            var cts = new CancellationTokenSource();

            for (int i = 0; i < reelCount; i++)
            {
                float delay = i * _reelDelay;

                if (delay > 0)
                    await UniTask.Delay(TimeSpan.FromSeconds(delay));

                bool turbo = _isTurboMode;
                float duration = _spinDuration + (i * 0.2f);

                spinTasks.Add(_reels[i].SpinAsync(duration, results[i], turbo, cts.Token));
            }

            await UniTask.WhenAll(spinTasks);

            // 3. Проверяем выигрыш по центральному ряду
            CheckWin(middleRow);

            _isSpinning = false;
            SetInteractable(true);
        }

        private async UniTask AutoSpinLoop()
        {
            Debug.Log("[Reels] Auto spin started");

            try
            {
                while (_isAutoSpinEnabled)
                {
                    // Проверяем баланс
                    if (GameServices.EconomyService.GetCoinsBalance() < _currentBet)
                    {
                        Debug.Log("[Reels] Auto spin stopped: not enough coins");
                        StopAutoSpin();
                        break;
                    }

                    // Делаем спин
                    await StartSpin();

                    // Если автоспин был остановлен во время спина (например, игрок нажал Stop)
                    if (!_isAutoSpinEnabled)
                        break;

                    // Пауза между спинами
                    await UniTask.Delay(TimeSpan.FromSeconds(_autoSpinDelay));
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальное завершение при отмене
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Reels] Auto spin error: {ex}");
                StopAutoSpin();
            }
            finally
            {
                Debug.Log("[Reels] Auto spin stopped");
                _isAutoSpinEnabled = false;
                UpdateUI();
            }
        }

        private void StartAutoSpin()
        {
            if (_isAutoSpinEnabled)
                return;

            _isAutoSpinEnabled = true;
            _autoSpinCts = new CancellationTokenSource();
            _autoSpinTask = AutoSpinLoop().AttachExternalCancellation(_autoSpinCts.Token);
            SetInteractable(false);
            UpdateUI();
        }

        private void StopAutoSpin()
        {
            if (!_isAutoSpinEnabled)
                return;

            _isAutoSpinEnabled = false;
            _autoSpinCts?.Cancel();
            _autoSpinCts?.Dispose();
            _autoSpinCts = null;
            SetInteractable(true);
            UpdateUI();
        }

        private void CheckWin(int[] symbolIndices)
        {
            if (symbolIndices.Length == 0)
                return;

            int matchCount = CountSequentialMatches(symbolIndices);

            bool isWin = IsWinningCombination(symbolIndices, matchCount);
            bool isAlreadyPlayed = false;
            if (_reelsType == ReelsType.Classic)
                isAlreadyPlayed = GameServices.PlayedAcradesService.IsArcadePlayed(GameConstants.GAME_REELS);
            else
                isAlreadyPlayed = GameServices.PlayedAcradesService.IsArcadePlayed(GameConstants.GAME_DIAMOND_RETRO);

            if (_reelsType == ReelsType.Diamond && IsDiamondSymbol(symbolIndices[0]))
                GameServices.Quests.ProgressQuest(GameConstants.TAG_COLLECT_5_DIAMONDS, matchCount);

            if (isWin)
            {
                if (_sfxSource != null && _winSFXClip != null)
                    _sfxSource.PlayOneShot(_winSFXClip);

                List<List<Vector2Int>> winningLines = new() { new List<Vector2Int>() };

                for (int i = 0; i < matchCount; i++)
                    winningLines[0].Add(new Vector2Int(i, 1));

                if (_lineDisplayController != null)
                    _lineDisplayController.DrawWinningLines(winningLines, _reels.ToArray());

                AnimateWinningSymbols(winningLines[0]);

                int totalWin = CalculateWin(symbolIndices[0], matchCount);

                _winGlowImage.gameObject.SetActive(true);
                StartCoroutine(GlowAnimationRoutine(() =>
                {
                    StopCoroutine(GlowAnimationRoutine());
                }));

                ShowResult(true, totalWin, symbolIndices);

                GameResult result = new(
                    isWin: true,
                    rewardCoins: totalWin,
                    rewardXP: 20,
                    questTag: GameConstants.TAG_SPIN_10_REELS,
                    gameId: GameConstants.GAME_REELS,
                    arcadePlayed: isAlreadyPlayed
                );

                Debug.Log($"WIN! {matchCount} symbols. Reward: {totalWin}");
                GameServices.GameCompletionHandler.HandleGameResult(result);
            }
            else
            {
                Debug.Log("No win");

                if (_lineDisplayController != null)
                    _lineDisplayController.ClearLines();

                ShowResult(false, 0, symbolIndices);

                GameResult result = new(
                    isWin: false,
                    rewardCoins: 0,
                    rewardXP: 5,
                    questTag: GameConstants.TAG_SPIN_10_REELS,
                    gameId: GameConstants.GAME_REELS,
                    arcadePlayed: isAlreadyPlayed
                );

                GameServices.GameCompletionHandler.HandleGameResult(result);
            }

            if (_reelsType == ReelsType.Classic)
                RecordArcadePlay(GameConstants.GAME_REELS);
            else
                RecordArcadePlay(GameConstants.GAME_DIAMOND_RETRO);
        }

        private int CalculateWin(int symbolIndex, int matchCount)
        {
            int baseReward = _symbolData[symbolIndex].BaseReward;
            int multiplier = GetMultiplier(matchCount);

            return _reelsType switch
            {
                ReelsType.Classic =>
                    baseReward * multiplier + _currentBet,

                ReelsType.Diamond =>
                    baseReward * multiplier + _currentBet * _diamondReelsMultiplier,

                _ => 0
            };
        }

        private bool IsWinningCombination(int[] symbolIndices, int matchCount)
        {
            return _reelsType switch
            {
                ReelsType.Classic => matchCount >= 2,

                ReelsType.Diamond =>
                    matchCount == 3 &&
                    IsDiamondSymbol(symbolIndices[0]),

                _ => false
            };
        }

        private int CountSequentialMatches(int[] symbolIndices)
        {
            if (symbolIndices.Length == 0)
                return 0;

            int firstSymbol = symbolIndices[0];
            int matchCount = 1;

            for (int i = 1; i < symbolIndices.Length; i++)
            {
                if (symbolIndices[i] != firstSymbol)
                    break;

                matchCount++;
            }

            return matchCount;
        }

        private bool IsDiamondSymbol(int symbolIndex)
        {
            return _symbolData[symbolIndex].Type == SymbolType.Diamond;
        }

        private int CountMatches(List<int> symbols)
        {
            if (symbols.Count == 0) 
                return 0;

            Dictionary<int, int> symbolCounts = new();
            foreach (var symbol in symbols)
            {
                if (!symbolCounts.ContainsKey(symbol))
                    symbolCounts[symbol] = 0;

                symbolCounts[symbol]++;
            }

            int maxCount = 0;
            foreach (var kvp in symbolCounts) 
                if (kvp.Value > maxCount)
                    maxCount = kvp.Value;
            return maxCount;
        }

        private int GetMultiplier(int matchCount) => matchCount switch
        {
            2 => 2,
            3 => 16,
            4 => 24,
            5 => 40,
            6 => 80,
            _ => 2,
        };

        private void ShowResult(bool isWin, int winAmount, int[] middleRow)
        {
            if (_winPanel != null) 
                _winPanel.SetActive(isWin);
            if (_winAmountLabel != null) 
                _winAmountLabel.text = isWin ? $"+{winAmount}" : "0";

            Debug.Log($"[Reels] Result: Win={isWin}, Amount={winAmount}, Matches={CountMatches(new List<int>(middleRow))}");

            if (isWin && _winPanel != null) 
                Invoke(nameof(HideWinPanel), 3.5f);
        }

        private void HideWinPanel()
        {
            if (_winPanel != null)
                _winPanel.SetActive(false);

            if (_lineDisplayController != null)
                _lineDisplayController.ClearLines();
        }

        private void SetInteractable(bool interactable)
        {
            if (_isAutoSpinEnabled)
                return;

            _spinButton.Interactable = interactable;
            _betPlusButton.Interactable = interactable;
            _betMinusButton.Interactable = interactable;
            _maxBetButton.Interactable = interactable;
        }

        private void RefreshInput()
        {
            if (_betInputField != null)
                _betInputField.SetTextWithoutNotify(Mathf.FloorToInt(_currentBet).ToString());
        }

        private void SetBet(int value)
        {
            int max = Mathf.Max(_minBet, Mathf.FloorToInt(_maxBet));

            _currentBet = Mathf.Clamp(value, _minBet, max);

            _betInputField.SetTextWithoutNotify(_currentBet.ToString("N0"));

            _currentBetLabel.text = _currentBet.ToString("N0");
        }

        private IEnumerator GlowAnimationRoutine(Action onComplete = null)
        {
            yield return new WaitForSeconds(2.5f);

            _winGlowImage.gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        /// <summary>
        /// Плавно увеличивает и пульсирует выигрышные символы.
        /// </summary>
        private void AnimateWinningSymbols(List<Vector2Int> winningPositions)
        {
            if (winningPositions == null || winningPositions.Count == 0)
                return;

            foreach (var pos in winningPositions)
            {
                int reelIndex = pos.x;
                int rowIndex = pos.y;

                // Получаем Transform слота напрямую из ReelView
                Transform symbolTransform = _reels[reelIndex].GetSlotTransform(rowIndex);

                if (symbolTransform == null)
                    continue;

                // Сбрасываем перед запуском
                symbolTransform.DOKill();
                symbolTransform.localScale = Vector3.one;

                // Создаем последовательность анимаций
                Sequence pulseSequence = DOTween.Sequence();

                // 1. Плавное увеличение (Pop-in эффект)
                pulseSequence.Append(symbolTransform.DOScale(1.25f, 0.3f).SetEase(Ease.OutBack));

                // 2. Пульсация (3 цикла туда-обратно)
                pulseSequence.Append(symbolTransform.DOScale(1.1f, 0.55f).SetEase(Ease.InOutSine).SetLoops(4, LoopType.Yoyo));

                // 3. Плавный возврат к исходному размеру
                pulseSequence.Append(symbolTransform.DOScale(1f, 0.2f).SetEase(Ease.OutSine));
            }
        }

        private void SetTurboMode(bool enabled)
        {
            _isTurboMode = enabled;
            _spinDuration = _isTurboMode ? _baseSpinDuration / 2f : _baseSpinDuration;

            UpdateTurboButtonVisual();

            // Ключевой момент: применяем к барабанам прямо сейчас,
            // даже если они в середине спина — это и есть цель турбо.
            foreach (var reel in _reels)
                reel.ApplyTurbo(_isTurboMode);

            Debug.Log($"[Reels] Turbo mode: {_isTurboMode}, duration: {_spinDuration}");
        }

        private void UpdateTurboButtonVisual()
        {
            if (_turboButton == null)
                return;

            if (_isTurboMode)
                _turboButton.Animations.PulseAnimation();
            else
                _turboButton.Animations.StopPulseAnimation();

            if (_turboButton.TryGetComponent<Image>(out var turboImage))
                turboImage.color = _isTurboMode ? Color.green : Color.white;
        }

        private void HandleSpinButtonClick()
        {
            if (_isSpinning)
                return;

            if (GameServices.EconomyService.GetCoinsBalance() < _currentBet)
            {
                Debug.LogWarning("[Reels] Not enough coins to spin");
                return;
            }

            foreach (var reel in _reels)
            {
                reel.transform.DOKill();
                reel.transform.localScale = Vector3.one;
            }

            _winGlowImage.gameObject.SetActive(false);

            if (_lineDisplayController != null)
                _lineDisplayController.ClearLines();

            StartSpin().Forget();
        }

        private void HandleBetUpButtonClick()
        {
            if (_isSpinning)
                return;

            if (_currentBet < _maxBet)
            {
                _currentBet = Mathf.Min(_currentBet + _betStep, Mathf.RoundToInt(_maxBet));
                UpdateUI();
            }
        }

        private void HandleBetDownButtonClick()
        {
            if (_isSpinning)
                return;

            if (_currentBet > _minBet)
            {
                _currentBet = Mathf.Max(_currentBet - _betStep, _minBet);
                UpdateUI();
            }
        }

        private void HandleTurboModeButtonClick()
        {
            SetTurboMode(!_isTurboMode);
            GameServices.Quests.ProgressQuest(GameConstants.TAG_TRIGGER_TURBO_BOOST);
        }

        private void HandleBetChanged(string raw)
        {
            if (int.TryParse(raw, out int bet))
                SetBet(bet);
            else
                RefreshInput();
        }

        private void HandleMaxBetButtonClick()
        {
            _currentBet = (int)_maxBet;
            UpdateUI();
        }

        private void HandleInfoButtonClick()
        {
            if (_infoPanel.activeSelf)
                _infoPanel.SetActive(false);
            else
                _infoPanel.SetActive(true);
        }

        private void HandleAutoButtonClick()
        {
            if (_isSpinning)
                return;

            if (_isAutoSpinEnabled)
            {
                StopAutoSpin();
                _autoSpinLabel.colorGradientPreset = _inactiveColor;
                SetInteractable(true);
            }
            else
            {
                if (GameServices.EconomyService.GetCoinsBalance() < _currentBet)
                {
                    Debug.LogWarning("[Reels] Not enough coins for auto spin");
                    return;
                }
                SetInteractable(false);
                StartAutoSpin();
                _autoSpinLabel.colorGradientPreset = _activeColor;
            }
        }

        private void HandleChangedCoinsBalance(float coins) => _maxBet = Mathf.RoundToInt(coins * 0.9f);
    }
}