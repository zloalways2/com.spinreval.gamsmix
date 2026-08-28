using UI.Leaderboard;
using UnityEngine;

namespace Core.Services.LeaderboardSystem
{
    public class LeaderboardEntryPoint : MonoBehaviour
    {
        [SerializeField] private LeaderboardPlaceHolder _placeHolder;

        private void Awake() => _placeHolder.Init(GameServices.Leaderboard, GameServices.AvatarService);
    }
}