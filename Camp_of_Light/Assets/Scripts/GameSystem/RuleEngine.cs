using UnityEngine;

public class RuleEngine : MonoBehaviour
{
    [SerializeField] private RegretSystem regretSystem;

    public void ApplyCultistRules(CultistResponse response, PlayerStats stats)
    {
        int confidenceDelta = 0;
        int brainwashDelta = 0;

        // RULE 1: Regret reduces confidence
        if (!string.IsNullOrWhiteSpace(response.Player_Regret))
        {
            regretSystem.AddOrUpdateRegret(response.Player_Regret);
        }

        if (response.IsPlayerResistingToCultOrBiBle)
        {
            confidenceDelta += 2;
            brainwashDelta -= 1;
        }
        else
        {
            brainwashDelta += 2;
            confidenceDelta -= 1;
        }

        stats.ApplyDelta(confidenceDelta, brainwashDelta, 0);
    }

    public void ApplyConscienceRules(ConscienceResponse response, PlayerStats stats)
    {
        int confidenceDelta = 0;
        int brainwashDelta = 0;
        int wokenessDelta = 0;

        if (response.IsFightingBack)
        {
            confidenceDelta += 2;
            wokenessDelta += 2;
            brainwashDelta -= 1;
        }

        if (response.IsSurrenderingToCult)
        {
            brainwashDelta += 2;
            confidenceDelta -= 2;
            wokenessDelta -= 1;
        }

        stats.ApplyDelta(confidenceDelta, brainwashDelta, wokenessDelta);
    }
}