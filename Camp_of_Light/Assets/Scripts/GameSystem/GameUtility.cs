using UnityEngine.SceneManagement;

public static class GameUtility
{
    public static bool GameManagerExists()
    {
        return GameManager.Instance != null;
    }

    public static bool FadingUIExists()
    {
        return FadingUI.Instance != null;
    }

    // Other utility methods
}