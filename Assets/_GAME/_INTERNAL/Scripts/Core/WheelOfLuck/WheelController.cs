using Core.Data;
using Core.Services;
using Core.Services.Analytics;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI.Other;
using UnityEngine;

namespace Core.WheelOfLuck
{
    public class WheelController : MonoBehaviour
    {
        private static readonly TimeSpan COOLDOWN = TimeSpan.FromHours(12);

        [Header("Wheel")]
        [SerializeField] private RectTransform _wheelTransform;
        [SerializeField] private RectTransform _pointer;

        [Space(5), Header("Views")]
        [SerializeField] private List<RewardView> _rewardViews = new();

        [Space(5), Header("Spin settings")]
        [SerializeField] private float _spinDuration = 4f;
        [SerializeField] private int _minFullRotations = 4;

        [Space(5), Header("Economy")]
        [SerializeField] private int _initialFreeSpins = 1;

        [Space(5), Header("Text")]
        [SerializeField] private TextMeshProUGUI _cooldownText;
        [SerializeField] private TextMeshProUGUI _spinsCountText;

        [Space(5), Header("Buttons")]
        [SerializeField] private ActionButton _startSpinButton;

        [Space(5), Header("Neon Wheel Setup")]
        [SerializeField] private bool _isNeonWheel = false;
        [SerializeField] private ActionButton _sector_250;
        [SerializeField] private ActionButton _sector_450;
        [SerializeField] private ActionButton _sector_500;
        [SerializeField] private ActionButton _sector_750;
        [SerializeField] private ActionButton _sector_950;
        [SerializeField] private ActionButton _sector_1000;
        [SerializeField] private ActionButton _sector_2500;
        [SerializeField] private ActionButton _sector_5000;

        [Space(5), Header("Other")]
        [SerializeField] private WarningMessageView _warningMessageView;
        [SerializeField] private ResultPanelView _resultPanelView;
        [SerializeField] private AudioSource _sfxSource;

        [Space(5), Header("Debug")]
        [SerializeField] private bool _isDebug = false;

        private Dictionary<WheelReward.RewardType, Action<WheelReward, int>> _rewardActions;
        private Dictionary<WheelReward.RewardType, ResultConfig> _resultConfigs;

        private const string PREF_FREE_SPINS = "Wheel_FreeSpins";
        private const string PREF_NEXT_AVAILABLE_TICKS = "Wheel_NextAvailableTicks";

        private int _selectedSector;
        private int _freeSpins;
        private DateTimeOffset _nextAvailableUtc;
        private bool _isSpinning;
        private WheelReward _pendingReward;
        private int _pendingIndex;

        private Coroutine _cooldownCoroutine;

        private Tween _spinTween;
        private Tween _pulseTween;

        private Action _prepareAndStartSpinAction;

        public event Action<WheelReward> OnSpinStarted;
        public event Action<WheelReward> OnSpinFinished;
        public event Action OnStateChanged;

        private void Awake()
        {
            AnalyticsService.Instance.ReportGameStart(GameConstants.GAME_WHEEL_OF_REVOLUT);

            _resultConfigs = new()
            {
                { WheelReward.RewardType.Coins, new ResultConfig(isWin: true, baseXP: 20) },
                { WheelReward.RewardType.XP, new ResultConfig(isWin: true, baseXP: 0) },
                { WheelReward.RewardType.FreeSpin, new ResultConfig(isWin: true, baseXP: 5) },
                { WheelReward.RewardType.Sector, new ResultConfig(isWin: true, baseXP: 20) },
                { WheelReward.RewardType.Nothing, new ResultConfig(isWin: false, baseXP: 0) },
                { WheelReward.RewardType.Energy, new ResultConfig(isWin:true, baseXP: 20) },
            };

            _rewardActions = new()
            {
                { WheelReward.RewardType.Coins, (reward, _) => 
                    {
                        Debug.Log($"[Wheel] Given coins: {reward.Amount}");
                        HandleGameResult(reward.Type, reward.Amount);
                    }  
                },
                { WheelReward.RewardType.XP, (reward, _) =>
                    {
                        Debug.Log($"[Wheel] Given coins: {reward.Amount}");
                        HandleGameResult(reward.Type, 0);
                    }
                },
                { WheelReward.RewardType.FreeSpin, (reward, mult) =>
                    {
                        int spins = (int)reward.Amount * Mathf.Max(1, mult);
                        _freeSpins += spins;
                        _nextAvailableUtc = DateTimeOffset.UtcNow;
                        SaveState();
                        Debug.Log($"[Wheel] Given free spins: {spins}");
                        HandleGameResult(reward.Type, 0);
                    }
                },
                { WheelReward.RewardType.Sector, (reward, _) =>
                    {
                        bool isSectorWin = _selectedSector == reward.Amount;
                        float sectorCoins = isSectorWin ? reward.Amount : 0;

                        if (isSectorWin)
                        {
                            Debug.Log($"[Wheel] Claimed sector: {reward.Amount}");
                            _selectedSector = 0;
                        }
                        else
                            Debug.LogWarning($"[Wheel] Sector mismatch. Expected: {_selectedSector}, but got: {reward.Amount}");

                        GameResult result = HandleGameResult(reward.Type, sectorCoins, isSectorWin);
                        _resultPanelView.ShowResultPanel(result.IsWin, false, (int)result.RewardCoins, (int)result.RewardCoins);
                    }
                },
                { WheelReward.RewardType.Energy, (reward, _) =>
                    {
                        GameServices.PlayerService.AddEnergy(Mathf.RoundToInt(reward.Amount));
                        Debug.Log($"[Wheel] Given energy: {reward.Amount}");
                        HandleGameResult(reward.Type, 0);
                    }
                },
                { WheelReward.RewardType.Nothing, (reward, _) =>
                    {
                        Debug.Log("[Wheel] Nothing to give");
                        HandleGameResult(reward.Type, 0);
                    }
                }
            };

            if (_isNeonWheel)
                _resultPanelView.SetAudioSource(_sfxSource);

            LoadState();
            UpdateCooldownLabel();
            StartCooldownUpdater();
            UpdatePulseState();
        }

        private void Start()
        {
            if(_sector_250 != null
                || _sector_450 != null
                || _sector_500 != null
                || _sector_750 != null
                || _sector_950 != null
                || _sector_1000 != null
                || _sector_2500 != null
                || _sector_5000 != null)
            {
                _sector_250.OnButtonClick += () => HandleSectorButtonClick(250);
                _sector_450.OnButtonClick += () => HandleSectorButtonClick(450);
                _sector_500.OnButtonClick += () => HandleSectorButtonClick(500);
                _sector_750.OnButtonClick += () => HandleSectorButtonClick(750);
                _sector_950.OnButtonClick += () => HandleSectorButtonClick(950);
                _sector_1000.OnButtonClick += () => HandleSectorButtonClick(1000);
                _sector_2500.OnButtonClick += () => HandleSectorButtonClick(2500);
                _sector_5000.OnButtonClick += () => HandleSectorButtonClick(5000);
            }

            if (_startSpinButton != null)
            {
                _prepareAndStartSpinAction = () => PrepareAndStartSpin(ClaimWithoutAd);
                _startSpinButton.OnButtonClick += _prepareAndStartSpinAction;

                _startSpinButton.Interactable = CanSpin();
                if (!CanSpin())
                    _startSpinButton.Animations.StopPulseAnimation();
            }
        }

        private void OnValidate()
        {
            if (_initialFreeSpins < 0) 
                _initialFreeSpins = 0;
        }

        private void OnDisable()
        {
            _spinTween?.Kill();
            _pulseTween?.Kill();
        }

        private void OnDestroy()
        {
            _spinTween?.Kill();
            _pulseTween?.Kill();

            if (_startSpinButton != null)
                _startSpinButton.OnButtonClick -= _prepareAndStartSpinAction;

            StopCooldownUpdater();
        }

        [ContextMenu("DEBUG: Reset Cooldown")]
        private void ResetCooldownForDebug()
        {
            _nextAvailableUtc = DateTime.MinValue;
            _freeSpins = _initialFreeSpins;
            if(_spinsCountText != null)
                _spinsCountText.text = $"FREE SPINS:{_freeSpins}";

            SaveState();
            UpdateCooldownLabel();
            UpdatePulseState();

            OnStateChanged?.Invoke();
            _startSpinButton.Interactable = true;
            Debug.Log("[Wheel] DEBUG: cooldown and free spins reset");
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!_isDebug) 
                return;

            // Нажмите R в режиме Play для сброса кулдауна
            if (Input.GetKeyDown(KeyCode.R))
                ResetCooldownForDebug();
        }
#endif

        private void UpdatePulseState()
        {
            if (_wheelTransform == null)
                return;

            bool shouldPulse = CanSpin();

            if (shouldPulse)
            {
                if (_pulseTween == null || !_pulseTween.IsActive())
                {
                    _pulseTween?.Kill();
                    _pulseTween = _wheelTransform
                        .DOScale(1.05f, 1f)
                        .SetEase(Ease.OutSine)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetTarget(_wheelTransform);
                }
            }
            else
            {
                _pulseTween?.Kill();
                _pulseTween = null;
            }
        }

        private void LoadState()
        {
            _freeSpins = PlayerPrefs.GetInt(PREF_FREE_SPINS, _initialFreeSpins);
            long unixTime = Convert.ToInt64(PlayerPrefs.GetString(PREF_NEXT_AVAILABLE_TICKS, "0"));

            _nextAvailableUtc = unixTime == 0 ? DateTimeOffset.MinValue : DateTimeOffset.FromUnixTimeSeconds(unixTime);

            if (!_isNeonWheel && IsAvailable() && _freeSpins == 0)
            {
                _freeSpins = 1;
                _nextAvailableUtc = DateTimeOffset.UtcNow.Add(COOLDOWN);
                SaveState();
            }

            if (_spinsCountText != null)
                _spinsCountText.text = $"FREE SPINS: {_freeSpins}";
        }

        private void SaveState()
        {
            PlayerPrefs.SetInt(PREF_FREE_SPINS, _freeSpins);
            long unixTime = _nextAvailableUtc == DateTimeOffset.MinValue ? 0 : _nextAvailableUtc.ToUnixTimeSeconds();
            PlayerPrefs.SetString(PREF_NEXT_AVAILABLE_TICKS, unixTime.ToString());
            PlayerPrefs.Save();
        }

        public bool IsAvailable()
        {
            return DateTimeOffset.UtcNow >= _nextAvailableUtc;
        }

        public int GetFreeSpins() => _freeSpins;

        public bool CanSpin()
        {
            if (_isNeonWheel)
                return true;

            if (!_isSpinning
                && _freeSpins > 0
                && IsAvailable()
                && _rewardViews != null
                && _rewardViews.Count > 0)
                return true;

            return false;
        }

        public void PrepareAndStartSpin(Action onComplete = null)
        {
            if(_isNeonWheel && !GameServices.EconomyService.HasEnoughBalance(_selectedSector))
            {
                _warningMessageView.SetWarningMessage("Not enough coins!", $"You don't have enough coins ({_selectedSector}) for this bet.");
                _warningMessageView.Show();
                return;
            }

            if (!GameServices.EnergyService.SpendEnergy(GameConstants.ENERGY_FOR_GAME))
            {
                _warningMessageView.SetWarningMessage("Not enough energy!", $"You don't have enough energy ({GameConstants.ENERGY_FOR_GAME}) for this game.");
                _warningMessageView.Show();
                return;
            }

            if (!CanSpin())
            {
                Debug.LogWarning("[Wheel] Невозможно начать спин. Проверьте CanSpin()");
                return;
            }

            _pulseTween?.Kill();

            _pendingIndex = SelectRewardIndexByWeight();
            _pendingReward = _rewardViews[_pendingIndex].Reward;

            OnSpinStarted?.Invoke(_pendingReward);

            StartTweenSpin(_pendingIndex, onComplete);

            if (_startSpinButton == null)
                return;

            _startSpinButton.Interactable = false;
            _startSpinButton.Animations.StopPulseAnimation();
            
            GameServices.Quests.ProgressQuest(GameConstants.TAG_SPIN_LUCKY_WHEEL);
        }

        private int SelectRewardIndexByWeight()
        {
            float total = 0f;
            foreach (var r in _rewardViews) total += Mathf.Max(0f, r.Reward.Weight);

            if (total <= 0f)
                return UnityEngine.Random.Range(0, _rewardViews.Count);

            float t = UnityEngine.Random.value * total;
            float accum = 0f;
            for (int i = 0; i < _rewardViews.Count; i++)
            {
                accum += Mathf.Max(0f, _rewardViews[i].Reward.Weight);
                if (t <= accum)
                    return i;
            }

            return _rewardViews.Count - 1;
        }

        private void StartTweenSpin(int targetIndex, Action onComplete = null)
        {
            if (_wheelTransform == null)
            {
                Debug.LogWarning("[Wheel] Wheel Transform не назначен");
                return;
            }

            _isSpinning = true;
            _spinTween?.Kill();

            RewardView targetRewardView = _rewardViews[targetIndex];
            
            if (!targetRewardView.TryGetComponent<RectTransform>(out var rewardTransform))
            {
                Debug.LogWarning("[Wheel] RewardView dont have RectTransform");
                return;
            }

            Debug.Log($"[Wheel] Spinning for reward index {targetIndex}, reward: {_pendingReward}");

            float currentAngle = _wheelTransform.eulerAngles.z;

            float deltaNeeded = CalculateAngleToPerfectAlignment(rewardTransform);

            float targetAbsoluteAngle = currentAngle + deltaNeeded;

            float minRequiredAngle = currentAngle + (_minFullRotations * 360f);

            while (targetAbsoluteAngle < minRequiredAngle)
                targetAbsoluteAngle += 360f;

            float endAngle = targetAbsoluteAngle;

            _spinTween = _wheelTransform
                .DORotate(new Vector3(0f, 0f, endAngle), _spinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    _wheelTransform.eulerAngles = new Vector3(0f, 0f, endAngle);

                    _freeSpins = Mathf.Max(0, _freeSpins - 1);
                    if(_spinsCountText != null)
                        _spinsCountText.text = $"FREE SPINS: {_freeSpins}";

                    if (!_isNeonWheel)
                    {
                        _nextAvailableUtc = DateTime.UtcNow.Add(COOLDOWN);
                        SaveState();
                    }

                    _isSpinning = false;

                    Debug.Log($"[Wheel] Reward: {_pendingReward}");

                    OnSpinFinished?.Invoke(_pendingReward);
                    OnStateChanged?.Invoke();

                    UpdateCooldownLabel();

                    if (_startSpinButton != null)
                    {
                        UpdatePulseState();

                        _startSpinButton.Interactable = false;
                        _startSpinButton.Animations.StopPulseAnimation();
                    }

                    onComplete?.Invoke();
                });
        }

        private float CalculateAngleToPerfectAlignment(RectTransform rewardTransform)
        {
            Vector3 wheelCenter = _wheelTransform.position;

            Vector3 rewardWorldPos = rewardTransform.position;

            Vector3 pointerWorldPos = _pointer != null ? _pointer.position : wheelCenter + Vector3.up * 100f;

            Vector3 toReward = rewardWorldPos - wheelCenter;
            Vector3 toPointer = pointerWorldPos - wheelCenter;

            float angleToReward = Mathf.Atan2(toReward.y, toReward.x) * Mathf.Rad2Deg;
            float angleToPointer = Mathf.Atan2(toPointer.y, toPointer.x) * Mathf.Rad2Deg;

            return angleToPointer - angleToReward;
        }

        public void ClaimWithoutAd()
        {
            if (_pendingReward == null)
            {
                Debug.LogWarning("[Wheel] Нет ожидаемой награды для выдачи");
                return;
            }

            ApplyReward(_pendingReward, bonusMultiplier: 1);
            _pendingReward = null;
            OnStateChanged?.Invoke();
            UpdateCooldownLabel();
            UpdatePulseState();

            if(_startSpinButton != null)
                _startSpinButton.Interactable = false;
        }

        private void ApplyReward(WheelReward reward, int bonusMultiplier)
        {
            if (reward == null) 
                return;

            if (_rewardActions.TryGetValue(reward.Type, out var action))
                action.Invoke(reward, bonusMultiplier);
            else
            {
                Debug.LogError($"[Wheel] No action found for reward type: {reward.Type}");
                return;
            }

            RecordGamePlay();

            if (_startSpinButton != null)
                _startSpinButton.Interactable = false;
        }

        private void RecordGamePlay()
        {
            string gameId = _isNeonWheel ? GameConstants.GAME_NEON_WHEEL : GameConstants.GAME_WHEEL_OF_REVOLUT;
            GameServices.FavoriteGamesService.RecordGamePlay(gameId);
        }

        private GameResult HandleGameResult(WheelReward.RewardType type, float rewardCoins, bool? overrideIsWin = null)
        {
            // Достаем конфиг для данного типа награды
            if (!_resultConfigs.TryGetValue(type, out var config))
            {
                Debug.LogError($"[Wheel] No result config found for type: {type}");
                return new();
            }

            string gameId = _isNeonWheel ? GameConstants.GAME_NEON_WHEEL : GameConstants.GAME_WHEEL_OF_REVOLUT;
            bool arcadePlayed = GameServices.PlayedAcradesService.IsArcadePlayed(gameId);

            // Если передали overrideIsWin (как для Sector), используем его, иначе берем из конфига
            bool finalIsWin = overrideIsWin ?? config.IsWin;

            GameResult result = new GameResult(
                isWin: finalIsWin,
                rewardCoins: rewardCoins,
                rewardXP: config.BaseXP,
                questTag: string.Empty,
                gameId: gameId,
                arcadePlayed: arcadePlayed
            );

            GameServices.GameCompletionHandler.HandleGameResult(result);

            if (config.ReportAnalytics)
            {
                if (finalIsWin)
                    AnalyticsService.Instance.ReportGameWin(gameId);
                else
                    AnalyticsService.Instance.ReportGameLoss(gameId);
            }

            return result;
        }

        private void HandleSectorButtonClick(int sectorValue)
        {
            if (!GameServices.EnergyService.HasEnoughEnergy || !GameServices.EconomyService.HasEnoughBalance(sectorValue))
            {
                Debug.Log("Не хватает энергии");
                return;
            }

            _selectedSector = sectorValue;
            GameServices.EconomyService.SpendCoins(_selectedSector);
            PrepareAndStartSpin(ClaimWithoutAd);
        }

        public TimeSpan GetRemainingCooldown()
        {
            if (IsAvailable())
                return TimeSpan.Zero;

            return _nextAvailableUtc - DateTimeOffset.UtcNow;
        }

        private void StartCooldownUpdater()
        {
            if (_cooldownText == null) 
                return;
            if (_cooldownCoroutine != null) 
                StopCoroutine(_cooldownCoroutine);
            _cooldownCoroutine = StartCoroutine(CooldownRoutine());
        }

        private void StopCooldownUpdater()
        {
            if (_cooldownCoroutine != null)
            {
                StopCoroutine(_cooldownCoroutine);
                _cooldownCoroutine = null;
            }
        }

        private IEnumerator CooldownRoutine()
        {
            while (true)
            {
                UpdateCooldownLabel();
                yield return new WaitForSeconds(1f);
            }
        }

        private void UpdateCooldownLabel()
        {
            if (_cooldownText == null) 
                return;

            var remaining = GetRemainingCooldown();
            _cooldownText.text = FormatTimeSpan(remaining);
        }

        private static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts <= TimeSpan.Zero)
                return "00:00:00";

            int hours = (int)ts.TotalHours;
            return $"{hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}