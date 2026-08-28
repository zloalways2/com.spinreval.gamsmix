using UI.Animations.Game;
using UnityEngine;
using Screen = UI.Other.Screen;

namespace UI.Screens
{
    public class AchievementsScreen : Screen
    {
        [SerializeField] private ObjectAnimations _popupPanel;

        void Start() => _popupPanel.MovingAppear();
    }
}