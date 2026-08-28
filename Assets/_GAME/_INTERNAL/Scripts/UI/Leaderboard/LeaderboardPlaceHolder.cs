using Core.Services;
using Core.Services.LeaderboardSystem;
using Core.Services.Player;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI.Leaderboard
{
    public class LeaderboardPlaceHolder : MonoBehaviour
    {
        [Header("Place View Prefab")]
        [SerializeField] private List<LeaderboardPlaceView> _top3Prefabs = new();
        [SerializeField] private LeaderboardPlaceView _placePrefab;
        [SerializeField] private RectTransform _container;

        [Space(5), Header("Player Place View")]
        [SerializeField] private LeaderboardPlaceView _playerPlace;

        private readonly List<LeaderboardPlaceView> _top3Places = new();
        private readonly List<LeaderboardPlaceView> _otherPlaces = new();

        private LeaderboardService _leaderboardService;
        private AvatarService _avatarService;

        private void OnDestroy()
        {
            DOTween.KillAll();
            _avatarService.OnAvatarSetted -= HandleSettedAvatar;
        }

        public void Init(LeaderboardService leaderboardService, AvatarService avatarService)
        {
            _leaderboardService = leaderboardService;
            _avatarService = avatarService;

            _avatarService.OnAvatarSetted += HandleSettedAvatar;

            InitPlayerPlace();
            InitTop3Places();
            InitOtherPlaces();
        }

        private void InitPlayerPlace()
        {
            var playerEntry = _leaderboardService.Leaderboard.FirstOrDefault(player => player.IsCurrentPlayer);
            if(playerEntry == null)
            {
                Debug.LogError($"[Leaderboard Place Holder] Player entry not found!");
                return;
            }

            _playerPlace.Init(playerEntry);
        }

        private void InitTop3Places()
        {
            var top3Places = _leaderboardService.GetTop(3);

            for (int i = 0; i < top3Places.Count; i++)
            {
                var entry = top3Places[i];
                var view = Instantiate(_top3Prefabs[i], _container);
                view.Init(entry);
                _top3Places.Add(view);
            }
        }

        private void InitOtherPlaces()
        {
            var otherPlaces = _leaderboardService.GetOtherPlayers();

            for (int i = 0; i < otherPlaces.Count; i++)
            {
                var entry = otherPlaces[i];
                var view = Instantiate(_placePrefab, _container);
                view.Init(entry);
                _otherPlaces.Add(view);
            }
        }

        private void HandleSettedAvatar(Texture2D texture)
        {
            _leaderboardService.UpdatePlayerInfo(texture, GameServices.PlayerService.PlayerName);

            var top = _leaderboardService.GetTop(3);
            for (int i = 0; i < _top3Places.Count && i < top.Count; i++)
                _top3Places[i].Init(top[i]);

            var other = _leaderboardService.GetOtherPlayers();
            for (int i = 0; i < _otherPlaces.Count && i < other.Count; i++)
                _otherPlaces[i].Init(other[i]);
        }
    }
}