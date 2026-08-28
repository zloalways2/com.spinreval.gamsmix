namespace Core.Data
{
    [System.Serializable]
    public class FavoriteGameData
    {
        public string GameId;
        public int TotalPlayed;

        public FavoriteGameData(string gameId, int totalPlayed)
        {
            GameId = gameId;
            TotalPlayed = totalPlayed;
        }
    }
}