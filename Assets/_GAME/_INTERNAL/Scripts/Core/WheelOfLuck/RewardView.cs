using UnityEngine;

namespace Core.WheelOfLuck
{
    [RequireComponent(typeof(RectTransform))]
    public class RewardView : MonoBehaviour
    {
        [Header("Reward Info")]
        [SerializeField] private WheelReward _reward;

        public WheelReward Reward => _reward;
    }
}