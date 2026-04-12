using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string loadSceneName = "LoadGameMenu";
    [SerializeField] private LoadGameMenuController loadGameMenuController;

    void Start()
    {
        SoundManager.Instance?.PlayMusic("MorningSound");
    }

    public void OnNewGamePressed()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("Click");

        if (GameRuntimeContext.Instance == null)
        {
            Debug.LogError("[MainMenuController] GameRuntimeContext not found.");
            return;
        }

        // Clear current runtime state, because player is starting a fresh run flow
        GameRuntimeContext.Instance.Clear();

        // IMPORTANT:
        // New Game should go to slot selection first.
        // The slot menu must be configured/set to New Game mode.
        loadGameMenuController.SetModeToNewGame();
    }

    public void OnLoadGamePressed()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("Click");

        if (GameRuntimeContext.Instance == null)
        {
            Debug.LogError("[MainMenuController] GameRuntimeContext not found.");
            return;
        }

        // Clear any unfinished pending new-game selection
        GameRuntimeContext.Instance.ClearPendingNewGameSlot();
        loadGameMenuController.SetModeToLoadGame();
    }

    public void OnCreditsPressed()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("Click");

        SceneManager.LoadScene("Credits");
    }

    public void OnBibleHelperPressed() 
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("Click");

        SceneManager.LoadScene("BibleHelper");
    }

    public void OnQuitPressed()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("Click");

        Application.Quit();
    }
}