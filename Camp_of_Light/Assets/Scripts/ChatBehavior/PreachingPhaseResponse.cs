using System;
using System.Collections.Generic;

[Serializable]
public class PreachingPhaseResponse
{
    public string Theme;
    public List<string> Lines;

    public static PreachingPhaseResponse Default()
    {
        return new PreachingPhaseResponse
        {
            Theme = "",
            Lines = new List<string>()
        };
    }
}