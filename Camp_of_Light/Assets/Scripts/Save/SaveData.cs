using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public string SlotId;
    public string SaveDisplayName;
    public string CreatedAtUtc;
    public string UpdatedAtUtc;

    public PlayerProfileData Profile = new();
    public CampaignData Campaign = new();
    public PlayerStatsData Stats = new();
    public SessionData Session = new();
    public List<RegretData> Regrets = new();
}

[Serializable]
public class PlayerProfileData
{
    public string Name;
    public int Age;
    public string Profession;
    public List<string> Interests = new();
}

[Serializable]
public class CampaignData
{
    public int CurrentDay = 1;
    public int MaxDays = 45;
    public int PromptsUsed = 0;
    public int MaxPromptsPerDay = 20;
    public bool IsGameOver = false;
    public bool Escaped = false;
}

[Serializable]
public class PlayerStatsData
{
    public int Confidence = 50;
    public int Brainwash = 0;
    public int Wokeness = 0;
}

[Serializable]
public class SessionData
{
    public string LastExtractedRegret = "";
    public string LastBibleVerse = "";
}

[Serializable]
public class RegretData
{
    public string Id;
    public string Text;
    public int Strength;
    public int TimesMentioned;
}

[Serializable]
public class SaveManifest
{
    public List<SaveSlotMeta> Slots = new();
}

[Serializable]
public class SaveSlotMeta
{
    public string SlotId;
    public string SaveDisplayName;
    public string UpdatedAtUtc;
}