using System;
using TMPro;
using UI.Other;
using UnityEngine;

namespace UI.Plinko
{
    public class PlinkoView : MonoBehaviour
    {
        [Header("Text Setup")]
        [SerializeField] private TextMeshProUGUI _betLabel;
        [SerializeField] private TMP_InputField _betInputField;

        [Space(5), Header("Action Buttons Setup")]
        [SerializeField] private ActionButton _betUpButton;
        [SerializeField] private ActionButton _betDownButton;
        [SerializeField] private ActionButton _dropBallButton;

        [Space(5), Header("Other Panels")]
        [SerializeField] private WarningMessageView _warningMessageView;

        public event Action OnBetUpClick;
        public event Action OnBetDownClick;
        public event Action<int> OnBetChanged;
        public event Action OnBetChangedFallback;
        public event Action OnDropButtonClick;

        public void Init()
        {
            _betUpButton.IsUseHeldFunc = true;
            _betDownButton.IsUseHeldFunc = true;

            _dropBallButton.OnButtonClick += HandleDropButtonClick;
            _betInputField.onEndEdit.AddListener(HandleBetInputEndEdit);
        }

        public void Dispose()
        {
            _dropBallButton.OnButtonClick -= HandleDropButtonClick;
            _betInputField.onEndEdit.RemoveListener(HandleBetInputEndEdit);
        }

        private void Update()
        {
            if (_betUpButton.IsHeld)
                HandleBetUpButtonClick();

            if (_betDownButton.IsHeld)
                HandleBetDownButtonClick();
        }

        public void UpdateUI(string currentBet)
        {
            _betInputField.SetTextWithoutNotify(currentBet);
            _betLabel.text = currentBet;
        }

        public void ToggleButtonsInteractable(bool value)
        {
            _dropBallButton.Interactable = value;
            _betDownButton.Interactable = value;
            _betUpButton.Interactable = value;
            _betInputField.interactable = value;
        }

        public void ShowWarningMessage(string title, string message)
        {
            if (_warningMessageView != null)
            {
                _warningMessageView.SetWarningMessage(title, message);
                _warningMessageView.Show();
            }
        }

        public void RefreshInput(string currentBet) => _betInputField.SetTextWithoutNotify(currentBet);

        private void HandleBetInputEndEdit(string raw)
        {
            if (int.TryParse(raw, out int bet))
                OnBetChanged?.Invoke(bet);
            else
                OnBetChangedFallback?.Invoke();
        }

        private void HandleDropButtonClick() => OnDropButtonClick?.Invoke();
        private void HandleBetUpButtonClick() => OnBetUpClick?.Invoke();
        private void HandleBetDownButtonClick() => OnBetDownClick?.Invoke();
    }
}