using System;
using System.Collections.Generic;

[System.Serializable]
public class DayRange
{
    public int start;
    public int end;
}

[System.Serializable]
public class CultDoctrineEntry
{
    public string verse;
    public string text;
    public string translation;
    public string use_case;
    public List<string> tags;
    public DayRange day_range;
}

[System.Serializable]
public class CultTacticEntry
{
    public string id;
    public string title;
    public string description;
    public string example_line;
    public List<string> tags;
    public DayRange day_range;
}

[Serializable]
public class StatEffects
{
    public int confidence;
    public int brainwash;
    public int wokeness;
}