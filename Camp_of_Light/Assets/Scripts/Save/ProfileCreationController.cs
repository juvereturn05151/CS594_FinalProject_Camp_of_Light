using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProfileCreationController : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private GameObject pageName;
    [SerializeField] private GameObject pageAppearance;
    [SerializeField] private GameObject pageInterests;

    [Header("Page 1 - Name")]
    [SerializeField] private TMP_InputField nameInput;

    [Header("Page 2 - Appearance")]
    [SerializeField] private TMP_InputField appearancePromptInput;
    [SerializeField] private RawImage playerCharacterPreviewImage;
    [SerializeField] private Button generatePlayerCharacterButton;

    [Header("Page 3 - Interests")]
    [SerializeField] private TMP_InputField interest1Input;
    [SerializeField] private TMP_InputField interest2Input;
    [SerializeField] private TMP_InputField interest3Input;
    [SerializeField] private RawImage spiritPreviewImage;
    [SerializeField] private Button generateSpiritButton;

    [Header("Shared UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button finishButton;

    [Header("Systems")]
    [SerializeField] private CharacterSpriteGenerator characterSpriteGenerator;

    [Header("Scenes")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [SerializeField] private GameObject generatingCharacterEffectProgress;
    [SerializeField] private GameObject generatingSpiritEffectProgress;
    [SerializeField] private GameObject generatingSpiritEffectFinished;
    [SerializeField] private Transform fireworkSpawnPoint;

    private string generatedPlayerCharacterPath = "";
    private string generatedSpiritImagePath = "";
    private string generatedSpiritPrompt = "";

    private Texture2D generatedPlayerTexture;
    private Texture2D generatedSpiritTexture;

    private void Start()
    {
        SoundManager.Instance?.PlayMusic("ProfileCreation");
        ShowPage(0);
        RefreshButtons();
        SetStatus("");
    }

    public void OnNextFromNamePressed()
    {
        if (string.IsNullOrWhiteSpace(GetPlayerName()))
        {
            SetStatus("Please enter your name.");
            return;
        }

        ShowPage(1);
        SetStatus("");
    }

    public void OnBackToNamePressed()
    {
        ShowPage(0);
        SetStatus("");
    }

    public void OnGeneratePlayerCharacterPressed()
    {
        if (!ValidateCommonBeforeGeneration(out string slotId, out string playerName))
            return;

        string appearancePrompt = GetAppearancePrompt();
        if (string.IsNullOrWhiteSpace(appearancePrompt))
        {
            SetStatus("Please describe how you want the character to look.");
            return;
        }

        if (generatePlayerCharacterButton != null)
            generatePlayerCharacterButton.interactable = false;

        if (finishButton != null)
            finishButton.interactable = false;

        SetStatus("Generating player character...");

        generatingCharacterEffectProgress.SetActive(true);

        characterSpriteGenerator.GeneratePlayerCharacter(
            slotId,
            playerName,
            appearancePrompt,
            OnPlayerCharacterGenerated
        );
    }

    public void OnNextFromAppearancePressed()
    {
        if (string.IsNullOrWhiteSpace(generatedPlayerCharacterPath))
        {
            SetStatus("Please generate the player character first.");
            return;
        }

        ShowPage(2);
        SetStatus("");
    }

    public void OnBackToAppearancePressed()
    {
        ShowPage(1);
        SetStatus("");
    }

    public void OnGenerateSpiritPressed()
    {
        if (!ValidateCommonBeforeGeneration(out string slotId, out string playerName))
            return;

        string appearancePrompt = GetAppearancePrompt();
        if (string.IsNullOrWhiteSpace(appearancePrompt))
        {
            SetStatus("Please describe how you want the character to look.");
            return;
        }

        List<string> interests = GetThreeInterests();
        if (interests == null)
        {
            SetStatus("Please enter exactly 3 interests.");
            return;
        }

        if (generateSpiritButton != null)
            generateSpiritButton.interactable = false;

        if (finishButton != null)
            finishButton.interactable = false;

        SetStatus("Generating spirit character...");

        generatingSpiritEffectProgress.SetActive(true);
        SoundManager.Instance.PlayMusic("Requiem");

        characterSpriteGenerator.GenerateSpiritCharacter(
            slotId,
            playerName,
            appearancePrompt,
            interests,
            OnSpiritGenerated
        );
    }

    public void OnCreateProfilePressed()
    {
        if (SaveManager.Instance == null)
        {
            SetStatus("SaveManager not found.");
            return;
        }

        if (GameRuntimeContext.Instance == null)
        {
            SetStatus("GameRuntimeContext not found.");
            return;
        }

        if (!GameRuntimeContext.Instance.HasPendingNewGameSlot())
        {
            SetStatus("No empty slot selected.");
            return;
        }

        string slotId = GameRuntimeContext.Instance.PendingNewGameSlotId;
        SaveData existingSave = SaveManager.Instance.Load(slotId);

        if (existingSave != null)
        {
            SetStatus("This slot already contains a save. Delete it first.");
            return;
        }

        string playerName = GetPlayerName();
        string appearancePrompt = GetAppearancePrompt();
        List<string> interests = GetThreeInterests();

        if (string.IsNullOrWhiteSpace(playerName))
        {
            SetStatus("Please enter your name.");
            return;
        }

        if (string.IsNullOrWhiteSpace(appearancePrompt))
        {
            SetStatus("Please describe how you want the character to look.");
            return;
        }

        if (string.IsNullOrWhiteSpace(generatedPlayerCharacterPath))
        {
            SetStatus("Please generate the player character first.");
            return;
        }

        if (interests == null)
        {
            SetStatus("Please enter exactly 3 interests.");
            return;
        }

        if (string.IsNullOrWhiteSpace(generatedSpiritImagePath))
        {
            SetStatus("Please generate the spirit character first.");
            return;
        }

        string displayName = $"{playerName}'s Save";

        SaveData save = new SaveData
        {
            SlotId = slotId,
            SaveDisplayName = displayName,
            CreatedAtUtc = System.DateTime.UtcNow.ToString("o"),
            UpdatedAtUtc = System.DateTime.UtcNow.ToString("o"),

            Profile = new PlayerProfileData
            {
                Name = playerName,
                CharacterAppearancePrompt = appearancePrompt,
                PlayerCharacterImagePath = generatedPlayerCharacterPath,
                Interests = interests,
                SpiritCharacterPrompt = generatedSpiritPrompt,
                SpiritCharacterImagePath = generatedSpiritImagePath
            },

            Stats = new PlayerStatsData
            {
                Confidence = 50,
                Brainwash = 0,
                Wokeness = 0
            },

            CurrentDay = 1,
            MaxDays = 45,
            CurrentPhase = GamePhase.WakeUp,
            PromptsUsedToday_Brainwash = 0,
            MaxPromptsPerDay_Brainwash = 7,
            PromptsUsedToday_Conscience = 0,
            MaxPromptsPerDay_Conscience = 5,
            IsGameOver = false,
            good_ending_1 = false,
            good_ending_2 = false,
            bad_ending_1 = false,
            bad_ending_2 = false,

            LastExtractedRegret = "",
            LastBibleVerse = "",
            CurrentDoctrineId = "",
            CurrentTacticId = "",

            Regrets = new List<RegretData>(),
            RecentDialogue = new List<DialogueTurnData>()
        };

        SaveManager.Instance.Save(save);

        GameRuntimeContext.Instance.SetCurrentSave(save);
        GameRuntimeContext.Instance.SetCurrentRunState(ConvertSaveToRunState(save));
        GameRuntimeContext.Instance.ClearPendingNewGameSlot();

        SceneManager.LoadScene(gameplaySceneName);
    }

    private void OnPlayerCharacterGenerated(bool success, string imagePath, Texture2D texture, string promptUsed, string errorMessage)
    {
        if (generatePlayerCharacterButton != null)
            generatePlayerCharacterButton.interactable = true;

        if (!success)
        {
            generatedPlayerCharacterPath = "";
            generatedPlayerTexture = null;
            RefreshButtons();
            SetStatus(string.IsNullOrWhiteSpace(errorMessage) ? "Player character generation failed." : errorMessage);
            return;
        }

        generatedPlayerCharacterPath = imagePath;
        generatedPlayerTexture = texture;

        if (playerCharacterPreviewImage != null && generatedPlayerTexture != null)
        {
            playerCharacterPreviewImage.texture = generatedPlayerTexture;
            playerCharacterPreviewImage.color = Color.white;
        }

        generatingCharacterEffectProgress.SetActive(false);

        RefreshButtons();
        SetStatus("Player character generated. You can continue to the next page.");
    }

    private void OnSpiritGenerated(bool success, string imagePath, Texture2D texture, string promptUsed, string errorMessage)
    {
        if (generateSpiritButton != null)
            generateSpiritButton.interactable = true;

        if (!success)
        {
            generatedSpiritImagePath = "";
            generatedSpiritTexture = null;
            generatedSpiritPrompt = "";
            RefreshButtons();
            SetStatus(string.IsNullOrWhiteSpace(errorMessage) ? "Spirit generation failed." : errorMessage);
            return;
        }

        generatedSpiritImagePath = imagePath;
        generatedSpiritTexture = texture;
        generatedSpiritPrompt = promptUsed ?? "";

        if (spiritPreviewImage != null && generatedSpiritTexture != null)
        {
            spiritPreviewImage.texture = generatedSpiritTexture;
            spiritPreviewImage.color = Color.white;
        }



        generatingSpiritEffectProgress.SetActive(false);
        Instantiate(generatingSpiritEffectFinished, fireworkSpawnPoint.position, Quaternion.identity);
        generatingSpiritEffectFinished.SetActive(true);

        RefreshButtons();
        SetStatus("Spirit character generated. You can now save and continue.");
    }

    private bool ValidateCommonBeforeGeneration(out string slotId, out string playerName)
    {
        slotId = "";
        playerName = "";

        if (SaveManager.Instance == null)
        {
            SetStatus("SaveManager not found.");
            return false;
        }

        if (GameRuntimeContext.Instance == null)
        {
            SetStatus("GameRuntimeContext not found.");
            return false;
        }

        if (!GameRuntimeContext.Instance.HasPendingNewGameSlot())
        {
            SetStatus("No empty slot selected.");
            return false;
        }

        slotId = GameRuntimeContext.Instance.PendingNewGameSlotId;
        SaveData existingSave = SaveManager.Instance.Load(slotId);

        if (existingSave != null)
        {
            SetStatus("This slot already contains a save. Delete it first.");
            return false;
        }

        if (characterSpriteGenerator == null)
        {
            SetStatus("CharacterSpriteGenerator is not assigned.");
            return false;
        }

        if (characterSpriteGenerator.IsGenerating)
        {
            SetStatus("Generation already in progress.");
            return false;
        }

        playerName = GetPlayerName();
        if (string.IsNullOrWhiteSpace(playerName))
        {
            SetStatus("Please enter your name.");
            return false;
        }

        return true;
    }

    private void ShowPage(int pageIndex)
    {
        if (pageName != null)
            pageName.SetActive(pageIndex == 0);

        if (pageAppearance != null)
            pageAppearance.SetActive(pageIndex == 1);

        if (pageInterests != null)
            pageInterests.SetActive(pageIndex == 2);
    }

    private void RefreshButtons()
    {
        if (finishButton != null)
            finishButton.interactable =
                !string.IsNullOrWhiteSpace(generatedPlayerCharacterPath) &&
                !string.IsNullOrWhiteSpace(generatedSpiritImagePath);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private string GetPlayerName()
    {
        return nameInput != null ? nameInput.text.Trim() : "";
    }

    private string GetAppearancePrompt()
    {
        return appearancePromptInput != null ? appearancePromptInput.text.Trim() : "";
    }

    private List<string> GetThreeInterests()
    {
        string i1 = interest1Input != null ? interest1Input.text.Trim() : "";
        string i2 = interest2Input != null ? interest2Input.text.Trim() : "";
        string i3 = interest3Input != null ? interest3Input.text.Trim() : "";

        if (string.IsNullOrWhiteSpace(i1) ||
            string.IsNullOrWhiteSpace(i2) ||
            string.IsNullOrWhiteSpace(i3))
        {
            return null;
        }

        return new List<string> { i1, i2, i3 };
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
}