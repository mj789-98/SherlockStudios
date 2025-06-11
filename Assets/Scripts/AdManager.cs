using UnityEngine;
using GoogleMobileAds.Api;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

    [Header("Ad Unit IDs (Test IDs)")]
    public string bannerAdUnitId = "ca-app-pub-3940256099942544/6300978111"; // Test ID
    public string interstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712"; // Test ID

    private BannerView bannerView;
    private InterstitialAd interstitialAd;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Initialize the Google Mobile Ads SDK
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("Google Mobile Ads initialized");
            LoadBannerAd();
            LoadInterstitialAd();
        });
    }

    private void LoadBannerAd()
    {
        // Clean up banner before creating a new one
        if (bannerView != null)
        {
            bannerView.Destroy();
        }

        // Create a banner ad
        bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);

        // Create an ad request - Updated API (no more Builder pattern)
        AdRequest adRequest = new AdRequest();
        bannerView.LoadAd(adRequest);

        // Add event handlers
        bannerView.OnBannerAdLoaded += () => Debug.Log("Banner ad loaded");
        bannerView.OnBannerAdLoadFailed += (LoadAdError error) => 
            Debug.LogError($"Banner ad failed to load: {error.GetMessage()}");
    }

    private void LoadInterstitialAd()
    {
        // Clean up interstitial before creating a new one
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
        }

        // Create an ad request - Updated API (no more Builder pattern)
        AdRequest adRequest = new AdRequest();
        InterstitialAd.Load(interstitialAdUnitId, adRequest, (InterstitialAd ad, LoadAdError loadError) =>
        {
            if (loadError != null)
            {
                Debug.LogError($"Interstitial ad failed to load: {loadError.GetMessage()}");
                return;
            }

            interstitialAd = ad;
            Debug.Log("Interstitial ad loaded");

            // Add event handlers
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Interstitial ad closed");
                LoadInterstitialAd(); // Load a new one
            };
            interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError($"Interstitial ad failed to show: {error.GetMessage()}");
                LoadInterstitialAd(); // Load a new one
            };
        });
    }

    public void ShowBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Show();
        }
    }

    public void HideBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Hide();
        }
    }

    public void ShowInterstitialAd()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
            Debug.Log("Interstitial ad is not ready yet");
            LoadInterstitialAd(); // Try to load a new one
        }
    }

    void OnDestroy()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
        }
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
        }
    }
}