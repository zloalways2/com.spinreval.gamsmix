using System.Collections.Generic;

namespace Core
{
    public static class GameConstants
    {
        #region Main Scene Names
        public const string MAIN_MENU = "Main_Menu";
        public const string LEADERBOARD = "Leaderboard";
        public const string QUESTS = "Quests";
        public const string PROFILE = "Profile";
        public const string SETTINGS = "Settings";
        public const string ACHIEVEMENTS = "Achievements";
        #endregion

        #region Game Scene Names
        public const string GAME_REELS = "Game_Reels";
        public const string GAME_VAULT = "Game_Vault";
        public const string GAME_NEON_WHEEL = "Game_Neon_Wheel";
        public const string GAME_CYBER_MASTER = "Game_Cyber_Master";
        public const string GAME_CRYPTO_VIBE = "Game_Crypto_Vibe";
        public const string GAME_DIAMOND_RETRO = "Game_Diamond_Retro";
        public const string GAME_WHEEL_OF_REVOLUT = "Game_Wheel_Of_Revolut";
        public const string GAME_PLINKO_VIBE = "Game_Plinko_Vibe";
        public const string GAME_INFINITE_SCORE = "Game_Infinite_Score";
        public const string GAME_ELECTRIC_DICE = "Game_Electric_Dice";
        #endregion

        #region Settings Prefs
        public const string KEY_NOTIFICATIONS = "Notifications";
        public const string KEY_VIBRATIONS = "Vibrations";
        #endregion

        #region Player Prefs
        public const string KEY_HAS_PROFILE = "Has_Profile";
        public const string KEY_PLAYER_DATA = "Player_Data_JSON";
        public const string KEY_DAILY_FREE_BONUS_CLAIMED = "Daily_Free_Bonus_Claimed";
        public const string KEY_SETTINGS = "Settings_JSON";
        public const string KEY_LAST_DAILY_DATE = "Last_Daily_Date";
        public const string KEY_LAST_DAILY_BONUS_CLAIM = "Last_Daily_Bonus_Claim";
        #endregion

        #region Economy & Limits
        public const float INITIAL_COINS = 1000f;
        public const int INITIAL_ENERGY = 50;
        public const int MAX_ENERGY = 50;
        public const float ENERGY_REGEN_MINUTES = 30f;
        public const int ENERGY_FOR_GAME = 1;
        #endregion

        #region Quest Tags
        public const string TAG_SPIN_10_REELS = "SPIN_10_REELS";
        public const string TAG_WIN_3_GAMES = "WIN_3_GAMES";
        public const string TAG_COLLECT_5_DIAMONDS = "COLLECT_5_DIAMONDS";
        public const string TAG_TRIGGER_TURBO_BOOST = "TRIGGER_TURBO_BOOST";
        public const string TAG_REACH_10X_MULTIPLIER = "REACH_10X_MULTIPLIER";
        public const string TAG_CLAIM_FREE_ENERGY = "CLAIM_FREE_ENERGY";
        public const string TAG_OPEN_THE_VAULT = "OPEN_THE_VAULT";
        public const string TAG_HIT_21 = "HIT_21";
        public const string TAG_LAUNCH_3_ROCKETS = "LAUNCH_3_ROCKETS";
        public const string TAG_DROP_10_PLINKO_BALLS = "DROP_10_PLINKO_BALLS";
        public const string TAG_SPIN_LUCKY_WHEEL = "SPIN_LUCKY_WHEEL";
        public const string TAG_ROLL_DOUBLE_DICE = "ROLL_DOUBLE_DICE";
        public const string TAG_EARN_2500_RCOINS = "EARN_2500_RCOINS";
        public const string TAG_COMPLETE_5_COMBOS = "COMPLETE_5_COMBOS";
        public const string TAG_UPGRADE_YOUR_LEVEL = "UPGRADE_YOUR_LEVEL";
        public const string TAG_PLAY_EVERY_ARCADE = "PLAY_EVERY_ARCADE";
        #endregion
    }
}