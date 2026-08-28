using Core.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Services.Player
{
    public class FavoriteGamesService
    {
        private PlayerData _playerData;

        private readonly List<FavoriteGameData> _topFavoriteGames = new();

        public void Init(PlayerData data) => _playerData = data;

        /// <summary>
        /// Отметить игру как сыгранную (увеличить счётчик игр)
        /// </summary>
        public void RecordGamePlay(string gameId)
        {
            if (string.IsNullOrEmpty(gameId))
            {
                Debug.LogWarning("[FavoriteGames] GameId is null or empty");
                return;
            }

            if (_playerData.FavoriteGames.TryGetValue(gameId, out var gameData))
            {
                gameData.TotalPlayed++;
            }
            else
            {
                _playerData.FavoriteGames[gameId] = new FavoriteGameData(gameId, 1);
            }
        }

        public List<FavoriteGameData> GetTopFavoriteGames(int count)
        {
            if (_playerData.FavoriteGames.Count == 0)
                return new List<FavoriteGameData>();

            var sortedGames = _playerData.FavoriteGames.Values
                .OrderByDescending(g => g.TotalPlayed)
                .Take(count)
                .ToList();

            return sortedGames;
        }
    }
}