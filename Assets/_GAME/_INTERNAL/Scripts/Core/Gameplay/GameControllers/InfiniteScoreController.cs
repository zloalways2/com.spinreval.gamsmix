using Core.Common;
using Core.Data;
using Core.Data.Cyber21;
using Core.Services;
using Core.Services.Analytics;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UI.InfiniteScore;
using UnityEngine;

namespace Core.Gameplay.GameControllers
{
    public class InfiniteScoreController : GameController
    {
        [SerializeField] private InfiniteScoreView _view;
        [SerializeField] private List<CardData> _deckCards;

        [Header("Settings")]
        [SerializeField] private float _bet = 100f;
        [SerializeField] private int _cardsPerTeam = 3;
        [SerializeField] private float _rewardMultiplier = 2f; // 2x = возврат ставки + 100% прибыли

        private bool _isDealing;

        public override void Enter()
        {
            AnalyticsService.Instance.ReportGameStart(GameConstants.GAME_INFINITE_SCORE);

            if (_deckCards == null || _deckCards.Count == 0)
            {
                Debug.LogError("[InfiniteScore] Deck is empty!");
                return;
            }

            _isDealing = false;
            _view.Init();
            UpdateButtonsState();
        }

        public override void Initialize()
        {
            GameServices.EconomyService.OnCoinsBalanceChanged += HandleBalanceChanged;
            _view.OnOrangeBetClicked += HandleOrangeBetClick;
            _view.OnRedBetClicked += HandleRedBetClick;
            _view.OnRestartGameClicked += HandleRestartGameButtonClick;
            _view.OnResultsPanelOpened += HandleOpenedResultsPanel;
        }

        public override void Exit()
        {
            GameServices.EconomyService.OnCoinsBalanceChanged -= HandleBalanceChanged;
            _view.OnOrangeBetClicked -= HandleOrangeBetClick;
            _view.OnRedBetClicked -= HandleRedBetClick;
            _view.OnRestartGameClicked -= HandleRestartGameButtonClick;
            _view.OnResultsPanelOpened -= HandleOpenedResultsPanel;
        }

        private async UniTaskVoid HandleBet(int team)
        {
            if (_isDealing) 
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

            GameServices.EconomyService.SpendCoins(_bet);
            _isDealing = true;
            _view.SetButtonsInteractable(false);

            // Генерируем карты
            List<CardData> orangeCards = DrawCards(_cardsPerTeam);
            List<CardData> redCards = DrawCards(_cardsPerTeam);

            // Анимация раздачи
            await _view.DealCardsAsync(orangeCards, redCards);

            // Подсчет очков
            int orangeScore = CalculateScore(orangeCards);
            int redScore = CalculateScore(redCards);

            // Определение победителя
            int winningTeam = -1; // -1 = ничья
            if (orangeScore > redScore) 
                winningTeam = 0;
            else if (redScore > orangeScore) 
                winningTeam = 1;

            int winScore = Mathf.Max(orangeScore, redScore);

            _view.ShowWinner(winningTeam);

            ResolveOutcome(team, winningTeam, winScore);

            _isDealing = false;
        }

        private void ResolveOutcome(int betTeam, int winningTeam, int score)
        {
            // Ничья
            if (winningTeam == -1)
            {
                GameServices.EconomyService.AddCoins(_bet); // Возврат ставки
                _view.ShowResultPanel(false, _bet, score, true).Forget();
                return;
            }

            bool isWin = betTeam == winningTeam;
            bool isAlreadyPlayed = GameServices.PlayedAcradesService.IsArcadePlayed(GameConstants.GAME_INFINITE_SCORE);
            float reward = isWin ? _bet * _rewardMultiplier : 0f;
            int xp = isWin ? 40 : 10;

            string questTag = isWin ? GameConstants.TAG_COMPLETE_5_COMBOS : null;

            var result = new GameResult(
                isWin: isWin,
                rewardCoins: reward,
                rewardXP: xp,
                questTag: questTag,
                gameId: GameConstants.GAME_INFINITE_SCORE,
                arcadePlayed: isAlreadyPlayed);

            GameServices.GameCompletionHandler.HandleGameResult(result);
            RecordArcadePlay(GameConstants.GAME_INFINITE_SCORE);

            _view.SetButtonsInteractable(false);
            _view.ShowResultPanel(isWin, reward, score).Forget();
        }

        private List<CardData> DrawCards(int count)
        {
            List<CardData> hand = new();
            for (int i = 0; i < count; i++)
                hand.Add(_deckCards[Random.Range(0, _deckCards.Count)]);
            return hand;
        }

        private int CalculateScore(List<CardData> cards)
        {
            int sum = 0;
            foreach (var card in cards)
                sum += card.CardValue;

            return sum;
        }

        private void UpdateButtonsState()
        {
            bool canInteract = !_isDealing && GameServices.EconomyService.HasEnoughBalance(_bet);
            _view.SetButtonsInteractable(canInteract);
        }

        private void HandleRestartGameButtonClick() => _view.SetButtonsInteractable(true);

        private void HandleRedBetClick() => HandleBet(1).Forget();

        private void HandleOrangeBetClick() => HandleBet(0).Forget();

        private void HandleOpenedResultsPanel() => _view.SetButtonsInteractable(false);

        private void HandleBalanceChanged(float balance) => UpdateButtonsState();
    }
}