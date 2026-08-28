using DG.Tweening;
using System;
using UnityEngine;

namespace UI.Animations.Game
{
    public abstract class ObjectAnimations : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private RectTransform _object;

        [Space(5), Header("Animation Duration Setup")]
        [SerializeField] protected float _scaleAppearAnimationDuration = 0.35f;
        [SerializeField] protected float _hoverAnimationDuration = 0.5f;
        [SerializeField] protected float _pulseAnimationDuration = 1f;
        [SerializeField] protected float _movingAppearAnimationDuration = 1f;

        [Space(5), Header("Move Appear Animation Setup")]
        [SerializeField] private Vector3 _targetPosition;
        [SerializeField] private Ease _movingAppearEase = Ease.InOutBack;

        [Space(5), Header("Hover Animation Setup")]
        [SerializeField] private float _yMoveOffset = 1.0f;
        [SerializeField] private LoopType _hoverLoopType = LoopType.Yoyo;
        [SerializeField, Tooltip("Set -1 for infinite loops count")] private int _hoverLoopCount = -1;

        [Space(5), Header("Pulse Animation Setup")]
        [SerializeField] private float _pulseTargetScale = 0.9f;
        [SerializeField] private LoopType _pulseLoopType = LoopType.Yoyo;
        [SerializeField, Tooltip("Set -1 for infinite loops count")] private int _pulseLoopsCount = -1;
        [SerializeField, Tooltip("Using for cyclic breath")] protected float _pulseDelay = 0.25f;

        [Space(5), Header("Animations Flags")]
        [SerializeField] protected bool _hoverAnimationEnabled = false;
        [SerializeField] protected bool _pulseAnimationEnabled = false;
        [SerializeField] protected bool _usePulseAnimationSequence = false;

        private Vector3 _originalScale;

        private Tween _scaleAppearTween;
        private Tween _movingAppearTween;
        private Tween _hoverTween;
        private Tween _pulseTween;
        private Sequence _cyclicPulseSequence;

        private void Awake()
        {
            if (_object == null)
                _object.GetComponent<RectTransform>();

            _originalScale = _object.localScale;
        }

        private void OnEnable()
        {
            if (_hoverAnimationEnabled)
                HoverAnimation();

            if (_pulseAnimationEnabled && !_usePulseAnimationSequence)
                PulseAnimation();

            if(_usePulseAnimationSequence && !_pulseAnimationEnabled)
                CyclicPulseAnimation();
        }

        private void OnDisable()
        {
            _hoverTween?.Kill();
            _scaleAppearTween?.Kill();
            _pulseTween?.Kill();
            _movingAppearTween?.Kill();
            
            if(_usePulseAnimationSequence)
                _cyclicPulseSequence?.Kill();
        }

        private void OnDestroy()
        {
            _hoverTween?.Kill();
            _scaleAppearTween?.Kill();
            _pulseTween?.Kill();
            _movingAppearTween?.Kill();

            if(_usePulseAnimationSequence)
                _cyclicPulseSequence?.Kill();
        }

        protected void HoverAnimation()
        {
            _hoverTween?.Kill();

            Vector3 originalPosition = _object.localPosition;
            float targetY = _object.localPosition.y + _yMoveOffset;

            _hoverTween = _object
                .DOLocalMoveY(targetY, _hoverAnimationDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(_hoverLoopCount, _hoverLoopType)
                .OnKill(() => _object.localPosition = originalPosition);
        }

        protected void CyclicPulseAnimation()
        {
            _cyclicPulseSequence?.Kill();

            _cyclicPulseSequence = DOTween.Sequence();

            _cyclicPulseSequence
                .AppendInterval(_pulseDelay)
                .Append(_object.DOScale(_pulseTargetScale, _pulseAnimationDuration))
                .Append(_object.DOScale(_originalScale, _pulseAnimationDuration))
                .SetLoops(_pulseLoopsCount, _pulseLoopType)
                .SetEase(Ease.InOutSine);
        }

        protected void PulseAnimation()
        {
            _pulseTween?.Kill();

            _originalScale = _object.localScale;

            _pulseTween = _object
                .DOScale(_pulseTargetScale, _pulseAnimationDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(_pulseLoopsCount, _pulseLoopType);
        }

        public void MovingAppear()
        {
            _movingAppearTween?.Kill();

            _movingAppearTween = _object
                .DOAnchorPos(_targetPosition, _movingAppearAnimationDuration)
                .SetEase(_movingAppearEase)
                .OnComplete(() => _object.anchoredPosition = _targetPosition);
        }

        public void ScaleAppear(Vector3 originalScale, Action onComplete = null)
        {
            _object.localScale = Vector3.zero;
            _originalScale = originalScale;

            _scaleAppearTween?.Kill();

            _scaleAppearTween = _object
                .DOScale(originalScale, _scaleAppearAnimationDuration)
                .SetEase(Ease.InOutBounce)
                .OnComplete(() => onComplete?.Invoke())
                .OnKill(() => _object.localScale = _originalScale);
        }
    }
}