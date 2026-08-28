using StartApp;
using System;
using UnityEngine;

namespace Core.Services.AdsService
{
    public class AdsService : MonoBehaviour
    {
        [SerializeField] private bool _isEnabled = false;
        [SerializeField] private bool _isTestMode = false;

        private bool _isInitialized = false;

        private static AdsService _instance;
        private bool _isRewardedVideoLoading;
        private string _currentPlacement;

        private InterstitialAd _rewardedAd;
        private Action _onRewardCallback;

        public static AdsService Instance
        {
            get => _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeAds();
        }

        private void OnDestroy()
        {
            if (_rewardedAd != null)
            {
                _rewardedAd.RaiseAdLoaded -= OnAdLoaded;
                _rewardedAd.RaiseAdVideoCompleted -= OnVideoCompleted;
                _rewardedAd.RaiseAdLoadingFailed -= OnAdLoadingFailed;
                _rewardedAd.RaiseAdClosed -= OnAdClosed;
            }
        }

        private void InitializeAds()
        {
            if (!_isEnabled)
            {
                Debug.LogWarning("[Ads] Ads Service is disabled.");
                return;
            }

            try
            {
                Debug.Log("[Ads] Initialization Start.io...");

                bool currentConsent = PlayerPrefs.GetInt("gdpr_consent_granted", 1) == 1;
                long timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;

                AdSdk.Instance.SetUserConsent("pas", currentConsent, timestamp);
                AdSdk.Instance.ShowSplash();
                AdSdk.Instance.SetTestAdsEnabled(_isTestMode);

                _isInitialized = true;
                Debug.Log("[Ads] Start.io SDK successfully initialized.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Ads] SDK initialization failed: {ex.Message}");
            }
        }

        public void ShowRewardedAd(Action onRewardCallback, string placement = "default")
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[Ads] Cannot show video. SDK is not initialized.");
                return;
            }

            if (_isRewardedVideoLoading)
            {
                Debug.LogWarning("[Ads] Rewarded Video is already loading. Please wait.");
                return;
            }

            _onRewardCallback = onRewardCallback;
            _currentPlacement = placement;

            SetLoadingState(true);

            _rewardedAd = AdSdk.Instance.CreateInterstitial();

            _rewardedAd.RaiseAdLoaded += OnAdLoaded;
            _rewardedAd.RaiseAdVideoCompleted += OnVideoCompleted;
            _rewardedAd.RaiseAdLoadingFailed += OnAdLoadingFailed;
            _rewardedAd.RaiseAdClosed += OnAdClosed;

            Debug.Log("[Ads] Start loading rewarded ad...");
            _rewardedAd.LoadAd(InterstitialAd.AdType.Rewarded);
        }

        public bool IsRewardedAdLoaded() => _rewardedAd != null && _rewardedAd.IsReady();

        private void OnAdLoaded(object sender, EventArgs e)
        {
            Debug.Log("[Ads] The rewarded ad has been uploaded and is ready to be displayed");

            if (_rewardedAd != null && _rewardedAd.IsReady())
            {
                _rewardedAd.ShowAd();
            }
        }

        private void OnVideoCompleted(object sender, EventArgs e)
        {
            Debug.Log("[Ads] The user has finished viewing the ad");

            _onRewardCallback?.Invoke();
            _onRewardCallback = null;
        }

        private void OnAdLoadingFailed(object sender, MessageArgs e)
        {
            Debug.LogWarning($"[Ads] Error loading a rewarded ad: {e.Message}");
            SetLoadingState(false);
        }

        private void OnAdClosed(object sender, EventArgs e)
        {
            Debug.Log("[Ads] Rewarded ad is closed");
            SetLoadingState(false);

            if (_onRewardCallback != null)
            {
                Debug.Log("[Ads] Reward conditions met. Triggering callback.");

                _onRewardCallback?.Invoke();
                _onRewardCallback = null;
            }

            if (_rewardedAd != null)
            {
                _rewardedAd.RaiseAdLoaded -= OnAdLoaded;
                _rewardedAd.RaiseAdVideoCompleted -= OnVideoCompleted;
                _rewardedAd.RaiseAdLoadingFailed -= OnAdLoadingFailed;
                _rewardedAd.RaiseAdClosed -= OnAdClosed;
                _rewardedAd = null;
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            _isRewardedVideoLoading = isLoading;
        }
    }
}