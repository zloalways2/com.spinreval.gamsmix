using Core.Services;
using UI.Player;
using UnityEngine;
using Screen = UI.Other.Screen;

namespace UI.Screens
{
    public class MainMenuScreenView : Screen
    {
        [SerializeField] private DailyQuestsMainMenuView _dailyQuestsView;

        private void Awake()
        {
            _dailyQuestsView.Init(GameServices.Quests);
        }

        private void OnDestroy()
        {
            _dailyQuestsView.Dispose();
        }
    }
}