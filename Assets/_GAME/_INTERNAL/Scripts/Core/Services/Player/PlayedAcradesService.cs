using Core.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Services.Player
{
    public class PlayedAcradesService
    {
        private PlayerData _data;

        private Dictionary<string, bool> _playedAcradesMap;

        public void Init(PlayerData data)
        {
            _data = data;
            _playedAcradesMap = _data.PlayedArcades;

            Debug.Log($"[Played Arcades Service] Init. Loaded entries: {_playedAcradesMap.Count}");
        }

        public void RemoveMap() => _playedAcradesMap.Clear();

        /// <summary>
        /// Проверить, была ли аркада сыграна
        /// </summary>
        public bool IsArcadePlayed(string key)
        {
            if (_playedAcradesMap.TryGetValue(key, out bool value))
                return value;  // true = сыграна, false = не сыграна

            return false;  // Новая игра = не сыграна
        }

        /// <summary>
        /// Проверить, была ли аркада НЕ сыграна
        /// </summary>
        public bool IsArcadeUnplayed(string key)
        {
            return !IsArcadePlayed(key);  // Инверсия логики
        }

        public void AddPlayedArcadeToMap(string arcadeKey)
        {
            if (!_playedAcradesMap.ContainsKey(arcadeKey))
            {
                _playedAcradesMap[arcadeKey] = true;
                Debug.Log($"Arcade {arcadeKey} added to played map: {_playedAcradesMap[arcadeKey]}");
            }
            else
                Debug.LogWarning($"Arcade {arcadeKey} is already existing.");
        }
    }
}