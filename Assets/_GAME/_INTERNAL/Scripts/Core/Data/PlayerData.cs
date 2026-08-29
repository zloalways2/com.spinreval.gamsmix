using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Core.Data
{
    [System.Serializable]
    public class PlayerData
    {
        public string Name;
        public Texture2D CurrentAvatar;
        public string AvatarPath => Path.Combine(Application.persistentDataPath, "avatar.png");

        public float Coins;
        public int Energy;
        public DateTime LastEnergyUpdate;
        public DateTime? LastFreeEnergyTime;
        public DateTime? LastDailyBonusTime;

        public int Level;
        public int Rank;
        public float XP;
        public float RequiredXP;
        public float WithdrawalAmount;
        public int TotalWins;
        public int TotalGames;
        public int PlayTimeSeconds;

        public List<string> CompletedQuests;
        public Dictionary<string, bool> PlayedArcades;
        public Dictionary<string, FavoriteGameData> FavoriteGames;
        public Dictionary<string, int> DailyQuestProgress;

        public event Action<float, float> OnXPChanged;
        public event Action<int> OnLevelChanged;

        public PlayerData()
        {
            CompletedQuests = new();
            FavoriteGames = new();
            DailyQuestProgress = new Dictionary<string, int>();
            PlayedArcades = new();

            Coins = GameConstants.INITIAL_COINS;
            Energy = GameConstants.INITIAL_ENERGY;
            LastEnergyUpdate = DateTime.Now;
            LastDailyBonusTime = DateTime.Now;

            Level = 1;
            Rank = 1;
            XP = 0f;
            RequiredXP = 100f;

            TotalWins = 0;
            TotalGames = 0;
            PlayTimeSeconds = 0;
        }

        public void RequestActualProgressState() => OnXPChanged?.Invoke(XP, RequiredXP);

        public void AddXP(int amount)
        {
            XP += amount;
            RequiredXP = Level * 100;

            while (XP >= RequiredXP)
            {
                Level++;
                XP -= RequiredXP;
                RequiredXP = Level * 100;
                OnLevelChanged?.Invoke(Level);
            }

            OnXPChanged?.Invoke(XP, RequiredXP);
        }

        public float GetWinRate()
        {
            if (TotalGames == 0) 
                return 0f;
            return (float)TotalWins / TotalGames * 100f;
        }
    }
}