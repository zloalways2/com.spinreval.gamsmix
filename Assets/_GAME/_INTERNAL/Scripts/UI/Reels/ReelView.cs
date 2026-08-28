using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Reels
{
    public class ReelView : MonoBehaviour
    {
        [Header("UI элементы отображения (строго сверху вниз!)")]
        [SerializeField] private RectTransform[] _symbolSlots; // 0 = Верх, 1 = Центр, 2 = Низ
        [SerializeField] private Image[] _symbolImages;

        [Space(5), Header("Настройки движения")]
        [SerializeField] private float _spinSpeed = 1500f;
        [SerializeField] private float _turboSpinSpeed = 2500f;
        [Tooltip("Высота одной клетки = расстояние между центрами соседних символов (обычно высота барабана / 3)")]
        [SerializeField] private float _symbolHeight = 150f;

        [Space(5), Header("Blur Image (с шейдером)")]
        [Tooltip("Image с наложенным шейдером UI_TextureScroll_AAA и текстурой размытого спрайтщита")]
        [SerializeField] private Image _blurReelImage;

        private bool _isSpinning = false;
        private bool _isTurbo = false;
        private float _spinEndTime;
        private bool _shouldStop = false;
        private int[] _targetSymbols = new int[3];
        private float _calculatedRowHeight = 150f;

        private Material _blurMaterial;
        private List<Sprite> _sprites;
        private Coroutine _spinCoroutine;

        public bool IsSpinning => _isSpinning;

        private void Awake()
        {
            _calculatedRowHeight = _symbolHeight;

            for (int i = 0; i < _symbolSlots.Length; i++)
            {
                if (_symbolSlots[i] == null) 
                    continue;

                _symbolSlots[i].anchorMin = new Vector2(0.5f, 0.5f);
                _symbolSlots[i].anchorMax = new Vector2(0.5f, 0.5f);
                _symbolSlots[i].pivot = new Vector2(0.5f, 0.5f);
            }
        }

        private void OnDisable()
        {
            if (_spinCoroutine != null)
            {
                StopCoroutine(_spinCoroutine);
                _spinCoroutine = null;
            }

            foreach (var slot in _symbolSlots)
            {
                if (slot != null)
                {
                    slot.DOKill();
                    slot.localScale = Vector3.one;
                }
            }

            _isSpinning = false;
            _shouldStop = true;

            if (_blurReelImage != null) 
                _blurReelImage.enabled = false;
            SetSlotsVisibility(true);
        }

        private void OnDestroy()
        {
            // Очищаем созданный материал, чтобы избежать утечек памяти
            if (_blurMaterial != null)
                Destroy(_blurMaterial);
        }

        public void Init(List<Sprite> sprites)
        {
            _sprites = sprites;

            if (_blurReelImage != null && _blurReelImage.material != null)
            {
                _blurMaterial = new Material(_blurReelImage.material);
                _blurReelImage.material = _blurMaterial;
            }

            if (_blurReelImage != null)
                _blurReelImage.enabled = false;

            SetSlotsVisibility(true);
            ApplyRestPositions();

            for (int r = 0; r < _symbolImages.Length; r++)
                SetRandomSprite(_symbolImages[r]);
        }

        public void ApplyTurbo(bool enabled)
        {
            _isTurbo = enabled;

            // Если турбо включился прямо во время вращения
            if (_isSpinning && enabled)
            {
                // Вычисляем, сколько времени осталось крутиться
                float remainingTime = Mathf.Max(0f, _spinEndTime - Time.time);

                // Сокращаем оставшееся время (можно использовать множитель 0.5 или 0.1 для мгновенного стопа)
                _spinEndTime = Time.time + (remainingTime * 0.5f);

                if (_blurReelImage != null && _blurReelImage.enabled && _blurMaterial != null)
                    _blurMaterial.SetFloat("_ScrollY", _turboSpinSpeed / 1000f);
            }
            else if (_isSpinning && !enabled)
                if (_blurReelImage != null && _blurReelImage.enabled && _blurMaterial != null)
                    _blurMaterial.SetFloat("_ScrollY", _spinSpeed / 1000f);
        }

        public async UniTask SpinAsync(float duration, int[] targetSymbols, bool isTurbo, CancellationToken cts)
        {
            if (_isSpinning)
                return;

            _targetSymbols = targetSymbols;
            StartSpinning(isTurbo);

            // Запоминаем расчетное время окончания спина
            _spinEndTime = Time.time + duration;

            // Заменяем фиксированный UniTask.Delay на цикл, который реагирует на изменение _spinEndTime
            while (Time.time < _spinEndTime)
                await UniTask.Yield(cts);

            StopSpinning();

            // Ждем завершения анимации отскока
            while (_isSpinning)
                await UniTask.Yield(cts);
        }

        public Transform GetSlotTransform(int rowIndex)
        {
            if (rowIndex >= 0 && rowIndex < _symbolSlots.Length && _symbolSlots[rowIndex] != null)
                return _symbolSlots[rowIndex];
            return null;
        }

        private void ApplyRestPositions()
        {
            for (int r = 0; r < _symbolSlots.Length; r++)
            {
                if (_symbolSlots[r] != null)
                    _symbolSlots[r].anchoredPosition = new Vector2(0f, _symbolHeight - r * _symbolHeight);
            }
        }

        private void StartSpinning(bool isTurbo)
        {
            foreach (var slot in _symbolSlots)
            {
                if (slot != null)
                {
                    slot.DOKill();
                    slot.localScale = Vector3.one;
                }
            }

            _isSpinning = true;
            _shouldStop = false;
            _spinCoroutine = StartCoroutine(SpinRoutine(isTurbo));
        }

        private void StopSpinning()
        {
            _shouldStop = true;
        }

        private IEnumerator SpinRoutine(bool isTurbo)
        {
            if (_blurReelImage != null)
                _blurReelImage.enabled = true;
            SetSlotsVisibility(false);

            if (_blurMaterial != null)
            {
                // Берем актуальную скорость (на случай если турбо успели включить до старта корутины)
                float speed = _isTurbo ? _turboSpinSpeed : _spinSpeed;
                _blurMaterial.SetFloat("_ScrollY", speed / 1000f);
            }

            // Ждем пока контроллер не скажет остановиться (когда выйдет время)
            while (!_shouldStop)
                yield return null;

            // Останавливаем движение текстуры в шейдере
            if (_blurMaterial != null)
                _blurMaterial.SetFloat("_ScrollY", 0f);

            if (_blurReelImage != null)
                _blurReelImage.enabled = false;
            SetSlotsVisibility(true);

            PrepareFinalPositionsBeforeBounce();

            yield return AnimateBounceRoutine();
            _isSpinning = false;
            _spinCoroutine = null;
        }

        private void SetSlotsVisibility(bool isVisible)
        {
            for (int i = 0; i < _symbolImages.Length; i++)
                if (_symbolImages[i] != null) _symbolImages[i].enabled = isVisible;
        }

        private void PrepareFinalPositionsBeforeBounce()
        {
            for (int r = 0; r < _symbolSlots.Length; r++)
            {
                if (_symbolSlots[r] != null)
                {
                    // Стартовая позиция для отскока (сдвигаем всё на одну клетку вверх)
                    _symbolSlots[r].anchoredPosition = new Vector2(0f, (_calculatedRowHeight * 2f) - (r * _calculatedRowHeight));

                    if (r < _targetSymbols.Length && r < _symbolImages.Length)
                        _symbolImages[r].sprite = GetSpriteForIndex(_targetSymbols[r]);
                    else
                        SetRandomSprite(_symbolImages[r]);
                }
            }
        }

        private IEnumerator AnimateBounceRoutine()
        {
            Vector2[] targetPositions = new Vector2[_symbolSlots.Length];
            for (int r = 0; r < _symbolSlots.Length; r++)
                targetPositions[r] = new Vector2(0f, _calculatedRowHeight - (r * _calculatedRowHeight));

            // 1. Падение вниз
            float dropTime = 0.12f;
            float elapsedDrop = 0f;
            Vector2[] startPositions = new Vector2[_symbolSlots.Length];
            for (int i = 0; i < _symbolSlots.Length; i++) 
                startPositions[i] = _symbolSlots[i].anchoredPosition;

            while (elapsedDrop < dropTime)
            {
                elapsedDrop += Time.deltaTime;
                float progress = elapsedDrop / dropTime;
                for (int i = 0; i < _symbolSlots.Length; i++)
                    _symbolSlots[i].anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], progress);

                yield return null;
            }

            // 2. Отскок (Bounce)
            float bounceTime = 0.18f;
            float elapsedBounce = 0f;
            for (int i = 0; i < _symbolSlots.Length; i++) 
                startPositions[i] = _symbolSlots[i].anchoredPosition;

            while (elapsedBounce < bounceTime)
            {
                elapsedBounce += Time.deltaTime;
                float progress = elapsedBounce / bounceTime;
                // Сила отскока (12% от высоты символа)
                float bounceOffset = Mathf.Sin(progress * Mathf.PI) * (_calculatedRowHeight * 0.12f);

                for (int i = 0; i < _symbolSlots.Length; i++)
                    _symbolSlots[i].anchoredPosition = startPositions[i] - new Vector2(0f, bounceOffset);

                yield return null;
            }

            // Финальная привязка к целевым позициям
            for (int i = 0; i < _symbolSlots.Length; i++) 
                _symbolSlots[i].anchoredPosition = targetPositions[i];
        }

        private void SetRandomSprite(Image img)
        {
            if (_sprites == null || _sprites.Count == 0) 
                return;

            int rand = UnityEngine.Random.Range(0, _sprites.Count);

            if (img != null) img.sprite = 
                    _sprites[rand];
        }

        private Sprite GetSpriteForIndex(int index)
        {
            if (_sprites == null || index < 0 || index >= _sprites.Count) 
                return null;

            return _sprites[index];
        }
    }
}