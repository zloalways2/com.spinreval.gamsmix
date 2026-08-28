using Core;
using DG.Tweening;
using System;
using UI.Other;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settigns
{
    public class SettingsView : Window
    {
        [Header("Buttons")]
        [SerializeField] private ActionButton _toggleNotifications;
        [SerializeField] private ActionButton _toggleVibrations;

        [Space(5), Header("Toogle Sprites")]
        [SerializeField] private Sprite _onSprite;
        [SerializeField] private Sprite _offSprite;
        [SerializeField] private Image _notificationsImage;
        [SerializeField] private Image _vibrationsImage;

        [Space(5), Header("Animation Setup")]
        [SerializeField] private float _toggleAnimationDuration = 0.5f;
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private RectTransform _notificationsHandlerRect;
        [SerializeField] private RectTransform _vibrationsHandlerRect;
        [SerializeField] private float _offTogglePostionX;
        [SerializeField] private float _onTogglePositionX;

        private bool _notificationsState = false;
        private bool _vibrationsState = false;

        private Tween _openTween;
        private Tween _toggleNottificationsTween;
        private Tween _toggleVibrationsTween;

        private void Awake()
        {
            _rectTransform.localScale = Vector3.zero;

            _toggleNotifications.OnButtonClick += HandleToggleNotifications;
            _toggleVibrations.OnButtonClick += HandleToggleVibrations;

            _notificationsState = PlayerPrefs.GetInt(GameConstants.KEY_NOTIFICATIONS, 1) == 1;
            _vibrationsState = PlayerPrefs.GetInt(GameConstants.KEY_VIBRATIONS, 1) == 1;
        }

        private void Start()
        {
            InitToggles();

            Open();
        }

        private void OnDestroy()
        {
            _openTween?.Kill();
            _toggleNottificationsTween?.Kill();
            _toggleVibrationsTween?.Kill();

            _toggleNotifications.OnButtonClick -= HandleToggleNotifications;
            _toggleVibrations.OnButtonClick -= HandleToggleVibrations;
        }

        public override void Open(Action onComplete = null)
        {
            gameObject.SetActive(true);

            _openTween?.Kill();

            _openTween = _rectTransform
                .DOScale(1f, _toggleAnimationDuration)
                .SetEase(Ease.OutSine);
        }

        private void InitToggles()
        {
            if (_notificationsState)
            {
                _notificationsImage.sprite = _onSprite;
                _notificationsHandlerRect.anchoredPosition = new(_onTogglePositionX, 0f);
                PlayerPrefs.SetInt(GameConstants.KEY_NOTIFICATIONS, 1);
            }
            else
            {
                _notificationsImage.sprite = _offSprite;
                _notificationsHandlerRect.anchoredPosition = new(_offTogglePostionX, 0f);
                PlayerPrefs.SetInt(GameConstants.KEY_NOTIFICATIONS, 0);
            }

            if (_vibrationsState)
            {
                _vibrationsImage.sprite = _onSprite;
                _vibrationsHandlerRect.anchoredPosition = new(_onTogglePositionX, 0f);
                PlayerPrefs.SetInt(GameConstants.KEY_VIBRATIONS, 1);
            }
            else
            {
                _vibrationsImage.sprite = _offSprite;
                _vibrationsHandlerRect.anchoredPosition = new(_offTogglePostionX, 0f);
                PlayerPrefs.SetInt(GameConstants.KEY_VIBRATIONS, 0);
            }

            PlayerPrefs.Save();
        }

        private void HandleToggleNotifications()
        {
            if (_notificationsState)
            {
                _toggleNottificationsTween?.Kill();
                _toggleNottificationsTween = 
                    _notificationsHandlerRect
                    .DOAnchorPosX(_offTogglePostionX, 0.15f)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        _notificationsImage.sprite = _offSprite;
                        _notificationsState = false;
                        PlayerPrefs.SetInt(GameConstants.KEY_NOTIFICATIONS, 0);
                    });
            }
            else
            {
                _toggleNottificationsTween?.Kill();
                _toggleNottificationsTween =
                    _notificationsHandlerRect
                    .DOAnchorPosX(_onTogglePositionX, 0.15f)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        _notificationsImage.sprite = _onSprite;
                        _notificationsState = true;
                        PlayerPrefs.SetInt(GameConstants.KEY_NOTIFICATIONS, 1);
                    });
            }

            PlayerPrefs.Save();
        }

        private void HandleToggleVibrations()
        {
            if (_vibrationsState)
            {
                _toggleVibrationsTween?.Kill();
                _toggleVibrationsTween =
                    _vibrationsHandlerRect
                    .DOAnchorPosX(_offTogglePostionX, 0.15f)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        _vibrationsImage.sprite = _offSprite;
                        _vibrationsState = false;
                        PlayerPrefs.SetInt(GameConstants.KEY_VIBRATIONS, 0);
                    });
            }
            else
            {
                _toggleVibrationsTween?.Kill();
                _toggleVibrationsTween =
                    _vibrationsHandlerRect
                    .DOAnchorPosX(_onTogglePositionX, 0.15f)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        _vibrationsImage.sprite = _onSprite;
                        _vibrationsState = true;
                        PlayerPrefs.SetInt(GameConstants.KEY_VIBRATIONS, 1);
                    });
            }

            PlayerPrefs.Save();
        }
    }
}