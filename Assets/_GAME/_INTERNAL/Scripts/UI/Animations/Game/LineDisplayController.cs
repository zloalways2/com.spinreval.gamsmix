using UnityEngine;
using System.Collections.Generic;
using UI.Reels;

namespace UI.Animations.Game
{
    public class LineDisplayController : MonoBehaviour
    {
        [Header("Настройки линии")]
        [SerializeField] private LineRenderer lineRendererPrefab;
        [SerializeField] private float lineWidth = 0.15f;
        [SerializeField] private float zOffset = -0.5f;

        [Header("🎨 Палитра цветов (AAA Разноцветие)")]
        [Tooltip("Список цветов для выигрышных линий. Если линий больше, чем цветов, они пойдут по кругу.")]
        [SerializeField]
        private List<Color> lineColors = new()
        {
            new(1f, 0.92f, 0.016f, 1f),   // Золотой / Желтый
            new(0.1f, 0.8f, 1f, 1f),      // Электрический Голубой
            new(1f, 0.2f, 0.2f, 1f),      // Огненный Красный
            new(0.2f, 1f, 0.3f, 1f),      // Лепреконский Зеленый
            new(0.8f, 0.2f, 1f, 1f)       // Магический Фиолетовый
        };

        [Header("AAA Настройки сглаживания")]
        [Range(1, 10)][SerializeField] private int segmentsPerConnection = 5;

        private readonly List<LineRenderer> _activeLines = new();

        public void DrawWinningLines(List<List<Vector2Int>> allWinningLines, ReelView[] reels)
        {
            ClearLines();

            if (allWinningLines == null || allWinningLines.Count == 0) 
                return;

            int lineVisualIndex = 0;

            // Создаем один блок свойств для оптимизации
            MaterialPropertyBlock propBlock = new();

            foreach (List<Vector2Int> singleLineCoords in allWinningLines)
            {
                if (singleLineCoords.Count < 2) 
                    continue;

                singleLineCoords.Sort((a, b) => a.x.CompareTo(b.x));

                List<Vector3> keyPositions = new();
                foreach (Vector2Int coord in singleLineCoords)
                {
                    if (coord.x >= reels.Length) 
                        continue;

                    ReelView targetReel = reels[coord.x];
                    Transform slotTransform = targetReel.GetSlotTransform(coord.y);

                    if (slotTransform != null)
                    {
                        Vector3 targetPos = slotTransform.position;
                        targetPos.z += zOffset;
                        keyPositions.Add(targetPos);
                    }
                }

                if (keyPositions.Count < 2) 
                    continue;

                List<Vector3> subDividedPositions = SubdivideLine(keyPositions, segmentsPerConnection);

                // Спавним LineRenderer
                LineRenderer line = Instantiate(lineRendererPrefab, transform);
                line.useWorldSpace = true;
                line.positionCount = subDividedPositions.Count;
                line.SetPositions(subDividedPositions.ToArray());

                line.startWidth = lineWidth;
                line.endWidth = lineWidth;

                // 🎨 1. Получаем базовый цвет из палитры инспектора
                Color baseGlowColor = LineColorVisuals(lineVisualIndex);

                // 🎨 2. Расчитываем цвет ядра (смешиваем базовый цвет с белым 50/50 для эффекта HDR-накала)
                Color coreColor = Color.Lerp(baseGlowColor, Color.white, 0.6f);

                // Применяем оба цвета в блок свойств индивидуально для этой линии
                line.GetPropertyBlock(propBlock);

                // Передаем точные имена свойств из твоего шейдера
                propBlock.SetColor("_GlowColor", baseGlowColor);
                propBlock.SetColor("_CoreColor", coreColor);

                line.SetPropertyBlock(propBlock);

                _activeLines.Add(line);
                lineVisualIndex++;
            }
        }

        /// <summary>
        /// Безопасно вытягивает цвет из списка. Если комбинаций больше пяти, 
        /// цвета пойдут по второму кругу (% оператор). Если список пуст — подстрахует дефолтным желтым.
        /// </summary>
        private Color LineColorVisuals(int index)
        {
            if (lineColors == null || lineColors.Count == 0)
                return new Color(1f, 0.92f, 0.016f, 1f); // Дефолтный золотой

            int safeIndex = index % lineColors.Count;
            return lineColors[safeIndex];
        }

        private List<Vector3> SubdivideLine(List<Vector3> originalPoints, int subDivisions)
        {
            List<Vector3> newPoints = new();

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

        public void ClearLines()
        {
            foreach (var line in _activeLines)
            {
                if (line != null) Destroy(line.gameObject);
            }
            _activeLines.Clear();
        }
    }
}