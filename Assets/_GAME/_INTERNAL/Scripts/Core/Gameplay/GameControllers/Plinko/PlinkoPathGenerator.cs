using Core.SO;
using UnityEngine;

namespace Core.Gameplay.GameControllers.Plinko
{
    public sealed class PlinkoPathGenerator
    {
        private readonly PlinkoConfig _config;
        private readonly System.Random _rng;
        private readonly int _seed;

        public PlinkoPathGenerator(PlinkoConfig config, int seed = -1)
        {
            _config = config;
            _seed = seed < 0 ? System.Environment.TickCount : seed;
            _rng = new System.Random(_seed);
        }

        public int GetPegCountInRow(int row) => _config.PegsInFirstRow + row;

        public int GetBucketCount() => _config.PegRows + 1;

        /// <summary>
        /// Выбирает индекс бакета на основе весов (weights) из конфига.
        /// </summary>
        private int SelectBucketByWeights()
        {
            if (_config.Buckets == null || _config.Buckets.Length == 0)
            {
                // Если весов нет, выбираем случайно равномерно
                return _rng.Next(GetBucketCount());
            }

            // Считаем сумму весов
            float totalWeight = 0f;
            for (int i = 0; i < _config.Buckets.Length; i++)
            {
                totalWeight += _config.Buckets[i].Weight;
            }

            if (totalWeight <= 0f)
            {
                // Если все веса нулевые, выбираем случайно равномерно
                return _rng.Next(GetBucketCount());
            }

            // Выбираем случайное число от 0 до totalWeight
            float randomValue = (float)(_rng.NextDouble() * totalWeight);
            float cumulativeWeight = 0f;

            for (int i = 0; i < _config.Buckets.Length; i++)
            {
                cumulativeWeight += _config.Buckets[i].Weight;
                if (randomValue < cumulativeWeight)
                {
                    return i;
                }
            }

            // На всякий случай возвращаем последний бакет
            return _config.Buckets.Length - 1;
        }

        /// <summary>
        /// Генерирует случайный путь мяча через пирамиду пегов.
        /// Сначала выбирается финальный бакет на основе весов,
        /// затем строится обратный путь к нему через случайные отскоки.
        /// Возвращает массив точек пути и индекс финального бакета.
        /// </summary>
        public PlinkoPath GeneratePath()
        {
            var hops = new System.Collections.Generic.List<PlinkoHop>();

            // 1. Сначала выбираем финальный бакет на основе весов
            int targetBucketIndex = SelectBucketByWeights();

            // 2. Начальная позиция мяча
            Vector3 startPosition = new(0f, GetPegPosition(0, 2).y + _config.SpawnYOffset);

            // 3. Генерируем последовательность отскоков (влево/вправо) для достижения целевого бакета
            // Для этого используем обратную логику: зная конечную позицию, определяем необходимые отскоки
            bool[] directions = GenerateDirectionsToBucket(targetBucketIndex);

            // 4. Проходим через каждый ряд пегов, строя путь
            Vector3 currentPosition = startPosition;
            int currentCol = Mathf.FloorToInt((_config.PegsInFirstRow - 1) / 2f);

            for (int row = 0; row < _config.PegRows; row++)
            {
                int pegsInRow = GetPegCountInRow(row);
                int direction = directions[row] ? 1 : 0; // true = вправо, false = влево

                int targetPegCol;

                if (direction == 0)
                {
                    // Отскок влево
                    targetPegCol = Mathf.Max(0, currentCol - 1);
                    currentCol = targetPegCol;
                }
                else
                {
                    // Отскок вправо
                    targetPegCol = Mathf.Min(pegsInRow - 1, currentCol);
                    currentCol = targetPegCol + 1;
                }

                // Ограничиваем текущую колонку допустимым диапазоном для следующего ряда
                currentCol = Mathf.Clamp(currentCol, 0, GetPegCountInRow(row + 1) - 1);

                // Позиция пега, в который попадаем
                Vector3 pegPosition = GetPegPosition(row, Mathf.Clamp(targetPegCol, 0, pegsInRow - 1));

                // Точка перед ударом о пег (чуть выше)
                Vector3 approachPoint = new Vector3(pegPosition.x, pegPosition.y + _config.RowSpacing * 0.5f, 0f);

                // Точка после удара о пег (чуть ниже, со смещением влево или вправо)
                float xOffset = (direction == 0) ? -_config.PegSpacing * 0.3f : _config.PegSpacing * 0.3f;
                Vector3 bouncePoint = new Vector3(pegPosition.x + xOffset, pegPosition.y - _config.RowSpacing * 0.3f, 0f);

                // Создаём хоп с точками пути
                Vector3[] hopPoints = new Vector3[] { currentPosition, approachPoint, bouncePoint };
                hops.Add(new PlinkoHop(hopPoints, row, Mathf.Clamp(targetPegCol, 0, pegsInRow - 1)));

                currentPosition = bouncePoint;
            }

            // Финальный хоп в бакет
            Vector3 bucketPosition = GetBucketPosition(targetBucketIndex);

            // Добавляем финальный хоп от последней точки до бакета
            Vector3[] finalHopPoints = new Vector3[] { currentPosition, bucketPosition };
            hops.Add(new PlinkoHop(finalHopPoints, -1, -1));

            return new PlinkoPath(hops.ToArray(), targetBucketIndex, _seed);
        }

        /// <summary>
        /// Генерирует последовательность отскоков (true=вправо, false=влево) для попадания в целевой бакет.
        /// Использует проверку достижимости, чтобы гарантировать, что мяч никогда не улетит в тупик у границы.
        /// </summary>
        private bool[] GenerateDirectionsToBucket(int targetBucketIndex)
        {
            bool[] directions = new bool[_config.PegRows];
            int startCol = Mathf.FloorToInt((_config.PegsInFirstRow - 1) / 2f);
            int currentCol = startCol;

            // Целевая колонка после последнего ряда. 
            // Математически финальный currentCol в GeneratePath всегда равен targetBucketIndex + startCol.
            int targetFinalCol = targetBucketIndex + startCol;

            for (int row = 0; row < _config.PegRows; row++)
            {
                int pegsInCurrentRow = GetPegCountInRow(row);
                int pegsInNextRow = GetPegCountInRow(row + 1);
                int rowsRemaining = _config.PegRows - row;

                // Вычисляем nextCol для обоих вариантов, в точности повторяя логику GeneratePath
                // Вправо (direction = true)
                int targetPegColRight = Mathf.Min(pegsInCurrentRow - 1, currentCol);
                int nextColRight = Mathf.Clamp(targetPegColRight + 1, 0, pegsInNextRow - 1);

                // Влево (direction = false)
                int targetPegColLeft = Mathf.Max(0, currentCol - 1);
                int nextColLeft = Mathf.Clamp(targetPegColLeft, 0, pegsInNextRow - 1);

                // Проверяем достижимость targetFinalCol из nextColRight и nextColLeft
                bool canReachRight = CanReach(nextColRight, targetFinalCol, rowsRemaining - 1, row + 1);
                bool canReachLeft = CanReach(nextColLeft, targetFinalCol, rowsRemaining - 1, row + 1);

                bool chooseRight;
                if (canReachRight && canReachLeft)
                {
                    // Оба варианта возможны, используем коррекцию для более естественного и прямого пути
                    int shiftNeeded = targetFinalCol - currentCol;
                    float bias = (float)shiftNeeded / rowsRemaining;
                    float probabilityRight = 0.5f + bias * 0.5f;
                    probabilityRight = Mathf.Clamp01(probabilityRight);
                    chooseRight = _rng.NextDouble() < probabilityRight;
                }
                else if (canReachRight)
                {
                    // Только правый путь ведет к цели (левый упирается в границу)
                    chooseRight = true;
                }
                else if (canReachLeft)
                {
                    // Только левый путь ведет к цели (правый упирается в границу)
                    chooseRight = false;
                }
                else
                {
                    // Fallback (теоретически недостижимо при валидных конфигурациях)
                    chooseRight = _rng.NextDouble() < 0.5;
                }

                directions[row] = chooseRight;
                currentCol = chooseRight ? nextColRight : nextColLeft;
            }

            return directions;
        }

        /// <summary>
        /// Проверяет, возможно ли достичь targetCol из currentCol за rowsRemaining шагов,
        /// учитывая физические границы пирамиды (левый край 0 и правый край pegsInRow - 1).
        /// </summary>
        private bool CanReach(int currentCol, int targetCol, int rowsRemaining, int currentRow)
        {
            if (rowsRemaining <= 0)
            {
                return currentCol == targetCol;
            }

            // Минимально возможная колонка через rowsRemaining шагов (с учетом левой границы)
            int minPossible = Mathf.Max(0, currentCol - rowsRemaining);

            // Максимально возможная колонка через rowsRemaining шагов (с учетом правой границы)
            int targetRow = currentRow + rowsRemaining;
            int pegsInTargetRow = GetPegCountInRow(targetRow);
            int maxPossible = Mathf.Min(pegsInTargetRow - 1, currentCol + rowsRemaining);

            return targetCol >= minPossible && targetCol <= maxPossible;
        }

        /// <summary>
        /// Позиция колышка. Формула центрирования сама даёт шахматное смещение:
        /// для нечётных рядов (pegsInRow - 1) / 2 даёт .5 → сдвиг на полшага.
        /// </summary>
        public Vector3 GetPegPosition(int row, int col)
        {
            var pegsInRow = GetPegCountInRow(row);

            if (col < 0 || col >= pegsInRow)
            {
                Debug.LogError($"[Plinko] Peg col {col} out of range [0..{pegsInRow - 1}] at row {row}");
                col = Mathf.Clamp(col, 0, pegsInRow - 1);
            }

            var x = (col - (pegsInRow - 1) / 2f) * _config.PegSpacing;
            var y = GetTopY() - row * _config.RowSpacing;
            return new Vector3(x, y, 0f);
        }

        /// <summary>Бакеты лежат во внутренних зазорах последнего ряда, шаг = PegSpacing.</summary>
        public Vector3 GetBucketPosition(int bucketIndex)
        {
            var count = GetBucketCount();
            if (bucketIndex < 0 || bucketIndex >= count)
            {
                Debug.LogError($"[Plinko] Bucket {bucketIndex} out of range [0..{count - 1}]");
                bucketIndex = Mathf.Clamp(bucketIndex, 0, count - 1);
            }

            var x = (bucketIndex - (count - 1) / 2f) * _config.BucketSpacing;
            var y = GetTopY() - _config.PegRows * _config.RowSpacing - _config.RowSpacing * 0.5f;
            return new Vector3(x, y, 0f);
        }

        private float GetTopY() => (_config.PegRows - 1) * _config.RowSpacing + _config.SpawnPoint.y;
    }
}