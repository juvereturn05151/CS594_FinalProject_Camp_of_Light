using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMainMenuButton : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public void GoToMainMenu()
    {
        if (GameUtility.FadingUIExists())
        {
            FadingUI.Instance.StartFadeIn();
            FadingUI.Instance.BindSceneToBeLoaded(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}