using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProfileCreationController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField ageInput;
    [SerializeField] private TMP_InputField professionInput;
    [SerializeField] private TMP_InputField interestsInput;

    [Header("Scenes")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    public void OnCreateProfilePressed()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[ProfileCreationController] SaveManager not found.");
            return;
        }

        if (GameRuntimeContext.Instance == null)
        {
            Debug.LogError("[ProfileCreationController] GameRuntimeContext not found.");
            return;
        }

        string playerName = nameInput != null ? nameInput.text.Trim() : "";
        int age = ParseInt(ageInput != null ? ageInput.text : "", 18);
        string profession = professionInput != null ? professionInput.text.Trim() : "";
        List<string> interests = ParseInterests(interestsInput != null ? interestsInput.text : "");

        string displayName = string.IsNullOrWhiteSpace(playerName)
            ? "New Save"
            : $"{playerName}'s Save";

        string slotId = SaveManager.Instance.CreateNewSlot(displayName);

        if (string.IsNullOrEmpty(slotId))
        {
            Debug.LogWarning("[ProfileCreationController] Could not create a new save slot.");
            return;
        }

        SaveData save = new SaveData
        {
            SlotId = slotId,
            SaveDisplayName = displayName,
            CreatedAtUtc = System.DateTime.UtcNow.ToString("o"),
            UpdatedAtUtc = System.DateTime.UtcNow.ToString("o"),

            Profile = new PlayerProfileData
            {
                Name = playerName,
                Age = age,
                Profession = profession,
                Interests = interests
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
            MaxPromptsPerDay_Brainwash = 10,
            PromptsUsedToday_Conscience = 0,
            MaxPromptsPerDay_Conscience = 5,
            IsGameOver = false,
            Escaped = false,

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

        SceneManager.LoadScene(gameplaySceneName);
    }

    private int ParseInt(string value, int fallback)
    {
        return int.TryParse(value, out int result) ? result : fallback;
    }

    private List<string> ParseInterests(string raw)
    {
        List<string> result = new();

        if (string.IsNullOrWhiteSpace(raw))
            return result;

        string[] parts = raw.Split(',');
        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
                result.Add(trimmed);
        }

        return result;
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
                    : new List<string>()
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
}