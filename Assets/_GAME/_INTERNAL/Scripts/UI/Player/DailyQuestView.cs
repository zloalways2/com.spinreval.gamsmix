using Core.Data.Quests;
using Core.Services;
using Core.SO;
using DG.Tweening;
using TMPro;
using UI.Other;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Player
{
    [RequireComponent(typeof(ActionButton))]
    public class DailyQuestView : MonoBehaviour
    {
        [Header("View Setup")]
        [SerializeField] private Image _questImage;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;
        [SerializeField] private TextMeshProUGUI _rewardLabel;
        [SerializeField] private Image _progressBar;
        [SerializeField] private GameObject _completedMask;
        [SerializeField] private ActionButton _goToGameButton;

        private DailyQuest _data;
        private QuestSpritesConfig _config;

        public DailyQuest Data => _data;

        private void OnDestroy()
        {
            _goToGameButton.OnButtonClick -= HandleGoToGameButtonClick;
        }

        public void Init(DailyQuest data, QuestSpritesConfig spritesConfig)
        {
            gameObject.name = $"Quest_View_{data.QuestTag}";

            if (_goToGameButton == null)
                _goToGameButton = GetComponent<ActionButton>();

            _goToGameButton.OnButtonClick += HandleGoToGameButtonClick;

            _data = data;
            _config = spritesConfig;

            if(_questImage != null)
            {
                if(_config.GetSprite(_data.Id) == null)
                {
                    Debug.LogWarning($"[Quest View] Quest data ID: {_data.Id}/Config ID: {_config.GetSprite(_data.Id)}." +
                        $" Sprite is null!");
                    return;
                }
                _questImage.sprite = _config.GetSprite(_data.Id);
            }

            if (_descriptionLabel != null)
                _descriptionLabel.text = data.Description;

            if(_rewardLabel != null)
                _rewardLabel.text = data.RewardCoins.ToString("N0");

            if (_progressBar != null)
                _progressBar.fillAmount = Mathf.Clamp01((float)_data.CurrentProgress / _data.TargetProgress);

            if (_completedMask != null && data.IsCompleted)
                _completedMask.SetActive(true);
            else
                _completedMask.SetActive(false);
        }

        public void UpdateQuestProgress(float progress)
        {
            if (progress >= 1f)
                _completedMask.SetActive(true);

            _progressBar.DOKill();
            _progressBar.DOFillAmount(progress, 0.5f);
        }

        private void HandleGoToGameButtonClick()
        {
            string targetScene = GameServices.QuestRouter.GetTargetSceneByQuestTag(_data.QuestTag);

            if (!string.IsNullOrEmpty(targetScene))
                SceneManager.LoadSceneAsync(targetScene);
            else
                Debug.Log($"Game for quest {_data.QuestTag} not found!");
        }
    }
}