# Firebase & Google Sign-In Configuration Checker
Write-Host "Firebase & Google Sign-In Configuration Checker" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""

Write-Host "=== Firebase Configuration Check ===" -ForegroundColor Cyan

# Check for google-services.json
$androidConfigPath = "Assets\Plugins\Android\google-services.json"
if (Test-Path $androidConfigPath) {
    Write-Host "[✓] Android config found: $androidConfigPath" -ForegroundColor Green
    $content = Get-Content $androidConfigPath -Raw
    if ($content -match '"client_id"') {
        Write-Host "[✓] Android config contains client_id" -ForegroundColor Green
    } else {
        Write-Host "[✗] Android config missing client_id" -ForegroundColor Red
    }
    
    # Check for OAuth client
    if ($content -match '"client_type"\s*:\s*3') {
        Write-Host "[✓] Web client (OAuth) configuration found" -ForegroundColor Green
        
        # Extract and display the client ID
        if ($content -match '"client_id"\s*:\s*"([^"]+)".*?"client_type"\s*:\s*3') {
            $webClientId = $matches[1]
            Write-Host "    Web Client ID: $webClientId" -ForegroundColor Yellow
            Write-Host "    Make sure this matches your GoogleAPI in LoginWithGoogle.cs" -ForegroundColor Yellow
        }
    } else {
        Write-Host "[✗] No Web client (OAuth) configuration found" -ForegroundColor Red
        Write-Host "    You need to add a Web Application OAuth client in Firebase Console" -ForegroundColor Yellow
    }
} else {
    Write-Host "[✗] Android config NOT found at: $androidConfigPath" -ForegroundColor Red
}

# Check for Firebase folders
Write-Host "`n=== Firebase SDK Check ===" -ForegroundColor Cyan
$firebaseFolders = @(
    "Assets\Firebase",
    "Assets\Plugins\Firebase",
    "Assets\FirebaseAnalytics",
    "Assets\FirebaseAuth"
)

foreach ($folder in $firebaseFolders) {
    if (Test-Path $folder) {
        Write-Host "[✓] Firebase folder exists: $folder" -ForegroundColor Green
    }
}

# Check for External Dependency Manager
if (Test-Path "Assets\ExternalDependencyManager") {
    Write-Host "[✓] External Dependency Manager found" -ForegroundColor Green
} else {
    Write-Host "[✗] External Dependency Manager not found - this is required for Firebase" -ForegroundColor Red
}

Write-Host "`n=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Make sure you have enabled Google Sign-In in Firebase Console" -ForegroundColor White
Write-Host "2. Ensure you have added both Android OAuth client AND Web Application OAuth client" -ForegroundColor White
Write-Host "3. The Web Client ID should be used in your Unity script" -ForegroundColor White
Write-Host "4. Run Tools > Firebase > Initialization Diagnostics in Unity Editor" -ForegroundColor White

Write-Host ""
Write-Host "Checking Debug Keystore SHA-1..." -ForegroundColor Yellow
Write-Host ""

# Check for debug keystore and get SHA-1
$debugKeystore = "$env:USERPROFILE\.android\debug.keystore"
if (Test-Path $debugKeystore) {
    Write-Host "Debug keystore found. Getting SHA-1 fingerprint..." -ForegroundColor Cyan
    Write-Host ""
    
    # Run keytool to get SHA-1
    try {
        $keytoolOutput = & keytool -list -v -keystore $debugKeystore -alias androiddebugkey -storepass android -keypass android 2>&1
        
        # Extract SHA1
        $sha1Line = $keytoolOutput | Select-String "SHA1:" | Select-Object -First 1
        if ($sha1Line) {
            $sha1 = $sha1Line.ToString().Trim()
            Write-Host $sha1 -ForegroundColor Green
            Write-Host ""
            Write-Host "To fix authentication issues:" -ForegroundColor Yellow
            Write-Host "1. Copy the SHA1 fingerprint above" -ForegroundColor White
            Write-Host "2. Go to Firebase Console > Project Settings > Your Android App" -ForegroundColor White
            Write-Host "3. Add this SHA1 fingerprint" -ForegroundColor White
            Write-Host "4. Download the updated google-services.json" -ForegroundColor White
            Write-Host "5. Replace the file in your Unity project" -ForegroundColor White
        } else {
            Write-Host "Could not extract SHA1 from keytool output" -ForegroundColor Red
        }
    } catch {
        Write-Host "Error running keytool: $_" -ForegroundColor Red
        Write-Host "Make sure you have Java JDK installed and keytool is in your PATH" -ForegroundColor Yellow
    }
} else {
    Write-Host "Debug keystore not found at: $debugKeystore" -ForegroundColor Red
    Write-Host "Build an Android app at least once to generate it" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 