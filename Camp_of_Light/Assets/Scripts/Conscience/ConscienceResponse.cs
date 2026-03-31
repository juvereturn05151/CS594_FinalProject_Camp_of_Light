using System;

[Serializable]
public class ConscienceResponse
{
    public bool IsRelevant = true;
    public string ReflectedFeeling = "";
    public string RegretFocus = "";
    public string ConscienceComment = "";
    public int ConfidenceDelta = 0;
    public int BrainwashDelta = 0;
    public int WokenessDelta = 0;

    public static ConscienceResponse Default()
    {
        return new ConscienceResponse
        {
            IsRelevant = true,
            ReflectedFeeling = "",
            RegretFocus = "",
            ConscienceComment = "Take a breath. What are you really feeling right now?",
            ConfidenceDelta = 0,
            BrainwashDelta = 0,
            WokenessDelta = 0
        };
    }
}