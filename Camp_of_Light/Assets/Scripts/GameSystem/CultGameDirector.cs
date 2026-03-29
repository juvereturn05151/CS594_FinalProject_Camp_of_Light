using UnityEngine;

[System.Serializable]
public class CultGameDirector : MonoBehaviour
{
    public int CurrentDay = 1;
    public int MaxDays = 45;

    public int PromptsUsed = 0;
    public int MaxPromptsPerDay = 20;

    public bool IsGameOver = false;
    public bool Escaped = false;

    public void OnTurnFinished()
    {
        PromptsUsed++;

        if (PromptsUsed >= MaxPromptsPerDay)
        {
            EndDay();
        }
    }

    private void EndDay()
    {
        CurrentDay++;
        PromptsUsed = 0;

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