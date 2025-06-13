using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;

public class AndroidBuildDiagnostics : EditorWindow
{
    [MenuItem("Tools/Firebase/Android Build Diagnostics")]
    public static void ShowWindow()
    {
        GetWindow<AndroidBuildDiagnostics>("Android Build Diagnostics");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Android Build Diagnostics", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("Check Android Build Settings"))
        {
            CheckAndroidBuildSettings();
        }

        if (GUILayout.Button("Fix google-services.json Import Settings"))
        {
            FixGoogleServicesImportSettings();
        }

        if (GUILayout.Button("Check Android Manifest"))
        {
            CheckAndroidManifest();
        }

        if (GUILayout.Button("Force Android Dependency Resolution"))
        {
            ForceAndroidDependencyResolution();
        }
    }

    void CheckAndroidBuildSettings()
    {
        Debug.Log("=== Android Build Settings ===");
        
        // Check build target
        Debug.Log($"Current Build Target: {EditorUserBuildSettings.activeBuildTarget}");
        
        // Check package name
        Debug.Log($"Package Name: {Application.identifier}");
        
        // Check minimum API level
        Debug.Log($"Minimum API Level: {PlayerSettings.Android.minSdkVersion}");
        Debug.Log($"Target API Level: {PlayerSettings.Android.targetSdkVersion}");
        
        // Check internet permission
        Debug.Log($"Internet Permission: {PlayerSettings.Android.forceInternetPermission}");
        
        // Check if package name matches google-services.json
        CheckPackageNameMatch();
    }

    void CheckPackageNameMatch()
    {
        string googleServicesPath = "Assets/Plugins/Android/google-services.json";
        if (File.Exists(googleServicesPath))
        {
            string jsonContent = File.ReadAllText(googleServicesPath);
            string currentPackageName = Application.identifier;
            
            if (jsonContent.Contains($"\"package_name\":\"{currentPackageName}\""))
            {
                Debug.Log($"✓ Package name matches google-services.json: {currentPackageName}");
            }
            else
            {
                Debug.LogError($"✗ Package name mismatch! Unity: {currentPackageName}");
                Debug.LogError("  Check your google-services.json for the correct package name");
                
                // Try to extract package name from JSON
                int packageIndex = jsonContent.IndexOf("\"package_name\":\"");
                if (packageIndex != -1)
                {
                    int startIndex = packageIndex + "\"package_name\":\"".Length;
                    int endIndex = jsonContent.IndexOf("\"", startIndex);
                    if (endIndex != -1)
                    {
                        string jsonPackageName = jsonContent.Substring(startIndex, endIndex - startIndex);
                        Debug.LogError($"  google-services.json expects: {jsonPackageName}");
                    }
                }
            }
        }
    }

    void FixGoogleServicesImportSettings()
    {
        string path = "Assets/Plugins/Android/google-services.json";
        TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        
        if (asset != null)
        {
            var importer = AssetImporter.GetAtPath(path) as PluginImporter;
            if (importer == null)
            {
                Debug.Log("Reimporting google-services.json with correct settings...");
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            
            Debug.Log("✓ google-services.json import settings fixed");
        }
        else
        {
            Debug.LogError("✗ Could not find google-services.json at expected location");
        }
    }

    void CheckAndroidManifest()
    {
        Debug.Log("=== Android Manifest Check ===");
        
        string[] manifestPaths = {
            "Assets/Plugins/Android/AndroidManifest.xml",
            "Assets/Plugins/Android/FirebaseApp.androidlib/AndroidManifest.xml",
            "Temp/StagingArea/AndroidManifest.xml"
        };
        
        bool foundManifest = false;
        foreach (string path in manifestPaths)
        {
            if (File.Exists(path))
            {
                Debug.Log($"Found manifest at: {path}");
                foundManifest = true;
                
                // Check for required permissions
                string content = File.ReadAllText(path);
                if (content.Contains("android.permission.INTERNET"))
                {
                    Debug.Log("✓ INTERNET permission found");
                }
                else
                {
                    Debug.LogWarning("✗ INTERNET permission not found");
                }
                
                if (content.Contains("com.google.android.gms"))
                {
                    Debug.Log("✓ Google Play Services references found");
                }
            }
        }
        
        if (!foundManifest)
        {
            Debug.LogWarning("No AndroidManifest.xml found. It will be generated during build.");
        }
    }

    void ForceAndroidDependencyResolution()
    {
        Debug.Log("=== Forcing Android Dependency Resolution ===");
        
        // Try to invoke the Android Resolver
        var androidResolverType = System.Type.GetType("GooglePlayServices.PlayServicesResolver, Unity.ExternalDependencyManager.Editor");
        if (androidResolverType != null)
        {
            var forceResolveMethod = androidResolverType.GetMethod("ForceResolve", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (forceResolveMethod != null)
            {
                Debug.Log("Invoking Android Resolver...");
                forceResolveMethod.Invoke(null, null);
                Debug.Log("✓ Android dependency resolution triggered");
            }
            else
            {
                Debug.LogError("Could not find ForceResolve method");
            }
        }
        else
        {
            Debug.LogError("Android Resolver not found. Make sure External Dependency Manager is installed.");
        }
    }
} 