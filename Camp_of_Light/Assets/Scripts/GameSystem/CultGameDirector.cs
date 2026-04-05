using UnityEngine;

[System.Serializable]
public class CultGameDirector : MonoBehaviour
{
    public int CurrentDay = 1;
    public int MaxDays = 45;

    public int PromptsUsed_Brainwash = 0;
    public int MaxPrompts_Brainwash = 7;
    public int PromptsUsed_Conscience = 0;
    public int MaxPrompts_Conscience = 5;

    public bool IsGameOver = false;
    public bool IsBrainwashedOver = false;
    public bool IsConscienceOver = false;
    public bool good_ending_1 = true;
    public bool good_ending_2 = true;
    public bool bad_ending_1 = true;
    public bool bad_ending_2 = true;

    public void UpdateCultGameDirector(GameRunState state)
    {
        CurrentDay = state.CurrentDay;
        IsGameOver = state.IsGameOver;
        good_ending_1 = state.good_ending_1;
        good_ending_2 = state.good_ending_2;
        bad_ending_1 = state.bad_ending_1;
        bad_ending_2 = state.bad_ending_2;
    }

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