using UI.Other;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.UI.Controllers
{
    public class ButtonsController : MonoBehaviour
    {
        [Header("Navigation Buttons Setup")]
        [SerializeField] private ActionButton _gamesScreenButton;
        [SerializeField] private ActionButton _leaderboardScreenButton;
        [SerializeField] private ActionButton _questsScreenButton;
        [SerializeField] private ActionButton _profileScreenButton;
        [SerializeField] private ActionButton _settingsScreenButton;

        [Space(5), Header("Other Buttons Setup")]
        [SerializeField] private ActionButton _achievementsButton;

        private void Awake()
        {
            if(_achievementsButton != null)
                _achievementsButton.OnButtonClick += HandleAchievementsButtonCLick;

            _gamesScreenButton.OnButtonClick += HandleGamesScreenButtonClick;
            _leaderboardScreenButton.OnButtonClick += HandleLeaderboardScreenButtonClick;
            _questsScreenButton.OnButtonClick += HandleQuestsScreenButtonClick;
            _profileScreenButton.OnButtonClick += HandleProfileScreenButtonClick;
            _settingsScreenButton.OnButtonClick += HandleSettingsScreenButtonClick;
        }

        private void OnDestroy()
        {
            if(_achievementsButton != null)
                _achievementsButton.OnButtonClick -= HandleAchievementsButtonCLick;
                
            _gamesScreenButton.OnButtonClick -= HandleGamesScreenButtonClick;
            _leaderboardScreenButton.OnButtonClick -= HandleLeaderboardScreenButtonClick;
            _questsScreenButton.OnButtonClick -= HandleQuestsScreenButtonClick;
            _profileScreenButton.OnButtonClick -= HandleProfileScreenButtonClick;
            _settingsScreenButton.OnButtonClick -= HandleSettingsScreenButtonClick;
        }

        private void HandleAchievementsButtonCLick()
        {
            if (SceneManager.GetActiveScene().name == GameConstants.ACHIEVEMENTS)
                return;

            SceneManager.LoadSceneAsync(GameConstants.ACHIEVEMENTS);
        }

        private void HandleSettingsScreenButtonClick()
        {
            if (SceneManager.GetActiveScene().name == GameConstants.SETTINGS)
                return;

            SceneManager.LoadSceneAsync(GameConstants.SETTINGS);
        }

        private void HandleProfileScreenButtonClick()
        {
            if (SceneManager.GetActiveScene().name == GameConstants.PROFILE)
                return;

            SceneManager.LoadSceneAsync(GameConstants.PROFILE);
        }

        private void HandleQuestsScreenButtonClick()
        {
            if (SceneManager.GetActiveScene().name == GameConstants.QUESTS)
                return;

            SceneManager.LoadSceneAsync(GameConstants.QUESTS);
        }

        private void HandleLeaderboardScreenButtonClick()
        {
            if (SceneManager.GetActiveScene().name == GameConstants.LEADERBOARD)
                return;

            SceneManager.LoadSceneAsync(GameConstants.LEADERBOARD);
        }

        private void HandleGamesScreenButtonClick()
        {
            if (SceneManager.GetActiveScene().name == GameConstants.MAIN_MENU)
                return;

            SceneManager.LoadSceneAsync(GameConstants.MAIN_MENU);
        }
    }
}