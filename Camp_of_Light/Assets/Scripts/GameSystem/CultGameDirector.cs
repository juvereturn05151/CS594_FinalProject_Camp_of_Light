using UnityEngine;

[System.Serializable]
public class CultGameDirector : MonoBehaviour
{
    public int CurrentDay = 1;
    public int MaxDays = 45;

    public int PromptsUsed_Brainwash = 0;
    public int MaxPrompts_Brainwash = 10;
    public int PromptsUsedToday_Conscience = 0;
    public int MaxPromptsPerDay_Conscience = 5;

    public bool IsGameOver = false;
    public bool IsBrainwashedOver = false;
    public bool IsConscienceOver = false;
    public bool Escaped = false;

    public bool OnTurnFinished_Brainwash()
    {
        PromptsUsed_Brainwash++;

        if (PromptsUsed_Brainwash >= MaxPrompts_Brainwash)
        {
            EndBrianwash();
            return true;
        }
        else 
        {
            return false;
        }
    }

    public void OnHack_Brainwash()
    {
        PromptsUsed_Brainwash = MaxPrompts_Brainwash;
        EndBrianwash();
    }

    public bool OnTurnFinished_Conscience()
    {
        PromptsUsedToday_Conscience++;

        if (PromptsUsedToday_Conscience >= MaxPromptsPerDay_Conscience)
        {
            EndConscience();
            return true;
        }
        else 
        {
            return false;
        }
    }

    public void OnHack_Conscience()
    {
        PromptsUsedToday_Conscience = MaxPromptsPerDay_Conscience;
        EndConscience();
    }

    private void EndBrianwash() 
    {
        IsBrainwashedOver = true;
    }

    private void EndConscience()
    {
        IsConscienceOver = true;
    }

    private void EndDay()
    {
        CurrentDay++;
        PromptsUsed_Brainwash = 0;
        PromptsUsedToday_Conscience = 0;
        IsBrainwashedOver = false;
        IsConscienceOver = false;

        Debug.Log($"Day advanced to {CurrentDay}");

        if (CurrentDay > MaxDays)
        {
            IsGameOver = true;
            Debug.Log("Player is trapped forever.");
        }
    }

    public void CheckWinCondition(PlayerStats stats)
    {
        if (stats.Wokeness >= 80 && stats.Brainwash <= 30)
        {
            Escaped = true;
            IsGameOver = true;
            Debug.Log("Player escaped!");
        }

        if (stats.Brainwash >= 100 && stats.Confidence <= 10)
        {
            IsGameOver = true;
            Debug.Log("Player fully brainwashed.");
        }
    }
}