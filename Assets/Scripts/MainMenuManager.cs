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

    private async void ShowProfile()
    {
        // Show profile panel immediately but with loading state
        profilePanel.SetActive(true);
        
        // Then refresh the data from Firebase
        if (ProfileManager.Instance != null)
        {
            Debug.Log("Refreshing profile data from Firebase...");
            await ProfileManager.Instance.RefreshUserProfileAsync();
        }
        else
        {
            Debug.LogError("ProfileManager.Instance is null!");
        }
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