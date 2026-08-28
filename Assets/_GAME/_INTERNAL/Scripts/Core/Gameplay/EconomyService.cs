using System;
using Core.Services.Audio;
using UnityEngine;

namespace Core.Gameplay
{
    public class EconomyService
    {
        private float _currentCoinsBalance;
        private bool _isDailyFreeBonusAvailable;

        private DateTime _dailyFreeBonusNextRefreshTimeUtc;

        public float CurrentCoinsBalance
        {
            get => _currentCoinsBalance;
            private set
            {
                if(value < 0f)
                    throw new System.ArgumentOutOfRangeException(nameof(value), "Coins Value cannot be a negative!");

                _currentCoinsBalance = value;
                AudioService.Instance.PlaySfx(SoundType.Coins_Changed);
                OnCoinsBalanceChanged?.Invoke(_currentCoinsBalance);
            }
        }

        public event Action<float> OnCoinsBalanceChanged;

        public void Init(float initialCoinsBalance)
        {
            _currentCoinsBalance = initialCoinsBalance;
            _dailyFreeBonusNextRefreshTimeUtc.AddDays(1);

            CheckDailyFreeBonus();
        }

        /// <summary>
        /// Получить текущий баланс Coins
        /// </summary>
        public float GetCoinsBalance() => CurrentCoinsBalance;

        /// <summary>
        /// Запросить текущий баланс Coins (invoke события)
        /// </summary>
        public void RequestCoinsBalance() => OnCoinsBalanceChanged?.Invoke(_currentCoinsBalance);

        /// <summary>
        /// Запросить актуальность ежедневного бонуса
        /// </summary>
        public bool RequestDailyFreeBonusAvailable() => _isDailyFreeBonusAvailable;

        public void DailyBonusClaimed() => _isDailyFreeBonusAvailable = false;

        /// <summary>
        /// Добавить средства (выигрыш, бонус)
        /// </summary>
        public void AddCoins(float amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Attempt to add a negattive amount: {amount}. Use the SpendCoins() method");
                return;
            }

            CurrentCoinsBalance += amount;

            Debug.Log($"[Economy] Added coins: +{amount}. New balance: {CurrentCoinsBalance}");
        }

        /// <summary>
        /// Списать средства (ставка, проигрыш)
        /// </summary>
        public bool SpendCoins(float amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Attempt to debit a negative amount: {amount}. Use the AddCoins() method");
                return false;
            }

            if (!HasEnoughBalance(amount))
            {
                Debug.LogWarning($"Not enough coins! Balance: {CurrentCoinsBalance}, needed: {amount}");
                return false;
            }

            CurrentCoinsBalance -= amount;

            Debug.Log($"[Economy] Debited: -{amount}. New balance: {CurrentCoinsBalance}");
            return true;
        }

        /// <summary>
        /// Проверить, достаточно ли средств
        /// </summary>
        public bool HasEnoughBalance(float amount) => CurrentCoinsBalance >= amount;

        /// <summary>
        /// Установить баланс (для тестирования или загрузки из сохранений)
        /// </summary>
        public void SetBalance(float amount) => CurrentCoinsBalance = Mathf.Max(0, amount);

        private void CheckDailyFreeBonus()
        {
            string todayUtc = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            string lastDate = PlayerPrefs.GetString(GameConstants.KEY_LAST_DAILY_BONUS_CLAIM, "");

            if (todayUtc != lastDate)
                _isDailyFreeBonusAvailable = true;
            else
            {
                _isDailyFreeBonusAvailable = false;
                Debug.LogWarning($"[Economy Service] Daily Free Bonus is already claimed!");
            }
        }
    }
}