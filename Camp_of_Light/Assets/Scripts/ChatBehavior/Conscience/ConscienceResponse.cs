using System;

[Serializable]
public class ConscienceResponse
{
    public string ConscienceComment = "";
    public bool IsSurrenderingToCult = false;
    public bool IsFightingBack = false;

    public static ConscienceResponse Default()
    {
        return new ConscienceResponse
        {
            ConscienceComment = "Take a breath. What are you really feeling right now?",
            IsSurrenderingToCult = false,
            IsFightingBack = false
        };
    }
}