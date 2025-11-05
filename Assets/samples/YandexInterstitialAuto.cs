using UnityEngine;
using YandexMobileAds;
using YandexMobileAds.Base;
using System.Collections;

public class YandexInterstitialAuto : MonoBehaviour
{
    private InterstitialAdLoader adLoader;
    private Interstitial interstitial;
    private bool isGameBlocked = true;

    [Header("Ad Settings")]
    [SerializeField] private string adUnitId = "demo-interstitial-yandex"; // ← ТВОЙ ID!

    private static YandexInterstitialAuto instance;

    private void Awake()
    {
        // Синглтон
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        adLoader = new InterstitialAdLoader();
        adLoader.OnAdLoaded += HandleAdLoaded;
        adLoader.OnAdFailedToLoad += HandleAdFailedToLoad;
    }

    private void Start()
    {
        if (PlayerPrefs.GetInt("AdShownToday", 0) == 1)
        {
            UnlockGame();
            return;
        }

        BlockGame();
        StartCoroutine(LoadAndShowAfterFrames(2));
    }

    private IEnumerator LoadAndShowAfterFrames(int frames)
    {
        for (int i = 0; i < frames; i++)
            yield return null;

        RequestAd();
    }

    private void RequestAd()
    {
        interstitial?.Destroy();
        interstitial = null;

        var request = new AdRequestConfiguration.Builder(adUnitId).Build();
        adLoader.LoadAd(request);
    }

    #region Обработчики

    private void HandleAdLoaded(object sender, InterstitialAdLoadedEventArgs args)
    {
        interstitial = args.Interstitial;
        interstitial.OnAdDismissed += HandleAdDismissed;
        interstitial.OnAdFailedToShow += HandleAdFailedToShow;
        ShowAd();
    }

    private void HandleAdFailedToLoad(object sender, AdFailedToLoadEventArgs args)
    {
        Debug.Log("Реклама не загрузилась: " + args.Message);
        UnlockGame();
    }

    private void ShowAd()
    {
        interstitial?.Show();
        if (interstitial == null) UnlockGame();
    }

    private void HandleAdDismissed(object sender, System.EventArgs args)
    {
        PlayerPrefs.SetInt("AdShownToday", 1);
        PlayerPrefs.Save();

        interstitial?.Destroy();
        interstitial = null;

        UnlockGame();
    }

    private void HandleAdFailedToShow(object sender, AdFailureEventArgs args)
    {
        Debug.Log("Реклама не показалась: " + args.Message);
        UnlockGame();
    }

    #endregion

    #region Блокировка игры

    private void BlockGame()
    {
        isGameBlocked = true;
        Time.timeScale = 0f;
        // Отключаем ввод
        Input.multiTouchEnabled = false;
    }

    private void UnlockGame()
    {
        isGameBlocked = false;
        Time.timeScale = 1f;
        Input.multiTouchEnabled = true;
    }

    public static bool IsGameBlocked => instance?.isGameBlocked ?? false;

    #endregion

    private void OnDestroy()
    {
        // УДАЛЕНО Dispose() — НЕТ В SDK!
        interstitial?.Destroy();
    }
}