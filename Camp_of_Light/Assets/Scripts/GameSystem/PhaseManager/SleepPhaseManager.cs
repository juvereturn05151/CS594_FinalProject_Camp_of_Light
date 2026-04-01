using TMPro;
using UnityEngine;

public class SleepPhaseManager : BasePhaseManager
{
    [SerializeField] private GameObject sleepPanel;
    [SerializeField] private TMP_Text sleepText;
    [SerializeField] private GameObject cultProgressUI;

    public override GamePhase Phase => GamePhase.Sleep;

    public override void EnterPhase(GameRunState state)
    {
        ApplyOvernightEffects(state);

        SetActive(gameObject, true);
        SetActive(sleepPanel, true);
        SetActive(cultProgressUI, false);

        if (sleepText != null)
            sleepText.text = BuildSleepSummary(state);
    }

    public override void ExitPhase()
    {
        SetActive(sleepPanel, false);
        base.ExitPhase();
    }

    private void ApplyOvernightEffects(GameRunState state)
    {
        if (state == null)
            return;

        state.Stats.Confidence = Mathf.Clamp(state.Stats.Confidence + 2, 0, 100);
        state.Stats.Brainwash = Mathf.Clamp(state.Stats.Brainwash - 1, 0, 100);

        if (state.Regrets == null)
            return;

        foreach (var regret in state.Regrets)
        {
            regret.Strength = Mathf.Clamp(regret.Strength - 1, 0, 100);
        }
    }

    private string BuildSleepSummary(GameRunState state)
    {
        return $"Night falls on Day {state.CurrentDay}.\n\nYou lie down and carry the weight of the day into sleep.";
    }
}