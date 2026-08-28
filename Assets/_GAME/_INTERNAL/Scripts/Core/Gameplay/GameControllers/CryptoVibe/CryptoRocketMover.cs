using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

namespace Core.Gameplay.GameControllers.CryptoVibe
{
     public class CryptoRocketMover : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _movementSpeed = 1f;
        [SerializeField] private float _rotationAngleOffset = 0f; 
        [SerializeField] private float _descentSpeedMultiplier = 3f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem _explosionVFXPrefab;
        [SerializeField] private AudioClip _explosionSound;
        [SerializeField] private AudioSource _audioSource;

        private CryptoPath _currentPath;
        private Coroutine _moveCoroutine;
        private Transform _rocketTransform;

        private bool _isMoving;

        private Tween _rotationTween;
        private float _currentAngle;

        private int _totalAscentSegments;
        private int _totalDescentSegments;

        public int CurrentProgressIndex { get; private set; }
        public int SegmentsPerConnection { get; private set;  }

        private void OnDestroy()
        {
            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _rotationTween?.Kill();
            CurrentProgressIndex = 0;
        }

        public void Initialize(Transform rocketTransform, int segmentsPerConnection)
        {
            _rocketTransform = rocketTransform;
            SegmentsPerConnection = segmentsPerConnection;

            if (_rocketTransform != null)
                _currentAngle = _rocketTransform.rotation.eulerAngles.z;
        }

        public void StartMove(CryptoPath path, float flightTime)
        {
            if (_rocketTransform == null)
            {
                Debug.LogError("[CryptoVibeRocketMover] Rocket transform is not initialized!");
                return;
            }

            CurrentProgressIndex = 0;
            _currentPath = path;
            _isMoving = true;

            float totalDistance = 0f;
            for (int i = 0; i < _currentPath.AscentPoints.Length - 1; i++)
                totalDistance += Vector3.Distance(_currentPath.AscentPoints[i], _currentPath.AscentPoints[i + 1]);

            float safeFlightTime = Mathf.Max(0.1f, flightTime);
            _movementSpeed = totalDistance / safeFlightTime;

            _totalAscentSegments = (_currentPath.AscentPoints.Length - 1) * SegmentsPerConnection;

            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _moveCoroutine = StartCoroutine(MoveAlongPath());
        }

        public void StopMove()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            _rotationTween?.Kill();
            _rotationTween = null;
            _isMoving = false;
        }

        public void StartDescent(Action onComplete = null)
        {
            if (_currentPath == null || _currentPath.DescentPoints == null || _currentPath.DescentPoints.Length == 0)
            {
                Debug.LogWarning("[CryptoVibeRocketMover] No descent path available!");
                onComplete?.Invoke();
                return;
            }

            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _rotationTween?.Kill();
            _rotationTween = null;

            // УБРАНО: _rocketTransform.rotation = Quaternion.identity; 
            // УБРАНО: _currentAngle = 0f;

            _moveCoroutine = StartCoroutine(PlayDescentAnimation(onComplete));
        }

        private Vector3 GetPointOnPathByProgress(Vector3[] points, float progress)
        {
            if (points == null || points.Length == 0) 
                return Vector3.zero;
            if (points.Length == 1) 
                return points[0];

            float totalLength = 0f;
            float[] segmentLengths = new float[points.Length - 1];

            for (int i = 0; i < points.Length - 1; i++)
            {
                segmentLengths[i] = Vector3.Distance(points[i], points[i + 1]);
                totalLength += segmentLengths[i];
            }

            float targetDistance = progress * totalLength;
            float accumulated = 0f;

            for (int i = 0; i < segmentLengths.Length; i++)
            {
                if (accumulated + segmentLengths[i] >= targetDistance)
                {
                    float localT = (targetDistance - accumulated) / segmentLengths[i];
                    return Vector3.Lerp(points[i], points[i + 1], localT);
                }
                accumulated += segmentLengths[i];
            }

            return points[^1];
        }

        private IEnumerator MoveAlongPath()
        {
            Vector3[] points = _currentPath.AscentPoints;
            if (points == null || points.Length < 2)
            {
                _rocketTransform.position = points[0];
                CurrentProgressIndex = _totalAscentSegments;
                yield break;
            }

            float totalDistance = 0f;
            float[] segmentLengths = new float[points.Length - 1];
            for (int i = 0; i < points.Length - 1; i++)
            {
                segmentLengths[i] = Vector3.Distance(points[i], points[i + 1]);
                totalDistance += segmentLengths[i];
            }

            _rocketTransform.position = points[0];
            float elapsed = 0f;
            float duration = totalDistance / Mathf.Max(0.1f, _movementSpeed);

            while (elapsed < duration && _isMoving)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Без аллокаций вычисляем позицию
                float targetDistance = t * totalDistance;
                float accumulated = 0f;
                Vector3 pos = points[^1];
                for (int i = 0; i < segmentLengths.Length; i++)
                {
                    if (accumulated + segmentLengths[i] >= targetDistance)
                    {
                        float localT = (targetDistance - accumulated) / segmentLengths[i];
                        pos = Vector3.Lerp(points[i], points[i + 1], localT);
                        break;
                    }
                    accumulated += segmentLengths[i];
                }

                _rocketTransform.position = pos;

                // Направление и поворот
                float nextT = Mathf.Min(1f, t + 0.01f);
                float nextTargetDistance = nextT * totalDistance;
                accumulated = 0f;
                Vector3 nextPos = points[^1];
                for (int i = 0; i < segmentLengths.Length; i++)
                {
                    if (accumulated + segmentLengths[i] >= nextTargetDistance)
                    {
                        float localT = (nextTargetDistance - accumulated) / segmentLengths[i];
                        nextPos = Vector3.Lerp(points[i], points[i + 1], localT);
                        break;
                    }
                    accumulated += segmentLengths[i];
                }

                Vector3 dir = nextPos - pos;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + _rotationAngleOffset;
                    _currentAngle = Mathf.MoveTowardsAngle(_currentAngle, targetAngle, 90f * Time.deltaTime);
                    _rocketTransform.rotation = Quaternion.Euler(0f, 0f, _currentAngle);
                }

                CurrentProgressIndex = Mathf.FloorToInt(t * _totalAscentSegments);
                yield return null;
            }

            _rocketTransform.position = points[^1];
            CurrentProgressIndex = _totalAscentSegments;
            _isMoving = false;
        }

        private IEnumerator PlayDescentAnimation(Action onComplete)
        {
            Vector3[] points = _currentPath.DescentPoints;
            if (points == null || points.Length == 0)
            {
                onComplete?.Invoke();
                yield break;
            }

            float totalDistance = 0f;
            float[] segmentLengths = new float[points.Length - 1];
            for (int i = 0; i < points.Length - 1; i++)
            {
                segmentLengths[i] = Vector3.Distance(points[i], points[i + 1]);
                totalDistance += segmentLengths[i];
            }

            float descentSpeed = _movementSpeed * _descentSpeedMultiplier;
            float totalDuration = totalDistance / Mathf.Max(0.1f, descentSpeed);

            PlayExplosionEffect();

            int ascentMaxIndex = CurrentProgressIndex;
            _totalDescentSegments = (points.Length - 1) * SegmentsPerConnection;

            float elapsed = 0f;
            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / totalDuration);
                float easedT = t * t;

                float targetDistance = easedT * totalDistance;
                float accumulated = 0f;
                Vector3 pos = points[^1];
                for (int i = 0; i < segmentLengths.Length; i++)
                {
                    if (accumulated + segmentLengths[i] >= targetDistance)
                    {
                        float localT = (targetDistance - accumulated) / segmentLengths[i];
                        pos = Vector3.Lerp(points[i], points[i + 1], localT);
                        break;
                    }
                    accumulated += segmentLengths[i];
                }

                float nextT = Mathf.Min(1f, easedT + 0.05f);
                float nextTargetDistance = nextT * totalDistance;
                accumulated = 0f;
                Vector3 nextPos = points[^1];
                for (int i = 0; i < segmentLengths.Length; i++)
                {
                    if (accumulated + segmentLengths[i] >= nextTargetDistance)
                    {
                        float localT = (nextTargetDistance - accumulated) / segmentLengths[i];
                        nextPos = Vector3.Lerp(points[i], points[i + 1], localT);
                        break;
                    }
                    accumulated += segmentLengths[i];
                }

                Vector3 dir = nextPos - pos;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    _currentAngle = Mathf.MoveTowardsAngle(_currentAngle, targetAngle, 180f * Time.deltaTime);
                    _rocketTransform.rotation = Quaternion.Euler(0f, 0f, _currentAngle);
                }

                _rocketTransform.position = pos;

                int descentProgress = Mathf.FloorToInt(easedT * _totalDescentSegments);
                CurrentProgressIndex = ascentMaxIndex + descentProgress;

                yield return null;
            }

            _rocketTransform.position = points[^1];
            CurrentProgressIndex = ascentMaxIndex + _totalDescentSegments;

            onComplete?.Invoke();
        }

        private void PlayExplosionEffect()
        {
            if (_explosionVFXPrefab != null)
            {
                var vfx = Instantiate(_explosionVFXPrefab, _rocketTransform.position, Quaternion.identity);
                var ps = vfx.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    float duration = main.duration + main.startLifetime.constant;
                    Destroy(vfx.gameObject, duration);
                }
                else
                {
                    Destroy(vfx.gameObject, 2f);
                }
            }

            if (_explosionSound != null && _audioSource != null)
                _audioSource.PlayOneShot(_explosionSound);
        }

        public void SetEffects(ParticleSystem explosionVFX, AudioClip explosionSound, AudioSource audioSource)
        {
            _explosionVFXPrefab = explosionVFX;
            _explosionSound = explosionSound;
            _audioSource = audioSource;
        }

        public void SetMovementSpeed(float speed)
        {
            _movementSpeed = speed;
        }

        public void SetRotationAngleOffset(float angleOffset)
        {
            _rotationAngleOffset = angleOffset;
        }
    }
}