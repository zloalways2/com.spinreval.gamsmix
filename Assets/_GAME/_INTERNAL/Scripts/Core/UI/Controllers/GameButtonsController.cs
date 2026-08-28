using UI.Other;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.UI.Controllers
{
    public class GameButtonsController : MonoBehaviour
    {
        [SerializeField] private ActionButton _reelsButton;
        [SerializeField] private ActionButton _vaultButton;
        [SerializeField] private ActionButton _wheelOfRevolutButton;
        [SerializeField] private ActionButton _diamondRetroButton;
        [SerializeField] private ActionButton _neonWheelButton;
        [SerializeField] private ActionButton _cyberMasterButton;
        [SerializeField] private ActionButton _electricDiceButton;
        [SerializeField] private ActionButton _infiniteScoreButton;
        [SerializeField] private ActionButton _cryptoVibeButton;
        [SerializeField] private ActionButton _plinkoVibeButton;

        private void Awake()
        {
            _reelsButton.OnButtonClick += HandleReelsButtonClick;
            _vaultButton.OnButtonClick += HandleVaultButtonClick;
            _wheelOfRevolutButton.OnButtonClick += HandleWheelOfRevolutButtonClick;
            _diamondRetroButton.OnButtonClick += HandleDiamondRetroButtonClick;
            _neonWheelButton.OnButtonClick += HandleNeonWheelButtonClick;
            _cyberMasterButton.OnButtonClick += HandleCyberMasterButtonClick;
            _electricDiceButton.OnButtonClick += HandleElectricDiceButtonClick;
            _infiniteScoreButton.OnButtonClick += HandleInfiniteScoreButtonClick;
            _cryptoVibeButton.OnButtonClick += HandleCryptoVibeButtonClick;
            _plinkoVibeButton.OnButtonClick += HandlePlinkoVibeButtonClick;
        }

        private void OnDestroy()
        {
            _reelsButton.OnButtonClick -= HandleReelsButtonClick;
            _vaultButton.OnButtonClick -= HandleVaultButtonClick;
            _wheelOfRevolutButton.OnButtonClick -= HandleWheelOfRevolutButtonClick;
            _diamondRetroButton.OnButtonClick -= HandleDiamondRetroButtonClick;
            _neonWheelButton.OnButtonClick -= HandleNeonWheelButtonClick;
            _cyberMasterButton.OnButtonClick -= HandleCyberMasterButtonClick;
            _electricDiceButton.OnButtonClick -= HandleElectricDiceButtonClick;
            _infiniteScoreButton.OnButtonClick -= HandleInfiniteScoreButtonClick;
            _cryptoVibeButton.OnButtonClick -= HandleCryptoVibeButtonClick;
            _plinkoVibeButton.OnButtonClick -= HandlePlinkoVibeButtonClick;
        }

        private void HandleReelsButtonClick() => SceneManager.LoadSceneAsync(GameConstants.GAME_REELS);
        private void HandleWheelOfRevolutButtonClick() => SceneManager.LoadSceneAsync(GameConstants.GAME_WHEEL_OF_REVOLUT);
        private void HandleVaultButtonClick() => SceneManager.LoadSceneAsync(GameConstants.GAME_VAULT);
        private void HandleDiamondRetroButtonClick() => SceneManager.LoadSceneAsync(GameConstants.GAME_DIAMOND_RETRO);
        private void HandleNeonWheelButtonClick() => SceneManager.LoadSceneAsync(GameConstants.GAME_NEON_WHEEL);
        private void HandleCyberMasterButtonClick() => SceneManager.LoadSceneAsync(GameConstants.GAME_CYBER_MASTER);
        private void HandleElectricDiceButtonClick() => SceneManager.LoadSceneAsync(GameConstants.GAME_ELECTRIC_DICE);
        private void HandleInfiniteScoreButtonClick() => SceneManager.LoadSceneAsync(GameConstants.GAME_INFINITE_SCORE);
        private void HandleCryptoVibeButtonClick() => SceneManager.LoadSceneAsync(GameConstants.GAME_CRYPTO_VIBE);
        private void HandlePlinkoVibeButtonClick() => SceneManager.LoadSceneAsync(GameConstants.GAME_PLINKO_VIBE);
    }
}