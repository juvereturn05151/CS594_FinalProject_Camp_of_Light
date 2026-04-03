using UnityEngine;

public class RuleEngine : MonoBehaviour
{
    [SerializeField] private RegretSystem regretSystem;
    [SerializeField] private StatusChangeFeedbackUI statusChangeFeedbackUI;

    public void ApplyCultistRules(CultistResponse response, PlayerStats stats)
    {
        int confidenceDelta = 0;
        int brainwashDelta = 0;

        if (response.IsPlayerJustBabbling)
        {
            if (response.IsPlayerResistingAgainstCultOrBiBle)
            {
                confidenceDelta += 3;
                brainwashDelta -= 2;
            }
            else 
            {
                confidenceDelta += 1;
                brainwashDelta -= 1;
            }
        }
        else 
        {
            if (response.IsPlayerResistingAgainstCultOrBiBle)
            {
                confidenceDelta += 3;
                brainwashDelta -= 2;
            }
            else 
            {
                if (response.IsPlayerTellingTheirRegret)
                {
                    if (!string.IsNullOrWhiteSpace(response.Player_Regret))
                    {
                        regretSystem.AddOrUpdateRegret(response.Player_Regret);
                    }

                    brainwashDelta += 3;
                    confidenceDelta -= 2;
                } 
                
                if (response.IsPlayerBelievingInJesus) 
                {
                    brainwashDelta += 2;
                    confidenceDelta -= 1;
                }

                if (response.IsPlayeWantingToFindNewMember)
                {
                    brainwashDelta += 3;
                    confidenceDelta -= 1;
                }
            }
        }

        SoundManager.Instance.PlaySFX("GoodFeedback");
        statusChangeFeedbackUI.ShowFeedback(confidenceDelta, brainwashDelta, 0);
        stats.ApplyDelta(confidenceDelta, brainwashDelta, 0);
    }

    public void ApplyConscienceRules(ConscienceResponse response, PlayerStats stats)
    {
        int confidenceDelta = 0;
        int brainwashDelta = 0;
        int wokenessDelta = 0;

        if (response.IsPlayerResistingToCultOrBiBle || response.IsPlayerBelievingInThemselves)
        {
            confidenceDelta += 2;
            wokenessDelta += 2;
            brainwashDelta -= 1;
        }

        if (response.IsPlayerTellingTheirRegret)
        {
            brainwashDelta += 2;
            confidenceDelta -= 2;
            wokenessDelta -= 1;
        }

        SoundManager.Instance.PlaySFX("GoodFeedback");
        statusChangeFeedbackUI.ShowFeedback(confidenceDelta, brainwashDelta, wokenessDelta);
        stats.ApplyDelta(confidenceDelta, brainwashDelta, wokenessDelta);
    }
}