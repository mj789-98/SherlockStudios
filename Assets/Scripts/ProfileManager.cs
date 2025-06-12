using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using System.Collections;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance;

    [Header("UI References")]
    public Image profilePicture;
    public TMPro.TextMeshProUGUI playerNameText;
    public TMPro.TextMeshProUGUI playerEmailText;
    public Button changeProfilePictureButton;
    public Button closeProfileButton;
    public GameObject profilePanel;
    public TMPro.TextMeshProUGUI statusText; // Optional: for showing save status

    [Header("Avatar Options")]
    public Sprite[] availableAvatars;
    public GameObject avatarSelectionPanel;
    public Transform avatarButtonsParent;
    public Button avatarButtonPrefab;

    private Sprite currentProfileSprite;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        changeProfilePictureButton.onClick.AddListener(ShowAvatarSelection);
        if (closeProfileButton != null)
        {
            closeProfileButton.onClick.AddListener(CloseProfile);
        }
        SetupAvatarButtons();
        LoadUserProfile();
    }

    public async void LoadUserProfile()
    {
        await LoadUserProfileAsync();
    }
    
    public async System.Threading.Tasks.Task RefreshUserProfileAsync()
    {
        Debug.Log("🔄 RefreshUserProfileAsync called - forcing fresh data from Firebase server");
        
        if (statusText != null)
        {
            statusText.text = "Refreshing profile...";
        }
        
        try
        {
            await LoadUserProfileAsync(forceRefresh: true);
            
            if (statusText != null)
            {
                statusText.text = "Profile updated!";
                StartCoroutine(ClearStatusAfterDelay(1.5f));
            }
            
            Debug.Log("✅ Profile refresh completed successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Profile refresh failed: {ex.Message}");
            
            if (statusText != null)
            {
                statusText.text = "Refresh failed!";
                StartCoroutine(ClearStatusAfterDelay(2f));
            }
        }
    }
    
    private async System.Threading.Tasks.Task LoadUserProfileAsync(bool forceRefresh = false)
    {
        FirebaseUser user = AuthenticationManager.Instance.GetCurrentUser();
        if (user != null)
        {
            Debug.Log($"User found! Populating profile info. ForceRefresh: {forceRefresh}");

            // If DisplayName is empty, create one from the email.
            if (string.IsNullOrEmpty(user.DisplayName))
            {
                // Take the first part of the email before the '@'
                string emailUsername = user.Email.Split('@')[0];
                
                // If the name is longer than 4 chars, shorten it. Otherwise, use the whole name.
                playerNameText.text = emailUsername.Length > 4 ? emailUsername.Substring(0, 5) : emailUsername;
            }
            else
            {
                // If DisplayName exists, use it.
                playerNameText.text = user.DisplayName;
            }

            playerEmailText.text = user.Email ?? "No Email";

            // Load avatar from Firebase first, then fallback to URL or default
            await LoadProfilePictureFromFirebase(forceRefresh);
        }
        else
        {
            Debug.LogError("LoadUserProfile was called, but no Firebase user is currently signed in.");
            playerNameText.text = "Not Signed In";
            playerEmailText.text = "";
            SetDefaultProfilePicture();
        }
    }

    private IEnumerator LoadProfilePictureFromURL(string url)
    {
        UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Texture2D texture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            profilePicture.sprite = sprite;
            currentProfileSprite = sprite;
        }
        else
        {
            SetDefaultProfilePicture();
        }
    }

    private void SetDefaultProfilePicture()
    {
        if (availableAvatars.Length > 0)
        {
            profilePicture.sprite = availableAvatars[0];
            currentProfileSprite = availableAvatars[0];
        }
    }

    private void SetupAvatarButtons()
    {
        foreach (Sprite avatar in availableAvatars)
        {
            Button avatarButton = Instantiate(avatarButtonPrefab, avatarButtonsParent);
            avatarButton.GetComponent<Image>().sprite = avatar;
            avatarButton.onClick.AddListener(() => SelectAvatar(avatar));
        }
    }

    public void ShowAvatarSelection()
    {
        avatarSelectionPanel.SetActive(true);
    }

    public void HideAvatarSelection()
    {
        avatarSelectionPanel.SetActive(false);
    }

    public void CloseProfile()
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(false);
        }
    }
    
    // Public method for manual refresh (can be called from buttons or other scripts)
    public async void ManualRefreshProfile()
    {
        Debug.Log("Manual profile refresh triggered");
        await RefreshUserProfileAsync();
    }

    private async void SelectAvatar(Sprite selectedAvatar)
    {
        Debug.Log($"Avatar selected: {selectedAvatar.name}");
        
        // Immediately update the UI
        profilePicture.sprite = selectedAvatar;
        currentProfileSprite = selectedAvatar;
        
        // Show saving status
        if (statusText != null)
        {
            statusText.text = "Saving avatar...";
        }
        
        // Save to Firebase
        bool saveSuccess = await SaveProfilePictureToFirebase();
        
        // Update status based on result
        if (statusText != null)
        {
            if (saveSuccess)
            {
                statusText.text = "Avatar saved!";
                Debug.Log("Avatar saved and UI updated successfully!");
                
                // Clear status after 2 seconds
                StartCoroutine(ClearStatusAfterDelay(2f));
            }
            else
            {
                statusText.text = "Save failed - using local storage";
                Debug.LogWarning("Avatar save failed, but UI is updated locally");
                
                // Clear status after 3 seconds
                StartCoroutine(ClearStatusAfterDelay(3f));
            }
        }
        
        HideAvatarSelection();
    }
    
    private IEnumerator ClearStatusAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (statusText != null)
        {
            statusText.text = "";
        }
    }

    private async System.Threading.Tasks.Task<bool> SaveProfilePictureToFirebase()
    {
        Debug.Log($"SaveProfilePictureToFirebase called. UserDataManager: {UserDataManager.Instance != null}, currentProfileSprite: {currentProfileSprite?.name}");
        
        if (UserDataManager.Instance != null && currentProfileSprite != null)
        {
            string spriteName = currentProfileSprite.name;
            Debug.Log($"Attempting to save avatar '{spriteName}' to Firebase...");
            
            bool success = await UserDataManager.Instance.SaveUserAvatarAsync(spriteName);
            
            if (success)
            {
                Debug.Log($"✅ Avatar successfully saved to Firebase: {spriteName}");
                return true;
            }
            else
            {
                Debug.LogError($"❌ Failed to save avatar '{spriteName}' to Firebase");
                // Fallback to local storage
                SaveProfilePictureLocally();
                return false;
            }
        }
        
        Debug.LogError("❌ Cannot save avatar: UserDataManager or currentProfileSprite is null");
        return false;
    }
    
    private void SaveProfilePictureLocally()
    {
        // Fallback: Save to PlayerPrefs for persistence
        if (currentProfileSprite != null)
        {
            string spriteName = currentProfileSprite.name;
            PlayerPrefs.SetString("ProfilePicture", spriteName);
            PlayerPrefs.Save();
            Debug.Log($"✅ Avatar saved locally: {spriteName}");
        }
        else
        {
            Debug.LogError("❌ Cannot save locally: currentProfileSprite is null");
        }
    }

    private async System.Threading.Tasks.Task LoadProfilePictureFromFirebase(bool forceRefresh = false)
    {
        if (UserDataManager.Instance != null)
        {
            Debug.Log($"Loading avatar from Firebase. ForceRefresh: {forceRefresh}");
            string savedAvatarName = await UserDataManager.Instance.GetUserAvatarAsync(forceRefresh);
            
            Debug.Log($"Retrieved avatar name from Firebase: '{savedAvatarName}'");
            Debug.Log($"Available avatars count: {availableAvatars.Length}");
            
            if (!string.IsNullOrEmpty(savedAvatarName))
            {
                // Debug: List all available avatar names
                for (int i = 0; i < availableAvatars.Length; i++)
                {
                    Debug.Log($"Available avatar {i}: '{availableAvatars[i]?.name}'");
                }
                
                // Find the avatar by name in our available avatars
                bool found = false;
                foreach (Sprite avatar in availableAvatars)
                {
                    if (avatar != null && avatar.name == savedAvatarName)
                    {
                        profilePicture.sprite = avatar;
                        currentProfileSprite = avatar;
                        Debug.Log($"✅ Successfully loaded avatar from Firebase: {savedAvatarName}");
                        found = true;
                        return; // Found and set, exit early
                    }
                }
                
                if (!found)
                {
                    Debug.LogWarning($"❌ Avatar '{savedAvatarName}' not found in available avatars. Using fallback.");
                }
            }
            else
            {
                Debug.Log("No avatar found in Firebase, using fallback");
            }
        }
        else
        {
            Debug.LogWarning("UserDataManager.Instance is null!");
        }
        
        // Fallback: Try to load from Firebase Auth PhotoUrl or use default
        FirebaseUser user = AuthenticationManager.Instance.GetCurrentUser();
        if (user != null && !string.IsNullOrEmpty(user.PhotoUrl?.ToString()))
        {
            Debug.Log("Using Firebase Auth PhotoUrl as fallback");
            StartCoroutine(LoadProfilePictureFromURL(user.PhotoUrl.ToString()));
        }
        else
        {
            // Always try local storage first, then default
            Debug.Log("Trying local storage fallback...");
            LoadSavedProfilePictureLocally();
        }
    }

    private void LoadSavedProfilePictureLocally()
    {
        string savedSpriteName = PlayerPrefs.GetString("ProfilePicture", "");
        Debug.Log($"LoadSavedProfilePictureLocally: savedSpriteName = '{savedSpriteName}'");
        
        if (!string.IsNullOrEmpty(savedSpriteName))
        {
            foreach (Sprite avatar in availableAvatars)
            {
                if (avatar != null && avatar.name == savedSpriteName)
                {
                    profilePicture.sprite = avatar;
                    currentProfileSprite = avatar;
                    Debug.Log($"✅ Loaded avatar from local storage: {savedSpriteName}");
                    return;
                }
            }
            Debug.LogWarning($"❌ Local avatar '{savedSpriteName}' not found in available avatars");
        }
        
        // If nothing found, use default
        Debug.Log("Using default profile picture");
        SetDefaultProfilePicture();
    }
}