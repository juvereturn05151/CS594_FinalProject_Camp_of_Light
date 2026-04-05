using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameRunState
{
    public PlayerProfile Profile = new();
    public PlayerStats Stats = new();

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

    public string CurrentDoctrineId = "";
    public string CurrentTacticId = "";

    public List<Regret> Regrets = new();
    public List<DialogueTurn> RecentDialogue = new();

    public void ResetForNewDay()
    {
        PromptsUsedToday_Brainwash = 0;
        PromptsUsedToday_Conscience = 0;
        CurrentPhase = GamePhase.WakeUp;
    }

    public void ClearEndingFlags()
    {
        good_ending_1 = false;
        good_ending_2 = false;
        bad_ending_1 = false;
        bad_ending_2 = false;
    }

    public void AddDialogue(string speaker, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        RecentDialogue.Add(new DialogueTurn
        {
            Speaker = speaker,
            Text = text,
            Timestamp = DateTime.UtcNow.ToString("o")
        });

        if (RecentDialogue.Count > 30)
        {
            RecentDialogue.RemoveAt(0);
        }
    }
}

[Serializable]
public class DialogueTurn
{
    public string Speaker;
    public string Text;
    public string Timestamp;
}