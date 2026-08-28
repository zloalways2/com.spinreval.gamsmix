using UnityEngine;

namespace Core.Services.LeaderboardSystem
{
    [System.Serializable]
    public class LeaderboardEntry
    {
        public Texture2D Avatar;
        public int Rank;
        public string Name;
        public float WithdrawalAmount;
        public bool IsCurrentPlayer;
    }
}