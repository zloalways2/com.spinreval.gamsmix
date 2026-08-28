using UnityEngine;

namespace Core.Gameplay.GameControllers.Plinko
{
    [System.Serializable]
    public readonly struct PlinkoHop
    {
        public readonly Vector3[] Points;
        /// <summary>Ряд и колонка гвоздя, в который попадает мяч. -1 — финальный хоп в бакет.</summary>
        public readonly int PegRow;
        public readonly int PegCol;

        public PlinkoHop(Vector3[] points, int pegRow, int pegCol)
        {
            Points = points;
            PegRow = pegRow;
            PegCol = pegCol;
        }

        public readonly Vector3 EndPoint => Points[^1];
    }
}