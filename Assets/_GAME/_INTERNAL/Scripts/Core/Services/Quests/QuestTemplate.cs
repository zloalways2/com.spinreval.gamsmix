using UnityEngine;

namespace Core.Services.Quests
{
    [System.Serializable]
    public class QuestTemplate
    {
        public string Id;
        public string Description;
        public string Tag;
        public int TargetValue;
        public int RewardCoins;
        public int RewardXP;

        public QuestTemplate(string id, string description, string tag, int target, int coins, int xp)
        {
            Id = id;
            Description = description;
            Tag = tag;
            TargetValue = target;
            RewardCoins = coins;
            RewardXP = xp;
        }
    }
}