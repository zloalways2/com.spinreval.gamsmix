using Core.Data;
using Core.Services;
using Core.SO;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI.Player
{
    public class FavoriteAcradesView : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private int _topCount = 3;
        [SerializeField] private List<FavoriteArcadeSlot> _slots = new();
        [SerializeField] private FavoriteGamesSpritesConfig _spritesConfig;

        private void Awake()
        {
            if (_spritesConfig == null)
                Debug.LogWarning("[FavoriteArcadesView] spritesConfig is not assigned!");

            if (_slots.Count == 0)
                Debug.LogWarning("[FavoriteArcadesView] No slots assigned!");
        }

        private void OnEnable()
        {
            RefreshFavoriteArcades();
        }

        /// <summary>
        /// Обновить отображение любимых аркад
        /// </summary>
        public void RefreshFavoriteArcades()
        {
            var favoriteGames = GameServices.FavoriteGamesService?.GetTopFavoriteGames(_topCount);

            if (favoriteGames == null || favoriteGames.Count == 0)
            {
                for (int i = 0; i < _slots.Count; i++)
                    _slots[i].gameObject.SetActive(false);

                return;
            }

            FillSlots(favoriteGames);
        }

        private void FillSlots(List<FavoriteGameData> games)
        {
            int count = Mathf.Min(games.Count, _slots.Count);

            for (int i = 0; i < count; i++)
            {
                var gameData = games[i];
                var slot = _slots[i];

                slot.SetData(
                    GetArcadeSprite(gameData.GameId),
                    gameData.TotalPlayed);

                if (gameData.TotalPlayed == 0)
                    slot.gameObject.SetActive(false);
                else
                    slot.gameObject.SetActive(true);
            }

            // Скрыть неиспользованные слоты
            for (int i = count; i < _slots.Count; i++)
                _slots[i].gameObject.SetActive(false);
        }

        private Sprite GetArcadeSprite(string gameId)
        {
            if (_spritesConfig == null)
            {
                Debug.LogWarning("[FavoriteArcadesView] spritesConfig is null!");
                return null;
            }

            return _spritesConfig.GetSprite(gameId);
        }

#if UNITY_EDITOR
        [ContextMenu("Refresh")]
        private void Editor_Refresh()
        {
            RefreshFavoriteArcades();
        }
#endif
    }
}