#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using UnityEditor.Android;

public class GoogleSignInBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android)
        {
            Debug.Log("[GoogleSignIn] Preprocessing Android build...");
            EnsureGoogleSignInLibraries();
        }
    }

    private void EnsureGoogleSignInLibraries()
    {
        // Force resolve dependencies using External Dependency Manager
        Debug.Log("[GoogleSignIn] Checking for Google Sign-In native libraries...");
        
        // Verify Google Sign-In support library exists
        string[] searchPaths = new string[]
        {
            "Assets/Plugins/Android",
            "Assets/GeneratedLocalRepo",
            "Assets/GeneratedLocalRepo/GoogleSignIn/Editor/m2repository/com/google/signin/google-signin-support/1.0.4"
        };

        bool foundLibrary = false;
        foreach (var path in searchPaths)
        {
            if (Directory.Exists(path))
            {
                string[] aarFiles = Directory.GetFiles(path, "google-signin-support*.aar", SearchOption.AllDirectories);
                if (aarFiles.Length > 0)
                {
                    foundLibrary = true;
                    Debug.Log($"[GoogleSignIn] Found library at: {aarFiles[0]}");
                    
                    // Ensure it's in the Plugins/Android directory for proper inclusion
                    string targetPath = "Assets/Plugins/Android/google-signin-support-1.0.4.aar";
                    if (!File.Exists(targetPath))
                    {
                        Directory.CreateDirectory("Assets/Plugins/Android");
                        File.Copy(aarFiles[0], targetPath, true);
                        Debug.Log($"[GoogleSignIn] Copied library to: {targetPath}");
                    }
                    break;
                }
            }
        }

        if (!foundLibrary)
        {
            Debug.LogError("[GoogleSignIn] CRITICAL: Google Sign-In support library not found!");
            Debug.LogError("[GoogleSignIn] Please run 'Assets > External Dependency Manager > Android Resolver > Force Resolve'");
            Debug.LogError("[GoogleSignIn] The app will crash with DllNotFoundException on Android!");
        }
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android)
        {
            Debug.Log("[GoogleSignIn] Android build completed.");
        }
    }
}
#endif 