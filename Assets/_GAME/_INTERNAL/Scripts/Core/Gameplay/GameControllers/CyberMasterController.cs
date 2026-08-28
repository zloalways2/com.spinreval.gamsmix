using Core.Common;
using Core.Data;
using Core.Data.Cyber21;
using Core.Services;
using Core.Services.Analytics;
using System.Collections;
using System.Collections.Generic;
using UI.CyberMaster;
using UnityEngine;

namespace Core.Gameplay.GameControllers
{
    public class CyberMasterController : GameController
    {
        private const string ARCADE_KEY = "Cyber_Master_Arcade";

        [Header("Cards Setup")]
        [SerializeField] private List<CardData> _deckCards;

        [Space(5), Header("View Setup")]
        [SerializeField] private CyberMasterView _view;

        [Space(5), Header("Game Settings")]
        [SerializeField] private int _baseBet = 150;
        [SerializeField] private int _perfect21Multiplier = 3; // x3 за ровно 21
        [Tooltip("Задержка между вылетом стартовых карт")]
        [SerializeField] private float _dealStagger = 0.15f;
        [Tooltip("Пауза перед показом результата, чтобы анимация доиграла")]
        [SerializeField] private float _resultShowDelay = 0.9f;

        private List<CardData> _playerHand;
        private bool _isGameActive;
        private int _currentScore;
        
        private int _softAces; // для подсчета тузов (1 или 11)

        public override void Enter()
        {
            AnalyticsService.Instance.ReportGameStart(GameConstants.GAME_CYBER_MASTER);

            _isGameActive = false;

            if (_view != null)
            {
                _view.OnHitButtonClicked += HandleHit;
                _view.OnStandButtonClicked += HandleStand;
                _view.OnRestartButtonClicked += HandleRestart;
                _view.OnStartButtonClicked += HandleStartButtonClick;
            }
        }

        public override void Initialize()
        {
            if (_view != null)
            {
                _view.Init(_deckCards.Count);
                _view.UpdateScore(_currentScore);
                _view.UpdateBet(_baseBet);
                _view.SetButtonsState(_isGameActive);
            }
        }

        public override void Exit()
        {
            if (_view != null)
            {
                _view.OnHitButtonClicked -= HandleHit;
                _view.OnStandButtonClicked -= HandleStand;
                _view.OnRestartButtonClicked -= HandleRestart;
                _view.OnStartButtonClicked -= HandleStartButtonClick;
            }

            _view.Dispose();
        }

        private void ResetGame()
        {
            GameServices.EconomyService.SpendCoins(_baseBet);

            _playerHand = new List<CardData>();
            _currentScore = 0;
            _softAces = 0;
            _isGameActive = false;

            if (_view != null)
            {
                _view.ClearHand();
                _view.UpdateScore(_currentScore);
                _view.UpdateBet(_baseBet);
                _view.SetButtonsState(false);
            }

            // Раздаем 2 стартовые карты
            DealCard();
            DealCard(_dealStagger);

            _isGameActive = true;

            if (_view != null)
                _view.SetButtonsState(_isGameActive);

            // Проверяем, не получили ли мы сразу 21
            CheckForBlackjack();
        }

        private void DealCard(float delay = 0f)
        {
            if (_deckCards == null || _deckCards.Count == 0)
            {
                Debug.LogError("[CyberMaster] Deck is empty!");
                return;
            }

            CardData card = _deckCards[Random.Range(0, _deckCards.Count)];
            _playerHand.Add(card);

            if (card.IsAce)
            {
                _softAces++;
                _currentScore += 11;
            }
            else
                _currentScore += card.CardValue;

            AdjustScoreForAces();

            if (_view != null)
            {
                _view.AddCardToHand(card, delay);
                _view.UpdateScore(_currentScore);
            }

            if (_currentScore == 21)
                EndGame(true, _baseBet * _perfect21Multiplier, true);

            Debug.Log($"[CyberMaster] Dealt card: Value={card.CardValue}, IsAce={card.IsAce}, Total={_currentScore}");
        }

        private void AdjustScoreForAces()
        {
            // Если перебор и есть тузы, считаем их как 1 вместо 11
            while (_currentScore > 21 && _softAces > 0)
            {
                _currentScore -= 10; // 11 -> 1
                _softAces--;
            }
        }

        private void CheckForBlackjack()
        {
            if (_currentScore == 21 && _playerHand.Count == 2)
            {
                // Blackjack! Завершаем игру с супер-бонусом
                EndGame(true, _baseBet * _perfect21Multiplier, true);
            }
        }

        private void EndGame(bool isWin, float reward, bool isBlackjack)
        {
            _isGameActive = false;

            _view.SetStartButtonState(_isGameActive);
            _view.SetButtonsState(_isGameActive);

            // Определяем quest tag
            string questTag = null;
            if (isWin && _currentScore == 21)
                questTag = GameConstants.TAG_HIT_21;

            bool isAlreadyPlayed = GameServices.PlayedAcradesService.IsArcadePlayed(GameConstants.GAME_CYBER_MASTER);

            // Создаем GameResult
            GameResult result = new(
                isWin: isWin,
                rewardCoins: isWin ? reward : 0f,
                rewardXP: isWin ? (isBlackjack ? 100f : 50f) : 10f,
                questTag: questTag,
                gameId: GameConstants.GAME_CYBER_MASTER,
                arcadePlayed: isAlreadyPlayed
            );

            // Отправляем результат в GameCompletionHandler
            GameServices.GameCompletionHandler.HandleGameResult(result);
            RecordArcadePlay(GameConstants.GAME_CYBER_MASTER);

            StartCoroutine(ShowResultRoutine(isWin, reward, isBlackjack));

            Debug.Log($"[CyberMaster] Game ended. Win={isWin}, Score={_currentScore}, Reward={reward}, Blackjack={isBlackjack}");
        }

        private IEnumerator ShowResultRoutine(bool isWin, float reward, bool isBlackjack)
        {
            yield return new WaitForSeconds(_resultShowDelay);

            if (_view == null) 
                yield break;

            _view.ShowResult(isWin, reward, _currentScore, isBlackjack);
        }

        private void HandleHit()
        {
            if (!_isGameActive)
                return;

            DealCard();

            // Проверяем перебор
            if (_currentScore > 21)
                EndGame(false, 0, false);
        }

        private void HandleStand()
        {
            if (!_isGameActive)
                return;

            // Игрок остановился
            bool isWin = _currentScore <= 21;
            float reward = 0f;

            if (isWin)
            {
                // Награда зависит от того, насколько близко к 21
                int distanceFrom21 = 21 - _currentScore;

                if (_currentScore == 21)
                    reward = _baseBet * _perfect21Multiplier;
                else if (distanceFrom21 <= 2)
                    reward = _baseBet * 2f;
                else
                    reward = _baseBet * 1.05f;
            }

            EndGame(isWin, reward, false);
        }

        private void HandleStartButtonClick()
        {
            if (!base.SpendEnergy())
            {
                _view.ShowWarningMessage("Not enough energy!", $"You don't have enough energy ({5}) for this game.");
                return;
            }

            if (!GameServices.EconomyService.HasEnoughBalance(_baseBet))
            {
                _view.ShowWarningMessage("Not enough coins!", $"You don't have enough coins ({_baseBet}) for this bet.");
                return;
            }

            ResetGame();
        }

        private void HandleRestart()
        {
            _view.UpdateScore(0);
            _view.ClearHand();
            _view.SetStartButtonState(true);
        }
    }
}