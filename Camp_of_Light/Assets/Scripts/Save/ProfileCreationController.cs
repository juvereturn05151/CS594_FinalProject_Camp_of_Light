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
        SaveData save = new SaveData();

        save.SaveDisplayName = string.IsNullOrWhiteSpace(nameInput.text)
            ? $"{nameInput.text}'s Save"
            : nameInput.text;

        save.Profile.Name = nameInput.text.Trim();
        save.Profile.Age = ParseInt(ageInput.text, 18);
        save.Profile.Profession = professionInput.text.Trim();
        save.Profile.Interests = ParseInterests(interestsInput.text);

        save.Campaign = new CampaignData();
        save.Stats = new PlayerStatsData();
        save.Session = new SessionData();

        string slotId = SaveManager.Instance.CreateNewSlot(save.SaveDisplayName);
        save.SlotId = slotId;
        save.CreatedAtUtc = System.DateTime.UtcNow.ToString("o");
        save.UpdatedAtUtc = save.CreatedAtUtc;

        SaveManager.Instance.Save(save);
        GameStateRuntime.Instance.SetCurrentSave(save);

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
}