using UnityEngine;

namespace UI.Plinko
{
    public class PegView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Sprites")]
        [SerializeField] private Sprite _defaultSprite;
        [SerializeField] private Sprite _hitSprite;

        [Space(5), Header("Glow Settings")]
        [SerializeField] private float _glowDuration = 0.3f;

        private bool _isGlowing = false;
        private float _glowTimer = 0f;

        private void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_spriteRenderer != null)
            {
                if (_defaultSprite != null)
                    _spriteRenderer.sprite = _defaultSprite;
            }
            else
                Debug.LogWarning($"[PegView] No SpriteRenderer found on {gameObject.name}");
        }

        /// <summary>
        /// Вызывается из MathBallMover при прохождении мяча рядом с пегом.
        /// Запускает эффект свечения и смены спрайта.
        /// </summary>
        public void TriggerHit()
        {
            if (_spriteRenderer == null)
            {
                Debug.LogWarning($"[PegView] TriggerHit called but no SpriteRenderer on {gameObject.name}");
                return;
            }

            // Сбрасываем таймер если уже светится
            _isGlowing = true;
            _glowTimer = _glowDuration;

            // Меняем спрайт на "hit"
            if (_hitSprite != null)
                _spriteRenderer.sprite = _hitSprite;
            else
                Debug.LogWarning($"[PegView] No hitSprite assigned on {gameObject.name}");
        }

        private void Update()
        {
            if (_isGlowing)
            {
                _glowTimer -= Time.deltaTime;

                if (_glowTimer <= 0)
                    ResetVisuals();
            }
        }

        /// <summary>
        /// Сбрасывает визуальные эффекты пега в исходное состояние.
        /// </summary>
        public void ResetVisuals()
        {
            _isGlowing = false;

            if (_spriteRenderer != null)
            {
                if (_defaultSprite != null)
                    _spriteRenderer.sprite = _defaultSprite;
            }
        }
    }
}