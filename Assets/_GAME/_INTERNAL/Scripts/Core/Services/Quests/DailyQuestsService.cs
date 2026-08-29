using Core.Data;
using Core.Data.Quests;
using Core.Gameplay;
using Core.Services.Player;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Core.Services.Quests
{
    public class DailyQuestsService
    {
        private PlayerData _data;
        private EconomyService _economyService;
        private PlayerService _playerService;
        private PlayedAcradesService _playedAcradesService;

        private List<DailyQuest> _currentQuests;
        private List<DailyQuest> _requestedQuestsToMainMenu;

        private DateTime _nextRefreshTimeUtc;

        private bool _isQuestsRequested = false;

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, "quests_data.sav");

        public IReadOnlyList<DailyQuest> CurrentQuests => _currentQuests.AsReadOnly();
        public IReadOnlyList<DailyQuest> RequestedQuests => _requestedQuestsToMainMenu.AsReadOnly();

        public event Action<List<DailyQuest>> OnQuestsUpdated;
        public event Action<DailyQuest> OnQuestUpdated;

        // Шаблоны квестов для генерации
        private static QuestTemplate[] QuestTemplates;

        public void Init(PlayerData data, PlayedAcradesService playedAcradesService, EconomyService economyService, PlayerService playerService)
        {
            QuestTemplates = new QuestTemplate[]
            {
                new("play_every_arcade", "Play Every Arcade", GameConstants.TAG_PLAY_EVERY_ARCADE, 10, 500, 200),
                new("spin_10_reels", "Spin 10 Reels", GameConstants.TAG_SPIN_10_REELS, 10, 100, 30),
                new("collect_5_diamonds", "Collect 5 Diamonds", GameConstants.TAG_COLLECT_5_DIAMONDS, 5, 100, 100),
                new("trigger_turbo_mode", "Trigger Turbo Mode", GameConstants.TAG_TRIGGER_TURBO_BOOST, 1, 150, 50),
                new("reach_10x_multiplier", "Reach a x10 Multiplier", GameConstants.TAG_REACH_10X_MULTIPLIER, 1, 200, 50),
                new("claim_free_energy", "Claim Free Energy", GameConstants.TAG_CLAIM_FREE_ENERGY, 1, 120, 35),
                new("open_the_vault", "Open the Vault", GameConstants.TAG_OPEN_THE_VAULT, 1, 180, 45),
                new("hit_21", "Hit 21 Exactly", GameConstants.TAG_HIT_21, 1, 500, 1000),
                new("launch_3_rockets", "Launch 3 Rockets", GameConstants.TAG_LAUNCH_3_ROCKETS, 3, 500, 150),
                new("drop_10_plinko_balls", "Drop 10 Plinko Balls", GameConstants.TAG_DROP_10_PLINKO_BALLS, 10, 200, 500),
                new("spin_lucky_wheel", "Spin the Lucky Wheel", GameConstants.TAG_SPIN_LUCKY_WHEEL, 1, 100, 25),
                new("roll_double_dice", "Roll Double Dice", GameConstants.TAG_ROLL_DOUBLE_DICE, 1, 100, 75),
                new("earn_2500_coins", "Earn 2,500 R-Coins", GameConstants.TAG_EARN_2500_RCOINS, 2500, 150, 50),
                new("complete_5_combos", "Complete 5 Combos", GameConstants.TAG_COMPLETE_5_COMBOS, 5, 175, 500),
                new("upgrade_level", "Upgrade Your Level", GameConstants.TAG_UPGRADE_YOUR_LEVEL, 1, 100, 25),
                new("win_3_games", "Win 3 Games", GameConstants.TAG_WIN_3_GAMES, 3, 250, 250),
            };

            _data = data;
            _economyService = economyService;
            _playerService = playerService;
            _playedAcradesService = playedAcradesService;

            _nextRefreshTimeUtc = DateTime.UtcNow.Date.AddDays(1);

            CheckDailyReset();

            // Если квесты ещё не сгенерированы, создаём новые
            if (_currentQuests == null || _currentQuests.Count == 0)
                GenerateNewQuests();
        }

        /// <summary>
        /// Проверить и выполнить сброс квестов если наступил новый день
        /// </summary>
        private void CheckDailyReset()
        {
            string todayUtc = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            string lastDate = PlayerPrefs.GetString(GameConstants.KEY_LAST_DAILY_DATE, "");

            if (todayUtc != lastDate)
            {
                Debug.Log($"[DailyQuests] New day detected! Resetting quests. {lastDate} -> {todayUtc}");
                PlayerPrefs.SetString(GameConstants.KEY_LAST_DAILY_DATE, todayUtc);
                _playedAcradesService.RemoveMap();
                GenerateNewQuests();
            }
            else
                LoadQuestsFromData();
        }

        /// <summary>
        /// Сгенерировать квесты
        /// </summary>
        private void GenerateNewQuests()
        {
            _currentQuests = new();

            var shuffledTemplates = Shuffle(QuestTemplates);

            for (int i = 0; i < shuffledTemplates.Length; i++)
            {
                var template = shuffledTemplates[i];
                var quest = new DailyQuest
                {
                    Id = template.Id,
                    Description = template.Description,
                    QuestTag = template.Tag,
                    TargetProgress = template.TargetValue,
                    CurrentProgress = 0,
                    RewardCoins = template.RewardCoins,
                    RewardXP = template.RewardXP,
                    IsCompleted = false,
                    IsClaimed = false
                };

                _currentQuests.Add(quest);
            }

            OnQuestsUpdated?.Invoke(_currentQuests);
            SaveQuestsToData();
            Debug.Log($"[DailyQuests] Generated {_currentQuests.Count} new daily quests.");
        }

        /// <summary>
        /// Запрос определённого колличества квестов
        /// </summary>
        public void RequestQuests(int count)
        {
            if (_currentQuests == null || _currentQuests.Count == 0 || count <= 0 || _isQuestsRequested)
                return;

            _requestedQuestsToMainMenu ??= new();

            var availableQuests = _currentQuests.Except(_requestedQuestsToMainMenu).ToList();

            for (int i = 0; i < count && availableQuests.Count > 0; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, availableQuests.Count);
                var quest = availableQuests[randomIndex];

                _requestedQuestsToMainMenu.Add(quest);
                availableQuests.RemoveAt(randomIndex);
            }

            _isQuestsRequested = true;
        }

        public void ProgressQuests(IEnumerable<string> tags, int amount = 1)
        {
            if (tags == null) 
                return;
            foreach (var tag in tags)
                ProgressQuest(tag, amount);
        }

        public TimeSpan GetTimeUntilRefresh()
        {
            var remaining = _nextRefreshTimeUtc - DateTime.UtcNow;

            // Если время вышло (наступила полночь)
            if (remaining <= TimeSpan.Zero)
            {
                string todayUtc = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
                string lastDate = PlayerPrefs.GetString(GameConstants.KEY_LAST_DAILY_DATE, "");

                if (todayUtc != lastDate)
                    CheckDailyReset();

                // Пересчитываем время до следующей полночи
                _nextRefreshTimeUtc = DateTime.UtcNow.Date.AddDays(1);
                remaining = _nextRefreshTimeUtc - DateTime.UtcNow;
            }

            return remaining;
        }

        /// <summary>
        /// Обновить прогресс квеста по тегу
        /// </summary>
        public void ProgressQuest(string tag, int amount = 1)
        {
            if (_currentQuests == null) 
                return;

            bool changed = false;

            foreach (var quest in _currentQuests)
            {
                if (quest.QuestTag != tag || quest.IsCompleted || quest.IsClaimed)
                    continue;

                quest.CurrentProgress += amount;

                if (quest.CurrentProgress >= quest.TargetProgress)
                {
                    quest.CurrentProgress = quest.TargetProgress;
                    quest.IsCompleted = true;

                    // Авто-клейм награды
                    var reward = ClaimRewardInternal(quest.Id);
                    if (reward.HasValue)
                    {
                        _economyService.AddCoins(reward.Value.coins);
                        _playerService.AddXP(reward.Value.xp);
                    }

                    Debug.Log($"[DailyQuests] Quest completed & claimed: {quest.Description}");
                }

                changed = true;
                OnQuestUpdated?.Invoke(quest);
                OnQuestsUpdated?.Invoke(_currentQuests);
            }

            if (changed)
                SaveQuestsToData();

            Debug.Log($"[Daily Quests Service] Quest {tag} updated" +
                $" {_currentQuests.FirstOrDefault(quest => quest.QuestTag == tag).CurrentProgress}");
        }

        /// <summary>
        /// Забрать награду за выполненный квест
        /// </summary>
        /// <returns>Награда (coins, XP) или null если нельзя забрать</returns>
        private(int coins, int xp)? ClaimRewardInternal(string questId)
        {
            if (_currentQuests == null || string.IsNullOrEmpty(questId))
                return null;

            var quest = _currentQuests.FirstOrDefault(q => q.Id == questId);
            if (quest == null)
                return null;

            if (!quest.IsCompleted || quest.IsClaimed)
                return null;

            quest.IsClaimed = true;
            return (quest.RewardCoins, quest.RewardXP);
        }

        /// <summary>
        /// Использовать только для ОТЛАДКИ!
        /// </summary>
        public void DeleteAllQuests()
        {
            if (File.Exists(SaveFilePath))
                File.Delete(SaveFilePath);

            PlayerPrefs.DeleteKey(GameConstants.KEY_LAST_DAILY_DATE);

            _currentQuests?.Clear();
        }

        /// <summary>
        /// Сохранить квесты в PlayerData
        /// </summary>
        private void SaveQuestsToData()
        {
            string json = JsonConvert.SerializeObject(_currentQuests);
            string tempPath = SaveFilePath + ".tmp";
            File.WriteAllText(tempPath, json);

            if (File.Exists(SaveFilePath))
                File.Delete(SaveFilePath);

            File.Move(tempPath, SaveFilePath);
        }

        /// <summary>
        /// Загрузить квесты из Сохранения
        /// </summary>
        private void LoadQuestsFromData()
        {
            if (File.Exists(SaveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SaveFilePath);
                    _currentQuests = JsonConvert.DeserializeObject<List<DailyQuest>>(json);
                    Debug.Log($"[DailyQuests] Loaded {_currentQuests?.Count ?? 0} quests from save.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[DailyQuests] Failed to load quests: {e.Message}. Generating new ones.");
                    GenerateNewQuests();
                }
            }
            else
                GenerateNewQuests();
        }

        /// <summary>
        /// Перемешать массив (Fisher-Yates shuffle)
        /// </summary>
        private T[] Shuffle<T>(T[] array)
        {
            T[] shuffled = (T[])array.Clone();
            var random = new System.Random();

            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            return shuffled;
        }
    }
}