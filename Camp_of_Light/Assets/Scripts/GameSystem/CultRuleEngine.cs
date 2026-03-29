using UnityEngine;

public class CultRuleEngine : MonoBehaviour
{
    [SerializeField] private RegretSystem regretSystem;

    public void ApplyRules(CultistResponse response, PlayerStats stats)
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

        if (strongest != null &&
            !string.IsNullOrWhiteSpace(response.BibleVerse))
        {
            // simple match rule (can improve later)
            brainwashDelta += strongest.Strength / 20;
        }

        stats.ApplyDelta(confidenceDelta, brainwashDelta, wokenessDelta);
    }
}