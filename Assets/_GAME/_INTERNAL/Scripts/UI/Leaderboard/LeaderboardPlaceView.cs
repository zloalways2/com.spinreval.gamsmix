using Core.Services.LeaderboardSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Leaderboard
{
    public class LeaderboardPlaceView : MonoBehaviour
    {
        [Header("View Setup")]
        [SerializeField] private TextMeshProUGUI _playerNameLabel;
        [SerializeField] private TextMeshProUGUI _rankLabel;
        [SerializeField] private TextMeshProUGUI _withdrawlAmountLabel;
        [SerializeField] private RawImage _avatarImage;

        public void Init(LeaderboardEntry entry)
        {
            _avatarImage.texture = entry.Avatar;
            _playerNameLabel.text = entry.Name;
            _rankLabel.text = $"#{entry.Rank}";
            _withdrawlAmountLabel.text = $"${entry.WithdrawalAmount:N0}";
        }
    }
}