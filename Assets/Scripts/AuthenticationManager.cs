using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using System.Threading.Tasks;
using System;

public class AuthenticationManager : MonoBehaviour
{
    public static AuthenticationManager Instance;

    [Header("Firebase Configuration")]
    public bool initializeOnStart = true;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent<FirebaseUser> OnSignInSuccess;
    public UnityEngine.Events.UnityEvent<string> OnSignInFailed;
    public UnityEngine.Events.UnityEvent OnSignOut;

    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private bool isInitialized = false;

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
            return;
        }
    }

    async void Start()
    {
        if (initializeOnStart)
        {
            await InitializeFirebase();
        }
    }

    public async Task<bool> InitializeFirebase()
    {
        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Initialize Firebase
                var app = FirebaseApp.DefaultInstance;
                auth = FirebaseAuth.DefaultInstance;
                
                // Subscribe to auth state changes
                auth.StateChanged += OnAuthStateChanged;
                auth.IdTokenChanged += OnIdTokenChanged;
                
                // Check current user
                currentUser = auth.CurrentUser;
                isInitialized = true;
                
                Debug.Log("Firebase Authentication initialized successfully");
                
                if (currentUser != null)
                {
                    Debug.Log($"User already signed in: {currentUser.Email ?? currentUser.UserId}");
                    OnSignInSuccess.Invoke(currentUser);
                }
                
                return true;
            }
            else
            {
                Debug.LogError($"Could not resolve Firebase dependencies: {dependencyStatus}");
                OnSignInFailed.Invoke($"Firebase initialization failed: {dependencyStatus}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Firebase initialization failed: {ex.Message}");
            OnSignInFailed.Invoke($"Firebase initialization error: {ex.Message}");
            return false;
        }
    }

    private void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        var newUser = auth.CurrentUser;
        
        if (currentUser != newUser)
        {
            bool wasSignedIn = currentUser != null;
            bool isSignedIn = newUser != null;
            
            currentUser = newUser;
            
            if (!wasSignedIn && isSignedIn)
            {
                // User signed in
                Debug.Log($"User signed in: {currentUser.Email ?? currentUser.UserId}");
                OnSignInSuccess.Invoke(currentUser);
            }
            else if (wasSignedIn && !isSignedIn)
            {
                // User signed out
                Debug.Log("User signed out");
                OnSignOut.Invoke();
            }
        }
    }

    private void OnIdTokenChanged(object sender, EventArgs eventArgs)
    {
        var newUser = auth.CurrentUser;
        if (newUser != null && newUser != currentUser)
        {
            Debug.Log($"Token refreshed for user: {newUser.Email ?? newUser.UserId}");
        }
    }

    #region Anonymous Authentication
    public async Task<bool> SignInAnonymouslyAsync()
    {
        if (!EnsureInitialized()) return false;

        try
        {
            var result = await auth.SignInAnonymouslyAsync();
            currentUser = result.User;
            Debug.Log($"Anonymous sign-in successful: {currentUser.UserId}");
            return true;
        }
        catch (FirebaseException ex)
        {
            Debug.LogError($"Anonymous sign-in failed: {ex.Message}");
            OnSignInFailed.Invoke($"Anonymous sign-in failed: {ex.Message}");
            return false;
        }
    }

    public void SignInAnonymously()
    {
        _ = SignInAnonymouslyAsync();
    }
    #endregion

    #region Email/Password Authentication
    public async Task<bool> SignInWithEmailPasswordAsync(string email, string password)
    {
        if (!EnsureInitialized()) return false;

        try
        {
            var result = await auth.SignInWithEmailAndPasswordAsync(email, password);
            currentUser = result.User;
            Debug.Log($"Email sign-in successful: {currentUser.Email}");
            return true;
        }
        catch (FirebaseException ex)
        {
            Debug.LogError($"Email sign-in failed: {ex.Message}");
            OnSignInFailed.Invoke($"Email sign-in failed: {ex.Message}");
            return false;
        }
    }

    public void SignInWithEmailPassword(string email, string password)
    {
        _ = SignInWithEmailPasswordAsync(email, password);
    }

    public async Task<bool> CreateUserWithEmailPasswordAsync(string email, string password)
    {
        if (!EnsureInitialized()) return false;

        try
        {
            var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            currentUser = result.User;
            Debug.Log($"User creation successful: {currentUser.Email}");
            return true;
        }
        catch (FirebaseException ex)
        {
            var authError = (AuthError)ex.ErrorCode;
            string errorMessage = GetUserFriendlyErrorMessage(authError, ex.Message);
            Debug.LogError($"User creation failed: {authError} - {errorMessage}");
            OnSignInFailed.Invoke($"User creation failed: {errorMessage}");
            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Unexpected error during user creation: {ex.Message}");
            OnSignInFailed.Invoke($"Unexpected error: {ex.Message}");
            return false;
        }
    }

    public void CreateUserWithEmailPassword(string email, string password)
    {
        _ = CreateUserWithEmailPasswordAsync(email, password);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string email)
    {
        if (!EnsureInitialized()) return false;

        try
        {
            await auth.SendPasswordResetEmailAsync(email);
            Debug.Log($"Password reset email sent to: {email}");
            return true;
        }
        catch (FirebaseException ex)
        {
            Debug.LogError($"Password reset failed: {ex.Message}");
            OnSignInFailed.Invoke($"Password reset failed: {ex.Message}");
            return false;
        }
    }

    public void SendPasswordResetEmail(string email)
    {
        _ = SendPasswordResetEmailAsync(email);
    }
    #endregion

    #region Sign Out
    public void SignOut()
    {
        if (auth != null)
        {
            auth.SignOut();
            currentUser = null;
            Debug.Log("User signed out");
        }
    }
    #endregion

    #region Utility Methods
    public bool IsSignedIn()
    {
        return currentUser != null && auth?.CurrentUser != null;
    }

    public FirebaseUser GetCurrentUser()
    {
        return currentUser;
    }

    public bool IsInitialized()
    {
        return isInitialized && auth != null;
    }

    private bool EnsureInitialized()
    {
        if (!IsInitialized())
        {
            Debug.LogError("Firebase Authentication is not initialized. Call InitializeFirebase() first.");
            OnSignInFailed.Invoke("Firebase not initialized");
            return false;
        }
        return true;
    }

    private string GetUserFriendlyErrorMessage(AuthError authError, string originalMessage)
    {
        switch (authError)
        {
            case AuthError.EmailAlreadyInUse:
                return "This email address is already registered. Please use a different email or sign in instead.";
            case AuthError.InvalidEmail:
                return "Please enter a valid email address.";
            case AuthError.WeakPassword:
                return "Password is too weak. Please choose a stronger password with at least 6 characters.";
            case AuthError.WrongPassword:
                return "Incorrect password. Please try again.";
            case AuthError.UserNotFound:
                return "No account found with this email address. Please check your email or create a new account.";
            case AuthError.UserDisabled:
                return "This account has been disabled. Please contact support for assistance.";
            case AuthError.TooManyRequests:
                return "Too many failed attempts. Please wait a moment before trying again.";
            case AuthError.NetworkRequestFailed:
                return "Network error. Please check your internet connection and try again.";
            case AuthError.MissingEmail:
                return "Please enter your email address.";
            case AuthError.MissingPassword:
                return "Please enter your password.";
            case AuthError.RequiresRecentLogin:
                return "This operation requires recent authentication. Please sign out and sign in again.";
            case AuthError.AccountExistsWithDifferentCredentials:
                return "An account already exists with the same email but different sign-in credentials.";
            case AuthError.CredentialAlreadyInUse:
                return "This credential is already associated with a different user account.";
            case AuthError.InvalidCredential:
                return "The provided credentials are invalid. Please check and try again.";
            case AuthError.OperationNotAllowed:
                return "This sign-in method is not enabled. Please contact support.";
            case AuthError.InvalidUserToken:
                return "Your session has expired. Please sign in again.";
            case AuthError.UserTokenExpired:
                return "Your session has expired. Please sign in again.";
            case AuthError.Cancelled:
                return "Operation was cancelled.";
            default:
                // Return the original message for unknown errors
                return string.IsNullOrEmpty(originalMessage) ? "An unexpected error occurred. Please try again." : originalMessage;
        }
    }

    public string GetUserInfo()
    {
        if (currentUser == null) return "No user signed in";
        
        return $"User ID: {currentUser.UserId}\n" +
               $"Email: {currentUser.Email ?? "N/A"}\n" +
               $"Display Name: {currentUser.DisplayName ?? "N/A"}\n" +
               $"Email Verified: {currentUser.IsEmailVerified}\n" +
               $"Anonymous: {currentUser.IsAnonymous}";
    }
    #endregion

    void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= OnAuthStateChanged;
            auth.IdTokenChanged -= OnIdTokenChanged;
        }
    }
}