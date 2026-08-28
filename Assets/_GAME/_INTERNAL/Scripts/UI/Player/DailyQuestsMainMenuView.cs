using Core.Data.Quests;
using Core.Services.Quests;
using Core.SO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace UI.Player
{
    public class DailyQuestsMainMenuView : MonoBehaviour
    {
        
        [Header("Views")]
        [SerializeField] private List<DailyQuestView> _views = new();

        [Space(5), Header("View Create Settings")]
        [SerializeField] private DailyQuestView _viewPrefab;
        [SerializeField] private RectTransform _viewsCointainer;

        [Space(5), Header("Request Quests Settings")]
        [SerializeField] private int _questsCount = 3;
        [SerializeField] private QuestSpritesConfig _config;

        [Space(5), Header("Refresh Label Setup")]
        [SerializeField] private TextMeshProUGUI _refreshTimerLabel;

        private readonly WaitForSeconds _requestDelay = new(1f);
        private DailyQuestsService _service;
        private Coroutine _timerCoroutine;

        public void Init(DailyQuestsService service)
        {
            _service = service;
            _service.OnQuestUpdated += HandleUpdatedQuest;

            _service.RequestQuests(_questsCount);
            SetupQuestViews();
            StartTimer();
        }

        private void SetupQuestViews()
        {
            int count = Mathf.Min(_service.RequestedQuests.Count, _questsCount);

            for (int i = 0; i < count; i++)
            {
                var questData = _service.RequestedQuests[i];
                var view = Instantiate(_viewPrefab, _viewsCointainer);
                view.Init(questData, _config);
                _views.Add(view);
            }
        }

        public void Dispose()
        {
            _service.OnQuestUpdated -= HandleUpdatedQuest;

            if (_timerCoroutine != null)
                StopCoroutine(_timerCoroutine);
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
                var timeLeft = _service.GetTimeUntilRefresh();

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

        private void HandleUpdatedQuest(DailyQuest changedQuest)
        {
            var quest = _views.FirstOrDefault(v => v.Data != null && v.Data.Id == changedQuest.Id);

            if (quest != null)
            {
                float progress = Mathf.Clamp01((float)changedQuest.CurrentProgress / changedQuest.TargetProgress);
                quest.UpdateQuestProgress(progress);
            }
        }
    }
}