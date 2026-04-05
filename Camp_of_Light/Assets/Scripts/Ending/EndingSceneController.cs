using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingSceneController : MonoBehaviour
{
    private enum EndingType
    {
        None,
        GoodEnding1,
        GoodEnding2,
        BadEnding1,
        BadEnding2
    }

    [Header("Panels")]
    [SerializeField] private GameObject goodEnding1Panel;
    [SerializeField] private GameObject goodEnding2Panel;
    [SerializeField] private GameObject badEnding1Panel;
    [SerializeField] private GameObject badEnding2Panel;
    [SerializeField] private GameObject fallbackPanel;

    [Header("Ending Images - SpriteRenderer")]
    [SerializeField] private SpriteRenderer playerImage;
    [SerializeField] private SpriteRenderer spiritImage;

    [SerializeField] private SpriteRenderer playerImage2;
    [SerializeField] private SpriteRenderer spiritImage2;

    [SerializeField] private SpriteRenderer playerImage3;

    [SerializeField] private SpriteRenderer playerImage4;
    [SerializeField] private SpriteRenderer spiritImage4;

    [Header("Ending Story UI")]
    [SerializeField] private TMP_Text endingTitleText;
    [SerializeField] private TMP_Text endingStoryText;
    [SerializeField] private TypewriterText typewriterText;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Sprite Settings")]
    [SerializeField] private float pixelsPerUnit = 100f;

    private GameRunState cachedState;
    private EndingType currentEnding = EndingType.None;

    private void Start()
    {
        cachedState = GameManager.Instance.State;
        currentEnding = ShowCorrectEnding(cachedState);
        UpdateCharacterSprites(cachedState);
        ApplyEndingNarrative(cachedState, currentEnding);
    }

    private EndingType ShowCorrectEnding(GameRunState state)
    {
        SetAllPanelsInactive();

        if (state == null)
        {
            if (fallbackPanel != null)
                fallbackPanel.SetActive(true);

            return EndingType.None;
        }

        if (state.good_ending_2)
        {
            if (goodEnding2Panel != null)
                goodEnding2Panel.SetActive(true);

            return EndingType.GoodEnding2;
        }

        if (state.good_ending_1)
        {
            if (goodEnding1Panel != null)
                goodEnding1Panel.SetActive(true);

            return EndingType.GoodEnding1;
        }

        if (state.bad_ending_1)
        {
            if (badEnding1Panel != null)
                badEnding1Panel.SetActive(true);

            return EndingType.BadEnding1;
        }

        if (state.bad_ending_2)
        {
            if (badEnding2Panel != null)
                badEnding2Panel.SetActive(true);

            return EndingType.BadEnding2;
        }

        if (fallbackPanel != null)
            fallbackPanel.SetActive(true);

        return EndingType.None;
    }

    private void ApplyEndingNarrative(GameRunState state, EndingType endingType)
    {
        string playerName = GetPlayerName(state);

        string title = "";
        string story = "";

        switch (endingType)
        {
            case EndingType.GoodEnding1:
                title = "Good Ending 1 - Quiet Escape";
                story =
                    $"{playerName} learned how to survive by wearing the right mask.\n\n" +
                    $"The cult believed the performance. Every nod, every careful answer, every moment of false submission helped build that illusion. " +
                    $"{playerName} looked obedient enough to avoid suspicion, but inside, doubt never disappeared.\n\n" +
                    $"By the end, that hidden skepticism became the key to escape. " +
                    $"{playerName} slipped away not by winning openly, but by enduring long enough to fool the people who wanted control.\n\n" +
                    $"The scars remain, but so does the truth: sometimes survival begins with pretending until the door finally opens.";
                break;

            case EndingType.GoodEnding2:
                title = "Good Ending 2 - Open Defiance";
                story =
                    $"{playerName} finally saw through the shape of the trap.\n\n" +
                    $"The words of God were being twisted into weapons. Fear was called truth. Submission was called salvation. " +
                    $"What once sounded holy now revealed itself as manipulation.\n\n" +
                    $"Instead of shrinking away, {playerName} chose to confront them. " +
                    $"That courage did not erase the danger, but it broke the spell. The cult could no longer define what was real.\n\n" +
                    $"By standing firm, {playerName} escaped with more than freedom. " +
                    $"{playerName} escaped with clarity, and with the strength to name the abuse for what it was.";
                break;

            case EndingType.BadEnding1:
                title = "Bad Ending 1 - Fed to the Dead";
                story =
                    $"{playerName}'s confidence became impossible for the cult to ignore.\n\n" +
                    $"There was too much resistance in the eyes, too much self left in every answer. " +
                    $"The cultists stopped seeing a recruit and started seeing a threat.\n\n" +
                    $"Punishment came without mercy. Dragged away as an example, {playerName} was offered to the hungry dead the cult kept close at hand. " +
                    $"The lesson was clear: those who would not bend would be broken.\n\n" +
                    $"The camp moved on, but the night remembered. " +
                    $"{playerName}'s defiance remained real, even if it ended in horror.";
                break;

            case EndingType.BadEnding2:
                title = "Bad Ending 2 - One With the Cult";
                story =
                    $"{playerName} stayed too long beneath their voices.\n\n" +
                    $"Little by little, fear replaced doubt. Repetition replaced thought. " +
                    $"The parts of the self that once questioned, resisted, and remembered began to fade.\n\n" +
                    $"By the end, escape no longer felt necessary. The cult's language had become the language of the mind itself. " +
                    $"{playerName} no longer stood apart from them.\n\n" +
                    $"The body still lived, but the person who first arrived here was gone. " +
                    $"What remained was loyalty, obedience, and an empty peace that belonged to the cult.";
                break;

            default:
                title = "Unknown Ending";
                story = $"{playerName}'s story has ended, but the final path could not be determined.";
                break;
        }

        if (endingTitleText != null)
            endingTitleText.text = title;

        if (typewriterText != null)
        {
            typewriterText.StartTyping(story);
        }
        else if (endingStoryText != null)
        {
            endingStoryText.text = story;
        }
    }

    private void UpdateCharacterSprites(GameRunState state)
    {
        if (state == null || state.Profile == null)
            return;

        Sprite playerSprite = LoadSpriteFromPath(state.Profile.PlayerCharacterImagePath);
        Sprite spiritSprite = LoadSpriteFromPath(state.Profile.SpiritCharacterImagePath);

        if (state.good_ending_2)
        {
            SetSprite(playerImage2, playerSprite);
            SetSprite(spiritImage2, spiritSprite);
        }

        if (state.good_ending_1)
        {
            SetSprite(playerImage, playerSprite);
            SetSprite(spiritImage, spiritSprite);
        }

        if (state.bad_ending_1)
        {
            SetSprite(playerImage3, playerSprite);
        }

        if (state.bad_ending_2)
        {
            SetSprite(playerImage4, playerSprite);
            SetSprite(spiritImage4, spiritSprite);
        }
    }

    private void SetSprite(SpriteRenderer targetRenderer, Sprite sprite)
    {
        if (targetRenderer == null)
            return;

        targetRenderer.sprite = sprite;

        if (sprite != null)
            targetRenderer.color = Color.white;
    }

    private void SetAllPanelsInactive()
    {
        if (goodEnding1Panel != null) goodEnding1Panel.SetActive(false);
        if (goodEnding2Panel != null) goodEnding2Panel.SetActive(false);
        if (badEnding1Panel != null) badEnding1Panel.SetActive(false);
        if (badEnding2Panel != null) badEnding2Panel.SetActive(false);
        if (fallbackPanel != null) fallbackPanel.SetActive(false);
    }

    public void OnBackToMainMenuPressed()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private GameRunState GetRuntimeState()
    {
        GameRunState state = null;

        if (GameRuntimeContext.Instance != null)
            state = GameRuntimeContext.Instance.CurrentRunState;

        if (state != null)
            return state;

        if (GameRuntimeContext.Instance != null && GameRuntimeContext.Instance.CurrentSave != null)
        {
            SaveData save = GameRuntimeContext.Instance.CurrentSave;

            return new GameRunState
            {
                CurrentDay = save.CurrentDay,
                MaxDays = save.MaxDays,
                IsGameOver = save.IsGameOver,
                good_ending_1 = save.good_ending_1,
                good_ending_2 = save.good_ending_2,
                bad_ending_1 = save.bad_ending_1,
                bad_ending_2 = save.bad_ending_2,
                Profile = new PlayerProfile
                {
                    Name = save.Profile != null ? save.Profile.Name : "",
                    PlayerCharacterImagePath = save.Profile != null ? save.Profile.PlayerCharacterImagePath : "",
                    SpiritCharacterImagePath = save.Profile != null ? save.Profile.SpiritCharacterImagePath : ""
                }
            };
        }

        return null;
    }

    private string GetPlayerName(GameRunState state)
    {
        if (state == null || state.Profile == null || string.IsNullOrWhiteSpace(state.Profile.Name))
            return "The player";

        return state.Profile.Name;
    }

    private Sprite LoadSpriteFromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            return null;

        byte[] bytes = System.IO.File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!texture.LoadImage(bytes))
            return null;

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Rect rect = new Rect(0, 0, texture.width, texture.height);
        Vector2 pivot = new Vector2(0.5f, 0.5f);

        return Sprite.Create(texture, rect, pivot, pixelsPerUnit);
    }
}