using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Phase Managers")]
    [SerializeField]
    private WakeUpPhaseManager wakeUpPhaseManager;
    [SerializeField]
    private PreachingPhaseManager preachingPhaseManager;
    [SerializeField]
    private BrainwashPhaseManager brainwashPhaseManager;
    [SerializeField]
    private ConsciencePhaseManager consciencePhaseManager;
    [SerializeField]
    private SleepPhaseManager sleepPhaseManager;

    [Header("Scene Names")]
    [SerializeField]
    private string mainMenuSceneName = "MainMenu";
    [SerializeField]
    private string gameplaySceneName = "Gameplay";

    public GameRunState State { get; private set; }

    private CultProgressUI progressUI;
    private CultGameDirector gameDirector;
    private RegretSystem regretSystem;
    public RegretSystem RegretSystem => regretSystem;

    private Dictionary<GamePhase, IPhaseManager> phaseManagers;
    private IPhaseManager activePhaseManager;

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

        phaseManagers = new Dictionary<GamePhase, IPhaseManager>
        {
            { GamePhase.WakeUp, wakeUpPhaseManager },
            { GamePhase.PreachingLesson, preachingPhaseManager },
            { GamePhase.BrainwashingLesson, brainwashPhaseManager },
            { GamePhase.ConscienceTalk, consciencePhaseManager },
            { GamePhase.Sleep, sleepPhaseManager }
        };
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
        if (isInitialized)
            return;

        GameSharedSystem.Instance.Initialize();

        progressUI = GameSharedSystem.Instance.ProgressUI;
        gameDirector = GameSharedSystem.Instance.GameDirector;
        regretSystem = GameSharedSystem.Instance.RegretSystem;

        progressUI.Init();

        preachingPhaseManager.PreacherController.Init();
        brainwashPhaseManager.BrainwasherController.Init();
        consciencePhaseManager.ConscienceController.Init();
        Debug.Log(Application.persistentDataPath);
        State = LoadInitialState();
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
        EnterCurrentPhase();
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
        if (State == null || State.IsGameOver)
        {
            Debug.Log("[GameManager] Cannot enter phase. State null or game over.");
            return;
        }

        activePhaseManager?.ExitPhase();

        if (phaseManagers.TryGetValue(State.CurrentPhase, out var phaseManager))
        {
            activePhaseManager = phaseManager;
            activePhaseManager.EnterPhase(State);
        }

        progressUI?.Refresh();
        SaveCheckpoint();
    }

    public void AdvancePhase()
    {
        if (State == null || State.IsGameOver)
            return;

        if (State.CurrentPhase == GamePhase.Sleep)
        {
            AdvanceToNextDay();
        }
        else
        {
            State.CurrentPhase += 1;
        }

        InjectStateIntoSystems();
        EnterCurrentPhase();
    }

    public void NotifyCultTurnCompleted()
    {
        if (State == null || State.IsGameOver)
            return;

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

    private GameRunState LoadInitialState()
    {
        if (GameRuntimeContext.Instance != null && GameRuntimeContext.Instance.HasSaveLoaded())
        {
            return ConvertSaveToRuntime(GameRuntimeContext.Instance.CurrentSave);
        }

        Debug.LogWarning("[GameManager] No loaded save found. Creating fallback default state.");
        return CreateDefaultState();
    }

    private void AdvanceToNextDay()
    {
        State.CurrentDay++;
        State.ResetForNewDay();
    }

    private GameRunState CreateDefaultState()
    {
        return new GameRunState
        {
            Profile = new PlayerProfile
            {
                Name = string.Empty,
                CharacterAppearancePrompt = string.Empty,
                PlayerCharacterImagePath = string.Empty,
                Interests = new List<string>(),
                SpiritCharacterPrompt = string.Empty,
                SpiritCharacterImagePath = string.Empty
            },
            Stats = new PlayerStats(),
            CurrentDay = 1,
            MaxDays = 45,
            CurrentPhase = GamePhase.WakeUp,
            PromptsUsedToday_Brainwash = 0,
            MaxPromptsPerDay_Brainwash = 7,
            PromptsUsedToday_Conscience = 0,
            MaxPromptsPerDay_Conscience = 4,
            IsGameOver = false,
            good_ending_1 = false,
            good_ending_2 = false,
            bad_ending_1 = false,
            bad_ending_2 = false,
            LastExtractedRegret = string.Empty,
            CurrentDoctrineId = string.Empty,
            CurrentTacticId = string.Empty,
            Regrets = new List<Regret>(),
            RecentDialogue = new List<DialogueTurn>()
        };
    }

    private void InjectStateIntoSystems()
    {
        if (State == null)
            return;

        if (gameDirector != null)
        {
            gameDirector.CurrentDay = State.CurrentDay;
            gameDirector.MaxDays = State.MaxDays;
            gameDirector.PromptsUsed_Brainwash = State.PromptsUsedToday_Brainwash;
            gameDirector.MaxPrompts_Brainwash = State.MaxPromptsPerDay_Brainwash;
            gameDirector.IsGameOver = State.IsGameOver;
            gameDirector.good_ending_1 = State.good_ending_1;
            gameDirector.good_ending_2 = State.good_ending_2;
            gameDirector.bad_ending_1 = State.bad_ending_1;
            gameDirector.bad_ending_2 = State.bad_ending_2;

        }

        if (regretSystem != null)
        {
            regretSystem.regrets = State.Regrets ?? new List<Regret>();
        }

        progressUI?.Refresh();
    }

    private void SyncSystemsToRuntimeState()
    {
        if (State == null)
            return;

        if (gameDirector != null)
        {
            State.CurrentDay = gameDirector.CurrentDay;
            State.MaxDays = gameDirector.MaxDays;
            State.PromptsUsedToday_Brainwash = gameDirector.PromptsUsed_Brainwash;
            State.MaxPromptsPerDay_Brainwash = gameDirector.MaxPrompts_Brainwash;
            State.PromptsUsedToday_Conscience = gameDirector.PromptsUsed_Conscience;
            State.MaxPromptsPerDay_Conscience = gameDirector.MaxPrompts_Conscience;
            State.IsGameOver = gameDirector.IsGameOver;
            State.good_ending_1 = gameDirector.good_ending_1;
            State.good_ending_2 = gameDirector.good_ending_2;
            State.bad_ending_1 = gameDirector.bad_ending_1;
            State.bad_ending_2 = gameDirector.bad_ending_2;
        }

        if (regretSystem != null)
            State.Regrets = regretSystem.regrets ?? new List<Regret>();
    }

    public void SaveCheckpoint()
    {
        if (State == null ||
            SaveManager.Instance == null ||
            GameRuntimeContext.Instance == null ||
            !GameRuntimeContext.Instance.HasSaveLoaded())
        {
            return;
        }

        SyncSystemsToRuntimeState();

        SaveData save = ConvertRuntimeToSave(State);
        save.SlotId = GameRuntimeContext.Instance.CurrentSave.SlotId;
        save.SaveDisplayName = GameRuntimeContext.Instance.CurrentSave.SaveDisplayName;
        save.CreatedAtUtc = GameRuntimeContext.Instance.CurrentSave.CreatedAtUtc;
        save.UpdatedAtUtc = DateTime.UtcNow.ToString("o");

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
                CharacterAppearancePrompt = save.Profile.CharacterAppearancePrompt,
                PlayerCharacterImagePath = save.Profile.PlayerCharacterImagePath,
                Interests = save.Profile.Interests != null
                    ? new List<string>(save.Profile.Interests)
                    : new List<string>(),
                SpiritCharacterPrompt = save.Profile.SpiritCharacterPrompt,
                SpiritCharacterImagePath = save.Profile.SpiritCharacterImagePath
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
            PromptsUsedToday_Brainwash = save.PromptsUsedToday_Brainwash,
            MaxPromptsPerDay_Brainwash = save.MaxPromptsPerDay_Brainwash,
            PromptsUsedToday_Conscience = save.PromptsUsedToday_Conscience,
            MaxPromptsPerDay_Conscience = save.MaxPromptsPerDay_Conscience,
            IsGameOver = save.IsGameOver,
            good_ending_1 = save.good_ending_1,
            good_ending_2 = save.good_ending_2,
            bad_ending_1 = save.bad_ending_1,
            bad_ending_2 = save.bad_ending_2,
            LastExtractedRegret = save.LastExtractedRegret,
            CurrentDoctrineId = save.CurrentDoctrineId,
            CurrentTacticId = save.CurrentTacticId,
            Regrets = MapRegretsFromSave(save.Regrets),
            RecentDialogue = MapDialogueFromSave(save.RecentDialogue)
        };

        return state;
    }

    private SaveData ConvertRuntimeToSave(GameRunState state)
    {
        return new SaveData
        {
            Profile = new PlayerProfileData
            {
                Name = state.Profile.Name,
                CharacterAppearancePrompt = state.Profile.CharacterAppearancePrompt,
                PlayerCharacterImagePath = state.Profile.PlayerCharacterImagePath,
                Interests = state.Profile.Interests != null
                    ? new List<string>(state.Profile.Interests)
                    : new List<string>(),
                SpiritCharacterPrompt = state.Profile.SpiritCharacterPrompt,
                SpiritCharacterImagePath = state.Profile.SpiritCharacterImagePath
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
            PromptsUsedToday_Brainwash = state.PromptsUsedToday_Brainwash,
            MaxPromptsPerDay_Brainwash = state.MaxPromptsPerDay_Brainwash,
            PromptsUsedToday_Conscience = state.PromptsUsedToday_Conscience,
            MaxPromptsPerDay_Conscience = state.MaxPromptsPerDay_Conscience,
            IsGameOver = state.IsGameOver,
            good_ending_1 = state.good_ending_1,
            good_ending_2 = state.good_ending_2,
            bad_ending_1 = state.bad_ending_1,
            bad_ending_2 = state.bad_ending_2,
            LastExtractedRegret = state.LastExtractedRegret,
            CurrentDoctrineId = state.CurrentDoctrineId,
            CurrentTacticId = state.CurrentTacticId,
            Regrets = MapRegretsToSave(state.Regrets),
            RecentDialogue = MapDialogueToSave(state.RecentDialogue)
        };
    }

    private List<Regret> MapRegretsFromSave(List<RegretData> regrets)
    {
        var result = new List<Regret>();
        if (regrets == null) return result;

        foreach (var r in regrets)
        {
            result.Add(new Regret
            {
                Id = r.Id,
                Text = r.Text,
                Strength = r.Strength,
                TimesMentioned = r.TimesMentioned
            });
        }

        return result;
    }

    private List<RegretData> MapRegretsToSave(List<Regret> regrets)
    {
        var result = new List<RegretData>();
        if (regrets == null) return result;

        foreach (var r in regrets)
        {
            result.Add(new RegretData
            {
                Id = r.Id,
                Text = r.Text,
                Strength = r.Strength,
                TimesMentioned = r.TimesMentioned
            });
        }

        return result;
    }

    private List<DialogueTurn> MapDialogueFromSave(List<DialogueTurnData> dialogue)
    {
        var result = new List<DialogueTurn>();
        if (dialogue == null) return result;

        foreach (var d in dialogue)
        {
            result.Add(new DialogueTurn
            {
                Speaker = d.Speaker,
                Text = d.Text,
                Timestamp = d.Timestamp
            });
        }

        return result;
    }

    private List<DialogueTurnData> MapDialogueToSave(List<DialogueTurn> dialogue)
    {
        var result = new List<DialogueTurnData>();
        if (dialogue == null) return result;

        foreach (var d in dialogue)
        {
            result.Add(new DialogueTurnData
            {
                Speaker = d.Speaker,
                Text = d.Text,
                Timestamp = d.Timestamp
            });
        }

        return result;
    }
}