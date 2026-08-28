using Core.Gameplay.GameControllers.CryptoVibe;
using DG.Tweening;
using System;
using TMPro;
using UI.Other;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CryptoVibe
{
    public class CryptoVibeView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_InputField _betInputField;
        [SerializeField] private TextMeshProUGUI _currentBetLabel;
        [SerializeField] private TextMeshProUGUI _multiplierLabel;
        [SerializeField] private ActionButton _startButton;
        [SerializeField] private ActionButton _ejectButton;
        [SerializeField] private ResultPanelView _resultPanelView;
        [SerializeField] private WarningMessageView _warningMessageView;
        [SerializeField] private RectTransform _grid;

        [Space(5), Header("Visuals")]
        [SerializeField] private RectTransform _graphContainer;
        [SerializeField] private RectTransform _rocketTransform;
        [SerializeField] private Image _rocketImage;
        [SerializeField] private CryptoRocketMover _rocketMover;
        [SerializeField] private CryptoVibeLineRenderController _lineRenderer;

        [Space(5), Header("Grid Reference")]
        [Tooltip("Фоновая сетка для определения границ полёта ракеты")]
        [SerializeField] private RectTransform _backgroundGrid;
        [SerializeField] private Sprite _rocketSprite;
        [SerializeField] private Sprite _crashedRocketSprite;

        [Space(5), Header("Fly Settings")]
        [SerializeField, Min(0.1f)] private float _movementSpeed = 1f;
        [SerializeField] private float _rotationAngleOffset = 0f;
        [SerializeField] private Vector2 _fallTargetPosition = new Vector2(0f, -5f);
        [SerializeField, Min(0.1f)] private float _ascentDeviation = 0.5f;
        [SerializeField, Min(0.1f)] private float _descentDeviation = 1f;

        [Space(5), Header("Effects")]
        [SerializeField] private ParticleSystem _explosionVFXPrefab;
        [SerializeField] private AudioClip _explosionSound;
        [SerializeField] private AudioSource _audioSource;

        private Vector2 _originalRocketPosition;

        private CryptoPathGenerator _pathGenerator;
        private CryptoPath _currentPath;

        private float _crashMultiplier;
        private bool _isInitialized;

        public event Action OnStartClicked;
        public event Action OnEjectClicked;
        public event Action OnRestartButtonClicked;
        public event Action<float> OnBetChanged;

        private void Start()
        {
            if (_startButton != null)
                _startButton.OnButtonClick += HandleStartButtonClick;

            if (_ejectButton != null)
                _ejectButton.OnButtonClick += HandleEjectButtonClick;

            if (_betInputField != null)
                _betInputField.onEndEdit.AddListener(HandleBetInput);

            if (_resultPanelView != null)
                _resultPanelView.OnRestartGameButtonClick += HandleRestartButtonClick;

            if (_rocketTransform != null)
                _originalRocketPosition = _rocketTransform.anchoredPosition;

            _resultPanelView.SetAudioSource(_audioSource);

            InitializeRocketSystem();
        }

        private void Update()
        {
            if (_lineRenderer != null && _rocketTransform != null && _graphContainer != null)
                _lineRenderer.UpdateLine(_rocketMover.CurrentProgressIndex);
        }

        private void OnDestroy()
        {
            if (_startButton != null)
                _startButton.OnButtonClick -= HandleStartButtonClick;

            if (_ejectButton != null)
                _ejectButton.OnButtonClick -= HandleEjectButtonClick;

            if (_betInputField != null)
                _betInputField.onEndEdit.RemoveListener(HandleBetInput);

            if (_resultPanelView != null)
                _resultPanelView.OnRestartGameButtonClick -= HandleRestartButtonClick;

            _rocketTransform.rotation = Quaternion.Euler(0f, 0f, _rotationAngleOffset);

            if (_rocketMover != null)
                _rocketMover.StopMove();

            if (_lineRenderer != null)
                _lineRenderer.ClearLine();

            _currentPath = null;
        }

        private void InitializeRocketSystem()
        {
            if (_graphContainer == null || _rocketTransform == null)
            {
                Debug.LogError("[CryptoVibeView] Graph container or rocket transform is missing!");
                return;
            }

            // Создаём генератор пути
            _pathGenerator = new CryptoPathGenerator(
                maxMultiplier: 35f,
                gridContainer: _graphContainer,
                fallTargetPosition: _fallTargetPosition,
                ascentDeviation: _ascentDeviation,
                descentDeviation: _descentDeviation
            );

            // Добавляем компонент движения на ракету
            _rocketMover = _rocketTransform.gameObject.GetComponent<CryptoRocketMover>();
            if (_rocketMover == null)
                _rocketMover = _rocketTransform.gameObject.AddComponent<CryptoRocketMover>();

            _rocketMover.Initialize(_rocketTransform, _lineRenderer.SegmentsPerConnection);
            _rocketMover.SetMovementSpeed(_movementSpeed);
            _rocketMover.SetEffects(_explosionVFXPrefab, _explosionSound, _audioSource);
            _rocketMover.SetRotationAngleOffset(_rotationAngleOffset);

            _isInitialized = true;
        }

        // ------------------------------------------------------------------
        // RESET
        // ------------------------------------------------------------------

        public void ResetView()
        {
            if (_rocketTransform != null)
            {
                if (_rocketTransform is RectTransform rectTransform)
                    rectTransform.anchoredPosition = _originalRocketPosition;

                _rocketTransform.gameObject.SetActive(true);

                // Сбрасываем вращение
                _rocketTransform.rotation = Quaternion.identity;
            }

            if (_rocketImage != null)
                _rocketImage.sprite = _rocketSprite;

            if (_graphContainer != null)
                _graphContainer.anchoredPosition = Vector2.zero;

            UpdateMultiplierText(1f);
            SetInteractable(false);

            if (_rocketMover != null)
                _rocketMover.StopMove();

            if (_lineRenderer != null)
                _lineRenderer.ClearLine();
        }

        // ------------------------------------------------------------------
        // GRAPH
        // ------------------------------------------------------------------

        public void PlayFlyAnimation(float crashMultiplier, float growRate = 0.5f)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[CryptoVibeView] Rocket system not initialized!");
                return;
            }

            _ejectButton.Interactable = true;
            _crashMultiplier = crashMultiplier;

            _currentPath = _pathGenerator.GeneratePath(crashMultiplier, Vector2.zero);

            if (_rocketTransform != null && _currentPath.AscentPoints != null && _currentPath.AscentPoints.Length > 0)
                _rocketTransform.position = _currentPath.AscentPoints[0];

            float flightTime = (crashMultiplier - 1f) / growRate;

            if (_lineRenderer != null && _graphContainer != null)
                _lineRenderer.InitPath(_currentPath, _graphContainer);

            // Запускаем движение ракеты с синхронизированной скоростью
            _rocketMover.StartMove(_currentPath, flightTime);
        }

        public void Crash(Action onComplete)
        {
            if (_rocketMover == null || _currentPath == null)
            {
                Debug.LogWarning("[CryptoVibeView] Cannot crash: rocket mover or path is null!");
                onComplete?.Invoke();
                return;
            }

            _ejectButton.Interactable = false;

            PlayCrashEffect();

            // Запускаем фазу падения
            _rocketMover.StartDescent(onComplete);
        }

        // ------------------------------------------------------------------
        // UI
        // ------------------------------------------------------------------

        public void UpdateBetText(float bet)
        {
            if (_currentBetLabel != null)
                _currentBetLabel.text = $"{bet:F0}";

            if (_betInputField != null && !_betInputField.isFocused)
                _betInputField.text = bet.ToString("F0");
        }

        public void UpdateMultiplierText(float multiplier)
        {
            if (_multiplierLabel != null)
                _multiplierLabel.text = $"{multiplier:F2}x";
        }

        public void SetInteractable(bool isPlaying)
        {
            if (_startButton != null)
            {
                if (!_startButton.gameObject.activeSelf)
                    _startButton.ForceInit();

                _startButton.gameObject.SetActive(!isPlaying);
            }

            if (_ejectButton != null)
            {
                if (!_ejectButton.gameObject.activeSelf)
                    _ejectButton.ForceInit();

                _ejectButton.gameObject.SetActive(isPlaying);
            }

            if (_betInputField != null)
                _betInputField.interactable = !isPlaying;
        }

        public void PlayCrashEffect()
        {
            // TODO: Добавить каких-нибудь эффектов

            if (_rocketImage != null)
                _rocketImage.sprite = _crashedRocketSprite;
        }

        public void ShowResult(bool isWin, int reward)
        {
            if (_resultPanelView != null)
                _resultPanelView.ShowResultPanel(isWin, false, reward);
        }

        public void ShowWarningMessage(string title, string message)
        {
            if (_warningMessageView != null)
            {
                _warningMessageView.SetWarningMessage(title, message);
                _warningMessageView.Show();
            }
        }

        // ------------------------------------------------------------------
        // INPUT
        // ------------------------------------------------------------------

        private void HandleBetInput(string input)
        {
            if (float.TryParse(input, out float bet))
                OnBetChanged?.Invoke(bet);
            else
                OnBetChanged?.Invoke(0f);
        }

        private void HandleStartButtonClick()
        {
            OnStartClicked?.Invoke();
        }

        private void HandleEjectButtonClick()
        {
            OnEjectClicked?.Invoke();
        }

        private void HandleRestartButtonClick()
        {
            OnRestartButtonClicked?.Invoke();
        }
    }
}