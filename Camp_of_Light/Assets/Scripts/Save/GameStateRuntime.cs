using UnityEngine;

public class GameStateRuntime : MonoBehaviour
{
    public static GameStateRuntime Instance { get; private set; }

    public SaveData CurrentSave { get; private set; }

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

    public void SetCurrentSave(SaveData save)
    {
        CurrentSave = save;
    }

    public bool HasSaveLoaded()
    {
        return CurrentSave != null;
    }

    public void Clear()
    {
        CurrentSave = null;
    }
}