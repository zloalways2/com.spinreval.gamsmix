using Core.Data;
using System;
using UnityEngine;

namespace Core.Services.Player
{
    public class PlayerService
    {
        private PlayerData _currentPlayerData;

        public string PlayerName => _currentPlayerData.Name;
        public float PlayerCoins => _currentPlayerData.Coins;
        public float PlayerXP => _currentPlayerData.XP;
        public int PlayerLevel => _currentPlayerData.Level;
        public int PlayerRank => _currentPlayerData.Rank;
        public int PlayerPlayedSeconds => _currentPlayerData.PlayTimeSeconds;
        public int PlayerTotalGames => _currentPlayerData.TotalGames;
        public int PlayerTotalWins => _currentPlayerData.TotalWins;

        public event Action<float, float> OnXPChanged
        {
            add => _currentPlayerData.OnXPChanged += value;
            remove => _currentPlayerData.OnXPChanged -= value;
        }
        public event Action<int> OnLevelChanged
        {
            add => _currentPlayerData.OnLevelChanged += value;
            remove => _currentPlayerData.OnLevelChanged -= value;
        }

        public void Init(PlayerData playerData)
        {
            _currentPlayerData = playerData;
            _currentPlayerData.OnLevelChanged += HandleChangedLevel;
        }

        public void Dispose() => _currentPlayerData.OnLevelChanged -= HandleChangedLevel;

        public void SetName(string name) => _currentPlayerData.Name = name;
        public void AddXP(float amount) => _currentPlayerData.AddXP(Mathf.RoundToInt(amount));
        public void AddEnergy(int amount) => GameServices.EnergyService.AddEnergy(amount);
        public void RequestActualProgressState() => _currentPlayerData.RequestActualProgressState();
        public float GetWinRate() => _currentPlayerData.GetWinRate();

        /// <summary>
        /// Записать результат сыгранной игры (победа/поражение)
        /// </summary>
        public void RecordGameResult(bool isWin)
        {
            _currentPlayerData.TotalGames++;
            if (isWin)
                _currentPlayerData.TotalWins++;
        }

        public Texture2D GetCurrentPlayerAvatar() => _currentPlayerData.CurrentAvatar;

        /// <summary>
        /// Получить прямой доступ к PlayerData для сложных операций
        /// Использовать осторожно, только для внутренних сервисов
        /// </summary>
        internal PlayerData GetData() => _currentPlayerData;

        private void HandleChangedLevel(int newLevel) => GameServices.Quests.ProgressQuest(GameConstants.TAG_UPGRADE_YOUR_LEVEL);
    }
}