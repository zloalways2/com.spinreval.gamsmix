using Core;
using Core.Services.Audio;
using Solo.MOST_IN_ONE;
using System;
using System.Collections;
using UI.Animations.GameScreen;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Other
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(RectTransform))]
    public class ActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float _headRepeatTime = 0.1f;
        [SerializeField] private MOST_HapticFeedback.HapticTypes _onClick = MOST_HapticFeedback.HapticTypes.SoftImpact;
        [SerializeField] private ButtonAnimations _animations;
        [SerializeField] private RectTransform _rectTransform;

        private Button _button;
        private bool _wasHeld;

        private Coroutine _heldCoroutine;

        public bool Interactable
        {
            get
            {
                return _button.interactable;
            }
            set
            {
                _button.interactable = value;
            }
        }
        public bool IsUseHeldFunc { get; set; } = false;
        public bool IsHeld { get; private set; }
        public ButtonAnimations Animations => _animations;

        public event Action OnButtonClick;

        private void Awake()
        {
            if(_button == null)
                _button = GetComponent<Button>();

            if(_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if(!_animations.Initialized)
                _animations.Init(_rectTransform);
        }

        private void Start() => _button.onClick.AddListener(HandleButtonClick);

        private void OnDestroy()
        {
            _animations.StopAnimations();
            _button.onClick.RemoveAllListeners();
            OnButtonClick = null;
        }

        public void ForceInit()
        {
            _button = GetComponent<Button>();
            _rectTransform = GetComponent<RectTransform>();

            _animations.Init(_rectTransform);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsUseHeldFunc)
                return;

            IsHeld = false;
            _animations.ClickUpAnimation();

            if (_heldCoroutine != null)
            {
                StopCoroutine(_heldCoroutine);
                _heldCoroutine = null;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsUseHeldFunc)
                return;

            IsHeld = true;
            _wasHeld = false;
            _animations.ClickDownAnimation();
            _heldCoroutine = StartCoroutine(HeldRepeatRoutine());
        }

        private IEnumerator HeldRepeatRoutine()
        {
            yield return new WaitForSeconds(_headRepeatTime);

            if (!IsHeld)
                yield break;

            OnButtonClick?.Invoke();
            _wasHeld = true;

            while (IsHeld)
            {
                yield return new WaitForSeconds(_headRepeatTime);
                if (!IsHeld) 
                    break;
                OnButtonClick?.Invoke();
            }
        }

        private void HandleButtonClick()
        {
            if (_wasHeld)
            {
                _wasHeld = false;
                return;
            }

            if (IsHeld)
                return;

            Interactable = false;

            if (PlayerPrefs.GetInt(GameConstants.KEY_VIBRATIONS) == 1)
                MOST_HapticFeedback.Generate(_onClick);

            AudioService.Instance.PlaySfx(SoundType.Click);

            _animations.ButtonClickAnimation(() =>
            {
                Interactable = true;
                OnButtonClick?.Invoke();
            });
        }
    }
}