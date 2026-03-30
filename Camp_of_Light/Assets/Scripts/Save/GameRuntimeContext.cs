using UnityEngine;

public class GameRuntimeContext : MonoBehaviour
{
    public static GameRuntimeContext Instance { get; private set; }

    public SaveData CurrentSave { get; private set; }
    public GameRunState CurrentRunState { get; private set; }

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

    public void SetCurrentRunState(GameRunState runState)
    {
        CurrentRunState = runState;
    }

    public bool HasSaveLoaded()
    {
        return CurrentSave != null;
    }

    public bool HasRunState()
    {
        return CurrentRunState != null;
    }

    public void Clear()
    {
        CurrentSave = null;
        CurrentRunState = null;
    }
}