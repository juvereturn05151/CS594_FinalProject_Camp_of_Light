using OpenAI.Samples.Chat;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene Systems")]
    [SerializeField] private CampOfLightChatBehaviour cultChatController;
    [SerializeField] private ConscienceDialogueController conscienceController;
    [SerializeField] private CultProgressUI progressUI;
    [SerializeField] private CultGameDirector gameDirector;
    [SerializeField] private RegretSystem regretSystem;
    [SerializeField] private GameplaySaveBridge gameplaySaveBridge;

    [Header("Phase UI")]
    [SerializeField] private GameObject wakeUpPanel;
    [SerializeField] private TMP_Text wakeUpText;
    [SerializeField] private GameObject sleepPanel;
    [SerializeField] private TMP_Text sleepText;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameplaySceneName = "Gameplay";

    public GameRunState State { get; private set; }

    private bool isInitialized;

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

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == gameplaySceneName)
        {
            InitializeFromRuntimeOrSave();
        }
    }

    public void InitializeFromRuntimeOrSave()
    {
        if (isInitialized) return;

        if (GameRuntimeContext.Instance != null && GameRuntimeContext.Instance.HasSaveLoaded())
        {
            SaveData save = GameRuntimeContext.Instance.CurrentSave;
            State = ConvertSaveToRuntime(save);
        }
        else
        {
            Debug.LogWarning("[GameManager] No loaded save found. Creating fallback default state.");
            State = CreateDefaultState();
        }

        InjectStateIntoSystems();
        EnterCurrentPhase();

        isInitialized = true;
    }

    public void StartNewGame(PlayerProfile profile)
    {
        State = CreateDefaultState();
        State.Profile = profile;
        State.CurrentDay = 1;
        State.CurrentPhase = GamePhase.WakeUp;

        InjectStateIntoSystems();
    }

    public void LoadFromSave(SaveData save)
    {
        State = ConvertSaveToRuntime(save);
        InjectStateIntoSystems();
        EnterCurrentPhase();
        isInitialized = true;
    }

    public void EnterCurrentPhase()
    {
        HideAllPhasePanels();

        if (State == null || State.IsGameOver)
        {
            Debug.Log("[GameManager] Cannot enter phase. State null or game over.");
            return;
        }

        Debug.Log($"[GameManager] Entering Phase: {State.CurrentPhase} on Day {State.CurrentDay}");

        switch (State.CurrentPhase)
        {
            case GamePhase.WakeUp:
                StartWakeUpPhase();
                break;

            case GamePhase.PreachingLesson:
                StartPreachingPhase();
                break;

            case GamePhase.BrainwashingLesson:
                StartBrainwashingPhase();
                break;

            case GamePhase.ConscienceTalk:
                StartConsciencePhase();
                break;

            case GamePhase.Sleep:
                StartSleepPhase();
                break;
        }

        progressUI?.Refresh();
        SaveCheckpoint();
    }

    public void AdvancePhase()
    {
        if (State == null || State.IsGameOver) return;

        if (State.CurrentPhase == GamePhase.Sleep)
        {
            State.CurrentDay++;

            if (State.CurrentDay > State.MaxDays)
            {
                State.IsGameOver = true;
                State.Escaped = false;
                Debug.Log("[GameManager] Player is trapped forever.");
                SaveCheckpoint();
                return;
            }

            State.ResetForNewDay();
        }
        else
        {
            State.CurrentPhase += 1;
        }

        SyncRuntimeStateToSystems();
        EnterCurrentPhase();
    }

    public void NotifyCultTurnCompleted()
    {
        if (State == null || State.IsGameOver) return;

        SyncSystemsToRuntimeState();
        SaveCheckpoint();
        progressUI?.Refresh();
    }

    public void ReturnToMainMenu()
    {
        SaveCheckpoint();
        isInitialized = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void StartWakeUpPhase()
    {
        if (wakeUpPanel != null)
            wakeUpPanel.SetActive(true);

        if (wakeUpText != null)
            wakeUpText.text = BuildWakeUpSummary();

        cultChatController?.gameObject.SetActive(false);
        conscienceController?.Hide();
    }

    private void StartPreachingPhase()
    {
        cultChatController?.gameObject.SetActive(true);
        conscienceController?.Hide();

        State.PromptsUsedToday = Mathf.Clamp(State.PromptsUsedToday, 0, State.MaxPromptsPerDay);
    }

    private void StartBrainwashingPhase()
    {
        cultChatController?.gameObject.SetActive(true);
        conscienceController?.Hide();
    }

    private void StartConsciencePhase()
    {
        cultChatController?.gameObject.SetActive(false);

        Regret strongest = regretSystem != null ? regretSystem.GetStrongestRegret() : null;
        conscienceController?.ShowReflection(State, strongest);
    }

    private void StartSleepPhase()
    {
        ApplyOvernightEffects();

        if (sleepPanel != null)
            sleepPanel.SetActive(true);

        if (sleepText != null)
            sleepText.text = BuildSleepSummary();

        cultChatController?.gameObject.SetActive(false);
        conscienceController?.Hide();

        SaveCheckpoint();
        progressUI?.Refresh();
    }

    private void ApplyOvernightEffects()
    {
        if (State == null) return;

        State.Stats.Confidence = Mathf.Clamp(State.Stats.Confidence + 2, 0, 100);
        State.Stats.Brainwash = Mathf.Clamp(State.Stats.Brainwash - 1, 0, 100);

        if (State.Regrets != null)
        {
            foreach (var regret in State.Regrets)
            {
                regret.Strength = Mathf.Clamp(regret.Strength - 1, 0, 100);
            }
        }
    }

    private string BuildWakeUpSummary()
    {
        string status;

        if (State.Stats.Wokeness >= 70)
            status = "You wake with growing clarity.";
        else if (State.Stats.Brainwash >= 70)
            status = "You wake with their doctrine still crowding your mind.";
        else if (State.Stats.Confidence <= 25)
            status = "You wake feeling fragile and uncertain.";
        else
            status = "You wake to another day inside the expedition.";

        return $"Day {State.CurrentDay}\n\n{status}";
    }

    private string BuildSleepSummary()
    {
        return $"Night falls on Day {State.CurrentDay}.\n\nYou lie down and carry the weight of the day into sleep.";
    }

    private void HideAllPhasePanels()
    {
        if (wakeUpPanel != null)
            wakeUpPanel.SetActive(false);

        if (sleepPanel != null)
            sleepPanel.SetActive(false);

        conscienceController?.Hide();
    }

    private GameRunState CreateDefaultState()
    {
        return new GameRunState
        {
            Profile = new PlayerProfile(),
            Stats = new PlayerStats(),
            CurrentDay = 1,
            MaxDays = 45,
            CurrentPhase = GamePhase.WakeUp,
            PromptsUsedToday = 0,
            MaxPromptsPerDay = 20,
            IsGameOver = false,
            Escaped = false,
            LastExtractedRegret = "",
            LastBibleVerse = "",
            CurrentDoctrineId = "",
            CurrentTacticId = "",
            Regrets = new List<Regret>(),
            RecentDialogue = new List<DialogueTurn>()
        };
    }

    private void InjectStateIntoSystems()
    {
        if (State == null) return;

        if (cultChatController != null)
        {
            GameSession session = new GameSession
            {
                Profile = State.Profile,
                Stats = State.Stats,
                LastExtractedRegret = State.LastExtractedRegret,
                LastBibleVerse = State.LastBibleVerse
            };

            cultChatController.UpdateGameSession(session);
        }

        if (gameDirector != null)
        {
            gameDirector.CurrentDay = State.CurrentDay;
            gameDirector.MaxDays = State.MaxDays;
            gameDirector.PromptsUsed = State.PromptsUsedToday;
            gameDirector.MaxPromptsPerDay = State.MaxPromptsPerDay;
            gameDirector.IsGameOver = State.IsGameOver;
            gameDirector.Escaped = State.Escaped;
        }

        if (regretSystem != null)
        {
            regretSystem.regrets = State.Regrets ?? new List<Regret>();
        }

        progressUI?.Refresh();
    }

    private void SyncSystemsToRuntimeState()
    {
        if (State == null) return;

        if (cultChatController != null && cultChatController.GetSession() != null)
        {
            GameSession session = cultChatController.GetSession();

            State.Profile = session.Profile ?? new PlayerProfile();
            State.Stats = session.Stats ?? new PlayerStats();
            State.LastExtractedRegret = session.LastExtractedRegret ?? "";
            State.LastBibleVerse = session.LastBibleVerse ?? "";
        }

        if (gameDirector != null)
        {
            State.CurrentDay = gameDirector.CurrentDay;
            State.MaxDays = gameDirector.MaxDays;
            State.PromptsUsedToday = gameDirector.PromptsUsed;
            State.MaxPromptsPerDay = gameDirector.MaxPromptsPerDay;
            State.IsGameOver = gameDirector.IsGameOver;
            State.Escaped = gameDirector.Escaped;
        }

        if (regretSystem != null)
        {
            State.Regrets = regretSystem.regrets ?? new List<Regret>();
        }
    }

    private void SyncRuntimeStateToSystems()
    {
        InjectStateIntoSystems();
    }

    private void SaveCheckpoint()
    {
        if (State == null) return;
        if (SaveManager.Instance == null) return;
        if (GameRuntimeContext.Instance == null) return;
        if (!GameRuntimeContext.Instance.HasSaveLoaded()) return;

        SyncSystemsToRuntimeState();

        SaveData save = ConvertRuntimeToSave(State);
        save.SlotId = GameRuntimeContext.Instance.CurrentSave.SlotId;
        save.SaveDisplayName = GameRuntimeContext.Instance.CurrentSave.SaveDisplayName;
        save.CreatedAtUtc = GameRuntimeContext.Instance.CurrentSave.CreatedAtUtc;
        save.UpdatedAtUtc = System.DateTime.UtcNow.ToString("o");

        SaveManager.Instance.Save(save);
        GameRuntimeContext.Instance.SetCurrentSave(save);
    }

    private GameRunState ConvertSaveToRuntime(SaveData save)
    {
        var state = new GameRunState
        {
            Profile = new PlayerProfile
            {
                Name = save.Profile.Name,
                Age = save.Profile.Age,
                Profession = save.Profile.Profession,
                Interests = save.Profile.Interests != null ? new List<string>(save.Profile.Interests) : new List<string>()
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
            foreach (var r in save.Regrets)
            {
                state.Regrets.Add(new Regret
                {
                    Id = r.Id,
                    Text = r.Text,
                    Strength = r.Strength,
                    TimesMentioned = r.TimesMentioned
                });
            }
        }

        if (save.RecentDialogue != null)
        {
            foreach (var d in save.RecentDialogue)
            {
                state.RecentDialogue.Add(new DialogueTurn
                {
                    Speaker = d.Speaker,
                    Text = d.Text,
                    Timestamp = d.Timestamp
                });
            }
        }

        return state;
    }

    private SaveData ConvertRuntimeToSave(GameRunState state)
    {
        var save = new SaveData
        {
            Profile = new PlayerProfileData
            {
                Name = state.Profile.Name,
                Age = state.Profile.Age,
                Profession = state.Profile.Profession,
                Interests = state.Profile.Interests != null ? new List<string>(state.Profile.Interests) : new List<string>()
            },
            Stats = new PlayerStatsData
            {
                Confidence = state.Stats.Confidence,
                Brainwash = state.Stats.Brainwash,
                Wokeness = state.Stats.Wokeness
            },
            CurrentDay = state.CurrentDay,
            MaxDays = state.MaxDays,
            CurrentPhase = state.CurrentPhase,
            PromptsUsedToday = state.PromptsUsedToday,
            MaxPromptsPerDay = state.MaxPromptsPerDay,
            IsGameOver = state.IsGameOver,
            Escaped = state.Escaped,
            LastExtractedRegret = state.LastExtractedRegret,
            LastBibleVerse = state.LastBibleVerse,
            CurrentDoctrineId = state.CurrentDoctrineId,
            CurrentTacticId = state.CurrentTacticId,
            Regrets = new List<RegretData>(),
            RecentDialogue = new List<DialogueTurnData>()
        };

        if (state.Regrets != null)
        {
            foreach (var r in state.Regrets)
            {
                save.Regrets.Add(new RegretData
                {
                    Id = r.Id,
                    Text = r.Text,
                    Strength = r.Strength,
                    TimesMentioned = r.TimesMentioned
                });
            }
        }

        if (state.RecentDialogue != null)
        {
            foreach (var d in state.RecentDialogue)
            {
                save.RecentDialogue.Add(new DialogueTurnData
                {
                    Speaker = d.Speaker,
                    Text = d.Text,
                    Timestamp = d.Timestamp
                });
            }
        }

        return save;
    }
}