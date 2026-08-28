using Core.Data.Quests;
using Core.Services.Quests;
using Core.SO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI.Player
{
    public class DailyQuestViewsHolder : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private QuestSpritesConfig _config;

        [Space(5), Header("Views")]
        [SerializeField] private List<DailyQuestView> _views = new();

        [Space(5), Header("View Create Settings")]
        [SerializeField] private DailyQuestView _viewPrefab;
        [SerializeField] private RectTransform _viewsContainer;

        private DailyQuestsService _dailyQuestsService;

        private void OnDestroy() => Dispose();

        public void Init(DailyQuestsService dailyQuestsService)
        {
            _dailyQuestsService = dailyQuestsService;
            _dailyQuestsService.OnQuestUpdated += HandleUpdatedQuest;
        }

        public void Dispose()
        {
            _dailyQuestsService.OnQuestUpdated -= HandleUpdatedQuest;
        }

        public void SetupQuestViews()
        {
            for (int i = 0; i < _dailyQuestsService.CurrentQuests.Count; i++)
            {
                var questData = _dailyQuestsService.CurrentQuests[i];
                var view = Instantiate(_viewPrefab, _viewsContainer);
                view.Init(questData, _config);
                _views.Add(view);
            }
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