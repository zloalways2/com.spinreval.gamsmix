#if UNITY_EDITOR
using Core.SO;
using System.Collections.Generic;
using UI.Plinko;
using UnityEditor;
using UnityEngine;

namespace Core.Gameplay.GameControllers.Plinko
{
    /// <summary>
    /// Строит визуальную доску в редакторе из того же конфига,
    /// который использует PlinkoPathGenerator. Расхождение визуала и логики невозможно.
    /// </summary>
    [ExecuteInEditMode]
    public sealed class PlinkoBoardBuilder : MonoBehaviour
    {
        [SerializeField] private PlinkoConfig _config;
        [SerializeField] private PegView _pegPrefab;
        [SerializeField] private BucketView _bucketPrefab;
        [SerializeField] private Transform _pegsRoot;
        [SerializeField] private Transform _bucketsRoot;
        [SerializeField] private Transform _boardContainer;

        private readonly List<BucketView> _generatedBuckets = new();
        private readonly List<PegView> _generatedPegs = new();

        public bool HasGeneratedGrid => _generatedBuckets.Count > 0 && _generatedPegs.Count > 0;

        [ContextMenu("Rebuild Board")]
        public void Rebuild()
        {
            if (_config == null || _boardContainer == null || Application.isPlaying || HasGeneratedGrid)
                return;

            Clear(_pegsRoot, generatedPegs: _generatedPegs);
            Clear(_bucketsRoot, generatedBuckets: _generatedBuckets);

            var generator = new PlinkoPathGenerator(_config);

            // Пирамида колышков 3 → 10
            for (int row = 0; row < _config.PegRows; row++)
            {
                var pegsInRow = generator.GetPegCountInRow(row);
                for (int col = 0; col < pegsInRow; col++)
                {
                    var peg = Instantiate(_pegPrefab, _pegsRoot);
                    peg.transform.position = generator.GetPegPosition(row, col);
                    peg.name = $"Peg_{row}_{col}";
                    _generatedPegs.Add(peg);
                }
            }

            // 9 бакетов с множителями из конфига
            for (int i = 0; i < generator.GetBucketCount(); i++)
            {
                var bucket = Instantiate(_bucketPrefab, _bucketsRoot);
                bucket.transform.position = generator.GetBucketPosition(i);
                bucket.name = $"Bucket_{i}";

                var bucketData = _config.Buckets[i];
                bucket.Init(bucketData.Multiplier, bucketData.Sprite);
                _generatedBuckets.Add(bucket);
            }

            _boardContainer.position = new(0f, -0,9f);

            EditorUtility.SetDirty(this);
            Debug.Log($"[Plinko Board Builder] Grid generated: Pegs: {_generatedPegs.Count}. Buckets: {_generatedBuckets.Count}");
        }

        [ContextMenu("Clear Generated Grid")]
        public void ClearGrid()
        {
            if (_boardContainer == null)
                return;

            Clear(_pegsRoot, generatedPegs: _generatedPegs);
            Clear(_bucketsRoot, generatedBuckets: _generatedBuckets);
            _boardContainer.position = new(0f, 0f);

            EditorUtility.SetDirty(this);
            Debug.Log($"[Plinko Board Builder] Grid cleared: Pegs: {_generatedPegs.Count}. Buckets: {_generatedBuckets.Count}");
        }

        private static void Clear(Transform root, List<BucketView> generatedBuckets = null, List<PegView> generatedPegs = null)
        {
            if (root == null) 
                return;

            generatedBuckets?.Clear();
            generatedPegs?.Clear();

            for (int i = root.childCount - 1; i >= 0; i--)
                DestroyImmediate(root.GetChild(i).gameObject);
        }
    }
}
#endif