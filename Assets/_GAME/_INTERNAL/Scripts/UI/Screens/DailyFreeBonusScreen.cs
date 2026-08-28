using Screen = UI.Other.Screen;
using UnityEngine;
using UI.Other;
using Core.Services;
using System;

namespace UI.Screens
{
    public class DailyFreeBonusScreen : Screen
    {
        [SerializeField] private ActionButton _claimButton;

        public event Action OnBonusClaimed;

        private void Awake()
        {
            _claimButton.OnButtonClick += HandleClaimBonusButtonClick;
        }

        private void OnDestroy()
        {
            _claimButton.OnButtonClick -= HandleClaimBonusButtonClick;
        }

        private void HandleClaimBonusButtonClick()
        {
            GameServices.EconomyService.AddCoins(2500f);
            OnBonusClaimed?.Invoke();
        }
    }
}