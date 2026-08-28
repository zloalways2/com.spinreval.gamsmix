using Core.Services.Audio;
using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Other
{
    public class ResultPanelView : MonoBehaviour
    {
        [Header("Result Panel Setup")]
        [SerializeField] private GameObject _resultPanel;

        [Space(5), Header("Result Panel Elements")]
        [SerializeField] private Image _resultPanelEffectImage;
        [SerializeField] private Image _resultPanelCupImage;

        [Space(5), Header("Result Text Elements")]
        [SerializeField] private TextMeshProUGUI _resultTitleText;
        [SerializeField] private TextMeshProUGUI _resultScoreText;
        [SerializeField] private TextMeshProUGUI _resultRewardText;

        [Space(5), Header("Color Gradients")]
        [SerializeField] private TMP_ColorGradient _blackjackGradient;
        [SerializeField] private TMP_ColorGradient _winGradient;
        [SerializeField] private TMP_ColorGradient _loseGradient;
        [SerializeField] private TMP_ColorGradient _drawGradient;

        [Space(5), Header("Sprites")]
        [SerializeField] private Sprite _winEffect;
        [SerializeField] private Sprite _loseEffect;
        [SerializeField] private Sprite _winCupSprite;
        [SerializeField] private Sprite _loseCupSprite;

        [Space(5), Header("SFX Settings")]
        [SerializeField] private AudioClip _winClip;
        [SerializeField] private AudioClip _loseClip;

        [Space(5), Header("Buttons")]
        [SerializeField] private ActionButton _restartGameButton;

        private AudioSource _sfxSource;

        private Tween _openTween;
        private Tween _closeTween;

        public event Action OnPanelOpened;
        public event Action OnRestartGameButtonClick;

        private void Start()
        {
            _restartGameButton.OnButtonClick += HandleRestartGameButtonClick;

            if (_winClip == null)
                _winClip = Resources.Load<AudioClip>("SFX/Sounds/Game_Win");

            if (_loseClip == null)
                _loseClip = Resources.Load<AudioClip>("SFX/Sounds/Game_Lose");
        }

        private void OnDestroy()
        {
            _restartGameButton.OnButtonClick -= HandleRestartGameButtonClick;

            _openTween?.Kill();
            _closeTween?.Kill();
        }

        public void SetAudioSource(AudioSource sfxSource)
        {
            _sfxSource = sfxSource;
            _sfxSource.volume = AudioService.Instance.GetSfxVolume();
        } 

        public void ShowResultPanel(bool isWin, bool isBlackjack, int reward, int score = 0, bool isDraw = false)
        {
            _openTween?.Kill();

            _resultPanel.SetActive(true);
            _resultPanel.transform.localScale = Vector3.zero;

            _openTween = _resultPanel.transform
                .DOScale(Vector3.one, 0.5f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => OnPanelOpened?.Invoke());

            if (isBlackjack)
            {
                _resultTitleText.text = "Blackjack!";
                _resultTitleText.colorGradientPreset = _blackjackGradient;
                _resultScoreText.text = $"Score:{score}";
                _resultPanelEffectImage.sprite = _winEffect;
                _resultPanelCupImage.sprite = _winCupSprite;

                _sfxSource.PlayOneShot(_winClip);
            }
            else if(isWin)
            {
                _resultTitleText.text = "Win!";
                _resultTitleText.colorGradientPreset = _winGradient;
                _resultScoreText.text = $"Score:{score}";
                _resultPanelEffectImage.sprite = _winEffect;
                _resultPanelCupImage.sprite = _winCupSprite;

                _sfxSource.PlayOneShot(_winClip);
            }
            else if (isDraw)
            {
                _resultTitleText.text = "Draw!";
                _resultTitleText.colorGradientPreset = _drawGradient;
                _resultScoreText.text = $"Score:{score}";
                _resultPanelEffectImage.sprite = _winEffect;
                _resultPanelCupImage.sprite = _winCupSprite;
            }
            else
            {
                _resultTitleText.text = "Lose!";
                _resultTitleText.colorGradientPreset = _loseGradient;
                _resultScoreText.text = $"Score:{score}";
                _resultPanelEffectImage.sprite = _loseEffect;
                _resultPanelCupImage.sprite = _loseCupSprite;

                _sfxSource.PlayOneShot(_loseClip);
            }

            if (_resultRewardText != null)
            {
                if (isWin)
                    _resultRewardText.text = $"+{reward} Coins";
                else
                    _resultRewardText.text = "No reward";
            }
        }

        private void HandleRestartGameButtonClick()
        {
            _openTween?.Kill();
            _closeTween?.Kill();

            _closeTween = _resultPanel.transform
                .DOScale(Vector3.zero, 0.5f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    _resultPanel.SetActive(false);
                    OnRestartGameButtonClick?.Invoke();
                });
        }
    }
}