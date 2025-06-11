using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance;

    [Header("Scene Names")]
    public string splashSceneName = "SplashScreen";
    public string loginSceneName = "LoginScreen";
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "GameScene";

    [Header("Loading")]
    public GameObject loadingPanel;
    public TMPro.TextMeshProUGUI loadingText;

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

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    public void LoadSplashScreen() => LoadScene(splashSceneName);
    public void LoadLoginScreen() => LoadScene(loginSceneName);
    public void LoadMainMenu() => LoadScene(mainMenuSceneName);
    public void LoadGameScene() => LoadScene(gameSceneName);

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Show loading panel if available
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (loadingText != null)
                loadingText.text = "Loading...";
        }

        // Hide ads before switching scenes
        if (AdManager.Instance != null)
        {
            AdManager.Instance.HideBannerAd();
        }

        yield return new WaitForSeconds(0.1f); // Small delay for smooth transition

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Wait until scene is almost loaded
        while (asyncLoad.progress < 0.9f)
        {
            if (loadingText != null)
                loadingText.text = $"Loading... {Mathf.RoundToInt(asyncLoad.progress * 100)}%";
            yield return null;
        }

        // Scene is ready, activate it
        asyncLoad.allowSceneActivation = true;

        // Wait for scene activation
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Hide loading panel
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        // Initialize scene-specific logic
        yield return new WaitForEndOfFrame();
        InitializeScene(sceneName);
    }

    private void InitializeScene(string sceneName)
    {
        switch (sceneName)
        {
            case "MainMenu":
                // Show banner ad in main menu
                if (AdManager.Instance != null)
                {
                    AdManager.Instance.ShowBannerAd();
                }
                break;

            case "GameScene":
                // Initialize game
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.StartNewGame();
                }
                break;

            case "LoginScreen":
                // Hide ads on login screen
                if (AdManager.Instance != null)
                {
                    AdManager.Instance.HideBannerAd();
                }
                break;
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
} 