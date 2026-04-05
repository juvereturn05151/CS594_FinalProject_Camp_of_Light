using System;

[Serializable]
public class ConscienceResponse
{
    public bool IsPlayerTellingTheirRegret = false;
    public bool IsPlayerResistingToCultOrBiBle = false;
    public bool IsPlayerBelievingInThemselves = false;
    public bool IsPlayerTalkingAboutTheirInterests = false;
    public bool IsPlayerThinkingTheirGodIsNotFromCult =  false;
    public string ConscienceComment = "";

    public static ConscienceResponse Default()
    {
        return new ConscienceResponse
        {
            IsPlayerTellingTheirRegret = false,
            IsPlayerResistingToCultOrBiBle = false,
            IsPlayerBelievingInThemselves = false,
            IsPlayerTalkingAboutTheirInterests = false,
            IsPlayerThinkingTheirGodIsNotFromCult =  false,
            ConscienceComment = "Take a breath. What are you really feeling right now?",
        };
    }
}