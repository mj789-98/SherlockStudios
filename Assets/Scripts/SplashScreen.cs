// SplashScreen.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SplashScreen : MonoBehaviour
{
    public float splashDuration = 3f;

    void Start()
    {
        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(splashDuration);

        // Check if user is already logged in
        if (AuthenticationManager.Instance != null && AuthenticationManager.Instance.IsSignedIn())
        {
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            SceneManager.LoadScene("LoginScreen");
        }
    }
}

