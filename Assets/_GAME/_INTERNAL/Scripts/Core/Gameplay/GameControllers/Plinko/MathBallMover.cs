using Core.SO;
using System.Collections;
using System.Collections.Generic;
using UI.Plinko;
using UnityEngine;

namespace Core.Gameplay.GameControllers.Plinko
{
    public class MathBallMover : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _hopDuration = 0.12f;
        [SerializeField] private float _finalFallDuration = 0.25f;
        [SerializeField] private float _jumpHeight = 0.5f;
        [SerializeField] private AnimationCurve _jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Кривая для плавности

        [Space(5), Header("Effects")]
        [SerializeField] private ParticleSystem _hitVFXPrefab;
        [SerializeField] private AudioClip _hitSound;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private float _minPitch = 0.75f;
        [SerializeField] private float _maxPitch = 1.25f;

        private PlinkoPath _path;
        private PlinkoConfig _config;
        private Coroutine _moveCoroutine;
        private int _currentHopIndex;

        private readonly List<PegView> _allPegs = new();
        private readonly List<BucketView> _allBuckets = new();

        public void SetInitialPosition(Vector3 position) => transform.position = position;

        /// <summary>
        /// Запускает анимацию движения мяча по заданному пути.
        /// </summary>
        public void StartMove(PlinkoPath path, PlinkoConfig config, List<PegView> allPegs, List<BucketView> allBuckets, AudioSource audioSource)
        {
            _path = path;
            _config = config;
            _currentHopIndex = 0;
            _audioSource = audioSource;

            _allPegs.Clear();
            _allPegs.AddRange(allPegs);

            _allBuckets.Clear();
            _allBuckets.AddRange(allBuckets);

            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _moveCoroutine = StartCoroutine(MoveAlongPath());
        }

        /// <summary>
        /// Останавливает анимацию движения.
        /// </summary>
        public void StopMove()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
        }

        private IEnumerator MoveAlongPath()
        {
            for (int i = 0; i < _path.Hops.Length; i++)
            {
                var hop = _path.Hops[i];
                bool isFinalHop = (i == _path.Hops.Length - 1);

                // Анимация текущего хопа
                yield return StartCoroutine(AnimateHop(hop, isFinalHop));

                _currentHopIndex++;
            }

            // Движение завершено, сообщаем о результате
            OnMoveCompleted();
        }

        private IEnumerator AnimateHop(PlinkoHop hop, bool isFinalHop)
        {
            if (hop.Points == null || hop.Points.Length < 2)
            {
                yield break;
            }

            float duration = isFinalHop ? _finalFallDuration : _hopDuration;

            // Анимация между точками хопа
            for (int i = 0; i < hop.Points.Length - 1; i++)
            {
                Vector3 startPoint = hop.Points[i];
                Vector3 endPoint = hop.Points[i + 1];

                float elapsed = 0f;
                float segmentDuration = duration / (hop.Points.Length - 1);

                while (elapsed < segmentDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / segmentDuration;

                    // Применяем кривую анимации для более резкого старта и финиша
                    float animatedT = _jumpCurve.Evaluate(t);

                    // Интерполяция позиции с добавлением высоты прыжка
                    Vector3 basePos = Vector3.Lerp(startPoint, endPoint, animatedT);

                    // Добавляем дугу прыжка (парабола)
                    float jumpOffset = Mathf.Sin(Mathf.PI * t) * _jumpHeight;

                    transform.position = new Vector3(basePos.x, basePos.y + jumpOffset, basePos.z);

                    yield return null;
                }

                transform.position = endPoint;

                // Если это не финальный хоп и мы достигли точки удара о пег
                if (!isFinalHop && hop.PegRow >= 0 && i == hop.Points.Length - 2)
                {
                    // Вызываем эффект на пеге
                    TriggerPegGlow(hop.PegRow, hop.PegCol);

                    // Воспроизводим остальные эффекты (VFX, звук)
                    PlayHitEffects(endPoint);
                }
            }
        }

        /// <summary>
        /// Находит пег по ряду и колонке и вызывает у него эффект свечения.
        /// </summary>
        private void TriggerPegGlow(int pegRow, int pegCol)
        {
            if (_allPegs == null || _allPegs.Count == 0)
            {
                Debug.LogWarning("[MathBallMover] No pegs found in scene!");
                return;
            }

            // Вычисляем ожидаемую позицию пега
            Vector3 expectedPos = GetExpectedPegPosition(pegRow, pegCol);

            // Ищем пег с соответствующими координатами
            PegView targetPeg = null;
            float minDistance = float.MaxValue;

            foreach (var peg in _allPegs)
            {
                if (peg == null) 
                    continue;

                Vector3 pegPos = peg.transform.position;
                float distance = Vector3.Distance(pegPos, expectedPos);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    targetPeg = peg;
                }
            }

            if (targetPeg != null && minDistance < 9.1f)
                targetPeg.TriggerHit();
            else
                Debug.LogWarning($"[MathBallMover] Peg not found! Closest distance={minDistance:F3}, threshold=0.5");
        }

        /// <summary>
        /// Вычисляет ожидаемую позицию пега по ряду и колонке на основе конфига.
        /// </summary>
        private Vector3 GetExpectedPegPosition(int row, int col)
        {
            int pegsInRow = _config.PegsInFirstRow + row;

            if (col < 0 || col >= pegsInRow)
                return Vector3.zero;

            float x = (col - (pegsInRow - 1) / 2f) * _config.PegSpacing;
            float y = ((_config.PegRows - 1) * _config.RowSpacing + _config.SpawnPoint.y) - row * _config.RowSpacing;

            return new Vector3(x, y, 0f);
        }

        private void PlayHitEffects(Vector3 position)
        {
            // VFX
            if (_hitVFXPrefab != null)
            {
                var vfx = Instantiate(_hitVFXPrefab, position, Quaternion.identity);
                Destroy(vfx.gameObject, 2f);
            }

            // Sound
            if (_audioSource != null && _hitSound != null)
            {
                float randomPlitch = Random.Range(_minPitch, _maxPitch);
                _audioSource.pitch = randomPlitch;
                _audioSource.PlayOneShot(_hitSound);
            }
        }

        private void OnMoveCompleted()
        {
            if (_path.BucketIndex >= 0 && _path.BucketIndex < _allBuckets.Count)
            {
                BucketView targetBucket = _allBuckets[_path.BucketIndex];
                targetBucket.InvokeBallEntered();
            }

            // Уничтожаем мяч после завершения
            Destroy(gameObject);
        }
    }
}