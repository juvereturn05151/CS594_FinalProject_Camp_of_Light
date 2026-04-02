using System;

[Serializable]
public class CultistResponse
{
    public bool IsPlayerResistingToCultOrBiBle;
    public string PlayerStoryOrRegret;
    public string CultistComment;
    public int ConfidenceDelta;
    public int BrainwashDelta;

    public static CultistResponse Default()
    {
        return new CultistResponse
        {
            IsPlayerResistingToCultOrBiBle = false,
            PlayerStoryOrRegret = "",
            CultistComment = "I am listening. Tell me more about your life and what weighs on your heart.",
            ConfidenceDelta = 0,
            BrainwashDelta = 0
        };
    }
}