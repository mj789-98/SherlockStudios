using UnityEngine;
using UnityEditor;
using Firebase;
using Firebase.Auth;
using System.Linq;

public class FirebaseInitDiagnostics : EditorWindow
{
    [MenuItem("Tools/Firebase/Initialization Diagnostics")]
    public static void ShowWindow()
    {
        GetWindow<FirebaseInitDiagnostics>("Firebase Diagnostics");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Firebase Diagnostics", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Check Firebase App
        if (GUILayout.Button("Check Firebase App Status"))
        {
            CheckFirebaseApp();
        }

        // Check Firebase configuration files
        if (GUILayout.Button("Check Firebase Configuration Files"))
        {
            CheckFirebaseConfigFiles();
        }

        // Force re-check dependencies
        if (GUILayout.Button("Force Re-check Dependencies"))
        {
            ForceRecheckDependencies();
        }
    }

    void CheckFirebaseApp()
    {
        Debug.Log("=== Firebase App Status ===");
        
        try
        {
            // Check if default instance exists
            if (FirebaseApp.DefaultInstance != null)
            {
                Debug.Log("✓ Default Firebase App exists!");
                Debug.Log($"  App Name: {FirebaseApp.DefaultInstance.Name}");
                
                var options = FirebaseApp.DefaultInstance.Options;
                if (options != null)
                {
                    Debug.Log($"  API Key: {(string.IsNullOrEmpty(options.ApiKey) ? "NOT SET" : "SET")}");
                    Debug.Log($"  App ID: {(string.IsNullOrEmpty(options.AppId) ? "NOT SET" : "SET")}");
                    Debug.Log($"  Database URL: {(options.DatabaseUrl == null ? "NOT SET" : options.DatabaseUrl.ToString())}");
                    Debug.Log($"  Project ID: {(string.IsNullOrEmpty(options.ProjectId) ? "NOT SET" : options.ProjectId)}");
                    Debug.Log($"  Storage Bucket: {(string.IsNullOrEmpty(options.StorageBucket) ? "NOT SET" : options.StorageBucket)}");
                    Debug.Log($"  Messaging Sender ID: {(string.IsNullOrEmpty(options.MessageSenderId) ? "NOT SET" : "SET")}");
                }
                else
                {
                    Debug.LogWarning("  Firebase App Options is NULL!");
                }
            }
            else
            {
                Debug.LogWarning("✗ Default Firebase App is NULL!");
                Debug.Log("  This means Firebase has not been initialized yet.");
            }
            
            // Try to get a specific app
            try
            {
                var app = FirebaseApp.GetInstance("[DEFAULT]");
                if (app != null)
                {
                    Debug.Log("✓ Can retrieve [DEFAULT] app instance");
                }
            }
            catch
            {
                Debug.Log("✗ Cannot retrieve [DEFAULT] app instance");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error checking Firebase App: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }

    void CheckFirebaseConfigFiles()
    {
        Debug.Log("=== Firebase Configuration Files ===");
        
        // Check for google-services.json
        string androidConfigPath = "Assets/Plugins/Android/google-services.json";
        if (System.IO.File.Exists(androidConfigPath))
        {
            Debug.Log($"✓ Android config found: {androidConfigPath}");
            string content = System.IO.File.ReadAllText(androidConfigPath);
            if (content.Contains("client_id"))
            {
                Debug.Log("✓ Android config contains client_id");
            }
            else
            {
                Debug.LogWarning("✗ Android config missing client_id");
            }
        }
        else
        {
            Debug.LogError($"✗ Android config NOT found at: {androidConfigPath}");
        }

        // Check for GoogleService-Info.plist
        string iosConfigPath = "Assets/Plugins/iOS/GoogleService-Info.plist";
        if (System.IO.File.Exists(iosConfigPath))
        {
            Debug.Log($"✓ iOS config found: {iosConfigPath}");
        }
        else
        {
            Debug.LogWarning($"iOS config not found at: {iosConfigPath} (This is OK if you're not building for iOS)");
        }

        // Check Firebase SDK folders
        string[] firebaseFolders = {
            "Assets/Firebase",
            "Assets/Plugins/Firebase",
            "Assets/FirebaseAnalytics",
            "Assets/FirebaseAuth"
        };

        foreach (var folder in firebaseFolders)
        {
            if (System.IO.Directory.Exists(folder))
            {
                Debug.Log($"✓ Firebase folder exists: {folder}");
            }
        }
    }

    void ForceRecheckDependencies()
    {
        Debug.Log("=== Force Re-checking Dependencies ===");
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            Debug.Log($"Dependency Status: {dependencyStatus}");
            
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                Debug.Log("✓ Firebase dependencies are available!");
            }
            else
            {
                Debug.LogError($"✗ Firebase dependencies failed: {dependencyStatus}");
            }
        }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
    }
} 