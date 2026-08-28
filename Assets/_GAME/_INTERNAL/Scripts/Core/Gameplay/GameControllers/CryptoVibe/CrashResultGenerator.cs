using UnityEngine;

namespace Core.Gameplay.GameControllers.CryptoVibe
{
    public sealed class CrashResultGenerator
    {
        private readonly float _maxMultiplier;

        public CrashResultGenerator(float maxMultiplier) => _maxMultiplier = maxMultiplier;

        public float Generate()
        {
            float random = Random.Range(0.01f, 0.5f);

            // Чем меньше random, тем выше crash point.
            float multiplier = 1.5f / random;

            return Mathf.Min(multiplier, _maxMultiplier);
        }
    }
}