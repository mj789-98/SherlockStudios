// MainMenuManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public Button startGameButton;
    public Button profileButton;
    public Button logoutButton;
    public GameObject profilePanel;

    void Start()
    {
        // Setup button listeners
        startGameButton.onClick.AddListener(StartGame);
        profileButton.onClick.AddListener(ShowProfile);
        logoutButton.onClick.AddListener(Logout);

        // Show banner ad in main menu
        if (AdManager.Instance != null)
        {
            AdManager.Instance.ShowBannerAd();
        }
    }

    private void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    private void ShowProfile()
    {
         // First, tell the ProfileManager to fetch the latest data
    if (ProfileManager.Instance != null)
    {
        ProfileManager.Instance.LoadUserProfile();
    }
        profilePanel.SetActive(true);
    }

    private void Logout()
    {
        if (AuthenticationManager.Instance != null)
        {
            AuthenticationManager.Instance.SignOut();
        }
        SceneManager.LoadScene("LoginScreen");
    }
}