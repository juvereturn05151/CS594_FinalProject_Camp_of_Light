using System;

[Serializable]
public class ConscienceResponse
{
    public bool IsRelevant = true;

    public string ReflectedFeeling = "";
    public string AnchorThought = "";
    public string ConscienceComment = "";

    public bool IsSurrenderingToCult = false;
    public bool IsFightingBack = false;
    public bool RecognizesManipulation = false;
    public bool ReclaimsSelfWorth = false;

    public int ConfidenceDelta = 0;
    public int BrainwashDelta = 0;
    public int WokenessDelta = 0;

    public static ConscienceResponse Default()
    {
        return new ConscienceResponse
        {
            IsRelevant = true,
            ReflectedFeeling = "",
            AnchorThought = "Pause. Their fear does not define you.",
            ConscienceComment = "Take a breath. What are you really feeling right now?",
            IsSurrenderingToCult = false,
            IsFightingBack = false,
            RecognizesManipulation = false,
            ReclaimsSelfWorth = false,
            ConfidenceDelta = 0,
            BrainwashDelta = 0,
            WokenessDelta = 0
        };
    }
}