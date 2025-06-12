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
        // Wait for Firebase to be initialized, then initialize Firestore
        StartCoroutine(InitializeFirestoreWhenReady());
        
        // Listen for authentication changes
        if (AuthenticationManager.Instance != null)
        {
            DebugLog("🔑 Connecting to AuthenticationManager events");
            AuthenticationManager.Instance.OnSignInSuccess.AddListener(OnUserSignedIn);
            AuthenticationManager.Instance.OnSignOut.AddListener(OnUserSignedOut);
        }
        else
        {
            Debug.LogError("❌ AuthenticationManager.Instance is null!");
        }
    }
    
    private System.Collections.IEnumerator InitializeFirestoreWhenReady()
    {
        DebugLog("⏳ Waiting for Firebase to be ready...");
        
        // Wait for AuthenticationManager to be initialized
        while (AuthenticationManager.Instance == null || !AuthenticationManager.Instance.IsInitialized())
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        DebugLog("✅ Firebase Authentication is ready, initializing Firestore...");
        
        // First attempt to initialize Firestore
        bool initSuccess = TryInitializeFirestore();
        
        // If first attempt failed, retry after delay
        if (!initSuccess)
        {
            yield return new WaitForSeconds(2f);
            DebugLog("🔄 Retrying Firestore initialization...");
            TryInitializeFirestore();
        }
    }
    
    private bool TryInitializeFirestore()
    {
        try
        {
            DebugLog("🔥 Initializing Firestore...");
            firestore = FirebaseFirestore.DefaultInstance;
            DebugLog($"✅ Firestore initialized: {firestore != null}");
            
            if (firestore != null)
            {
                DebugLog("🎯 UserDataManager fully initialized and ready!");
                return true;
            }
            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Firestore initialization failed: {ex.Message}");
            return false;
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
        DebugLog($"💾 SaveUserAvatarAsync called with avatar: '{avatarName}'");
        
        if (currentUser == null)
        {
            DebugLog("❌ Cannot save avatar: No user signed in");
            return false;
        }
        
        if (firestore == null)
        {
            DebugLog("⏳ Firestore is null, waiting for initialization...");
            
            // Wait up to 10 seconds for Firestore to initialize
            float waitTime = 0f;
            while (firestore == null && waitTime < 10f)
            {
                await Task.Delay(100);
                waitTime += 0.1f;
            }
            
            if (firestore == null)
            {
                DebugLog("❌ Cannot save avatar: Firestore failed to initialize after 10 seconds");
                return false;
            }
            else
            {
                DebugLog("✅ Firestore is now ready!");
            }
        }
        
        try
        {
            DebugLog($"👤 Current user: {currentUser.Email} (UID: {currentUser.UserId})");
            DebugLog($"🔥 Firestore instance: {firestore != null}");
            
            // Create/update user profile data as Dictionary (Firestore-friendly)
            Dictionary<string, object> profileData = new Dictionary<string, object>
            {
                ["selectedAvatarName"] = avatarName,
                ["displayName"] = currentUser.DisplayName ?? "",
                ["email"] = currentUser.Email ?? "",
                ["lastUpdated"] = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            
            DebugLog($"📝 Profile data prepared: {profileData.Count} fields");
            
            // Save to Firestore using user's UID as document ID
            DocumentReference docRef = firestore.Collection("userProfiles").Document(currentUser.UserId);
            DebugLog($"📄 Document reference created: userProfiles/{currentUser.UserId}");
            
            DebugLog("🚀 Starting SetAsync operation...");
            await docRef.SetAsync(profileData, SetOptions.MergeAll);
            DebugLog("✅ SetAsync completed successfully");
            
            DebugLog($"✅ Avatar saved successfully: {avatarName} for user {currentUser.Email}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Failed to save avatar - Exception Type: {ex.GetType().Name}");
            Debug.LogError($"❌ Exception Message: {ex.Message}");
            Debug.LogError($"❌ Stack Trace: {ex.StackTrace}");
            
            // Check for specific Firebase exceptions
            if (ex.Message.Contains("network"))
            {
                Debug.LogError("🌐 Network-related error detected");
            }
            if (ex.Message.Contains("permission"))
            {
                Debug.LogError("🔒 Permission-related error detected");
            }
            if (ex.Message.Contains("auth"))
            {
                Debug.LogError("🔑 Authentication-related error detected");
            }
            
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
    public async Task<string> GetUserAvatarAsync(bool forceRefresh = false)
    {
        DebugLog($"🔍 GetUserAvatarAsync called with forceRefresh: {forceRefresh}");
        
        if (currentUser == null)
        {
            DebugLog("❌ Cannot get avatar: No user signed in");
            return "";
        }
        
        if (firestore == null)
        {
            DebugLog("⏳ Firestore is null, waiting for initialization...");
            
            // Wait up to 10 seconds for Firestore to initialize
            float waitTime = 0f;
            while (firestore == null && waitTime < 10f)
            {
                await Task.Delay(100);
                waitTime += 0.1f;
            }
            
            if (firestore == null)
            {
                DebugLog("❌ Cannot get avatar: Firestore failed to initialize after 10 seconds");
                return "";
            }
            else
            {
                DebugLog("✅ Firestore is now ready for retrieval!");
            }
        }
        
        try
        {
            DebugLog($"👤 Current user: {currentUser.Email} (UID: {currentUser.UserId})");
            
            DocumentReference docRef = firestore.Collection("userProfiles").Document(currentUser.UserId);
            DebugLog($"📄 Document reference: userProfiles/{currentUser.UserId}");
            
            // Force refresh from server if requested
            DocumentSnapshot snapshot;
            if (forceRefresh)
            {
                DebugLog("🌐 Force refreshing avatar data from Firebase SERVER (bypassing cache)...");
                snapshot = await docRef.GetSnapshotAsync(Source.Server);
                DebugLog($"📡 Server response received. Document exists: {snapshot.Exists}");
            }
            else
            {
                DebugLog("📱 Getting avatar data from Firebase (cache-first)...");
                snapshot = await docRef.GetSnapshotAsync();
            }
            
            if (snapshot.Exists)
            {
                Dictionary<string, object> data = snapshot.ToDictionary();
                string avatarName = data.ContainsKey("selectedAvatarName") ? data["selectedAvatarName"].ToString() : "";
                
                // Debug: Show all data in the document
                DebugLog($"📄 Document data contains {data.Count} fields:");
                foreach (var kvp in data)
                {
                    DebugLog($"  - {kvp.Key}: {kvp.Value}");
                }
                
                DebugLog($"🎯 Avatar retrieved: '{avatarName}' (forceRefresh: {forceRefresh})");
                return avatarName;
            }
            else
            {
                DebugLog("❌ No avatar document found in Firebase");
                return "";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Failed to get avatar - Exception Type: {ex.GetType().Name}");
            Debug.LogError($"❌ Exception Message: {ex.Message}");
            Debug.LogError($"❌ Stack Trace: {ex.StackTrace}");
            
            // Check for specific Firebase exceptions
            if (ex.Message.Contains("network"))
            {
                Debug.LogError("🌐 Network-related error detected");
            }
            if (ex.Message.Contains("permission"))
            {
                Debug.LogError("🔒 Permission-related error detected");
            }
            if (ex.Message.Contains("auth"))
            {
                Debug.LogError("🔑 Authentication-related error detected");
            }
            
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
    
    public bool IsFirestoreReady()
    {
        return firestore != null && currentUser != null;
    }
    
    public bool IsReady()
    {
        bool authReady = AuthenticationManager.Instance != null && AuthenticationManager.Instance.IsInitialized();
        bool firestoreReady = firestore != null;
        bool userReady = currentUser != null;
        
        DebugLog($"📊 UserDataManager readiness - Auth: {authReady}, Firestore: {firestoreReady}, User: {userReady}");
        return authReady && firestoreReady && userReady;
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