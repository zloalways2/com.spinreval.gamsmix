using Core.Services;
using Core.Services.AdsService;
using Core.Services.Analytics;
using Core.Services.Audio;
using Core.SO.Common;
using Io.AppMetrica;
using System.Collections;
using UI.Loading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Boot
{
    public class GameBootstrap
    {
        private static GameBootstrap _instance;

        private DebugConfig _debugConfig;
        private AnalyticsService _analyticsService;
        private AdsService _adsService;
        private AudioService _audioService;

        private Coroutine _loadingCoroutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void AutoStart()
        {
            _instance = new();

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Run();
        }
        
        private static void InitializeExternalSDK()
        {
            CheckFirstLaunch();

            var analyticsServicePrefab = Resources.Load<AnalyticsService>("Prefabs/Services/[ANALYTICS_SERVICE]");
            var adsControllerPrefab = Resources.Load<AdsService>("Prefabs/Services/[ADS_CONTROLLER]");
            var audioControllerPrefab = Resources.Load<AudioService>("Prefabs/Services/[AUDIO_CONTROLLER]");

            _instance._debugConfig = Resources.Load<DebugConfig>("Tools/Configs/DebugConfig");

            if (_instance._debugConfig.IsDebug)
                PlayerPrefs.DeleteAll();

            if(analyticsServicePrefab == null || adsControllerPrefab == null || audioControllerPrefab == null)
            {
                Debug.LogError($"[Game Bootstrap] Analytics Service/Ads Service/Audio Service prefab is null!");
                return;
            }

            _instance._analyticsService = Object.Instantiate(analyticsServicePrefab);
            _instance._adsService = Object.Instantiate(adsControllerPrefab);
            _instance._audioService = Object.Instantiate(audioControllerPrefab);

            try
            {
                AppMetrica.Activate(new AppMetricaConfig("32e9a816-0394-4115-ac04-ecd60f9bebea")
                {
                    FirstActivationAsUpdate = !IsFirstLaunch()
                });
                Debug.Log($"[GlobalAction Bootstrap] AppMetrica initialized successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GlobalAction Bootstrap] Failed to initialize AppMetrica: {ex.Message}");
            }
        }

        private static void Run()
        {
            try
            {
                InitializeExternalSDK();

                _instance.LoadMainScene();
                GameServices.SetDebugConfig(_instance._debugConfig);
                GameServices.InitializeAll();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GlobalAction Bootstrap] Failed to run game: {ex.Message}");
            }
        }

        private static bool IsFirstLaunch()
        {
            // TODO: Сделать проверку не только по ключу PlayerPrefs, но и по другим критериям
            if (!PlayerPrefs.HasKey("First_Launch"))
                return false;

            return true;
        }

        private static void CheckFirstLaunch()
        {
            if (PlayerPrefs.HasKey("First_Launch"))
                PlayerPrefs.SetInt("First_Launch", 1);
        }

        private void LoadMainScene()
        {
            var loadingScreenViewPrefab = Resources.Load<UILoadingView>("Prefabs/UI/UILoadingView");

            if (loadingScreenViewPrefab == null)
            {
                Debug.LogError($"[GlobalAction Bootstrap] Loading Screen View is null!");
                return;
            }

            var loadingScreenView = Object.Instantiate(loadingScreenViewPrefab);

            var monoBehaviourHelper = new GameObject("[MONOBEHAVIOUR_HELPER]").AddComponent<MonoBehaviourHelper>();

            if(_loadingCoroutine != null)
                monoBehaviourHelper.StopCoroutine(_loadingCoroutine);

            _loadingCoroutine = monoBehaviourHelper.StartCoroutine(LoadMainSceneCoroutine(loadingScreenView));
        }

        private IEnumerator LoadMainSceneCoroutine(UILoadingView loadingScreenView)
        {
            Debug.Log($"[Game Bootstrap] Loading coroutine started");

            loadingScreenView.ResetProgress();

            float startTime = Time.time;
            float minLoadingDuration = 2f;
            float currentProgress = 0f;

            yield return new WaitForSeconds(minLoadingDuration * 0.5f);

            AsyncOperation operation = SceneManager.LoadSceneAsync(GameConstants.MAIN_MENU);
            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                float rawProgress = Mathf.Clamp01(operation.progress / 0.9f);

                while (currentProgress < rawProgress)
                {
                    currentProgress += Time.deltaTime * 1.5f;
                    currentProgress = Mathf.Min(currentProgress, rawProgress);
                    loadingScreenView.SetLoadingProgress(currentProgress);
                    yield return null;
                }

                if(operation.progress >= 0.9f)
                {
                    float elapsedTime = Time.time - startTime;
                    if (elapsedTime >= minLoadingDuration)
                    {
                        currentProgress = 1f;
                        loadingScreenView.SetLoadingProgress(currentProgress);
                        yield return new WaitForSeconds(0.75f);

                        operation.allowSceneActivation = true;
                    }
                }

                yield return null;
            }

            _loadingCoroutine = null;
            loadingScreenView.ResetProgress();
            _analyticsService.ReportGameStart();

            Debug.Log($"[Game Bootstrap] Loading coroutine finished");
        }
    }

    public class MonoBehaviourHelper : MonoBehaviour 
    {
        private void Awake() => DontDestroyOnLoad(gameObject);

        private void OnApplicationPause(bool pause)
        {
            if (pause)
                GameServices.SaveAll();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
                GameServices.SaveAll();
        }

        private void OnApplicationQuit()
        {
            GameServices.SaveAll();
            GameServices.Dispose();
        }
    }
}