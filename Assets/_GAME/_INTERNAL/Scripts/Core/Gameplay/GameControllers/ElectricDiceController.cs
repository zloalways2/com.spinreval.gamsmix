using Core.Common;
using Core.Data;
using Core.Services;
using Core.Services.Analytics;
using Cysharp.Threading.Tasks;
using UI.ElectricDice;
using UnityEngine;

namespace Core.Gameplay.GameControllers
{
    public class ElectricDiceController : GameController
    {
        [SerializeField] private ElectricDiceView _view;

        [Header("Settings")]
        [SerializeField] private float _bet = 100f;
        [SerializeField, Range(0.5f, 0.98f)] private float _rtp = 0.85f;

        private enum Condition { Less = 0, Equal = 1, More = 2 }

        private int _targetSum = 7;
        private Condition _currentCondition = Condition.Less;
        private bool _isRolling;

        // Вероятности сумм 2d6 (количество комбинаций из 36)
        private static readonly int[] SumCombinations = { 0, 0, 1, 2, 3, 4, 5, 6, 5, 4, 3, 2, 1 };
        // Кумулятивные комбинации sum <= T
        private static readonly int[] CumulativeLE = { 0, 0, 1, 3, 6, 10, 15, 21, 26, 30, 33, 35, 36 };

        public override void Enter()
        {
            AnalyticsService.Instance.ReportGameStart(GameConstants.GAME_ELECTRIC_DICE);

            _isRolling = false;
            _targetSum = 7;
            _currentCondition = Condition.Less;

            _view.Init(_targetSum);
            UpdateAllUI();
        }

        public override void Initialize()
        {
            GameServices.EconomyService.OnCoinsBalanceChanged += HandleBalanceChanged;

            _view.OnDecreaseTargetClicked += HandleDecreaseTarget;
            _view.OnIncreaseTargetClicked += HandleIncreaseTarget;
            _view.OnConditionChanged += HandleConditionChanged;
            _view.OnSpinButtonClicked += HandleSpinButtonClick;
        }

        public override void Exit()
        {
            GameServices.EconomyService.OnCoinsBalanceChanged -= HandleBalanceChanged;

            _view.OnDecreaseTargetClicked -= HandleDecreaseTarget;
            _view.OnIncreaseTargetClicked -= HandleIncreaseTarget;
            _view.OnConditionChanged -= HandleConditionChanged;
            _view.OnSpinButtonClicked -= HandleSpinButtonClick;
        }

        private void ResolveOutcome(int die1, int die2)
        {
            int sum = die1 + die2;
            bool isDouble = die1 == die2;
            string questTag = isDouble ? GameConstants.TAG_ROLL_DOUBLE_DICE : null;

            bool isWin = false;
            bool isAlreadyPlayed = GameServices.PlayedAcradesService.IsArcadePlayed(GameConstants.GAME_ELECTRIC_DICE);

            if (_currentCondition == Condition.Less && sum < _targetSum)
                isWin = true;
            else if (_currentCondition == Condition.Equal && sum == _targetSum)
                isWin = true;
            else if (_currentCondition == Condition.More && sum > _targetSum)
                isWin = true;

            float reward = 0f;
            int xp = 10;

            if (isWin)
            {
                reward = CalculateReward();
                xp = 30;
            }

            var result = new GameResult(
                isWin: isWin,
                rewardCoins: reward,
                rewardXP: xp,
                questTag: questTag,
                gameId: GameConstants.GAME_ELECTRIC_DICE,
                arcadePlayed: isAlreadyPlayed);

            GameServices.GameCompletionHandler.HandleGameResult(result);
            RecordArcadePlay(GameConstants.GAME_ELECTRIC_DICE);

            _view.ShowResultPanel(isWin, sum, Mathf.RoundToInt(reward));
        }

        private int GetFavorableCombinations()
        {
            if (_currentCondition == Condition.Less)
                return CumulativeLE[_targetSum - 1]; // sum < T → sum <= T-1
            else if (_currentCondition == Condition.Equal)
                return SumCombinations[_targetSum];
            else // More
                return 36 - CumulativeLE[_targetSum]; // sum > T
        }

        private float CalculateReward()
        {
            int favorableCombos = GetFavorableCombinations();
            if (favorableCombos <= 0) 
                return 0f;

            float pWin = favorableCombos / 36f;
            float raw = _bet * (1f / pWin) * _rtp;
            return Mathf.Max(raw, _bet); // clamp: не меньше ставки
        }

        private void UpdateAllUI()
        {
            _view.UpdateTargetNumber(_targetSum);
            _view.UpdateConditionLabel((int)_currentCondition);
            UpdateSpinButton();
        }

        private void UpdateSpinButton()
        {
            bool canSpin = !_isRolling
                && GetFavorableCombinations() > 0
                && GameServices.EconomyService.HasEnoughBalance(_bet);
            _view.UpdateSpinButton(canSpin);
        }

        private void HandleDecreaseTarget()
        {
            if (_isRolling)
                return;

            _targetSum = Mathf.Max(2, _targetSum - 1);
            _view.UpdateTargetNumber(_targetSum);
            UpdateSpinButton();
        }

        private void HandleIncreaseTarget()
        {
            if (_isRolling)
                return;

            _targetSum = Mathf.Min(12, _targetSum + 1);
            _view.UpdateTargetNumber(_targetSum);
            UpdateSpinButton();
        }

        private void HandleConditionChanged(int condition)
        {
            if (_isRolling)
                return;

            _currentCondition = (Condition)condition;
            _view.UpdateConditionLabel(condition);
            UpdateSpinButton();
        }

        private async UniTaskVoid HandleSpinClickAsync()
        {
            if (_isRolling)
                return;

            if (!base.SpendEnergy())
            {
                _view.ShowWarningMessage("Not enough energy!", $"You don't have enough energy ({5}) for this game.");
                return;
            }

            if (!GameServices.EconomyService.HasEnoughBalance(_bet))
            {
                _view.ShowWarningMessage("Not enough coins!", $"You don't have enough coins ({_bet}) for this bet.");
                return;
            }

            // Проверка на невалидные ставки (например, "Меньше 2" выиграть невозможно)
            if (GetFavorableCombinations() <= 0)
            {
                _view.ShowWarningMessage("An impossible outcome!", "It is impossible to win with the current conditions.");
                return;
            }

            GameServices.EconomyService.SpendCoins(_bet);

            _isRolling = true;
            _view.SetControlsInteractable(false);

            int die1Value = Random.Range(1, 7);
            int die2Value = Random.Range(1, 7);

            await _view.RollDiceAsync(die1Value, die2Value);

            ResolveOutcome(die1Value, die2Value);

            _isRolling = false;
            _view.SetControlsInteractable(true);
            UpdateSpinButton();
        }

        private void HandleSpinButtonClick()
        {
            HandleSpinClickAsync().Forget();
            base.SpendEnergy();
        }

        private void HandleBalanceChanged(float balance)
        {
            UpdateSpinButton();
        }
    }
}