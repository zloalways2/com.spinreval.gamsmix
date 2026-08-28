using UnityEngine;

namespace Core.SO
{
    [CreateAssetMenu(menuName = "Games/Plinko/Config")]
    public sealed class PlinkoConfig : ScriptableObject
    {
        [Header("Board")]
        public int PegRows = 12;          // количество рядов гвоздей
        public int PegsInFirstRow = 3;   // вершина пирамиды: 3, 4, 5...
        public float PegSpacing = 0.8f;
        public float RowSpacing = 0.9f;
        public float BucketSpacing = 1.0f;
        public float BucketOffsetY = 0.35f; // отступ корзин вниз от последнего ряда

        [Header("Drop")]
        public Vector3 SpawnPoint;
        public float SpawnYOffset = 0.5f;

        [Header("Animations")]
        public bool SimplifiedAnimation = false;
        public float FinalFallDuration = 0.3f;

        [Header("Buckets")]
        public PlinkoBucket[] Buckets;

        /// <summary>Гвоздей в ряду: вершина + row.</summary>
        public int GetPegsInRow(int row) => PegsInFirstRow + row;

        private void OnValidate()
        {
            var expected = PegRows + 1;
            if (Buckets != null && Buckets.Length != expected)
                Debug.LogError($"[Plinko] Buckets.Length = {Buckets.Length}, expected {expected} (PegRows + 1)");
        }
    }

    [System.Serializable]
    public struct PlinkoBucket
    {
        public float Multiplier;     // 0.5x, 1x, 10x, 100x...
        public float Weight;         // для визуального баланса вероятностей
        public Sprite Sprite;
    }
}