using Core.Services;
using UnityEngine;

namespace Core.Common
{
    public abstract class GameController : MonoBehaviour
    {
        public abstract void Enter();
        public abstract void Initialize();
        public abstract void Exit();

        public virtual bool SpendEnergy() => GameServices.EnergyService.SpendEnergy(GameConstants.ENERGY_FOR_GAME);

        protected void RecordArcadePlay(string gameId = null)
        {
            var id = gameId;
            GameServices.FavoriteGamesService?.RecordGamePlay(id);
            GameServices.PlayedAcradesService?.AddPlayedArcadeToMap(id);
        }
    }
}