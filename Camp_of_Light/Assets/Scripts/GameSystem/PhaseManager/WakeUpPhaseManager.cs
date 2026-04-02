using TMPro;
using UnityEngine;

public class WakeUpPhaseManager : BasePhaseManager
{
    [SerializeField] private GameObject wakeUpPanel;
    [SerializeField] private TMP_Text wakeUpText;
    [SerializeField] private TypewriterText typewriterText;

    public override GamePhase Phase => GamePhase.WakeUp;

    public override void EnterPhase(GameRunState state)
    {
        SetActive(gameObject, true);
        SetActive(wakeUpPanel, true);

        if (wakeUpText != null) 
        {
            if (typewriterText != null) 
            {
                typewriterText.StartTyping(BuildWakeUpSummary(state));
            }
        }

        SoundManager.Instance.PlayMusic("MorningSound");
    }

    public override void ExitPhase()
    {
        SetActive(wakeUpPanel, false);
        SoundManager.Instance.StopMusic();

        base.ExitPhase();
    }

    private string BuildWakeUpSummary(GameRunState state)
    {
        string status;

        if (state.Stats.Wokeness >= 70)
            status = "You wake with growing clarity.";
        else if (state.Stats.Brainwash >= 70)
            status = "You wake with their doctrine still crowding your mind.";
        else if (state.Stats.Confidence <= 25)
            status = "You wake feeling fragile and uncertain.";
        else
            status = "You wake to another day inside the expedition.";

        return $"Day {state.CurrentDay}\n\n{status}";
    }
}