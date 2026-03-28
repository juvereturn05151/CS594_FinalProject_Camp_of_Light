using OpenAI.Samples.Chat;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntroPhaseController : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField ageInput;
    [SerializeField] private TMP_InputField professionInput;
    [SerializeField] private TMP_InputField interestsInput;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;

    [Header("UI Panels")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject nextPanel;

    [SerializeField] private CampOfLightChatBehaviour campOfLightChatBehaviour;

    [Header("Feedback")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text previewText;

    private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
    }

    private void Start()
    {
        if (nextPanel != null)
        {
            nextPanel.SetActive(false);
        }

        if (introPanel != null)
        {
            introPanel.SetActive(true);
        }

        SetFeedback(string.Empty);
        UpdatePreview(null);
    }

    private void OnConfirmClicked()
    {
        PlayerProfile profile = BuildProfileFromInput();

        if (!profile.IsValid())
        {
            SetFeedback("Please enter a valid Name, Age, and Profession.");
            return;
        }

        if (GameSessionHolder.Instance == null)
        {
            SetFeedback("GameSessionHolder is missing in the scene.");
            return;
        }

        GameSessionHolder.Instance.Session.Profile = profile;

        SetFeedback("Profile created successfully.");
        UpdatePreview(profile);

        Debug.Log("=== Player Profile Created ===");
        Debug.Log(profile.ToString());

        if (introPanel != null)
        {
            introPanel.SetActive(false);
        }

        if (nextPanel != null)
        {
            if (campOfLightChatBehaviour != null) 
            {
                campOfLightChatBehaviour.UpdateGameSession(GameSessionHolder.Instance.Session);
            }
            
            nextPanel.SetActive(true);
        }
    }

    private PlayerProfile BuildProfileFromInput()
    {
        string playerName = GetSafeText(nameInput);
        string profession = GetSafeText(professionInput);

        int age = ParseAge(GetSafeText(ageInput));
        List<string> interests = ParseInterests(GetSafeText(interestsInput));

        return new PlayerProfile(playerName, age, profession, interests);
    }

    private string GetSafeText(TMP_InputField field)
    {
        if (field == null || string.IsNullOrWhiteSpace(field.text))
        {
            return string.Empty;
        }

        return field.text.Trim();
    }

    private int ParseAge(string value)
    {
        if (int.TryParse(value, out int result))
        {
            return Mathf.Max(0, result);
        }

        return 0;
    }

    private List<string> ParseInterests(string raw)
    {
        List<string> result = new();

        if (string.IsNullOrWhiteSpace(raw))
        {
            return result;
        }

        string[] split = raw.Split(',');

        foreach (string item in split)
        {
            string trimmed = item.Trim();

            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                result.Add(trimmed);
            }
        }

        return result;
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }

    private void UpdatePreview(PlayerProfile profile)
    {
        if (previewText == null)
        {
            return;
        }

        if (profile == null)
        {
            previewText.text = "No profile created yet.";
            return;
        }

        previewText.text = profile.ToString();
    }

    [ContextMenu("Load Mock Intro Data")]
    public void LoadMockIntroData()
    {
        if (nameInput != null) nameInput.text = "Ju-ve";
        if (ageInput != null) ageInput.text = "28";
        if (professionInput != null) professionInput.text = "Game Developer";
        if (interestsInput != null) interestsInput.text = "Fighting Games, AI, Japanese";
    }
}