using Core.Data;
using System;
using UnityEngine;

namespace Core.Services.Player
{
    public class EnergyService
    {
        private PlayerData _playerData;
        private Action _onEnergyClaimed;

        private readonly Action _onEnergyChanged;

        public int CurrentEnergy => _playerData.Energy;
        public int MaxEnergy => GameConstants.MAX_ENERGY;
        public bool HasEnoughEnergy => _playerData.Energy > GameConstants.ENERGY_FOR_GAME;
        public DateTime LastEnergyUpdate => _playerData.LastEnergyUpdate;
        public DateTime? LastFreeEnergyTime { get; private set; }

        public event Action<int> OnEnergyChanged;

        public EnergyService(Action onEnergyChanged, Action onEnergyClaimed)
        {
            _onEnergyChanged = onEnergyChanged;
            _onEnergyClaimed = onEnergyClaimed;
        }

        public void Init(PlayerData playerData)
        {
            _playerData = playerData;

            LastFreeEnergyTime = _playerData.LastFreeEnergyTime;

            RegenerateEnergy();
        }

        /// <summary>
        /// Регенерировать энергию на основе прошедшего времени
        /// </summary>
        public void RegenerateEnergy()
        {
            var now = DateTime.Now;
            var timePassed = now - _playerData.LastEnergyUpdate;

            int energyToRegen = Mathf.FloorToInt((float)timePassed.TotalMinutes / GameConstants.ENERGY_REGEN_MINUTES);

            if (energyToRegen > 0)
            {
                int newEnergy = Mathf.Min(_playerData.Energy + energyToRegen, MaxEnergy);
                int actualRegen = newEnergy - _playerData.Energy;

                if (actualRegen > 0)
                {
                    _playerData.Energy = newEnergy;

                    var timeConsumed = TimeSpan.FromMinutes(actualRegen * GameConstants.ENERGY_REGEN_MINUTES);
                    _playerData.LastEnergyUpdate = _playerData.LastEnergyUpdate.Add(timeConsumed);

                    OnEnergyChanged?.Invoke(_playerData.Energy);
                    Debug.Log($"[Energy] Regenerated {actualRegen} energy. New energy: {_playerData.Energy}/{MaxEnergy}");
                }
            }
        }

        /// <summary>
        /// Потратить энергию на игру
        /// </summary>
        /// <param name="amount">Количество энергии</param>
        /// <returns>true если успешно</returns>
        public bool SpendEnergy(int amount = 1)
        {
            RegenerateEnergy();

            if (_playerData.Energy >= amount)
            {
                _playerData.Energy -= amount;
                _playerData.LastEnergyUpdate = DateTime.Now;
                OnEnergyChanged?.Invoke(_playerData.Energy);

                Debug.Log($"[Energy] Spent {amount} energy. Remaining: {_playerData.Energy}/{MaxEnergy}");
                return true;
            }

            Debug.LogWarning($"[Energy] Not enough energy! Have: {_playerData.Energy}, Need: {amount}");
            return false;
        }

        /// <summary>
        /// Добавить энергию (например, из рекламы)
        /// </summary>
        public void AddEnergy(int amount)
        {
            _playerData.Energy = Mathf.Min(_playerData.Energy + amount, MaxEnergy);
            _playerData.LastEnergyUpdate = DateTime.Now;
            OnEnergyChanged?.Invoke(_playerData.Energy);

            Debug.Log($"[Energy] Added {amount} energy. New energy: {_playerData.Energy}/{MaxEnergy}");
        }

        /// <summary>
        /// Получить бесплатную энергию (кулдаун 1 час)
        /// </summary>
        /// <returns>true если успешно получено</returns>
        public bool TryGetFreeEnergy(int amount = 5)
        {
            if (_playerData.Energy == GameConstants.MAX_ENERGY)
                return false;

            RegenerateEnergy();

            var now = DateTime.Now;

            if (LastFreeEnergyTime.HasValue)
            {
                var timeSinceFree = now - LastFreeEnergyTime.Value;
                if (timeSinceFree.TotalHours < 1f)
                {
                    double minutesLeft = 60 - timeSinceFree.TotalMinutes;
                    Debug.LogWarning($"[Energy] Free energy on cooldown. Wait {Mathf.CeilToInt((float)minutesLeft)} minutes.");
                    return false;
                }
            }

            AddEnergy(amount);

            LastFreeEnergyTime = now;
            _playerData.LastFreeEnergyTime = now;
            _onEnergyClaimed?.Invoke();
            _onEnergyClaimed = null;

            Debug.Log($"[Energy] Free energy received: +{amount}");
            return true;
        }

        /// <summary>
        /// Запросить текущее состояние бесплатной энергии (доступна ли она)
        /// </summary>
        public bool GetFreeEnergyStatus()
        {
            if (_playerData.Energy >= MaxEnergy)
                return false;

            var now = DateTime.Now;

            if (LastFreeEnergyTime.HasValue)
            {
                var timeSinceFree = now - LastFreeEnergyTime.Value;
                if (timeSinceFree.TotalHours < 1f)
                {
                    double minutesLeft = 60 - timeSinceFree.TotalMinutes;
                    Debug.LogWarning($"[Energy] Free energy on cooldown. Wait {Mathf.CeilToInt((float)minutesLeft)} minutes.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Получить время до следующей бесплатной энергии
        /// </summary>
        public TimeSpan GetTimeUntilFreeEnergy()
        {
            if (!LastFreeEnergyTime.HasValue)
                return TimeSpan.Zero;

            var timeSinceFree = DateTime.Now - LastFreeEnergyTime.Value;
            var cooldown = TimeSpan.FromHours(1);

            if (timeSinceFree >= cooldown)
                return TimeSpan.Zero;

            return cooldown - timeSinceFree;
        }

        /// <summary>
        /// Получить время до полной регенерации
        /// </summary>
        public TimeSpan GetTimeUntilFullEnergy()
        {
            if (_playerData.Energy >= MaxEnergy)
                return TimeSpan.Zero;

            int energyNeeded = MaxEnergy - _playerData.Energy;
            return TimeSpan.FromMinutes(energyNeeded * GameConstants.ENERGY_REGEN_MINUTES);
        }

        /// <summary>
        /// Принудительно обновить время последней регенерации (для тестов)
        /// </summary>
        public void ForceUpdateTime(DateTime time)
        {
            _playerData.LastEnergyUpdate = time;
        }
    }
}