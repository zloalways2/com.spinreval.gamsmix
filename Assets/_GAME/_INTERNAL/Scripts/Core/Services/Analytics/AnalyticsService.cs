using Io.AppMetrica;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Services.Analytics
{
    public class AnalyticsService : MonoBehaviour
    {
        private static AnalyticsService _instance;
        public static AnalyticsService Instance => _instance;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
                Destroy(gameObject);
        }

        // ===== GAME CYCLE EVENTS =====

        /// <summary>
        /// Событие начала игры
        /// </summary>
        public void ReportGameStart(string gameName = null)
        {
            var @event = new GameCycleEvent(AnalyticsActions.START, gameName);
            ReportGameEvent(@event);
        }

        /// <summary>
        /// Событие выигрыша
        /// </summary>
        public void ReportGameWin(string gameName = null)
        {
            var @event = new GameCycleEvent(AnalyticsActions.WIN, gameName);
            ReportGameEvent(@event);
        }

        /// <summary>
        /// Событие проигрыша
        /// </summary>
        public void ReportGameLoss(string gameName = null)
        {
            var @event = new GameCycleEvent(AnalyticsActions.LOSS, gameName);
            ReportGameEvent(@event);
        }

        /// <summary>
        /// Событие изменения ставки пользователем
        /// </summary>
        public void ReportBetChange(string gameName = null)
        {
            var @event = new GameCycleEvent(AnalyticsActions.BET_CHANGE, gameName);
            ReportGameEvent(@event);
        }

        private void ReportGameEvent(GameCycleEvent gameEvent)
        {
            var parameters = new Dictionary<string, object>
            {
                { "action", gameEvent.Action }
            };

            if (!string.IsNullOrEmpty(gameEvent.GameName))
            {
                parameters.Add("game", gameEvent.GameName);
            }

            ReportEvent(AnalyticsEventNames.GAME, parameters);
        }

        // ===== PAYWALL EVENTS =====

        /// <summary>
        /// Событие показа пейволла
        /// </summary>
        public void ReportPaywallView(string source)
        {
            var @event = new PaywallEvent(AnalyticsActions.VIEW, source);
            ReportPaywallEvent(@event);
        }

        /// <summary>
        /// Событие закрытия пейволла
        /// </summary>
        public void ReportPaywallClose(string source)
        {
            var @event = new PaywallEvent(AnalyticsActions.CLOSE, source);
            ReportPaywallEvent(@event);
        }

        private void ReportPaywallEvent(PaywallEvent paywallEvent)
        {
            var parameters = new Dictionary<string, object>
            {
                { "action", paywallEvent.Action },
                { "source", paywallEvent.Source }
            };

            ReportEvent(AnalyticsEventNames.PAYWALL, parameters);
        }

        // ===== PURCHASE EVENTS =====

        /// <summary>
        /// Событие клика на покупку
        /// </summary>
        public void ReportPurchaseClick(string itemId)
        {
            var @event = new PurchaseEvent(AnalyticsActions.CLICK, itemId);
            ReportPurchaseEvent(@event);
        }

        /// <summary>
        /// Событие успешной покупки
        /// </summary>
        public void ReportPurchaseSuccess(string itemId, decimal price)
        {
            var @event = new PurchaseEvent(AnalyticsActions.SUCCESS, itemId, price);
            ReportPurchaseEvent(@event);
        }

        /// <summary>
        /// Событие ошибки покупки
        /// </summary>
        public void ReportPurchaseError(string itemId)
        {
            var @event = new PurchaseEvent(AnalyticsActions.ERROR, itemId);
            ReportPurchaseEvent(@event);
        }

        private void ReportPurchaseEvent(PurchaseEvent purchaseEvent)
        {
            var parameters = new Dictionary<string, object>
            {
                { "action", purchaseEvent.Action },
                { "item_id", purchaseEvent.ItemId }
            };

            if (purchaseEvent.Price.HasValue)
            {
                parameters.Add("price", purchaseEvent.Price.Value);
            }

            ReportEvent(AnalyticsEventNames.PURCHASE, parameters);
        }

        // ===== REWARDED AD EVENTS =====

        /// <summary>
        /// Событие клика на просмотр рекламы
        /// </summary>
        public void ReportRewardedAdClick(string placement)
        {
            var @event = new RewardedAdEvent(AnalyticsActions.CLICK, placement);
            ReportRewardedAdEvent(@event);
        }

        /// <summary>
        /// Событие завершения просмотра рекламы
        /// </summary>
        public void ReportRewardedAdComplete(string placement)
        {
            var @event = new RewardedAdEvent(AnalyticsActions.COMPLETE, placement);
            ReportRewardedAdEvent(@event);
        }

        /// <summary>
        /// Событие получения награды
        /// </summary>
        public void ReportRewardedAdReward(string placement)
        {
            var @event = new RewardedAdEvent(AnalyticsActions.REWARD, placement);
            ReportRewardedAdEvent(@event);
        }

        private void ReportRewardedAdEvent(RewardedAdEvent rewardedAdEvent)
        {
            var parameters = new Dictionary<string, object>
            {
                { "action", rewardedAdEvent.Action },
                { "placement", rewardedAdEvent.Placement }
            };

            ReportEvent(AnalyticsEventNames.REWARDED_AD, parameters);
        }

        // ===== SETTINGS EVENTS =====

        /// <summary>
        /// Событие открытия настроек
        /// </summary>
        public void ReportSettingsOpen()
        {
            var @event = new SettingsEvent(AnalyticsActions.OPEN);
            var parameters = new Dictionary<string, object>
            {
                { "action", @event.Action }
            };

            ReportEvent(AnalyticsEventNames.SETTINGS, parameters);
        }

        // ===== CORE METHOD =====

        /// <summary>
        /// Основной метод для отправки события в AppMetrica
        /// Преобразует Dictionary параметров в JSON и отправляет в интегрированную аналитику
        /// </summary>
        private void ReportEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("[Analytics] Event name is empty!");
                return;
            }

            try
            {
                string jsonValue = JsonUtility.ToJson(new DictionaryWrapper(parameters));

#if UNITY_EDITOR
                Debug.Log($"[Analytics] Event: {eventName}, Parameters: {jsonValue}");
#endif

                SendAnalyticsEvent(eventName, jsonValue);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Analytics] Error reporting event '{eventName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Отправка события в интегрированную аналитику
        /// </summary>
        private void SendAnalyticsEvent(string eventName, string jsonValue) => AppMetrica.ReportEvent(eventName, jsonValue);

        // ===== HELPER CLASS =====

        /// <summary>
        /// Вспомогательный класс для сериализации Dictionary в JSON
        /// </summary>
        [System.Serializable]
        private class DictionaryWrapper
        {
            public Dictionary<string, object> data;

            public DictionaryWrapper(Dictionary<string, object> dictionary)
            {
                data = dictionary;
            }
        }
    }
}