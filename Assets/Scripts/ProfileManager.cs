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
        SetupAvatarButtons();
        LoadUserProfile();
    }

    public async void LoadUserProfile()
    {
        FirebaseUser user = AuthenticationManager.Instance.GetCurrentUser();
        if (user != null)
        {
            Debug.Log("User found! Populating profile info.");

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
            await LoadProfilePictureFromFirebase();
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

    private async void SelectAvatar(Sprite selectedAvatar)
    {
        profilePicture.sprite = selectedAvatar;
        currentProfileSprite = selectedAvatar;
        await SaveProfilePictureToFirebase();
        HideAvatarSelection();
    }

    private async System.Threading.Tasks.Task SaveProfilePictureToFirebase()
    {
        if (UserDataManager.Instance != null && currentProfileSprite != null)
        {
            string spriteName = currentProfileSprite.name;
            bool success = await UserDataManager.Instance.SaveUserAvatarAsync(spriteName);
            
            if (success)
            {
                Debug.Log($"Avatar saved to Firebase: {spriteName}");
            }
            else
            {
                Debug.LogError("Failed to save avatar to Firebase");
                // Fallback to local storage
                SaveProfilePictureLocally();
            }
        }
    }
    
    private void SaveProfilePictureLocally()
    {
        // Fallback: Save to PlayerPrefs for persistence
        string spriteName = currentProfileSprite.name;
        PlayerPrefs.SetString("ProfilePicture", spriteName);
        PlayerPrefs.Save();
    }

    private async System.Threading.Tasks.Task LoadProfilePictureFromFirebase()
    {
        if (UserDataManager.Instance != null)
        {
            string savedAvatarName = await UserDataManager.Instance.GetUserAvatarAsync();
            
            if (!string.IsNullOrEmpty(savedAvatarName))
            {
                // Find the avatar by name in our available avatars
                foreach (Sprite avatar in availableAvatars)
                {
                    if (avatar.name == savedAvatarName)
                    {
                        profilePicture.sprite = avatar;
                        currentProfileSprite = avatar;
                        Debug.Log($"Loaded avatar from Firebase: {savedAvatarName}");
                        return; // Found and set, exit early
                    }
                }
                Debug.LogWarning($"Avatar '{savedAvatarName}' not found in available avatars");
            }
        }
        
        // Fallback: Try to load from Firebase Auth PhotoUrl or use default
        FirebaseUser user = AuthenticationManager.Instance.GetCurrentUser();
        if (user != null && !string.IsNullOrEmpty(user.PhotoUrl?.ToString()))
        {
            StartCoroutine(LoadProfilePictureFromURL(user.PhotoUrl.ToString()));
        }
        else
        {
            // Last fallback: Check local storage then default
            LoadSavedProfilePictureLocally();
        }
    }

    private void LoadSavedProfilePictureLocally()
    {
        string savedSpriteName = PlayerPrefs.GetString("ProfilePicture", "");
        if (!string.IsNullOrEmpty(savedSpriteName))
        {
            foreach (Sprite avatar in availableAvatars)
            {
                if (avatar.name == savedSpriteName)
                {
                    profilePicture.sprite = avatar;
                    currentProfileSprite = avatar;
                    return;
                }
            }
        }
        
        // If nothing found, use default
        SetDefaultProfilePicture();
    }
}