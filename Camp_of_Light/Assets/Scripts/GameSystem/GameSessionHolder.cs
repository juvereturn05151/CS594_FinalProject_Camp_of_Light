using UnityEngine;

public class GameSessionHolder : MonoBehaviour
{
    public static GameSessionHolder Instance { get; private set; }

    public GameSession Session = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}