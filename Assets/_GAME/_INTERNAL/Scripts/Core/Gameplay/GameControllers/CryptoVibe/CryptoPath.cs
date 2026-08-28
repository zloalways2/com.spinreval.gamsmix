using System;
using UnityEngine;

namespace Core.Gameplay.GameControllers.CryptoVibe
{
    public class CryptoPath
    {
        /// <summary>Точки пути взлёта (от старта до точки краша).</summary>
        public Vector3[] AscentPoints { get; }

        /// <summary>Точки пути падения (от точки краша до финальной точки).</summary>
        public Vector3[] DescentPoints { get; }

        /// <summary>Индекс последней достигнутой точки взлёта (точка краша).</summary>
        public int CrashPointIndex { get; }

        /// <summary>Множитель, на котором произошёл краш.</summary>
        public float CrashMultiplier { get; }

        /// <summary>Seed для воспроизводимости пути.</summary>
        public int Seed { get; }

        public CryptoPath(
            Vector3[] ascentPoints,
            Vector3[] descentPoints,
            int crashPointIndex,
            float crashMultiplier,
            int seed)
        {
            AscentPoints = ascentPoints;
            DescentPoints = descentPoints;
            CrashPointIndex = crashPointIndex;
            CrashMultiplier = crashMultiplier;
            Seed = seed;
        }
    }
}