using System;

[Serializable]
public class CultistResponse
{
    public bool IsRelevant;
    public bool IsPlayerResisting;
    public string PlayerStoryOrRegret;
    public string BibleVerse;
    public string CultistComment;
    public int ConfidenceDelta;
    public int BrainwashDelta;
    public int WokenessDelta;

    public static CultistResponse Default()
    {
        return new CultistResponse
        {
            IsRelevant = true,
            IsPlayerResisting = false,
            PlayerStoryOrRegret = "",
            BibleVerse = "",
            CultistComment = "I am listening. Tell me more about your life and what weighs on your heart.",
            ConfidenceDelta = 0,
            BrainwashDelta = 0,
            WokenessDelta = 0
        };
    }
}