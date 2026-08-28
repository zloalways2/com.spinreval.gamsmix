using Core.Services.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Services.Quests
{
    public class QuestRouter
    {
        private PlayedAcradesService _playedAcradesService;

        private readonly List<string> _allArcades = new()
        {
                GameConstants.GAME_CRYPTO_VIBE,
                GameConstants.GAME_CYBER_MASTER,
                GameConstants.GAME_DIAMOND_RETRO,
                GameConstants.GAME_ELECTRIC_DICE,
                GameConstants.GAME_INFINITE_SCORE,
                GameConstants.GAME_VAULT,
                GameConstants.GAME_NEON_WHEEL,
                GameConstants.GAME_REELS,
                GameConstants.GAME_WHEEL_OF_REVOLUT,
                GameConstants.GAME_PLINKO_VIBE
        };
        private readonly Dictionary<string, List<string>> _multiGameMap = new()
        {
            { GameConstants.TAG_COLLECT_5_DIAMONDS, new() {GameConstants.GAME_DIAMOND_RETRO} },
            { GameConstants.TAG_COMPLETE_5_COMBOS, 
                new() 
                { 
                        GameConstants.GAME_CYBER_MASTER,
                        GameConstants.GAME_DIAMOND_RETRO,
                        GameConstants.GAME_ELECTRIC_DICE,
                        GameConstants.GAME_INFINITE_SCORE,
                        GameConstants.GAME_VAULT,
                        GameConstants.GAME_NEON_WHEEL,
                        GameConstants.GAME_REELS,
                        GameConstants.GAME_PLINKO_VIBE,
                        GameConstants.GAME_CRYPTO_VIBE
                }
            },
            { GameConstants.TAG_DROP_10_PLINKO_BALLS, new() { GameConstants.GAME_PLINKO_VIBE }},
            { GameConstants.TAG_EARN_2500_RCOINS,
                new()
                {
                        GameConstants.GAME_CYBER_MASTER,
                        GameConstants.GAME_DIAMOND_RETRO,
                        GameConstants.GAME_ELECTRIC_DICE,
                        GameConstants.GAME_INFINITE_SCORE,
                        GameConstants.GAME_VAULT,
                        GameConstants.GAME_NEON_WHEEL,
                        GameConstants.GAME_REELS,
                        GameConstants.GAME_PLINKO_VIBE,
                        GameConstants.GAME_CRYPTO_VIBE
                } 
            },
            { GameConstants.TAG_PLAY_EVERY_ARCADE,
                new() 
                {       
                        GameConstants.GAME_CYBER_MASTER,
                        GameConstants.GAME_DIAMOND_RETRO,
                        GameConstants.GAME_ELECTRIC_DICE,
                        GameConstants.GAME_INFINITE_SCORE,
                        GameConstants.GAME_VAULT,
                        GameConstants.GAME_NEON_WHEEL,
                        GameConstants.GAME_REELS,
                        GameConstants.GAME_PLINKO_VIBE,
                        GameConstants.GAME_CRYPTO_VIBE,
                        GameConstants.GAME_WHEEL_OF_REVOLUT
                } 
            },
            { GameConstants.TAG_HIT_21, new() { GameConstants.GAME_CYBER_MASTER } },
            { GameConstants.TAG_LAUNCH_3_ROCKETS, new() { GameConstants.GAME_CRYPTO_VIBE } },
            { GameConstants.TAG_SPIN_LUCKY_WHEEL, new() { GameConstants.GAME_WHEEL_OF_REVOLUT } },
            { GameConstants.TAG_OPEN_THE_VAULT, new() { GameConstants.GAME_VAULT } },
            { GameConstants.TAG_REACH_10X_MULTIPLIER, new() { GameConstants.GAME_CRYPTO_VIBE } },
            { GameConstants.TAG_ROLL_DOUBLE_DICE, new() { GameConstants.GAME_ELECTRIC_DICE } },
            { GameConstants.TAG_SPIN_10_REELS, new() { GameConstants.GAME_REELS, GameConstants.GAME_DIAMOND_RETRO } },
            { GameConstants.TAG_TRIGGER_TURBO_BOOST, new() { GameConstants.GAME_DIAMOND_RETRO, GameConstants.GAME_REELS } },
            { GameConstants.TAG_WIN_3_GAMES, 
                new() 
                {
                        GameConstants.GAME_CYBER_MASTER,
                        GameConstants.GAME_DIAMOND_RETRO,
                        GameConstants.GAME_ELECTRIC_DICE,
                        GameConstants.GAME_INFINITE_SCORE,
                        GameConstants.GAME_VAULT,
                        GameConstants.GAME_NEON_WHEEL,
                        GameConstants.GAME_REELS,
                        GameConstants.GAME_PLINKO_VIBE,
                        GameConstants.GAME_CRYPTO_VIBE,
                        GameConstants.GAME_WHEEL_OF_REVOLUT
                } 
            },
            { GameConstants.TAG_UPGRADE_YOUR_LEVEL, new() 
            {
                        GameConstants.GAME_CYBER_MASTER,
                        GameConstants.GAME_DIAMOND_RETRO,
                        GameConstants.GAME_ELECTRIC_DICE,
                        GameConstants.GAME_INFINITE_SCORE,
                        GameConstants.GAME_VAULT,
                        GameConstants.GAME_NEON_WHEEL,
                        GameConstants.GAME_REELS,
                        GameConstants.GAME_PLINKO_VIBE,
                        GameConstants.GAME_CRYPTO_VIBE,
                        GameConstants.GAME_WHEEL_OF_REVOLUT
            } }
        };

        public void Init(PlayedAcradesService playedAcradesService)
        {
            _playedAcradesService = playedAcradesService;
        }

        public string GetTargetSceneByQuestTag(string questTag)
        {
            switch (questTag)
            {
                case GameConstants.TAG_DROP_10_PLINKO_BALLS:
                    return GameConstants.GAME_PLINKO_VIBE;

                case GameConstants.TAG_HIT_21:
                    return GameConstants.GAME_CYBER_MASTER;

                case GameConstants.TAG_LAUNCH_3_ROCKETS:
                    return GameConstants.GAME_CRYPTO_VIBE;

                case GameConstants.TAG_OPEN_THE_VAULT:
                    return GameConstants.GAME_VAULT;

                case GameConstants.TAG_REACH_10X_MULTIPLIER:
                    return GameConstants.GAME_CRYPTO_VIBE;

                case GameConstants.TAG_ROLL_DOUBLE_DICE:
                    return GameConstants.GAME_ELECTRIC_DICE;

                case GameConstants.TAG_SPIN_10_REELS:
                    var games = _multiGameMap[questTag];
                    return games[Random.Range(0, games.Count)];

                case GameConstants.TAG_WIN_3_GAMES:
                    return GetRandomGame();

                case GameConstants.TAG_COLLECT_5_DIAMONDS:
                    return GameConstants.GAME_DIAMOND_RETRO;

                case GameConstants.TAG_SPIN_LUCKY_WHEEL:
                    return GameConstants.GAME_WHEEL_OF_REVOLUT;

                case GameConstants.TAG_TRIGGER_TURBO_BOOST:
                    var reelGames = _multiGameMap[questTag];
                    return reelGames[Random.Range(0, reelGames.Count)];

                case GameConstants.TAG_PLAY_EVERY_ARCADE:
                    var randomGame = _allArcades[Random.Range(0, _allArcades.Count)];
                    return GetUnplayedArcadeGame(_allArcades);

                case GameConstants.TAG_EARN_2500_RCOINS:
                    return GetRandomGame();

                case GameConstants.TAG_COMPLETE_5_COMBOS:
                    return GetRandomGame();

                default:
                    return GameConstants.MAIN_MENU;
            }
        }

        private string GetRandomGame()
        {
            var randomGame = _allArcades[Random.Range(0, _allArcades.Count)];
            return randomGame;
        }

        private string GetUnplayedArcadeGame(List<string> arcadesToCheck)
        {
            var unplayedGames = arcadesToCheck
                .Where(game => _playedAcradesService.IsArcadeUnplayed(game))
                .ToList();

            if (unplayedGames.Count > 0)
                return unplayedGames[Random.Range(0, unplayedGames.Count)];

            return GameConstants.MAIN_MENU;
        }
    }
}