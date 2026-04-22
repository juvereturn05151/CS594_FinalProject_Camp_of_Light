using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadGameMenuController : MonoBehaviour
{
    public enum MenuMode
    {
        LoadExistingGame,
        StartNewGame
    }

    [Header("UI")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject saveSlotButtonPrefab;
    [SerializeField] private GameObject rootPanel;

    [Header("Scenes")]
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [SerializeField] private string profileCreationSceneName = "ProfileCreation";

    [Header("Mode")]
    [SerializeField] private MenuMode mode = MenuMode.LoadExistingGame;

    private void Start()
    {
        if (rootPanel != null)
            rootPanel.SetActive(true);

        Populate();
    }

    private void Populate()
    {
        if (contentRoot == null)
        {
            Debug.LogError("[LoadGameMenuController] Content root is missing.");
            return;
        }

        if (saveSlotButtonPrefab == null)
        {
            Debug.LogError("[LoadGameMenuController] Save slot button prefab is missing.");
            return;
        }

        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        if (SaveManager.Instance == null)
        {
            Debug.LogError("[LoadGameMenuController] SaveManager not found.");
            return;
        }

        List<SaveSlotMeta> slots = SaveManager.Instance.GetAllSlots();

        foreach (SaveSlotMeta slot in slots)
        {
            GameObject go = Instantiate(saveSlotButtonPrefab, contentRoot);
            SaveSlotButtonUI ui = go.GetComponent<SaveSlotButtonUI>();

            if (ui == null)
            {
                Debug.LogError("[LoadGameMenuController] SaveSlotButtonUI component missing on prefab.");
                continue;
            }

            ui.Bind(slot, this);
        }
    }

    public void SetModeToNewGame()
    {
        mode = MenuMode.StartNewGame;

        if (rootPanel != null)
            rootPanel.SetActive(true);

        Populate();
    }

    public void SetModeToLoadGame()
    {
        mode = MenuMode.LoadExistingGame;

        if (rootPanel != null)
            rootPanel.SetActive(true);

        Populate();
    }

    public void OnSelectSlot(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId))
        {
            Debug.LogWarning("[LoadGameMenuController] Invalid slot id.");
            return;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogError("[LoadGameMenuController] SaveManager not found.");
            return;
        }

        if (GameRuntimeContext.Instance == null)
        {
            Debug.LogError("[LoadGameMenuController] GameRuntimeContext not found.");
            return;
        }

        bool hasData = SaveManager.Instance.SlotHasData(slotId);
        SaveData save = hasData ? SaveManager.Instance.Load(slotId) : null;

        if (mode == MenuMode.LoadExistingGame)
        {
            if (!hasData || save == null)
            {
                Debug.LogWarning($"[LoadGameMenuController] Slot '{slotId}' is empty. Cannot load.");
                return;
            }

            GameRuntimeContext.Instance.ClearPendingNewGameSlot();
            GameRuntimeContext.Instance.SetCurrentSave(save);
            GameRuntimeContext.Instance.SetCurrentRunState(ConvertSaveToRunState(save));

            if (GameUtility.FadingUIExists())
            {
                FadingUI.Instance.StartFadeIn();
                FadingUI.Instance.BindSceneToBeLoaded(gameplaySceneName);
            }
            else
            {
                SceneManager.LoadScene(gameplaySceneName);
            }
            return;
        }

        if (mode == MenuMode.StartNewGame)
        {
            if (hasData)
            {
                Debug.LogWarning($"[LoadGameMenuController] Slot '{slotId}' already has a save. Delete it first.");
                return;
            }

            GameRuntimeContext.Instance.Clear();
            GameRuntimeContext.Instance.SetPendingNewGameSlot(slotId);

            if (GameUtility.FadingUIExists())
            {
                FadingUI.Instance.StartFadeIn();
                FadingUI.Instance.BindSceneToBeLoaded(profileCreationSceneName);
            }
            else
            {
                SceneManager.LoadScene(profileCreationSceneName);
            }
        }
    }

    public void OnDeleteSlot(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId))
        {
            Debug.LogWarning("[LoadGameMenuController] Invalid slot id.");
            return;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogError("[LoadGameMenuController] SaveManager not found.");
            return;
        }

        SaveManager.Instance.DeleteSlot(slotId);
        Populate();
    }

    public void OnBackPressed()
    {
        gameObject.SetActive(false);
    }

    private GameRunState ConvertSaveToRunState(SaveData save)
    {
        GameRunState runState = new GameRunState
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
                Spirituality = save.Stats.Spirituality,
                Skepticism = save.Stats.Skepticism
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

    public bool IsNewGameMode()
    {
        return mode == MenuMode.StartNewGame;
    }

    public bool IsLoadGameMode()
    {
        return mode == MenuMode.LoadExistingGame;
    }
}