#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class GoogleSignInLibraryFixer : Editor
{
    [MenuItem("Tools/Fix Google Sign-In Libraries")]
    public static void FixGoogleSignInLibraries()
    {
        Debug.Log("[GoogleSignIn] Starting library fix process...");
        
        // Step 1: Force Android Resolver to run
        ForceAndroidResolve();
        
        // Step 2: Check for and copy the AAR file
        if (!CheckAndCopyAARFile())
        {
            // Step 3: If AAR not found, provide download instructions
            ShowDownloadInstructions();
        }
        
        // Step 4: Verify Android manifest
        CheckAndroidManifest();
        
        Debug.Log("[GoogleSignIn] Library fix process completed!");
        EditorUtility.DisplayDialog("Google Sign-In Fix", 
            "Google Sign-In library fix process completed!\n\n" +
            "Please check the Console for any warnings or errors.\n\n" +
            "After this, try building your project again.", 
            "OK");
    }
    
    private static void ForceAndroidResolve()
    {
        Debug.Log("[GoogleSignIn] Forcing Android dependency resolution...");
        
        try
        {
            // Try to invoke the Android Resolver
            var resolverType = System.Type.GetType("GooglePlayServices.PlayServicesResolver, Google.JarResolver");
            if (resolverType != null)
            {
                var forceResolveMethod = resolverType.GetMethod("ForceResolve", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (forceResolveMethod != null)
                {
                    forceResolveMethod.Invoke(null, null);
                    Debug.Log("[GoogleSignIn] Android Resolver: Force resolve completed.");
                }
                else
                {
                    // Try alternative method
                    var resolveMethod = resolverType.GetMethod("Resolve", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (resolveMethod != null)
                    {
                        resolveMethod.Invoke(null, new object[] { null, true, null });
                        Debug.Log("[GoogleSignIn] Android Resolver: Resolve completed.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[GoogleSignIn] Could not find Android Resolver. Make sure External Dependency Manager is imported.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GoogleSignIn] Failed to run Android Resolver: {e.Message}");
        }
    }
    
    private static bool CheckAndCopyAARFile()
    {
        Debug.Log("[GoogleSignIn] Checking for Google Sign-In AAR file...");
        
        string[] searchPaths = new string[]
        {
            "Assets/GeneratedLocalRepo/GoogleSignIn/Editor/m2repository/com/google/signin/google-signin-support/1.0.4",
            "Assets/GeneratedLocalRepo",
            "Assets/Plugins/Android",
            "Assets"
        };
        
        foreach (var searchPath in searchPaths)
        {
            if (!Directory.Exists(searchPath)) continue;
            
            string[] aarFiles = Directory.GetFiles(searchPath, "google-signin-support*.aar", SearchOption.AllDirectories);
            if (aarFiles.Length > 0)
            {
                string sourceFile = aarFiles[0];
                string targetDir = "Assets/Plugins/Android";
                string targetFile = Path.Combine(targetDir, "google-signin-support-1.0.4.aar");
                
                Debug.Log($"[GoogleSignIn] Found AAR at: {sourceFile}");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                    AssetDatabase.Refresh();
                }
                
                // Copy the file if it doesn't exist in target location
                if (!File.Exists(targetFile) || new FileInfo(sourceFile).Length != new FileInfo(targetFile).Length)
                {
                    File.Copy(sourceFile, targetFile, true);
                    Debug.Log($"[GoogleSignIn] Copied AAR to: {targetFile}");
                    
                    // Import the AAR file settings
                    AssetDatabase.ImportAsset(targetFile);
                    var importer = AssetImporter.GetAtPath(targetFile) as PluginImporter;
                    if (importer != null)
                    {
                        importer.SetCompatibleWithPlatform(BuildTarget.Android, true);
                        importer.SaveAndReimport();
                        Debug.Log("[GoogleSignIn] AAR import settings configured.");
                    }
                }
                else
                {
                    Debug.Log("[GoogleSignIn] AAR already exists in Plugins/Android.");
                }
                
                return true;
            }
        }
        
        Debug.LogError("[GoogleSignIn] Google Sign-In AAR file not found!");
        return false;
    }
    
    private static void ShowDownloadInstructions()
    {
        string message = "Google Sign-In native library not found!\n\n" +
                        "To fix this:\n" +
                        "1. Download the Google Sign-In Unity plugin from:\n" +
                        "   https://github.com/googlesamples/google-signin-unity/releases\n\n" +
                        "2. Import the .unitypackage file\n\n" +
                        "3. Run 'Assets > External Dependency Manager > Android Resolver > Force Resolve'\n\n" +
                        "4. Run this fix again from 'Tools > Fix Google Sign-In Libraries'";
        
        EditorUtility.DisplayDialog("Google Sign-In Library Missing", message, "OK");
        Debug.LogError("[GoogleSignIn] " + message);
    }
    
    private static void CheckAndroidManifest()
    {
        Debug.Log("[GoogleSignIn] Checking Android Manifest...");
        
        string manifestPath = "Assets/Plugins/Android/AndroidManifest.xml";
        if (!File.Exists(manifestPath))
        {
            Debug.LogWarning("[GoogleSignIn] No custom AndroidManifest.xml found. Unity will use the default one.");
            Debug.LogWarning("[GoogleSignIn] If you have issues, you may need to create a custom manifest.");
        }
        else
        {
            string content = File.ReadAllText(manifestPath);
            if (!content.Contains("com.google.android.gms.auth.api.signin"))
            {
                Debug.LogWarning("[GoogleSignIn] AndroidManifest.xml doesn't contain Google Sign-In activities.");
                Debug.LogWarning("[GoogleSignIn] The External Dependency Manager should add these automatically.");
            }
            else
            {
                Debug.Log("[GoogleSignIn] AndroidManifest.xml appears to be configured correctly.");
            }
        }
    }
    
    [MenuItem("Tools/Verify Google Sign-In Setup")]
    public static void VerifyGoogleSignInSetup()
    {
        Debug.Log("[GoogleSignIn] === Verifying Google Sign-In Setup ===");
        
        // Check for AAR file
        bool aarFound = File.Exists("Assets/Plugins/Android/google-signin-support-1.0.4.aar");
        Debug.Log($"[GoogleSignIn] AAR in Plugins/Android: {(aarFound ? "✓ Found" : "✗ Not Found")}");
        
        // Check for Google Services JSON
        bool jsonFound = File.Exists("Assets/google-services.json") || File.Exists("Assets/StreamingAssets/google-services.json");
        Debug.Log($"[GoogleSignIn] google-services.json: {(jsonFound ? "✓ Found" : "✗ Not Found")}");
        
        // Check dependencies XML
        bool depsFound = File.Exists("ProjectSettings/AndroidResolverDependencies.xml");
        if (depsFound)
        {
            string deps = File.ReadAllText("ProjectSettings/AndroidResolverDependencies.xml");
            bool hasSignIn = deps.Contains("google-signin-support");
            Debug.Log($"[GoogleSignIn] Dependencies XML: ✓ Found, Contains Sign-In: {(hasSignIn ? "✓" : "✗")}");
        }
        else
        {
            Debug.Log("[GoogleSignIn] Dependencies XML: ✗ Not Found");
        }
        
        // Check for Firebase
        bool hasFirebase = Directory.Exists("Assets/Firebase");
        Debug.Log($"[GoogleSignIn] Firebase SDK: {(hasFirebase ? "✓ Found" : "✗ Not Found")}");
        
        Debug.Log("[GoogleSignIn] === Verification Complete ===");
        
        string status = aarFound ? "Setup appears correct!" : "Setup incomplete - run 'Tools > Fix Google Sign-In Libraries'";
        EditorUtility.DisplayDialog("Google Sign-In Verification", status, "OK");
    }
}
#endif 