using System;
using System.Collections.Generic;

[Serializable]
public class CultDoctrineEntry
{
    public string id;
    public string reference;
    public string text;
    public List<string> tags;
    public string theme;
    public List<string> regret_types;
    public List<string> intent;
    public StatEffects stat_effects;
    public int priority;
    public List<string> phase;
    public List<string> tone;
    public string use_case;
}

[Serializable]
public class CultTacticEntry
{
    public string id;
    public string title;
    public string description;
    public List<string> tags;
    public List<string> trigger_conditions;
    public List<string> intent;
    public int priority;
    public List<string> phase;
    public List<string> tone;
    public string example_line;
}

[Serializable]
public class StatEffects
{
    public int confidence;
    public int brainwash;
    public int wokeness;
}