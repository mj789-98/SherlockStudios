using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;
using Google;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using Firebase.Auth;
using Firebase;
using System;

public class LoginWithGoogle : MonoBehaviour
{
    [Header("Google Sign-In Configuration")]
    public string GoogleAPI = "870916027714-ka7r1ka5vbiecdsp2ogei3pnvm7npi90.apps.googleusercontent.com"; // Updated with your Web Client ID
    private GoogleSignInConfiguration configuration;

    [Header("Firebase")]
    Firebase.Auth.FirebaseAuth auth;
    Firebase.Auth.FirebaseUser user;
    Firebase.DependencyStatus dependencyStatus = Firebase.DependencyStatus.UnavailableOther;

    [Header("UI References")]
    public Text Username, UserEmail;
    public Image UserProfilePic;
    public Text StatusText; // Add a status text to show what's happening
    
    private string imageUrl;
    private bool isSigningIn = false;
    private bool initializationStarted = false;
    private float initializationTimeout = 30f; // 30 second timeout
    private float initializationStartTime;

    private void Awake()
    {
        // Ensure MainThreadDispatcher exists
        if (FindObjectOfType<MainThreadDispatcher>() == null)
        {
            GameObject dispatcher = new GameObject("MainThreadDispatcher");
            dispatcher.AddComponent<MainThreadDispatcher>();
        }
        
        configuration = new GoogleSignInConfiguration
        {
            WebClientId = GoogleAPI,
            RequestIdToken = true,
            RequestEmail = true,
            UseGameSignIn = false,
            RequestAuthCode = false,
            RequestProfile = true
        };
        
        UpdateStatus("Initializing...");
        
        // Log platform info
        Debug.Log($"[LoginWithGoogle] Platform: {Application.platform}");
        Debug.Log($"[LoginWithGoogle] Is Editor: {Application.isEditor}");
        #if UNITY_ANDROID
        Debug.Log("[LoginWithGoogle] UNITY_ANDROID is defined");
        #endif
    }

    private void Start()
    {
        initializationStarted = true;
        initializationStartTime = Time.time;
        
        // Check Firebase dependencies first
        Debug.Log("[LoginWithGoogle] Starting Firebase dependency check...");
        UpdateStatus("Checking Firebase dependencies...");
        
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            dependencyStatus = task.Result;
            Debug.Log($"[LoginWithGoogle] Dependency check completed with status: {dependencyStatus}");
            
            // Ensure we're on the main thread for UI updates
            MainThreadDispatcher.Post(() => {
                if (dependencyStatus == Firebase.DependencyStatus.Available)
                {
                    Debug.Log("[LoginWithGoogle] Firebase dependencies available!");
                    UpdateStatus("Dependencies OK, initializing Firebase...");
                    InitFirebase();
                }
                else
                {
                    Debug.LogError($"[LoginWithGoogle] Could not resolve Firebase dependencies: {dependencyStatus}");
                    UpdateStatus($"Firebase Error: {dependencyStatus}");
                    
                    // Log more details about the error
                    if (task.Exception != null)
                    {
                        Debug.LogError($"[LoginWithGoogle] Exception details: {task.Exception}");
                    }
                }
            });
        });
        
        // Start timeout coroutine
        StartCoroutine(InitializationTimeoutCheck());
    }

    IEnumerator InitializationTimeoutCheck()
    {
        yield return new WaitForSeconds(initializationTimeout);
        
        if (initializationStarted && dependencyStatus != Firebase.DependencyStatus.Available)
        {
            Debug.LogError($"[LoginWithGoogle] Firebase initialization timed out after {initializationTimeout} seconds");
            UpdateStatus("Firebase initialization timed out!");
        }
    }

    void InitFirebase()
    {
        Debug.Log("[LoginWithGoogle] InitFirebase called on thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId);
        Debug.Log("[LoginWithGoogle] Initializing Firebase Auth...");
        
        try
        {
            // Log Firebase App status before getting auth instance
            if (Firebase.FirebaseApp.DefaultInstance != null)
            {
                Debug.Log("[LoginWithGoogle] Firebase DefaultInstance exists before Auth init");
                var options = Firebase.FirebaseApp.DefaultInstance.Options;
                if (options != null)
                {
                    Debug.Log($"[LoginWithGoogle] Project ID: {options.ProjectId}");
                    Debug.Log($"[LoginWithGoogle] App ID: {options.AppId}");
                }
            }
            else
            {
                Debug.LogError("[LoginWithGoogle] Firebase DefaultInstance is NULL!");
            }
            
            auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
            Debug.Log("[LoginWithGoogle] FirebaseAuth.DefaultInstance obtained successfully");
            
            // Listen for auth state changes
            auth.StateChanged += AuthStateChanged;
            Debug.Log("[LoginWithGoogle] Auth state change listener registered");
            
            // Check if user is already signed in
            if (auth.CurrentUser != null)
            {
                user = auth.CurrentUser;
                Debug.Log($"[LoginWithGoogle] User already signed in: {user.Email}");
                UpdateUI();
            }
            else
            {
                Debug.Log("[LoginWithGoogle] No user currently signed in");
                UpdateStatus("Ready to sign in");
            }
            
            // Mark as initialized
            dependencyStatus = Firebase.DependencyStatus.Available;
            Debug.Log("[LoginWithGoogle] Firebase initialization completed successfully");
            
            // Log successful initialization time
            float initTime = Time.time - initializationStartTime;
            Debug.Log($"[LoginWithGoogle] Initialization took {initTime:F2} seconds");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoginWithGoogle] Failed to initialize Firebase: {e.Message}");
            Debug.LogError($"[LoginWithGoogle] Stack trace: {e.StackTrace}");
            UpdateStatus($"Firebase init failed: {e.Message}");
        }
    }

    void AuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
            if (!signedIn && user != null)
            {
                Debug.Log("[LoginWithGoogle] User signed out");
                UpdateStatus("Signed out");
            }
            user = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log($"[LoginWithGoogle] User signed in: {user.DisplayName}");
                UpdateUI();
            }
        }
    }

    public void Login()
    {
        if (isSigningIn)
        {
            Debug.LogWarning("[LoginWithGoogle] Already signing in, please wait...");
            return;
        }
        
        if (dependencyStatus != Firebase.DependencyStatus.Available)
        {
            Debug.LogError($"[LoginWithGoogle] Firebase not ready! Status: {dependencyStatus}");
            UpdateStatus("Firebase not ready!");
            
            // Try to reinitialize if it failed
            Debug.Log("[LoginWithGoogle] Attempting to reinitialize Firebase...");
            UpdateStatus("Retrying Firebase initialization...");
            Start(); // Restart the initialization process
            return;
        }
        
        Debug.Log("[LoginWithGoogle] Starting Google Sign-In process...");
        UpdateStatus("Starting Google Sign-In...");
        isSigningIn = true;
        
        GoogleSignIn.Configuration = configuration;
        
        // Start the sign-in process
        Task<GoogleSignInUser> signInTask = GoogleSignIn.DefaultInstance.SignIn();
        
        signInTask.ContinueWith(OnGoogleSignInCompleted);
    }

    void OnGoogleSignInCompleted(Task<GoogleSignInUser> task)
    {
        if (task.IsCanceled)
        {
            Debug.LogWarning("[LoginWithGoogle] Google Sign-In was cancelled by user");
            UpdateStatus("Sign-in cancelled");
            isSigningIn = false;
            return;
        }
        
        if (task.IsFaulted)
        {
            Debug.LogError($"[LoginWithGoogle] Google Sign-In failed with error: {task.Exception}");
            UpdateStatus($"Sign-in failed: {task.Exception?.GetBaseException()?.Message}");
            isSigningIn = false;
            return;
        }
        
        GoogleSignInUser googleUser = task.Result;
        Debug.Log($"[LoginWithGoogle] Google Sign-In successful! User: {googleUser.Email}");
        Debug.Log($"[LoginWithGoogle] ID Token: {(string.IsNullOrEmpty(googleUser.IdToken) ? "NULL/EMPTY" : "Present")}");
        Debug.Log($"[LoginWithGoogle] Auth Code: {(string.IsNullOrEmpty(googleUser.AuthCode) ? "NULL/EMPTY" : "Present")}");
        
        UpdateStatus($"Google Sign-In successful for {googleUser.Email}, authenticating with Firebase...");
        
        // Now sign in to Firebase with the Google credentials
        SignInWithFirebase(googleUser);
    }

    void SignInWithFirebase(GoogleSignInUser googleUser)
    {
        if (string.IsNullOrEmpty(googleUser.IdToken))
        {
            Debug.LogError("[LoginWithGoogle] ID Token is null or empty! Cannot authenticate with Firebase.");
            UpdateStatus("Error: No ID Token received from Google");
            isSigningIn = false;
            return;
        }
        
        Debug.Log("[LoginWithGoogle] Creating Firebase credential...");
        Credential credential = Firebase.Auth.GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
        
        Debug.Log("[LoginWithGoogle] Signing in to Firebase with credential...");
        auth.SignInWithCredentialAsync(credential).ContinueWith(task => {
            if (task.IsCanceled)
            {
                Debug.LogWarning("[LoginWithGoogle] Firebase authentication was cancelled");
                MainThreadDispatcher.Post(() => {
                    UpdateStatus("Firebase auth cancelled");
                    isSigningIn = false;
                });
                return;
            }
            
            if (task.IsFaulted)
            {
                Debug.LogError($"[LoginWithGoogle] Firebase authentication failed: {task.Exception}");
                FirebaseException firebaseEx = task.Exception.GetBaseException() as FirebaseException;
                if (firebaseEx != null)
                {
                    Firebase.Auth.AuthError errorCode = (Firebase.Auth.AuthError)firebaseEx.ErrorCode;
                    Debug.LogError($"[LoginWithGoogle] Firebase Auth Error Code: {errorCode}");
                    MainThreadDispatcher.Post(() => {
                        UpdateStatus($"Firebase auth failed: {GetErrorMessage(errorCode)}");
                        isSigningIn = false;
                    });
                }
                else
                {
                    MainThreadDispatcher.Post(() => {
                        UpdateStatus($"Firebase auth failed: {task.Exception?.GetBaseException()?.Message}");
                        isSigningIn = false;
                    });
                }
                return;
            }
            
            // Success!
            user = task.Result;
            Debug.Log($"[LoginWithGoogle] Firebase authentication successful! UID: {user.UserId}");
            Debug.Log($"[LoginWithGoogle] User Display Name: {user.DisplayName}");
            Debug.Log($"[LoginWithGoogle] User Email: {user.Email}");
            
            // Update UI on main thread
            MainThreadDispatcher.Post(() => {
                UpdateStatus($"Signed in as {user.Email}");
                UpdateUI();
                isSigningIn = false;
            });
        });
    }

    private string GetErrorMessage(Firebase.Auth.AuthError error)
    {
        switch (error)
        {
            case Firebase.Auth.AuthError.AccountExistsWithDifferentCredentials:
                return "Account exists with different credentials";
            case Firebase.Auth.AuthError.InvalidCredential:
                return "Invalid credential";
            case Firebase.Auth.AuthError.InvalidEmail:
                return "Invalid email";
            case Firebase.Auth.AuthError.WrongPassword:
                return "Wrong password";
            case Firebase.Auth.AuthError.UserNotFound:
                return "User not found";
            case Firebase.Auth.AuthError.NetworkRequestFailed:
                return "Network error";
            default:
                return error.ToString();
        }
    }

    private void UpdateUI()
    {
        if (user != null)
        {
            Username.text = user.DisplayName ?? "No Display Name";
            UserEmail.text = user.Email ?? "No Email";

            if (user.PhotoUrl != null)
            {
                StartCoroutine(LoadImage(user.PhotoUrl.ToString()));
            }
            
            Debug.Log($"[LoginWithGoogle] UI Updated for user: {user.Email}");
        }
    }

    private void UpdateStatus(string status)
    {
        Debug.Log($"[LoginWithGoogle] Status: {status}");
        if (StatusText != null)
        {
            StatusText.text = status;
        }
    }

    IEnumerator LoadImage(string imageUri)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUri))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                UserProfilePic.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
            else
            {
                Debug.LogError($"[LoginWithGoogle] Failed to load profile image: {request.error}");
            }
        }
    }

    public void SignOut()
    {
        if (auth != null && auth.CurrentUser != null)
        {
            Debug.Log("[LoginWithGoogle] Signing out...");
            auth.SignOut();
            GoogleSignIn.DefaultInstance.SignOut();
            
            // Clear UI
            Username.text = "";
            UserEmail.text = "";
            UserProfilePic.sprite = null;
            UpdateStatus("Signed out");
            
            Debug.Log("[LoginWithGoogle] User signed out successfully");
        }
    }
    
    void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
        }
    }
}

// Simple main thread dispatcher
public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher instance;
    private static readonly Queue<Action> actions = new Queue<Action>();
    private static readonly object lockObject = new object();

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void Post(Action action)
    {
        if (action == null) return;
        
        lock (lockObject)
        {
            actions.Enqueue(action);
        }
        
        // Ensure dispatcher exists
        if (instance == null)
        {
            GameObject go = new GameObject("MainThreadDispatcher");
            instance = go.AddComponent<MainThreadDispatcher>();
        }
    }

    void Update()
    {
        lock (lockObject)
        {
            while (actions.Count > 0)
            {
                try
                {
                    actions.Dequeue()?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"MainThreadDispatcher: {e}");
                }
            }
        }
    }
}