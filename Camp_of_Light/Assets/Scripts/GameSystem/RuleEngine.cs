using UnityEngine;

public class RuleEngine : MonoBehaviour
{
    [SerializeField] private RegretSystem regretSystem;
    [SerializeField] private StatusChangeFeedbackUI statusChangeFeedbackUI;

    public void ApplyCultistRules(CultistResponse response, PlayerStats stats)
    {
        int confidenceDelta = 0;
        int spritualityDelta = 0;
        int skepticismDelta = 0;

        if (response.IsPlayerJustBabbling)
        {
            if (response.IsPlayerResistingAgainstCultOrBiBle)
            {
                confidenceDelta += 3;
                spritualityDelta -= 2;
            }
            else 
            {
                confidenceDelta += 1;
                spritualityDelta -= 1;
            }
        }
        else 
        {
            if (response.IsPlayerResistingAgainstCultOrBiBle)
            {
                confidenceDelta += 2;
                spritualityDelta -= 2;
                skepticismDelta += 3;
            }
            else 
            {
                if (response.IsPlayerTellingTheirRegret)
                {
                    if (!string.IsNullOrWhiteSpace(response.Player_Regret))
                    {
                        regretSystem.AddOrUpdateRegret(response.Player_Regret);
                    }

                    spritualityDelta += 3;
                    confidenceDelta -= 2;
                } 
                
                if (response.IsPlayerBelievingInJesus) 
                {
                    spritualityDelta += 2;
                    confidenceDelta -= 1;
                }

                if (response.IsPlayeWantingToFindNewMember)
                {
                    spritualityDelta += 3;
                    confidenceDelta -= 1;
                }
            }
        }

        SoundManager.Instance.PlaySFX("GoodFeedback");
        statusChangeFeedbackUI.ShowFeedback(confidenceDelta, spritualityDelta, skepticismDelta);
        stats.ApplyDelta(confidenceDelta, spritualityDelta, skepticismDelta);
    }

    public void ApplyConscienceRules(ConscienceResponse response, PlayerStats stats)
    {
        int confidenceDelta = 0;
        int spiritualityDelta = 0;
        int skepticismDelta = 0;

        if (response.IsPlayerResistingToCultOrBiBle)
        {
            confidenceDelta += 2;
            skepticismDelta += 2;
            spiritualityDelta -= 2;
        }

        if (response.IsPlayerBelievingInThemselves) 
        {
            confidenceDelta += 2;
            skepticismDelta += 2;
            spiritualityDelta -= 1;
        }

        if (response.IsPlayerTellingTheirRegret)
        {
            spiritualityDelta += 2;
            confidenceDelta -= 2;
            skepticismDelta -= 1;
        }

        if (response.IsPlayerTalkingAboutTheirInterests)
        {
            confidenceDelta += 1;
            skepticismDelta += 2;
        }


        if (response.IsPlayerThinkingTheirGodIsNotFromCult)
        {
            spiritualityDelta += 2;
            confidenceDelta += 1;
            skepticismDelta += 4;
        }

        SoundManager.Instance.PlaySFX("GoodFeedback");
        statusChangeFeedbackUI.ShowFeedback(confidenceDelta, spiritualityDelta, skepticismDelta);
        stats.ApplyDelta(confidenceDelta, spiritualityDelta, skepticismDelta);
    }
}