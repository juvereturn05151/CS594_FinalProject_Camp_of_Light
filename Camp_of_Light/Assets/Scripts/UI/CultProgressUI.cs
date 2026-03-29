using OpenAI.Samples.Chat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CultProgressUI : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private CultGameDirector gameDirector;
    [SerializeField] private RegretSystem regretSystem;
    [SerializeField] private CampOfLightChatBehaviour chatBehaviour;

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

        if (promptSlider != null && gameDirector != null)
        {
            promptSlider.minValue = 0;
            promptSlider.maxValue = gameDirector.MaxPromptsPerDay;
        }
    }

    public void Refresh()
    {
        if (gameDirector == null || chatBehaviour == null)
            return;

        PlayerStats stats = chatBehaviour.GetStats();
        if (stats == null)
            return;

        if (dayText != null)
            dayText.text = $"Day: {gameDirector.CurrentDay}/{gameDirector.MaxDays}";

        if (promptText != null)
            promptText.text = $"Prompts: {gameDirector.PromptsUsed}/{gameDirector.MaxPromptsPerDay}";

        if (confidenceText != null)
            confidenceText.text = $"Confidence: {stats.Confidence}";

        if (brainwashText != null)
            brainwashText.text = $"Brainwash: {stats.Brainwash}";

        if (wokenessText != null)
            wokenessText.text = $"Wokeness: {stats.Wokeness}";

        if (confidenceSlider != null)
            confidenceSlider.value = stats.Confidence;

        if (brainwashSlider != null)
            brainwashSlider.value = stats.Brainwash;

        if (wokenessSlider != null)
            wokenessSlider.value = stats.Wokeness;

        if (promptSlider != null)
            promptSlider.value = gameDirector.PromptsUsed;

        if (strongestRegretText != null)
        {
            Regret strongest = regretSystem != null ? regretSystem.GetStrongestRegret() : null;

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
            statusText.text = BuildStatusText(stats);
    }

    private string BuildStatusText(PlayerStats stats)
    {
        if (gameDirector.IsGameOver)
        {
            if (gameDirector.Escaped)
                return "Status: Escaped";

            if (stats.Brainwash >= 100 && stats.Confidence <= 10)
                return "Status: Fully Brainwashed";

            return "Status: Trapped Forever";
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