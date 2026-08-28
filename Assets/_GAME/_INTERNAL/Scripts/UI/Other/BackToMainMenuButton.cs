using Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Other
{
    [RequireComponent(typeof(ActionButton))]
    public class BackToMainMenuButton : MonoBehaviour
    {
        private ActionButton _actionButton;

        private void Awake()
        {
            _actionButton = GetComponent<ActionButton>();
        }

        private void Start()
        {
            _actionButton.OnButtonClick += HandleClick;
        }

        private void OnDestroy()
        {
            _actionButton.OnButtonClick -= HandleClick;
        }

        private void HandleClick()
        {
            DOTween.KillAll();
            SceneManager.LoadSceneAsync(GameConstants.MAIN_MENU);
        }
    }
}