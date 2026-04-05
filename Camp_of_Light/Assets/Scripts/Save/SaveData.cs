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
    public PlayerStatsData Stats = new();

    public int CurrentDay = 1;
    public int MaxDays = 45;

    public GamePhase CurrentPhase = GamePhase.WakeUp;

    public int PromptsUsedToday_Brainwash = 0;
    public int MaxPromptsPerDay_Brainwash = 7;

    public int PromptsUsedToday_Conscience = 0;
    public int MaxPromptsPerDay_Conscience = 4;

    public bool IsGameOver = false;
    public bool good_ending_1 = false;
    public bool good_ending_2 = false;
    public bool bad_ending_1 = false;
    public bool bad_ending_2 = false;

    public string LastExtractedRegret = "";
    public string LastBibleVerse = "";

    public string CurrentDoctrineId = "";
    public string CurrentTacticId = "";

    public List<RegretData> Regrets = new();
    public List<DialogueTurnData> RecentDialogue = new();
}

[Serializable]
public class PlayerProfileData
{
    public string Name;

    // Page 2
    public string CharacterAppearancePrompt;
    public string PlayerCharacterImagePath;

    // Page 3
    public List<string> Interests = new();
    public string SpiritCharacterPrompt;
    public string SpiritCharacterImagePath;
}

[Serializable]
public class PlayerStatsData
{
    public int Confidence = 50;
    public int Brainwash = 0;
    public int Wokeness = 0;
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
public class DialogueTurnData
{
    public string Speaker;
    public string Text;
    public string Timestamp;
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
    public bool HasData;
}