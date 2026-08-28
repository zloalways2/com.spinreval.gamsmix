using Core.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Services.LeaderboardSystem
{
    public class LeaderboardService
    {
        private PlayerData _playerData;

        private List<LeaderboardEntry> _leaderboard;

        private List<LeaderboardEntry> _top3Places;
        private List<LeaderboardEntry> _otherPlaces;

        public IReadOnlyList<LeaderboardEntry> Leaderboard => _leaderboard.AsReadOnly();

        public void Init(PlayerData data)
        {
            _playerData = data;
            GenerateMockLeaderboard();
        }

        /// <summary>
        /// Сгенерировать мок-таблицу лидеров с позицией игрока
        /// </summary>
        private void GenerateMockLeaderboard()
        {
            _leaderboard = new List<LeaderboardEntry>();

            // Генерируем 20 фейковых игроков с разным уровнем и XP
            var random = new System.Random();
            string[] names = { "Alex", "Maria", "John", "Emma", "David", "Sophie", "Michael", "Olivia",
                              "James", "Isabella", "Robert", "Mia", "William", "Charlotte", "Daniel",
                              "Amelia", "Matthew", "Harper", "Andrew", "Evelyn" };

            for (int i = 0; i < names.Length; i++)
            {
                int level = random.Next(5, 30);
                int xp = random.Next(level * 100, (level + 1) * 100);
                float withdrawalAmount = random.Next(50, 5000);

                _leaderboard.Add(new LeaderboardEntry
                {
                    Avatar = null,
                    Rank = i + 1,
                    Name = names[i],
                    WithdrawalAmount = withdrawalAmount,
                    IsCurrentPlayer = false
                });
            }

            // Добавляем текущего игрока
            _leaderboard.Add(new LeaderboardEntry
            {
                Avatar = _playerData.CurrentAvatar,
                Rank = 0, // Будет пересчитан после сортировки
                Name = _playerData.Name != "" ? _playerData.Name : "Player",
                WithdrawalAmount = _playerData.WithdrawalAmount,
                IsCurrentPlayer = true
            });

            // Сортируем по Withdrawal Amount (убывание)
            _leaderboard.Sort((a, b) => b.WithdrawalAmount.CompareTo(a.WithdrawalAmount));

            // Пересчитываем ранги
            for (int i = 0; i < _leaderboard.Count; i++)
                _leaderboard[i].Rank = i + 1;

            Debug.Log($"[Leaderboard] Generated {_leaderboard.Count} entries. Player position: {GetPlayerPosition()}");
        }

        /// <summary>
        /// Получить позицию текущего игрока
        /// </summary>
        public int GetPlayerPosition()
        {
            for (int i = 0; i < _leaderboard.Count; i++)
            {
                if (_leaderboard[i].IsCurrentPlayer)
                    return i + 1;
            }
            return _leaderboard.Count;
        }

        public void UpdatePlayerInfo(Texture2D texture, string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "Player";

            for (int i = 0; i < _leaderboard.Count; i++)
            {
                if (_leaderboard[i].IsCurrentPlayer)
                {
                    _leaderboard[i].Avatar = texture;
                    _leaderboard[i].Name = name;
                    break;
                }
            }
        }

        /// <summary>
        /// Обновить таблицу после изменения Withdrawal Amount игрока
        /// </summary>
        public void RefreshLeaderboard()
        {
            // Находим запись игрока и обновляем её
            for (int i = 0; i < _leaderboard.Count; i++)
            {
                if (_leaderboard[i].IsCurrentPlayer)
                    _leaderboard[i].WithdrawalAmount = _playerData.TotalWins;
            }

            // Пересортировываем
            _leaderboard.Sort((a, b) => b.WithdrawalAmount.CompareTo(a.WithdrawalAmount));

            // Пересчитываем ранги
            for (int i = 0; i < _leaderboard.Count; i++)
                _leaderboard[i].Rank = i + 1;

            Debug.Log($"[Leaderboard] Refreshed. Player position: {GetPlayerPosition()}");
        }
        

        /// <summary>
        /// Получить всё, что ниже топа игроков
        /// </summary>
        public List<LeaderboardEntry> GetOtherPlayers()
        {
            _otherPlaces ??= new();
            _otherPlaces.Clear();

            int startIndex = _top3Places.Count;

            if (startIndex >= _leaderboard.Count)
                return _otherPlaces;

            _otherPlaces.AddRange(_leaderboard.GetRange(startIndex, _leaderboard.Count - startIndex));

            UpdatePlayerInfo(_playerData.CurrentAvatar, _playerData.Name);

            return _otherPlaces;
        }

        /// <summary>
        /// Получить топ-N игроков
        /// </summary>
        public List<LeaderboardEntry> GetTop(int count)
        {
            _top3Places ??= new();

            _top3Places.Clear();
            _top3Places.AddRange(_leaderboard.GetRange(0, Mathf.Min(count, _leaderboard.Count)));

            return _top3Places;
        }
    }
}