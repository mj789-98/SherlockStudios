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

public class LoginWithGoogle : MonoBehaviour
{
    [Header("Google Sign-In Configuration")]
    public string GoogleAPI = "870916027714-ka7r1ka5vbiecdsp2ogei3pnvm7npi90.apps.googleusercontent.com"; // Updated with your Web Client ID
    private GoogleSignInConfiguration configuration;

    [Header("Firebase")]
    Firebase.Auth.FirebaseAuth auth;
    Firebase.Auth.FirebaseUser user;

    [Header("UI References")]
    public Text Username, UserEmail;
    public Image UserProfilePic;
    
    private string imageUrl;

    private void Awake()
    {
        configuration = new GoogleSignInConfiguration
        {
            WebClientId = GoogleAPI,
            RequestIdToken = true,
            RequestEmail = true
        };
    }

    private void Start()
    {
        InitFirebase();
    }

    void InitFirebase()
    {
        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        
        // Check if user is already signed in
        if (auth.CurrentUser != null)
        {
            user = auth.CurrentUser;
            UpdateUI();
        }
    }

    public void Login()
    {
        GoogleSignIn.Configuration = configuration;

        Task<GoogleSignInUser> signIn = GoogleSignIn.DefaultInstance.SignIn();

        TaskCompletionSource<FirebaseUser> signInCompleted = new TaskCompletionSource<FirebaseUser>();
        signIn.ContinueWith(task => {
            if (task.IsCanceled)
            {
                signInCompleted.SetCanceled();
                Debug.LogWarning("Google Sign-In was cancelled");
            }
            else if (task.IsFaulted)
            {
                signInCompleted.SetException(task.Exception);
                Debug.LogError("Google Sign-In failed: " + task.Exception);
            }
            else
            {
                Credential credential = Firebase.Auth.GoogleAuthProvider.GetCredential(task.Result.IdToken, null);
                auth.SignInWithCredentialAsync(credential).ContinueWith(authTask => {
                    if (authTask.IsCanceled)
                    {
                        signInCompleted.SetCanceled();
                        Debug.LogWarning("Firebase authentication was cancelled");
                    }
                    else if (authTask.IsFaulted)
                    {
                        signInCompleted.SetException(authTask.Exception);
                        Debug.LogError("Firebase authentication failed: " + authTask.Exception);
                    }
                    else
                    {
                        signInCompleted.SetResult(authTask.Result);
                        Debug.Log("Firebase authentication successful");
                        user = auth.CurrentUser;
                        
                        // Update UI on main thread
                        UnityMainThreadDispatcher.Instance().Enqueue(() => {
                            UpdateUI();
                        });
                    }
                });
            }
        });
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
        }
    }

    private string CheckImageUrl(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            return url;
        }
        return imageUrl;
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
                Debug.LogError("Failed to load profile image: " + request.error);
            }
        }
    }

    public void SignOut()
    {
        if (auth != null && auth.CurrentUser != null)
        {
            auth.SignOut();
            GoogleSignIn.DefaultInstance.SignOut();
            
            // Clear UI
            Username.text = "";
            UserEmail.text = "";
            UserProfilePic.sprite = null;
            
            Debug.Log("User signed out successfully");
        }
    }
}

// Helper class for main thread operations
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly Queue<System.Action> _executionQueue = new Queue<System.Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        return _instance;
    }

    public void Enqueue(System.Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
}