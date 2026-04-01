using System;

[Serializable]
public class GameSession
{
    public PlayerProfile Profile = new();
    public PlayerStats Stats = new();

    public string LastExtractedRegret = string.Empty;
    public string LastBibleVerse = string.Empty;
}