using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using System.Threading.Tasks;
using System.Collections.Generic;

[System.Serializable]
public class UserProfileData
{
    public string selectedAvatarName;
    public string displayName;
    public string email;
    public long lastUpdated; // timestamp
    
    public UserProfileData()
    {
        selectedAvatarName = "";
        displayName = "";
        email = "";
        lastUpdated = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}

public class UserDataManager : MonoBehaviour
{
    public static UserDataManager Instance;
    
    private FirebaseFirestore firestore;
    private FirebaseUser currentUser;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
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
        // Initialize Firestore
        firestore = FirebaseFirestore.DefaultInstance;
        
        // Listen for authentication changes
        if (AuthenticationManager.Instance != null)
        {
            AuthenticationManager.Instance.OnSignInSuccess.AddListener(OnUserSignedIn);
            AuthenticationManager.Instance.OnSignOut.AddListener(OnUserSignedOut);
        }
    }
    
    private void OnUserSignedIn(FirebaseUser user)
    {
        currentUser = user;
        DebugLog($"UserDataManager: User signed in - {user.UserId}");
    }
    
    private void OnUserSignedOut()
    {
        currentUser = null;
        DebugLog("UserDataManager: User signed out");
    }
    
    // Save user's selected avatar to Firebase
    public async Task<bool> SaveUserAvatarAsync(string avatarName)
    {
        if (currentUser == null)
        {
            DebugLog("Cannot save avatar: No user signed in");
            return false;
        }
        
        try
        {
            // Create/update user profile data as Dictionary (Firestore-friendly)
            Dictionary<string, object> profileData = new Dictionary<string, object>
            {
                ["selectedAvatarName"] = avatarName,
                ["displayName"] = currentUser.DisplayName ?? "",
                ["email"] = currentUser.Email ?? "",
                ["lastUpdated"] = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            
            // Save to Firestore using user's UID as document ID
            DocumentReference docRef = firestore.Collection("userProfiles").Document(currentUser.UserId);
            await docRef.SetAsync(profileData, SetOptions.MergeAll);
            
            DebugLog($"Avatar saved successfully: {avatarName} for user {currentUser.Email}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save avatar: {ex.Message}");
            return false;
        }
    }
    
    // Load user's profile data from Firebase
    public async Task<UserProfileData> LoadUserProfileAsync()
    {
        if (currentUser == null)
        {
            DebugLog("Cannot load profile: No user signed in");
            return new UserProfileData();
        }
        
        try
        {
            DocumentReference docRef = firestore.Collection("userProfiles").Document(currentUser.UserId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            
            if (snapshot.Exists)
            {
                Dictionary<string, object> data = snapshot.ToDictionary();
                UserProfileData profileData = new UserProfileData
                {
                    selectedAvatarName = data.ContainsKey("selectedAvatarName") ? data["selectedAvatarName"].ToString() : "",
                    displayName = data.ContainsKey("displayName") ? data["displayName"].ToString() : "",
                    email = data.ContainsKey("email") ? data["email"].ToString() : "",
                    lastUpdated = data.ContainsKey("lastUpdated") ? System.Convert.ToInt64(data["lastUpdated"]) : 0
                };
                
                DebugLog($"Profile loaded successfully for user {currentUser.Email}. Avatar: {profileData.selectedAvatarName}");
                return profileData;
            }
            else
            {
                DebugLog($"No profile data found for user {currentUser.Email}. Creating new profile.");
                return new UserProfileData();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load profile: {ex.Message}");
            return new UserProfileData();
        }
    }
    
    // Quick method to just get the avatar name
    public async Task<string> GetUserAvatarAsync()
    {
        if (currentUser == null)
        {
            DebugLog("Cannot get avatar: No user signed in");
            return "";
        }
        
        try
        {
            DocumentReference docRef = firestore.Collection("userProfiles").Document(currentUser.UserId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            
            if (snapshot.Exists)
            {
                Dictionary<string, object> data = snapshot.ToDictionary();
                string avatarName = data.ContainsKey("selectedAvatarName") ? data["selectedAvatarName"].ToString() : "";
                DebugLog($"Avatar retrieved: {avatarName}");
                return avatarName;
            }
            else
            {
                DebugLog("No avatar data found");
                return "";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to get avatar: {ex.Message}");
            return "";
        }
    }
    
    // Save any user data (can be extended for other profile info)
    public async Task<bool> SaveUserDataAsync(Dictionary<string, object> data)
    {
        if (currentUser == null) return false;
        
        try
        {
            DocumentReference docRef = firestore.Collection("userProfiles").Document(currentUser.UserId);
            await docRef.UpdateAsync(data);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save user data: {ex.Message}");
            return false;
        }
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[UserDataManager] {message}");
        }
    }
    
    void OnDestroy()
    {
        // Clean up event listeners
        if (AuthenticationManager.Instance != null)
        {
            AuthenticationManager.Instance.OnSignInSuccess.RemoveListener(OnUserSignedIn);
            AuthenticationManager.Instance.OnSignOut.RemoveListener(OnUserSignedOut);
        }
    }
} 