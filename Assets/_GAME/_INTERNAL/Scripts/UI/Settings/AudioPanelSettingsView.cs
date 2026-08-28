using Core.Services.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settigns
{
    public class AudioPanelSettingsView : MonoBehaviour
    {
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;

        private void Awake()
        {
            InitializeSliders();

            _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            _sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        private void OnDestroy()
        {
            _musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            _sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        }

        private void InitializeSliders()
        {
            var audioController = AudioService.Instance;
            if (audioController == null)
            {
                Debug.LogError("[AudioSettingsPanel] AudioManager not found!");
                return;
            }

            _musicVolumeSlider.minValue = 0f;
            _musicVolumeSlider.maxValue = 1f;
            _sfxVolumeSlider.minValue = 0f;
            _sfxVolumeSlider.maxValue = 1f;

            _musicVolumeSlider.value = audioController.GetMusicVolume();
            _sfxVolumeSlider.value = audioController.GetSfxVolume();
        }

        private void OnMusicVolumeChanged(float value)
        {
            AudioService.Instance.SetMusicVolume(value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            AudioService.Instance.SetSfxVolume(value);
        }
    }
}