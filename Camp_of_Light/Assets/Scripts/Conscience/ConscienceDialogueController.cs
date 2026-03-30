using System.Text;
using TMPro;
using UnityEngine;

public class ConscienceDialogueController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject consciencePanel;
    [SerializeField] private TMP_Text conscienceText;

    public void ShowReflection(GameRunState state, Regret strongestRegret)
    {
        if (consciencePanel != null)
            consciencePanel.SetActive(true);

        if (conscienceText != null)
            conscienceText.text = BuildReflection(state, strongestRegret);
    }

    public void Hide()
    {
        if (consciencePanel != null)
            consciencePanel.SetActive(false);
    }

    private string BuildReflection(GameRunState state, Regret strongestRegret)
    {
        StringBuilder sb = new();

        sb.AppendLine($"Day {state.CurrentDay}");
        sb.AppendLine();

        if (state.Stats.Wokeness >= 70)
        {
            sb.AppendLine("Something feels wrong. You can feel the pressure, but you can still think for yourself.");
        }
        else if (state.Stats.Brainwash >= 70)
        {
            sb.AppendLine("Their words keep echoing in your mind. It is getting harder to separate your thoughts from theirs.");
        }
        else if (state.Stats.Confidence <= 25)
        {
            sb.AppendLine("You feel shaken. The things they said today are still clinging to you.");
        }
        else
        {
            sb.AppendLine("You review the day quietly, trying to understand what is happening to you.");
        }

        sb.AppendLine();

        if (strongestRegret != null)
        {
            sb.AppendLine($"The regret that weighs on you most: \"{strongestRegret.Text}\"");
            sb.AppendLine($"Its current strength: {strongestRegret.Strength}");
            sb.AppendLine();
        }

        if (state.RecentDialogue != null && state.RecentDialogue.Count > 0)
        {
            DialogueTurn lastCultistLine = null;

            for (int i = state.RecentDialogue.Count - 1; i >= 0; i--)
            {
                if (state.RecentDialogue[i].Speaker == "Cultist")
                {
                    lastCultistLine = state.RecentDialogue[i];
                    break;
                }
            }

            if (lastCultistLine != null)
            {
                sb.AppendLine("One line keeps returning to you:");
                sb.AppendLine($"\"{lastCultistLine.Text}\"");
                sb.AppendLine();
            }
        }

        if (state.Stats.Wokeness > state.Stats.Brainwash)
        {
            sb.AppendLine("You still have a chance to hold onto yourself.");
        }
        else
        {
            sb.AppendLine("If you are not careful, tomorrow may be worse.");
        }

        return sb.ToString();
    }
}