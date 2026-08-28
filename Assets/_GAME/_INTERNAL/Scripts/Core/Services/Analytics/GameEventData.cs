using System.Collections.Generic;

namespace Core.Services.Analytics
{
    [System.Serializable]
    public struct GameEventData
    {
        public string EventName;
        public Dictionary<string, object> Parameters;

        public GameEventData(string eventName)
        {
            EventName = eventName;
            Parameters = new Dictionary<string, object>();
        }
    }

    [System.Serializable]
    public struct GameCycleEvent
    {
        public string Action;
        public string GameName;

        public GameCycleEvent(string action, string gameName = null)
        {
            Action = action;
            GameName = gameName;
        }
    }

    [System.Serializable]
    public struct PaywallEvent
    {
        public string Action;
        public string Source;

        public PaywallEvent(string action, string source)
        {
            Action = action;
            Source = source;
        }
    }

    [System.Serializable]
    public struct PurchaseEvent
    {
        public string Action;
        public string ItemId;
        public decimal? Price;

        public PurchaseEvent(string action, string itemId, decimal? price = null)
        {
            Action = action;
            ItemId = itemId;
            Price = price;
        }
    }

    [System.Serializable]
    public struct RewardedAdEvent
    {
        public string Action;
        public string Placement;

        public RewardedAdEvent(string action, string placement)
        {
            Action = action;
            Placement = placement;
        }
    }

    [System.Serializable]
    public struct SettingsEvent
    {
        public string Action;

        public SettingsEvent(string action)
        {
            Action = action;
        }
    }

    public static class AnalyticsEventNames
    {
        public const string GAME = "game";
        public const string PAYWALL = "paywall";
        public const string PURCHASE = "purchase";
        public const string REWARDED_AD = "rewarded_ad";
        public const string SETTINGS = "settings";
    }

    public static class AnalyticsActions
    {
        // Game cycle
        public const string START = "start";
        public const string WIN = "win";
        public const string LOSS = "loss";
        public const string BET_CHANGE = "bet_change";

        // Paywall
        public const string VIEW = "view";
        public const string CLOSE = "close";

        // Purchase
        public const string CLICK = "click";
        public const string SUCCESS = "success";
        public const string ERROR = "error";

        // Rewarded ad
        public const string COMPLETE = "complete";
        public const string REWARD = "reward";

        // Settings
        public const string OPEN = "open";
    }

    public static class AnalyticsSources
    {
        public const string ONBOARDING = "onboarding";
        public const string SHOP = "shop";
    }

    public static class AnalyticsPlacement
    {
        public const string FREE_COINS = "free_coins";
        public const string WHEEL = "wheel";
        public const string WIN_BONUS = "win_bonus";
    }
}