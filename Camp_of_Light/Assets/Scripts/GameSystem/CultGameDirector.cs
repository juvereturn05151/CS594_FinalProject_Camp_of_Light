using UnityEngine;

[System.Serializable]
public class CultGameDirector : MonoBehaviour
{
    public int CurrentDay = 1;
    public int MaxDays = 45;

    public int PromptsUsed_Brainwash = 0;
    public int MaxPrompts_Brainwash = 10;
    public int PromptsUsed_Conscience = 0;
    public int MaxPrompts_Conscience = 5;

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
        PromptsUsed_Conscience++;

        if (PromptsUsed_Conscience >= MaxPrompts_Conscience)
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
        PromptsUsed_Conscience = MaxPrompts_Conscience;
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
}