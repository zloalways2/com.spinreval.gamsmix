using Core.Services;
using System;
using UI.Screens;
using UnityEngine;

namespace Core.UI.Controllers
{
    public class MainSceneScreenController : MonoBehaviour
    {
        [SerializeField] private DailyFreeBonusScreen _dailyFreeBonusScreen;
        [SerializeField] private WelcomeScreenView _welcomeScreenView;
        [SerializeField] private MainMenuScreenView _mainMenuScreenView;

        private void Awake()
        {
            _welcomeScreenView.OnPlayerReady += HandlePlayerReady;
            _dailyFreeBonusScreen.OnBonusClaimed += HandleBonusClaimed;
        }

        private void Start()
        {
            if (GameServices.SaveService.HasProfile())
                HandlePlayerReady();
        }

        private void OnDestroy()
        {
            _welcomeScreenView.OnPlayerReady -= HandlePlayerReady;
            _dailyFreeBonusScreen.OnBonusClaimed -= HandleBonusClaimed;
        }

        private void HandlePlayerReady()
        {
            Debug.Log($"[Main Scene Screen Controller] Player is ready.");

            _welcomeScreenView.Close();

            if (GameServices.EconomyService.RequestDailyFreeBonusAvailable())
            {
                _dailyFreeBonusScreen.Open();
                _mainMenuScreenView.Open();
            }
            else
            {
                _dailyFreeBonusScreen.Close();
                _mainMenuScreenView.Open();
            }
        }

        private void HandleBonusClaimed()
        {
            GameServices.PlayerService.GetData().LastDailyBonusTime = DateTime.Now;
            string todayUtc = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            PlayerPrefs.SetString(GameConstants.KEY_LAST_DAILY_BONUS_CLAIM, todayUtc);
            GameServices.EconomyService.DailyBonusClaimed();

            _dailyFreeBonusScreen.Close();
            _mainMenuScreenView.Open();
        }
    }
}