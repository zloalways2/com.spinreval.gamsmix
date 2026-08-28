using System.Collections.Generic;
using UnityEngine;

namespace Core.Gameplay.GameControllers.CryptoVibe
{
    public class CryptoVibeLineRenderController : MonoBehaviour
    {
        [Header("Настройки линии")]
        [SerializeField] private LineRenderer _lineRendererPrefab;
        [SerializeField] private float _lineWidth = 0.15f;
        [SerializeField] private float _zOffset = -0.5f;

        [Header("Палитра цветов (градиент пути)")]
        [Tooltip("Градиент цветов для линии пути ракеты")]
        [SerializeField]
        private Gradient _pathGradient = new();

        [Header("AAA Настройки сглаживания")]
        [Range(1, 10)][SerializeField] private int _segmentsPerConnection = 5;

        [Header("Canvas Reference")]
        [Tooltip("Canvas для корректного преобразования координат")]
        [SerializeField] private RectTransform _canvasRectTransform;
        [SerializeField] private RectTransform _rocketTransform;

        private LineRenderer _activeLine;
        private bool _isWorldSpaceCanvas;

        private List<Vector3> _allPathPositions = new();
        private int _currentDrawIndex;
        private bool _isDrawing;
        private RectTransform _graphContainer;

        public int SegmentsPerConnection => _segmentsPerConnection;

        private void Awake()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _isWorldSpaceCanvas = canvas.renderMode == RenderMode.WorldSpace ||
                                    canvas.renderMode == RenderMode.ScreenSpaceCamera;
            }

            if (_pathGradient.colorKeys.Length == 0)
            {
                SetupDefaultGradient();
            }
        }

        private void SetupDefaultGradient()
        {
            GradientColorKey[] colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.1f, 0.8f, 1f, 1f), 0f),      // Голубой (старт)
                new GradientColorKey(new Color(0.8f, 0.2f, 1f, 1f), 0.5f),   // Фиолетовый (середина)
                new GradientColorKey(new Color(1f, 0.2f, 0.2f, 1f), 1f)      // Красный (краш)
            };

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };

            _pathGradient.SetKeys(colorKeys, alphaKeys);
        }

        public void InitPath(CryptoPath path, RectTransform graphContainer)
        {
            ClearLine();

            if (path == null || path.AscentPoints == null || path.AscentPoints.Length == 0)
                return;

            _graphContainer = graphContainer;
            _allPathPositions.Clear();

            Vector3 worldOffsetToCenter = Vector3.zero;
            if (_rocketTransform != null)
            {
                Vector3 centerWorld = _rocketTransform.TransformPoint(_rocketTransform.rect.center);
                worldOffsetToCenter = centerWorld - _rocketTransform.position;
            }

            foreach (Vector3 point in path.AscentPoints)
            {
                Vector3 adjustedPoint = ConvertPositionForCanvas(point, graphContainer);
                adjustedPoint.z += _zOffset;

                if (_rocketTransform != null)
                    adjustedPoint += worldOffsetToCenter;

                _allPathPositions.Add(adjustedPoint);
            }

            // ИСПРАВЛЕНИЕ: Пропускаем первую точку падения, так как она дублирует точку краша
            // Это убирает рассинхронизацию индексов между линией и движением ракеты
            if (path.DescentPoints != null && path.DescentPoints.Length > 1)
            {
                for (int i = 1; i < path.DescentPoints.Length; i++)
                {
                    Vector3 adjustedPoint = ConvertPositionForCanvas(path.DescentPoints[i], graphContainer);
                    adjustedPoint.z += _zOffset;

                    if (_rocketTransform != null)
                        adjustedPoint += worldOffsetToCenter;

                    _allPathPositions.Add(adjustedPoint);
                }
            }

            if (_allPathPositions.Count < 2)
                return;

            List<Vector3> subDividedPositions = SubdivideLine(_allPathPositions, _segmentsPerConnection);
            _allPathPositions = subDividedPositions;

            _activeLine = Instantiate(_lineRendererPrefab, transform);
            _activeLine.useWorldSpace = true;
            _activeLine.positionCount = 0;
            _activeLine.startWidth = _lineWidth;
            _activeLine.endWidth = _lineWidth;
            
            // ИСПРАВЛЕНИЕ: Задаём градиент один раз при инициализации, а не каждый кадр
            _activeLine.colorGradient = _pathGradient;

            _currentDrawIndex = 0;
            _isDrawing = true;
        }

        public void UpdateLine(int progressIndex)
        {
            if (!_isDrawing || _activeLine == null || _allPathPositions.Count == 0)
                return;

            int targetIndex = Mathf.Min(progressIndex, _allPathPositions.Count - 1);

            if (targetIndex >= _allPathPositions.Count - 1)
            {
                _isDrawing = false;
                targetIndex = _allPathPositions.Count - 1;
            }

            if (targetIndex + 1 > _currentDrawIndex)
            {
                _currentDrawIndex = targetIndex + 1;
                _activeLine.positionCount = _currentDrawIndex;

                // ИСПРАВЛЕНИЕ: Избегаем выделения памяти (new Vector3[]) каждый кадр
                for (int i = 0; i < _currentDrawIndex; i++)
                {
                    _activeLine.SetPosition(i, _allPathPositions[i]);
                }
            }
        }

        public void DrawFullpath(CryptoPath path, RectTransform graphContainer)
        {
            InitPath(path, graphContainer);
            UpdateLine(_allPathPositions.Count - 1);
            _isDrawing = false;
        }

        private Vector3 ConvertPositionForCanvas(Vector3 localPosition, RectTransform graphContainer)
        {
            if (graphContainer == null)
                return localPosition;

            if (_isWorldSpaceCanvas && _canvasRectTransform != null)
            {
                return graphContainer.TransformPoint(localPosition);
            }

            return localPosition;
        }

        private List<Vector3> SubdivideLine(List<Vector3> originalPoints, int subDivisions)
        {
            List<Vector3> newPoints = new List<Vector3>();

            for (int i = 0; i < originalPoints.Count - 1; i++)
            {
                Vector3 startNode = originalPoints[i];
                Vector3 endNode = originalPoints[i + 1];

                newPoints.Add(startNode);

                for (int j = 1; j < subDivisions; j++)
                {
                    float t = (float)j / subDivisions;
                    Vector3 intermediatePoint = Vector3.Lerp(startNode, endNode, t);
                    newPoints.Add(intermediatePoint);
                }
            }

            newPoints.Add(originalPoints[^1]);
            return newPoints;
        }

        public void ClearLine()
        {
            if (_activeLine != null)
            {
                Destroy(_activeLine.gameObject);
                _activeLine = null;
            }
        }

        public void SetLineRendererPrefab(LineRenderer prefab)
        {
            _lineRendererPrefab = prefab;
        }

        public void SetLineWidth(float width)
        {
            _lineWidth = width;
            if (_activeLine != null)
            {
                _activeLine.startWidth = width;
                _activeLine.endWidth = width;
            }
        }
    }
}