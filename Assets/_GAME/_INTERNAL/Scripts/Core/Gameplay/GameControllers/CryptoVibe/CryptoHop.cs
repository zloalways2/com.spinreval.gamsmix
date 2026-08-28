using UnityEngine;

namespace Core.Gameplay.GameControllers.CryptoVibe
{
    [System.Serializable]
    public readonly struct CryptoHop
    {
        public readonly Vector3[] Points;
        public readonly bool IsCrashHop; // true если это хопа падения после краша

        public CryptoHop(Vector3[] points, bool isCrashHop = false)
        {
            if (points == null || points.Length < 2)
                throw new System.ArgumentException("Hop must contain at least 2 points", nameof(points));

            Points = points;
            IsCrashHop = isCrashHop;
        }

        public readonly Vector3 EndPoint => Points[^1];
        public readonly Vector3 StartPoint => Points[0];
    }
}