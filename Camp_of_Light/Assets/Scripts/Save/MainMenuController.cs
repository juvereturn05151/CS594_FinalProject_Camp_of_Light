using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string profileSceneName = "ProfileCreation";
    [SerializeField] private string loadSceneName = "LoadGameMenu";

    public void OnNewGamePressed()
    {
        SoundManager.Instance.PlaySFX("Click");

        GameRuntimeContext.Instance.Clear();
        SceneManager.LoadScene(profileSceneName);
    }

    public void OnLoadGamePressed()
    {
        SoundManager.Instance.PlaySFX("Click");

        SceneManager.LoadScene(loadSceneName);
    }

    public void OnQuitPressed()
    {
        Application.Quit();
    }
}