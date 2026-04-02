using System;

[Serializable]
public class CultistResponse
{
    public bool IsPlayerResistingToCultOrBiBle;
    public string Player_Regret;
    public string CultistComment;

    public static CultistResponse Default()
    {
        return new CultistResponse
        {
            IsPlayerResistingToCultOrBiBle = false,
            Player_Regret = "",
            CultistComment = "I am listening. Tell me more about your life and what weighs on your heart."
        };
    }
}