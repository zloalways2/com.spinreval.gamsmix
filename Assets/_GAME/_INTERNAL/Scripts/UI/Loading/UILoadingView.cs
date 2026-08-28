using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Loading
{
    public class UILoadingView : MonoBehaviour
    {
        [SerializeField] private float _progressAnimDuration = 0.25f;
        [SerializeField] private Image _progressBarFill;

        private Tween _fillTween;

        private void OnDestroy()
        {
            _fillTween?.Kill();
        }

        public void ResetProgress()
        {
            _fillTween?.Kill();
            _fillTween = null;

            if (_progressBarFill)
                _progressBarFill.fillAmount = 0f;
        }

        public void SetLoadingProgress(float progress)
        {
            _fillTween?.Kill();
            Debug.Log($"[UI Loading View] Progress: {progress}");

            _fillTween = _progressBarFill.DOFillAmount(progress, _progressAnimDuration)
                .OnComplete(() => _progressBarFill.fillAmount = 1f);

            _fillTween.OnKill(() => _fillTween = null);
        }
    }
}