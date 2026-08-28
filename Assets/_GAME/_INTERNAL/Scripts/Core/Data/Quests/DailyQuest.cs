namespace Core.Data.Quests
{
    [System.Serializable]
    public class DailyQuest
    {
        public string Id;
        public string Description;
        public string QuestTag;
        public int TargetProgress;
        public int CurrentProgress;
        public int RewardCoins;
        public int RewardXP;
        public bool IsCompleted;
        public bool IsClaimed;
    }
}