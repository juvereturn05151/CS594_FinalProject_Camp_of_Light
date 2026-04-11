using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoSceneTransition : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string nextSceneName;
    [SerializeField] private float delay = 3f;

    [Header("Skip Settings")]
    [SerializeField] private bool allowSkip = true;

    private float timer = 0f;
    private bool isLoading = false;

    private void Update()
    {
        if (isLoading)
            return;

        timer += Time.unscaledDeltaTime;

        // Auto transition after delay
        if (timer >= delay)
        {
            LoadNextScene();
            return;
        }

        // Skip input
        if (allowSkip && IsSkipPressed())
        {
            LoadNextScene();
        }
    }

    private bool IsSkipPressed()
    {
        return Input.anyKeyDown ||
               Input.GetMouseButtonDown(0) ||
               Input.touchCount > 0;
    }

    private void LoadNextScene()
    {
        if (isLoading)
            return;

        isLoading = true;

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("Next scene name is not set.");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}