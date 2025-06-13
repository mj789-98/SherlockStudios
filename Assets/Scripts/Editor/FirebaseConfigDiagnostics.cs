#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

public class FirebaseConfigDiagnostics : EditorWindow
{
    private Vector2 scrollPosition;
    private Dictionary<string, DiagnosticResult> results = new Dictionary<string, DiagnosticResult>();
    
    private class DiagnosticResult
    {
        public bool success;
        public string message;
        public string details;
    }
    
    [MenuItem("Tools/Firebase & Google Sign-In Diagnostics")]
    public static void ShowWindow()
    {
        var window = GetWindow<FirebaseConfigDiagnostics>("Firebase Diagnostics");
        window.minSize = new Vector2(500, 600);
        window.RunDiagnostics();
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Firebase & Google Sign-In Configuration Diagnostics", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Run Diagnostics", GUILayout.Height(30)))
        {
            RunDiagnostics();
        }
        
        GUILayout.Space(10);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        foreach (var kvp in results)
        {
            EditorGUILayout.BeginVertical("box");
            
            var result = kvp.Value;
            var oldColor = GUI.color;
            GUI.color = result.success ? Color.green : Color.red;
            
            EditorGUILayout.LabelField(kvp.Key, result.success ? "✓ PASS" : "✗ FAIL", EditorStyles.boldLabel);
            
            GUI.color = oldColor;
            
            EditorGUILayout.LabelField(result.message, EditorStyles.wordWrappedLabel);
            
            if (!string.IsNullOrEmpty(result.details))
            {
                EditorGUILayout.HelpBox(result.details, result.success ? MessageType.Info : MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }
        
        EditorGUILayout.EndScrollView();
        
        GUILayout.Space(10);
        
        if (results.Count > 0 && results.Values.Any(r => !r.success))
        {
            EditorGUILayout.HelpBox("Some checks failed. Please fix the issues above and run diagnostics again.", MessageType.Error);
            
            if (GUILayout.Button("Copy Diagnostic Report to Clipboard"))
            {
                CopyReportToClipboard();
            }
        }
    }
    
    private void RunDiagnostics()
    {
        results.Clear();
        
        // Check 1: google-services.json
        CheckGoogleServicesJson();
        
        // Check 2: Firebase dependencies
        CheckFirebaseDependencies();
        
        // Check 3: Google Sign-In configuration
        CheckGoogleSignInConfig();
        
        // Check 4: Android settings
        CheckAndroidSettings();
        
        // Check 5: Web Client ID
        CheckWebClientId();
        
        // Check 6: Firebase project configuration
        CheckFirebaseProjectConfig();
        
        // Check 7: SHA certificates
        CheckSHACertificates();
        
        Repaint();
    }
    
    private void CheckGoogleServicesJson()
    {
        string path = "Assets/google-services.json";
        if (!File.Exists(path))
        {
            path = "Assets/StreamingAssets/google-services.json";
        }
        
        if (File.Exists(path))
        {
            string content = File.ReadAllText(path);
            
            // Parse key information
            var packageMatch = Regex.Match(content, "\"package_name\"\\s*:\\s*\"([^\"]+)\"");
            var projectIdMatch = Regex.Match(content, "\"project_id\"\\s*:\\s*\"([^\"]+)\"");
            var webClientIdMatch = Regex.Match(content, "\"client_id\"\\s*:\\s*\"([^\"]+)\".*\"client_type\"\\s*:\\s*3", RegexOptions.Singleline);
            
            string details = "";
            if (packageMatch.Success) details += $"Package: {packageMatch.Groups[1].Value}\n";
            if (projectIdMatch.Success) details += $"Project ID: {projectIdMatch.Groups[1].Value}\n";
            if (webClientIdMatch.Success) details += $"Web Client ID: {webClientIdMatch.Groups[1].Value}";
            
            results["google-services.json"] = new DiagnosticResult
            {
                success = true,
                message = $"File found at: {path}",
                details = details
            };
        }
        else
        {
            results["google-services.json"] = new DiagnosticResult
            {
                success = false,
                message = "google-services.json not found!",
                details = "Download from Firebase Console > Project Settings > Your Android App"
            };
        }
    }
    
    private void CheckFirebaseDependencies()
    {
        bool hasFirebaseAuth = Directory.Exists("Assets/Firebase/Auth");
        bool hasFirebaseApp = Directory.Exists("Assets/Firebase/App");
        
        if (hasFirebaseAuth && hasFirebaseApp)
        {
            results["Firebase SDK"] = new DiagnosticResult
            {
                success = true,
                message = "Firebase Auth and App SDKs found",
                details = "Firebase SDKs are properly installed"
            };
        }
        else
        {
            results["Firebase SDK"] = new DiagnosticResult
            {
                success = false,
                message = "Missing Firebase SDKs",
                details = $"Auth: {(hasFirebaseAuth ? "✓" : "✗")}, App: {(hasFirebaseApp ? "✓" : "✗")}\nReimport Firebase Unity SDK"
            };
        }
    }
    
    private void CheckGoogleSignInConfig()
    {
        bool hasGoogleSignIn = File.Exists("Assets/GoogleSignIn/GoogleSignIn.cs");
        bool hasNativeLib = File.Exists("Assets/Plugins/Android/google-signin-support-1.0.4.aar") ||
                           Directory.GetFiles("Assets", "google-signin-support*.aar", SearchOption.AllDirectories).Length > 0;
        
        if (hasGoogleSignIn && hasNativeLib)
        {
            results["Google Sign-In Plugin"] = new DiagnosticResult
            {
                success = true,
                message = "Google Sign-In plugin properly configured",
                details = "Plugin files and native library found"
            };
        }
        else
        {
            results["Google Sign-In Plugin"] = new DiagnosticResult
            {
                success = false,
                message = "Google Sign-In configuration incomplete",
                details = $"Plugin: {(hasGoogleSignIn ? "✓" : "✗")}, Native Lib: {(hasNativeLib ? "✓" : "✗")}\nRun Tools > Fix Google Sign-In Libraries"
            };
        }
    }
    
    private void CheckAndroidSettings()
    {
        string packageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        int minSdkVersion = (int)PlayerSettings.Android.minSdkVersion;
        ScriptingImplementation scriptingBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
        
        bool validPackage = !string.IsNullOrEmpty(packageName) && packageName != "com.DefaultCompany.SherlockStudios";
        bool validMinSdk = minSdkVersion >= 21;
        bool validBackend = scriptingBackend == ScriptingImplementation.IL2CPP;
        
        results["Android Settings"] = new DiagnosticResult
        {
            success = validPackage && validMinSdk,
            message = validPackage && validMinSdk ? "Android settings configured correctly" : "Android settings need adjustment",
            details = $"Package: {packageName} {(validPackage ? "✓" : "(Should not use default)")}\n" +
                     $"Min SDK: {minSdkVersion} {(validMinSdk ? "✓" : "(Should be 21+)")}\n" +
                     $"Backend: {scriptingBackend} {(validBackend ? "✓ (Recommended)" : "(IL2CPP recommended)")}"
        };
    }
    
    private void CheckWebClientId()
    {
        // Try to find the Web Client ID in use
        var loginScripts = AssetDatabase.FindAssets("t:MonoScript LoginWithGoogle");
        if (loginScripts.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(loginScripts[0]);
            var content = File.ReadAllText(path);
            var match = Regex.Match(content, "GoogleAPI\\s*=\\s*\"([^\"]+)\"");
            
            if (match.Success)
            {
                string webClientId = match.Groups[1].Value;
                bool isValid = webClientId.EndsWith(".apps.googleusercontent.com") && 
                              !webClientId.Contains("Enter web client id here");
                
                results["Web Client ID"] = new DiagnosticResult
                {
                    success = isValid,
                    message = isValid ? "Web Client ID is configured" : "Web Client ID not properly set",
                    details = isValid ? $"ID: {webClientId}" : "Set the Web Client ID from google-services.json (oauth_client with type 3)"
                };
            }
        }
    }
    
    private void CheckFirebaseProjectConfig()
    {
        // Check if google-services.json matches package name
        string packageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        string googleServicesPath = File.Exists("Assets/google-services.json") ? "Assets/google-services.json" : "Assets/StreamingAssets/google-services.json";
        
        if (File.Exists(googleServicesPath))
        {
            string content = File.ReadAllText(googleServicesPath);
            bool packageMatches = content.Contains($"\"package_name\": \"{packageName}\"") || 
                                 content.Contains($"\"package_name\":\"{packageName}\"");
            
            results["Package Name Match"] = new DiagnosticResult
            {
                success = packageMatches || packageName == "com.DefaultCompany.SherlockStudios",
                message = packageMatches ? "Package name matches google-services.json" : "Package name mismatch",
                details = packageMatches ? "Configuration is consistent" : 
                         $"Unity: {packageName}\ngoogle-services.json expects: com.DefaultCompany.SherlockStudios\n" +
                         "Either change Unity package name or update Firebase project"
            };
        }
    }
    
    private void CheckSHACertificates()
    {
        results["SHA Certificates"] = new DiagnosticResult
        {
            success = false, // We can't verify this from Unity
            message = "Manual verification required",
            details = "Ensure SHA-1 fingerprint is added in Firebase Console:\n" +
                     "1. Build APK\n" +
                     "2. Run: keytool -list -printcert -jarfile YourApp.apk\n" +
                     "3. Add SHA-1 to Firebase Console > Project Settings > Your App\n" +
                     "4. Download updated google-services.json"
        };
    }
    
    private void CopyReportToClipboard()
    {
        string report = "Firebase & Google Sign-In Diagnostic Report\n";
        report += "==========================================\n\n";
        
        foreach (var kvp in results)
        {
            report += $"{kvp.Key}: {(kvp.Value.success ? "PASS" : "FAIL")}\n";
            report += $"  {kvp.Value.message}\n";
            if (!string.IsNullOrEmpty(kvp.Value.details))
            {
                report += $"  Details: {kvp.Value.details}\n";
            }
            report += "\n";
        }
        
        GUIUtility.systemCopyBuffer = report;
        EditorUtility.DisplayDialog("Report Copied", "Diagnostic report copied to clipboard", "OK");
    }
}
#endif 