using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;

public class AuthenticationUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button signInButton;
    public Button signUpButton;
    public Button anonymousButton;
    public Button signOutButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI userInfoText;

    void Start()
    {
        // Setup button events
        if (signInButton) signInButton.onClick.AddListener(SignIn);
        if (signUpButton) signUpButton.onClick.AddListener(SignUp);
        if (anonymousButton) anonymousButton.onClick.AddListener(SignInAnonymously);
        if (signOutButton) signOutButton.onClick.AddListener(SignOut);

        // Setup authentication events
        if (AuthenticationManager.Instance != null)
        {
            AuthenticationManager.Instance.OnSignInSuccess.AddListener(OnSignInSuccess);
            AuthenticationManager.Instance.OnSignInFailed.AddListener(OnSignInFailed);
            AuthenticationManager.Instance.OnSignOut.AddListener(OnSignOut);
        }

        UpdateUI();
    }

    public async void SignIn()
    {
        if (string.IsNullOrEmpty(emailInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            UpdateStatus("Please enter email and password");
            return;
        }

        UpdateStatus("Signing in...");
        signInButton.interactable = false;

        bool success = await AuthenticationManager.Instance.SignInWithEmailPasswordAsync(
            emailInput.text, passwordInput.text);

        signInButton.interactable = true;

        if (!success)
        {
            UpdateStatus("Sign in failed - check console for details");
        }
    }

    public async void SignUp()
    {
        if (string.IsNullOrEmpty(emailInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            UpdateStatus("Please enter email and password");
            return;
        }

        UpdateStatus("Creating account...");
        signUpButton.interactable = false;

        bool success = await AuthenticationManager.Instance.CreateUserWithEmailPasswordAsync(
            emailInput.text, passwordInput.text);

        signUpButton.interactable = true;

        if (!success)
        {
            UpdateStatus("Account creation failed - check console for details");
        }
    }

    public async void SignInAnonymously()
    {
        UpdateStatus("Signing in anonymously...");
        anonymousButton.interactable = false;

        bool success = await AuthenticationManager.Instance.SignInAnonymouslyAsync();

        anonymousButton.interactable = true;

        if (!success)
        {
            UpdateStatus("Anonymous sign in failed - check console for details");
        }
    }

    public void SignOut()
    {
        AuthenticationManager.Instance.SignOut();
    }

    private void OnSignInSuccess(FirebaseUser user)
    {
        UpdateStatus($"Signed in successfully!");
        UpdateUserInfo();
        UpdateUI();
    }

    private void OnSignInFailed(string error)
    {
        UpdateStatus($"Sign in failed: {error}");
        UpdateUI();
    }

    private void OnSignOut()
    {
        UpdateStatus("Signed out");
        UpdateUserInfo();
        UpdateUI();
    }

    private void UpdateUI()
    {
        bool isSignedIn = AuthenticationManager.Instance != null && 
                         AuthenticationManager.Instance.IsSignedIn();

        // Enable/disable buttons based on sign-in state
        if (signInButton) signInButton.interactable = !isSignedIn;
        if (signUpButton) signUpButton.interactable = !isSignedIn;
        if (anonymousButton) anonymousButton.interactable = !isSignedIn;
        if (signOutButton) signOutButton.interactable = isSignedIn;

        // Clear input fields when signed in
        if (isSignedIn)
        {
            if (emailInput) emailInput.text = "";
            if (passwordInput) passwordInput.text = "";
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText)
        {
            statusText.text = message;
            Debug.Log($"Auth Status: {message}");
        }
    }

    private void UpdateUserInfo()
    {
        if (userInfoText)
        {
            if (AuthenticationManager.Instance != null)
            {
                userInfoText.text = AuthenticationManager.Instance.GetUserInfo();
            }
            else
            {
                userInfoText.text = "Authentication Manager not available";
            }
        }
    }

    void OnDestroy()
    {
        // Clean up event listeners
        if (AuthenticationManager.Instance != null)
        {
            AuthenticationManager.Instance.OnSignInSuccess.RemoveListener(OnSignInSuccess);
            AuthenticationManager.Instance.OnSignInFailed.RemoveListener(OnSignInFailed);
            AuthenticationManager.Instance.OnSignOut.RemoveListener(OnSignOut);
        }
    }
} 