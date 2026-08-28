using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UI.Other
{
    public class CustomSlider : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [Header("UI References")]
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _fill;
        [SerializeField] private RectTransform _handle;

        [Header("Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float _value = 0.5f; // Значение от 0 до 1

        // Событие, которое будет вызываться при изменении значения (аналог onValueChanged)
        public UnityEvent<float> OnValueChanged = new UnityEvent<float>();

        private float _trackWidth;
        private bool _isInitialized = false;

        public float Value => _value;

#if UNITY_EDITOR
        private void OnValidate() => SetValue(_value);
#endif

        private void Awake()
        {
            UpdateTrackWidth();
            UpdateVisuals();
            _isInitialized = true;
        }

        private void OnRectTransformDimensionsChange()
        {
            // Обновляем ширину, если слайдер меняет размер на экране
            if (_isInitialized)
            {
                UpdateTrackWidth();
                UpdateVisuals();
            }
        }

        private void UpdateTrackWidth()
        {
            // Ширина трека за вычетом ширины ручки, чтобы ручка не вылезала за края
            _trackWidth = _background.rect.width - _handle.rect.width;
        }

        // Клик мышкой по слайдеру
        public void OnPointerDown(PointerEventData eventData) => UpdateValueFromPointer(eventData);

        // Перетаскивание мышкой
        public void OnDrag(PointerEventData eventData) => UpdateValueFromPointer(eventData);

        private void UpdateValueFromPointer(PointerEventData eventData)
        {
            // Конвертируем позицию мыши в локальные координаты Background
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background,
                eventData.position,
                eventData.pressEventCamera, // Используем камеру UI
                out Vector2 localPoint);

            // Учитываем смещение, чтобы ручка начиналась с края
            float minX = -_background.rect.width / 2 + _handle.rect.width / 2;
            float maxX = _background.rect.width / 2 - _handle.rect.width / 2;

            // Вычисляем значение от 0 до 1
            _value = Mathf.InverseLerp(minX, maxX, localPoint.x);
            _value = Mathf.Clamp01(_value);

            UpdateVisuals();
            OnValueChanged?.Invoke(_value);
        }

        // Публичный метод для изменения значения из кода
        public void SetValue(float newValue)
        {
            _value = Mathf.Clamp01(newValue);
            UpdateVisuals();
            OnValueChanged?.Invoke(_value);
        }

        private void UpdateVisuals()
        {
            if (_trackWidth <= 0) UpdateTrackWidth();

            // Двигаем ручку
            // Если Pivot у Background (0.5, 0.5), то 0 по X — это центр. 
            // Смещаем от -trackWidth/2 до +trackWidth/2
            float handleX = Mathf.Lerp(-_trackWidth / 2, _trackWidth / 2, _value);
            _handle.anchoredPosition = new Vector2(handleX, 0);

            // Масштабируем Fill по ширине (он будет расти от левого края, так как Pivot X = 0)
            float fillWidth = Mathf.Lerp(0, _background.rect.width, _value);
            _fill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fillWidth);
        }
    }
}