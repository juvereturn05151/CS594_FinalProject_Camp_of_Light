using OpenAI.Samples.Chat;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplaySaveBridge : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private CampOfLightChatBehaviour chatBehaviour;
    [SerializeField] private CultGameDirector gameDirector;
    [SerializeField] private RegretSystem regretSystem;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public void SaveGame()
    {
        if (GameRuntimeContext.Instance == null)
        {
            Debug.LogError("[GameplaySaveBridge] GameRuntimeContext not found.");
            return;
        }

        if (!GameRuntimeContext.Instance.HasSaveLoaded())
        {
            Debug.LogError("[GameplaySaveBridge] No loaded save in GameRuntimeContext.");
            return;
        }

        GameRunState runState = BuildRunStateFromSceneSystems();
        GameRuntimeContext.Instance.SetCurrentRunState(runState);

        SaveData save = ConvertRunStateToSave(runState);

        SaveData existing = GameRuntimeContext.Instance.CurrentSave;
        save.SlotId = existing.SlotId;
        save.SaveDisplayName = existing.SaveDisplayName;
        save.CreatedAtUtc = existing.CreatedAtUtc;
        save.UpdatedAtUtc = System.DateTime.UtcNow.ToString("o");

        SaveManager.Instance.Save(save);
        GameRuntimeContext.Instance.SetCurrentSave(save);

        Debug.Log("[GameplaySaveBridge] Game saved.");
    }

    public void SaveAndReturnToMenu()
    {
        SaveGame();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public bool LoadIntoSceneSystems()
    {
        if (GameRuntimeContext.Instance == null)
        {
            Debug.LogError("[GameplaySaveBridge] GameRuntimeContext not found.");
            return false;
        }

        if (!GameRuntimeContext.Instance.HasSaveLoaded())
        {
            Debug.LogWarning("[GameplaySaveBridge] No save loaded. Gameplay scene will use defaults.");
            return false;
        }

        SaveData save = GameRuntimeContext.Instance.CurrentSave;
        GameRunState runState = ConvertSaveToRunState(save);

        GameRuntimeContext.Instance.SetCurrentRunState(runState);
        ApplyRunStateToSceneSystems(runState);

        return true;
    }

    private GameRunState BuildRunStateFromSceneSystems()
    {
        GameSession session = chatBehaviour != null ? chatBehaviour.GetSession() : null;

        GameRunState runState = new GameRunState
        {
            Profile = session != null && session.Profile != null ? session.Profile : new PlayerProfile(),
            Stats = session != null && session.Stats != null ? session.Stats : new PlayerStats(),

            CurrentDay = gameDirector != null ? gameDirector.CurrentDay : 1,
            MaxDays = gameDirector != null ? gameDirector.MaxDays : 45,

            CurrentPhase = GameRuntimeContext.Instance.HasRunState()
                ? GameRuntimeContext.Instance.CurrentRunState.CurrentPhase
                : GamePhase.WakeUp,

            PromptsUsedToday = gameDirector != null ? gameDirector.PromptsUsed : 0,
            MaxPromptsPerDay = gameDirector != null ? gameDirector.MaxPromptsPerDay : 20,

            IsGameOver = gameDirector != null && gameDirector.IsGameOver,
            Escaped = gameDirector != null && gameDirector.Escaped,

            LastExtractedRegret = session != null ? session.LastExtractedRegret : "",
            LastBibleVerse = session != null ? session.LastBibleVerse : "",

            CurrentDoctrineId = GameRuntimeContext.Instance.HasRunState()
                ? GameRuntimeContext.Instance.CurrentRunState.CurrentDoctrineId
                : "",

            CurrentTacticId = GameRuntimeContext.Instance.HasRunState()
                ? GameRuntimeContext.Instance.CurrentRunState.CurrentTacticId
                : "",

            Regrets = regretSystem != null && regretSystem.regrets != null
                ? CloneRegrets(regretSystem.regrets)
                : new List<Regret>(),

            RecentDialogue = GameRuntimeContext.Instance.HasRunState() &&
                             GameRuntimeContext.Instance.CurrentRunState.RecentDialogue != null
                ? CloneDialogue(GameRuntimeContext.Instance.CurrentRunState.RecentDialogue)
                : new List<DialogueTurn>()
        };

        return runState;
    }

    private void ApplyRunStateToSceneSystems(GameRunState runState)
    {
        if (runState == null)
        {
            Debug.LogWarning("[GameplaySaveBridge] Tried to apply null GameRunState.");
            return;
        }

        GameSession session = new GameSession
        {
            Profile = runState.Profile ?? new PlayerProfile(),
            Stats = runState.Stats ?? new PlayerStats(),
            LastExtractedRegret = runState.LastExtractedRegret ?? "",
            LastBibleVerse = runState.LastBibleVerse ?? ""
        };

        if (chatBehaviour != null)
            chatBehaviour.UpdateGameSession(session);

        if (gameDirector != null)
        {
            gameDirector.CurrentDay = runState.CurrentDay;
            gameDirector.MaxDays = runState.MaxDays;
            gameDirector.PromptsUsed = runState.PromptsUsedToday;
            gameDirector.MaxPromptsPerDay = runState.MaxPromptsPerDay;
            gameDirector.IsGameOver = runState.IsGameOver;
            gameDirector.Escaped = runState.Escaped;
        }

        if (regretSystem != null)
            regretSystem.regrets = CloneRegrets(runState.Regrets);
    }

    private SaveData ConvertRunStateToSave(GameRunState runState)
    {
        SaveData save = new SaveData
        {
            Profile = new PlayerProfileData
            {
                Name = runState.Profile.Name,
                Age = runState.Profile.Age,
                Profession = runState.Profile.Profession,
                Interests = runState.Profile.Interests != null
                    ? new List<string>(runState.Profile.Interests)
                    : new List<string>(),
            },

            Stats = new PlayerStatsData
            {
                Confidence = runState.Stats.Confidence,
                Brainwash = runState.Stats.Brainwash,
                Wokeness = runState.Stats.Wokeness
            },

            CurrentDay = runState.CurrentDay,
            MaxDays = runState.MaxDays,
            CurrentPhase = runState.CurrentPhase,
            PromptsUsedToday = runState.PromptsUsedToday,
            MaxPromptsPerDay = runState.MaxPromptsPerDay,
            IsGameOver = runState.IsGameOver,
            Escaped = runState.Escaped,

            LastExtractedRegret = runState.LastExtractedRegret,
            LastBibleVerse = runState.LastBibleVerse,
            CurrentDoctrineId = runState.CurrentDoctrineId,
            CurrentTacticId = runState.CurrentTacticId,

            Regrets = new List<RegretData>(),
            RecentDialogue = new List<DialogueTurnData>()
        };

        if (runState.Regrets != null)
        {
            foreach (Regret regret in runState.Regrets)
            {
                save.Regrets.Add(new RegretData
                {
                    Id = regret.Id,
                    Text = regret.Text,
                    Strength = regret.Strength,
                    TimesMentioned = regret.TimesMentioned
                });
            }
        }

        if (runState.RecentDialogue != null)
        {
            foreach (DialogueTurn turn in runState.RecentDialogue)
            {
                save.RecentDialogue.Add(new DialogueTurnData
                {
                    Speaker = turn.Speaker,
                    Text = turn.Text,
                    Timestamp = turn.Timestamp
                });
            }
        }

        return save;
    }

    private GameRunState ConvertSaveToRunState(SaveData save)
    {
        GameRunState runState = new GameRunState
        {
            Profile = new PlayerProfile
            {
                Name = save.Profile.Name,
                Age = save.Profile.Age,
                Profession = save.Profile.Profession,
                Interests = save.Profile.Interests != null
                    ? new List<string>(save.Profile.Interests)
                    : new List<string>(),
            },

            Stats = new PlayerStats
            {
                Confidence = save.Stats.Confidence,
                Brainwash = save.Stats.Brainwash,
                Wokeness = save.Stats.Wokeness
            },

            CurrentDay = save.CurrentDay,
            MaxDays = save.MaxDays,
            CurrentPhase = save.CurrentPhase,
            PromptsUsedToday = save.PromptsUsedToday,
            MaxPromptsPerDay = save.MaxPromptsPerDay,
            IsGameOver = save.IsGameOver,
            Escaped = save.Escaped,

            LastExtractedRegret = save.LastExtractedRegret,
            LastBibleVerse = save.LastBibleVerse,
            CurrentDoctrineId = save.CurrentDoctrineId,
            CurrentTacticId = save.CurrentTacticId,

            Regrets = new List<Regret>(),
            RecentDialogue = new List<DialogueTurn>()
        };

        if (save.Regrets != null)
        {
            foreach (RegretData regret in save.Regrets)
            {
                runState.Regrets.Add(new Regret
                {
                    Id = regret.Id,
                    Text = regret.Text,
                    Strength = regret.Strength,
                    TimesMentioned = regret.TimesMentioned
                });
            }
        }

        if (save.RecentDialogue != null)
        {
            foreach (DialogueTurnData turn in save.RecentDialogue)
            {
                runState.RecentDialogue.Add(new DialogueTurn
                {
                    Speaker = turn.Speaker,
                    Text = turn.Text,
                    Timestamp = turn.Timestamp
                });
            }
        }

        return runState;
    }

    private List<Regret> CloneRegrets(List<Regret> source)
    {
        List<Regret> result = new();
        if (source == null) return result;

        foreach (Regret regret in source)
        {
            result.Add(new Regret
            {
                Id = regret.Id,
                Text = regret.Text,
                Strength = regret.Strength,
                TimesMentioned = regret.TimesMentioned
            });
        }

        return result;
    }

    private List<DialogueTurn> CloneDialogue(List<DialogueTurn> source)
    {
        List<DialogueTurn> result = new();
        if (source == null) return result;

        foreach (DialogueTurn turn in source)
        {
            result.Add(new DialogueTurn
            {
                Speaker = turn.Speaker,
                Text = turn.Text,
                Timestamp = turn.Timestamp
            });
        }

        return result;
    }
}