using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroSceneController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [Header("Page Roots")]
    [SerializeField] private GameObject page1Story;
    [SerializeField] private GameObject page2Arrival;
    [SerializeField] private GameObject page3HowToPlay;
    [SerializeField] private GameObject page4Ready;

    [Header("Navigation")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text pageIndicatorText;
    [SerializeField] private TMP_Text statusText;

    [Header("Page 1 - Story")]
    [SerializeField] private TMP_Text page1TitleText;
    [SerializeField] private TMP_Text page1BodyText;
    [SerializeField] private TypewriterText page1Typewriter;
    [SerializeField] private RawImage playerImage;
    [SerializeField] private RawImage spiritImage;

    [Header("Page 2 - Arrival")]
    [SerializeField] private TMP_Text page2TitleText;
    [SerializeField] private TMP_Text page2BodyText;
    [SerializeField] private TypewriterText page2Typewriter;
    [SerializeField] private RawImage arrivalImage;

    [Header("Page 3 - How To Play")]
    [SerializeField] private TMP_Text page3TitleText;
    [SerializeField] private TMP_Text page3BodyText;
    [SerializeField] private TypewriterText page3Typewriter;

    [Header("Page 4 - Ready")]
    [SerializeField] private TMP_Text page4TitleText;
    [SerializeField] private TMP_Text page4BodyText;
    [SerializeField] private TypewriterText page4Typewriter;

    [Header("Audio")]
    [SerializeField] private string introMusicName = "Requiem";

    private const int TotalPages = 4;

    private int currentPageIndex = 0;
    private GameRunState state;

    private void Start()
    {
        if (GameRuntimeContext.Instance == null || GameRuntimeContext.Instance.CurrentRunState == null)
        {
            SetStatus("GameRuntimeContext or run state not found.");
            return;
        }

        state = GameRuntimeContext.Instance.CurrentRunState;

        SoundManager.Instance?.PlayMusic(introMusicName);

        BuildAllPages();
        ShowPage(0);
        SetStatus("");
    }

    public void OnNextPressed()
    {
        SkipTypingForCurrentPage();

        if (currentPageIndex >= TotalPages - 1)
            return;

        ShowPage(currentPageIndex + 1);
    }

    public void OnBackPressed()
    {
        if (currentPageIndex <= 0)
            return;

        ShowPage(currentPageIndex - 1);
    }

    public void OnStartGamePressed()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    private void BuildAllPages()
    {
        BuildPage1Story();
        BuildPage2Arrival();
        BuildPage3HowToPlay();
        BuildPage4Ready();
    }

    private void BuildPage1Story()
    {
        if (page1TitleText != null)
            page1TitleText.text = "Why You Are Here";

        string playerName = SafeName();
        List<string> interests = SafeInterests();
        string interestsText = FormatInterests(interests);

        string story =
            $"{playerName}, you did not come here by accident.\n\n" +
            $"You spent your life reaching toward things that felt meaningful — {interestsText}. " +
            $"They gave you moments of comfort, excitement, and identity, but never enough to quiet the part of you that still felt lost.\n\n" +
            $"When that emptiness became too hard to ignore, a voice invited you to a place where answers were promised.\n\n" +
            $"That place is this camp.\n\n" +
            $"Now you stand at its gates, watched by people who seem to believe they already know what brought you here... and what you still have not escaped.";

        if (page1BodyText != null)
            page1BodyText.text = story;

        if (page1Typewriter != null)
            page1Typewriter.StartTyping(story);

        LoadTextureIntoRawImage(state.Profile.PlayerCharacterImagePath, playerImage);
        LoadTextureIntoRawImage(state.Profile.SpiritCharacterImagePath, spiritImage);
    }

    private void BuildPage2Arrival()
    {
        if (page2TitleText != null)
            page2TitleText.text = "Your Arrival";

        string playerName = SafeName();

        string arrivalText =
            $"{playerName} steps forward.\n\n" +
            $"A line of cultists turns toward the entrance.\n\n" +
            $"Clap.\n" +
            $"Clap.\n" +
            $"Clap.\n\n" +
            $"Hands strike together in perfect rhythm.\n\n" +
            $"No one looks surprised to see you.\n" +
            $"No one asks why you came.\n\n" +
            $"They smile as if you were expected.\n\n" +
            $"\"Welcome,\" one of them says.\n" +
            $"\"You are finally here.\"";

        if (page2BodyText != null)
            page2BodyText.text = arrivalText;
    }

    private void BuildPage3HowToPlay()
    {
        if (page3TitleText != null)
            page3TitleText.text = "How To Play";

        int maxDays = state != null ? state.MaxDays : 45;

        string howToPlay =
            $"Escape within {maxDays} days.\n\n" +
            $"To escape, you must reach:\n" +
            $"• 50 Spirituality Points\n" +
            $"• 50 Skepticism Points\n\n" +
            $"The key is balance.\n\n" +
            $"You must convince the cult that you are one of them...\n" +
            $"while making sure you do not become too brainwashed by them.\n\n" +
            $"Blend in.\n" +
            $"Survive the conversations.\n" +
            $"Do not lose yourself.";

        if (page3BodyText != null)
            page3BodyText.text = howToPlay;
    }

    private void BuildPage4Ready()
    {
        if (page4TitleText != null)
            page4TitleText.text = "Are You Ready?";

        string readyText =
            "Once you enter, the days will begin.\n\n" +
            "Every answer matters.\n" +
            "Every conversation can pull you closer to escape...\n" +
            "or deeper into the cult.\n\n" +
            "Press Begin when you are ready.";

        if (page4BodyText != null)
            page4BodyText.text = readyText;
    }

    private void ShowPage(int pageIndex)
    {
        currentPageIndex = Mathf.Clamp(pageIndex, 0, TotalPages - 1);

        if (page1Story != null) page1Story.SetActive(currentPageIndex == 0);
        if (page2Arrival != null) page2Arrival.SetActive(currentPageIndex == 1);
        if (page3HowToPlay != null) page3HowToPlay.SetActive(currentPageIndex == 2);
        if (page4Ready != null) page4Ready.SetActive(currentPageIndex == 3);

        RefreshNavigation();
        StartTypingForCurrentPage();
    }

    private void RefreshNavigation()
    {
        if (backButton != null)
            backButton.gameObject.SetActive(currentPageIndex > 0);

        if (nextButton != null)
            nextButton.gameObject.SetActive(currentPageIndex < TotalPages - 1);

        if (startButton != null)
            startButton.gameObject.SetActive(currentPageIndex == TotalPages - 1);

        if (pageIndicatorText != null)
            pageIndicatorText.text = $"Page {currentPageIndex + 1}/{TotalPages}";
    }

    private void StartTypingForCurrentPage()
    {
        switch (currentPageIndex)
        {
            case 0:
                if (page1Typewriter != null && page1BodyText != null)
                    page1Typewriter.StartTyping(page1BodyText.text);
                break;

            case 1:
                if (page2Typewriter != null && page2BodyText != null)
                    page2Typewriter.StartTyping(page2BodyText.text);
                break;

            case 2:
                if (page3Typewriter != null && page3BodyText != null)
                    page3Typewriter.StartTyping(page3BodyText.text);
                break;

            case 3:
                if (page4Typewriter != null && page4BodyText != null)
                    page4Typewriter.StartTyping(page4BodyText.text);
                break;
        }
    }

    private void SkipTypingForCurrentPage()
    {
        switch (currentPageIndex)
        {
            case 0:
                if (page1Typewriter != null) page1Typewriter.SkipTyping();
                break;
            case 1:
                if (page2Typewriter != null) page2Typewriter.SkipTyping();
                break;
            case 2:
                if (page3Typewriter != null) page3Typewriter.SkipTyping();
                break;
            case 3:
                if (page4Typewriter != null) page4Typewriter.SkipTyping();
                break;
        }
    }

    private void LoadTextureIntoRawImage(string path, RawImage target)
    {
        if (target == null)
            return;

        target.texture = null;
        target.color = new Color(1f, 1f, 1f, 0f);

        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!File.Exists(path))
            return;

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!texture.LoadImage(bytes))
            return;

        texture.name = Path.GetFileNameWithoutExtension(path);
        target.texture = texture;
        target.color = Color.white;
    }

    private string SafeName()
    {
        if (state == null || state.Profile == null || string.IsNullOrWhiteSpace(state.Profile.Name))
            return "The player";

        return state.Profile.Name.Trim();
    }

    private List<string> SafeInterests()
    {
        if (state == null || state.Profile == null || state.Profile.Interests == null)
            return new List<string>();

        return new List<string>(state.Profile.Interests);
    }

    private string FormatInterests(List<string> interests)
    {
        if (interests == null || interests.Count == 0)
            return "things that once felt meaningful";

        if (interests.Count == 1)
            return interests[0];

        if (interests.Count == 2)
            return $"{interests[0]} and {interests[1]}";

        return $"{interests[0]}, {interests[1]}, and {interests[2]}";
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}