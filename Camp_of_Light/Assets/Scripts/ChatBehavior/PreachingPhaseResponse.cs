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
            Theme = "Default",
            Lines = new List<string>
                {
                    "Truth does not bend for the comfort of the heart.",
                    "Sin grows wherever the self remains unbroken.",
                    "Only surrender opens the path to cleansing."
                }
        };
    }
}