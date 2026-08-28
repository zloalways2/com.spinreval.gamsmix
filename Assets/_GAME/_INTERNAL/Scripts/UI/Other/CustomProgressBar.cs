using DG.Tweening;
using UnityEngine;

namespace UI.Other
{
    public class CustomProgressBar : MonoBehaviour
    {
        [Header("Progress Setup")]
        [SerializeField, Range(0f, 1f)] private float _progress;

        [Space(5), Header("View Setup")]
        [SerializeField] private RectTransform _fillRect;
        [SerializeField] private RectTransform _viewportRect;
        [SerializeField] private float _animationDuration = 0.5f;

        private Tween _moveTween;
        private Vector2 _originalPosition;

        public float Progress => _progress;

#if UNITY_EDITOR
        private void OnValidate()
        {
            SetProgress(_progress);
        }
#endif

        private void Start()
        {
            _originalPosition = _fillRect.anchoredPosition;
        }

        private void OnDestroy()
        {
            _moveTween?.Kill();
        }

        public void ResetProgress()
        {
            _progress = 0f;
            _fillRect.anchoredPosition = new(_originalPosition.x, _originalPosition.y);
            _moveTween?.Kill();
        }

        public void SetProgress(float progress)
        {
            if (_viewportRect == null || _fillRect == null)
                return;

            _moveTween?.Kill();

            float viewWidth = _viewportRect.rect.width;

            float targetX = -viewWidth * (1f - progress);
            Vector2 target = new(targetX, _fillRect.anchoredPosition.y);

            _moveTween = _fillRect.DOAnchorPosX(target.x, _animationDuration).SetEase(Ease.InSine);
        }
    }
}