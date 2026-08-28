using Core.Data.Quests;
using Core.Services.Quests;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI.Player
{
    public class DailyQuestsUIView : MonoBehaviour
    {
        [SerializeField] private DailyQuestViewsHolder _viewsHolder;
        [SerializeField] private TextMeshProUGUI _refreshTimerLabel;

        private DailyQuestsService _dailyQuestsService;
        private Coroutine _timerCoroutine;
        private readonly WaitForSeconds _requestDelay = new(1f);

        private void OnDestroy()
        {
            _dailyQuestsService.OnQuestsUpdated -= HandleUpdatedQuests;

            if (_timerCoroutine != null)
                StopCoroutine(_timerCoroutine);
        }

        public void Init(DailyQuestsService dailyQuestsService)
        {
            _dailyQuestsService = dailyQuestsService;
            _viewsHolder.Init(_dailyQuestsService);

            _dailyQuestsService.OnQuestsUpdated += HandleUpdatedQuests;
        }

        public void SetupViewsHolder()
        {
            _viewsHolder.SetupQuestViews();
            StartTimer();
        }

        private void StartTimer()
        {
            if (_timerCoroutine != null) 
                StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(UpdateTimerRoutine());
        }

        private IEnumerator UpdateTimerRoutine()
        {
            while (true)
            {
                // Опрос API сервиса
                var timeLeft = _dailyQuestsService.GetTimeUntilRefresh();

                if (_refreshTimerLabel != null)
                    _refreshTimerLabel.text = FormatTime(timeLeft);

                yield return _requestDelay;
            }
        }

        private string FormatTime(TimeSpan ts)
        {
            // Если по какой-то причине время отрицательное, показываем нули
            if (ts <= TimeSpan.Zero) 
                return "00:00:00";
            return $"Refresh in: {ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        private void HandleUpdatedQuests(List<DailyQuest> quests)
        {
            // Автообновление квестов
            _viewsHolder.SetupQuestViews();
        }
    }
}