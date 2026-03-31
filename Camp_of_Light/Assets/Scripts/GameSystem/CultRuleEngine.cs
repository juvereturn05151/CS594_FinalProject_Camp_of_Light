using UnityEngine;

public class CultRuleEngine : MonoBehaviour
{
    [SerializeField] private RegretSystem regretSystem;

    public void ApplyCultistRules(CultistResponse response, PlayerStats stats)
    {
        int confidenceDelta = response.ConfidenceDelta;
        int brainwashDelta = response.BrainwashDelta;
        int wokenessDelta = response.WokenessDelta;

        // RULE 1: Regret reduces confidence
        if (!string.IsNullOrWhiteSpace(response.PlayerStoryOrRegret))
        {
            confidenceDelta -= 2;
            regretSystem.AddOrUpdateRegret(response.PlayerStoryOrRegret);
        }

        // RULE 2: Resistance increases wokeness
        if (response.IsPlayerResisting)
        {
            wokenessDelta += 2;
            brainwashDelta -= 1;
        }

        // RULE 3: Matching belief strengthens brainwash
        Regret strongest = regretSystem.GetStrongestRegret();

        if (strongest != null && !string.IsNullOrWhiteSpace(response.BibleVerse))
        {
            brainwashDelta += strongest.Strength / 20;
        }

        stats.ApplyDelta(confidenceDelta, brainwashDelta, wokenessDelta);
    }

    public void ApplyConscienceRules(ConscienceResponse response, PlayerStats stats)
    {
        int confidenceDelta = response.ConfidenceDelta;
        int brainwashDelta = response.BrainwashDelta;
        int wokenessDelta = response.WokenessDelta;

        // RULE 1: Fighting back restores the self
        if (response.IsFightingBack)
        {
            confidenceDelta += 2;
            wokenessDelta += 2;
            brainwashDelta -= 1;
        }

        // RULE 2: Recognizing manipulation weakens cult control
        if (response.RecognizesManipulation)
        {
            wokenessDelta += 3;
            brainwashDelta -= 2;
        }

        // RULE 3: Reclaiming self-worth restores confidence
        if (response.ReclaimsSelfWorth)
        {
            confidenceDelta += 3;
            brainwashDelta -= 1;
        }

        // RULE 4: Surrendering to cult influence is dangerous
        if (response.IsSurrenderingToCult)
        {
            confidenceDelta -= 2;
            brainwashDelta += 2;
            wokenessDelta -= 1;
        }

        // RULE 5: An anchor thought gives a small stabilizing effect
        if (!string.IsNullOrWhiteSpace(response.AnchorThought))
        {
            confidenceDelta += 1;
        }

        // RULE 6: Strong regret can be healed instead of exploited
        Regret strongest = regretSystem.GetStrongestRegret();
        if (strongest != null)
        {
            if (response.IsFightingBack || response.ReclaimsSelfWorth)
            {
                brainwashDelta -= Mathf.Max(1, strongest.Strength / 25);
            }
        }

        stats.ApplyDelta(confidenceDelta, brainwashDelta, wokenessDelta);
    }
}