using OpenAI.Samples.Chat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CultProgressUI : MonoBehaviour
{
    [Header("Systems")]
    private CultGameDirector gameDirector;
    private RegretSystem regretSystem;

    [Header("Text UI")]
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text confidenceText;
    [SerializeField] private TMP_Text brainwashText;
    [SerializeField] private TMP_Text wokenessText;
    [SerializeField] private TMP_Text strongestRegretText;
    [SerializeField] private TMP_Text statusText;

    [Header("Progress Bars")]
    [SerializeField] private Slider confidenceSlider;
    [SerializeField] private Slider brainwashSlider;
    [SerializeField] private Slider wokenessSlider;
    [SerializeField] private Slider promptSlider;

    [Header("Bar Ranges")]
    [SerializeField] private int statMin = 0;
    [SerializeField] private int statMax = 100;

    private void Start()
    {
        ConfigureSliders();
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    public void Init()
    {
        gameDirector = GameSharedSystem.Instance.GameDirector;
        regretSystem = GameSharedSystem.Instance.RegretSystem;
        ConfigureSliders();
        Refresh();
    }

    private void ConfigureSliders()
    {
        if (confidenceSlider != null)
        {
            confidenceSlider.minValue = statMin;
            confidenceSlider.maxValue = statMax;
        }

        if (brainwashSlider != null)
        {
            brainwashSlider.minValue = statMin;
            brainwashSlider.maxValue = statMax;
        }

        if (wokenessSlider != null)
        {
            wokenessSlider.minValue = statMin;
            wokenessSlider.maxValue = statMax;
        }

        if (promptSlider != null)
        {
            promptSlider.minValue = 0;
            promptSlider.maxValue = 1; 
        }
    }

    public void Refresh()
    {
        if (GameManager.Instance == null || GameManager.Instance.State == null)
            return;

        if (gameDirector == null)
            return;

        PlayerStats stats = GameManager.Instance.State.Stats;
        if (stats == null)
            return;

        GameRunState state = GameManager.Instance.State;
        GamePhase phase = state.CurrentPhase;

        int promptsUsed = 0;
        int maxPrompts = 0;

        switch (phase)
        {
            case GamePhase.BrainwashingLesson:
                promptsUsed = state.PromptsUsedToday_Brainwash;
                maxPrompts = state.MaxPromptsPerDay_Brainwash;
                break;

            case GamePhase.ConscienceTalk:
                promptsUsed = state.PromptsUsedToday_Conscience;
                maxPrompts = state.MaxPromptsPerDay_Conscience;
                break;

            default:
                promptsUsed = 0;
                maxPrompts = 0;
                break;
        }

        if (dayText != null)
            dayText.text = $"Day: {state.CurrentDay}/{state.MaxDays}";

        if (promptText != null)
        {
            if (phase == GamePhase.BrainwashingLesson || phase == GamePhase.ConscienceTalk)
                promptText.text = $"Prompt Used: {promptsUsed}/{maxPrompts}";
            else
                promptText.text = "Prompts: -";
        }

        if (confidenceText != null)
            confidenceText.text = $"Confidence: {stats.Confidence}";

        if (brainwashText != null)
            brainwashText.text = $"Spirituality: {stats.Brainwash}";

        if (wokenessText != null)
            wokenessText.text = $"Skepticism: {stats.Wokeness}";

        if (confidenceSlider != null)
            confidenceSlider.value = stats.Confidence;

        if (brainwashSlider != null)
            brainwashSlider.value = stats.Brainwash;

        if (wokenessSlider != null)
            wokenessSlider.value = stats.Wokeness;

        if (promptSlider != null)
        {
            promptSlider.maxValue = Mathf.Max(1, maxPrompts);

            if (phase == GamePhase.BrainwashingLesson || phase == GamePhase.ConscienceTalk)
                promptSlider.value = promptsUsed;
            else
                promptSlider.value = 0;
        }

        if (strongestRegretText != null)
        {
            Regret strongest = regretSystem != null ? regretSystem.GetStrongestRecentRegret() : null;

            if (strongest == null)
            {
                strongestRegretText.text = "Strongest Regret: None";
            }
            else
            {
                strongestRegretText.text =
                    $"Strongest Regret: {strongest.Text} ({strongest.Strength})";
            }
        }

        if (statusText != null)
            statusText.text = BuildStatusText(stats, phase);
    }

    private string BuildStatusText(PlayerStats stats, GamePhase phase)
    {
        if (gameDirector.IsGameOver)
        {

            if (stats.Brainwash >= 100 && stats.Confidence <= 10)
                return "Status: Fully Brainwashed";

            return "Status: Trapped Forever";
        }

        switch (phase)
        {
            case GamePhase.WakeUp:
                return "Status: Waking Up";

            case GamePhase.PreachingLesson:
                return "Status: Listening";

            case GamePhase.BrainwashingLesson:
                if (stats.Brainwash >= 70)
                    return "Status: Under Pressure";
                return "Status: Brainwashing";

            case GamePhase.ConscienceTalk:
                if (stats.Wokeness >= 70)
                    return "Status: Self-Reflection";
                return "Status: Reflecting";

            case GamePhase.Sleep:
                return "Status: Resting";
        }

        if (stats.Wokeness >= 70)
            return "Status: Awakening";

        if (stats.Brainwash >= 70)
            return "Status: Deeply Indoctrinated";

        if (stats.Confidence <= 20)
            return "Status: Vulnerable";

        return "Status: Ongoing";
    }
}