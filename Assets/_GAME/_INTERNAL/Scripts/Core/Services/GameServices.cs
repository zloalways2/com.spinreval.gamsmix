using Core.Gameplay;
using Core.Services.LeaderboardSystem;
using Core.Services.Player;
using Core.Services.Quests;
using Core.Services.SaveSystem;
using Core.SO;
using Core.SO.Common;
using System;
using UnityEngine;

namespace Core.Services
{
    public static class GameServices
    {
        private static DebugConfig _debugConfig;

        public static PlayerService PlayerService { get; private set; }
        public static EnergyService EnergyService { get; private set; }
        public static GameCompletionHandler GameCompletionHandler { get; private set; }
        public static SaveService SaveService { get; private set; }
        public static EconomyService EconomyService { get; private set; }
        public static DailyQuestsService Quests { get; private set; }
        public static LeaderboardService Leaderboard { get; private set; }
        public static AvatarService AvatarService { get; private set; }
        public static FavoriteGamesService FavoriteGamesService { get; private set; }
        public static PlayedAcradesService PlayedAcradesService {  get; private set; }
        public static QuestRouter QuestRouter { get; private set; }

        public static void SetDebugConfig(DebugConfig config) => _debugConfig = config;

        public static void InitializeAll()
        {
            SaveService = new();
            SaveService.Init(_debugConfig.IsDebug);

            PlayerService = new();
            PlayerService.Init(SaveService.PlayerData);

            EconomyService = new();
            EconomyService.Init(PlayerService.PlayerCoins);

            PlayedAcradesService = new();
            PlayedAcradesService.Init(PlayerService.GetData());

            Quests = new DailyQuestsService();
            Quests.Init(PlayerService.GetData(), PlayedAcradesService, EconomyService, PlayerService);

            EnergyService = new(() => SaveService.SavePlayerData(), () => Quests.ProgressQuest(GameConstants.TAG_CLAIM_FREE_ENERGY));
            EnergyService.Init(PlayerService.GetData());

            AvatarService = new(PlayerService.GetData());

            Leaderboard = new LeaderboardService();
            Leaderboard.Init(PlayerService.GetData());

            FavoriteGamesService = new();
            FavoriteGamesService.Init(PlayerService.GetData());

            QuestRouter = new();
            QuestRouter.Init(PlayedAcradesService);

            GameCompletionHandler = new(EconomyService, PlayerService, Quests, () => SaveService.SavePlayerData());
        }

        public static void SaveAll()
        {
            SaveService.PlayerData.Coins = EconomyService.GetCoinsBalance();
            SaveService.SavePlayerData();
        }

        public static void Dispose() => PlayerService.Dispose();
    }
}