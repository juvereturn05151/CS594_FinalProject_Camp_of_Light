using TMPro;
using UnityEngine;

public class SleepPhaseManager : BasePhaseManager
{
    [SerializeField] private GameObject sleepPanel;
    [SerializeField] private TMP_Text sleepText;
    [SerializeField] private GameObject cultProgressUI;
    [SerializeField] private GameObject nextday_button;

    public override GamePhase Phase => GamePhase.Sleep;

    public override void EnterPhase(GameRunState state)
    {
        ApplyOvernightEffects(state);

        SetActive(gameObject, true);
        SetActive(sleepPanel, true);
        SetActive(cultProgressUI, false);
    }

    public override void ExitPhase()
    {
        SetActive(sleepPanel, false);
        base.ExitPhase();
    }

    private void ApplyOvernightEffects(GameRunState state)
    {
        if (state == null)
            return;

        EvaluateEndState(state);
        nextday_button.SetActive(!state.IsGameOver);

        if (sleepText != null)
            sleepText.text = BuildSleepSummary(state);

        GameManager.Instance.SaveCheckpoint();
    }

    private void EvaluateEndState(GameRunState state)
    {
        // FAIL: ran out of days
        if (state.CurrentDay > state.MaxDays)
        {
            state.IsGameOver = true;
            state.Escaped = false;
            return;
        }

        // PASS: reached awakening + brainwash threshold
        if (state.Stats.Wokeness >= 50.0f && state.Stats.Brainwash > 50.0f)
        {
            state.IsGameOver = true;
            state.Escaped = true;
            return;
        }

        state.IsGameOver = false;
    }

    private string BuildSleepSummary(GameRunState state)
    {
        if (state.IsGameOver)
        {
            if (state.Escaped)
                return BuildPassSummary(state);

            return BuildFailSummary(state);
        }

        return BuildNormalSleepSummary(state);
    }

    private string BuildNormalSleepSummary(GameRunState state)
    {
        int day = state.CurrentDay;

        float confidence = state.Stats.Confidence;
        float brainwash = state.Stats.Brainwash;
        float wokeness = state.Stats.Wokeness;

        string regret = GetStrongestRegretText();
        if (string.IsNullOrWhiteSpace(regret))
            regret = "something you still cannot fully name";

        string intro = $"Night falls on Day {day}.";

        string regretLine = $"As you lie down, your mind drifts back to {regret}.";

        string mentalStateLine = BuildMentalStateLine(confidence, brainwash, wokeness);
        string closingLine = BuildClosingLine(confidence, brainwash, wokeness);

        return $"{intro}\n\n{regretLine}\n\n{mentalStateLine}\n\n{closingLine}";
    }

    private string BuildFailSummary(GameRunState state)
    {
        int finalDay = state.CurrentDay;
        float confidence = state.Stats.Confidence;
        float brainwash = state.Stats.Brainwash;
        float wokeness = state.Stats.Wokeness;

        string regret = GetStrongestRegretText();
        if (string.IsNullOrWhiteSpace(regret))
            regret = "the weight you kept carrying";

        // Failed by running out of time
        if (state.CurrentDay > state.MaxDays)
        {
            return
                $"Night falls on Day {finalDay}.\n\n" +
                $"The days have run out.\n\n" +
                $"You lie in silence, still carrying {regret}. " +
                $"Whatever doubt, fear, or hope remained in you was not enough to change your path in time.\n\n" +
                BuildEndingMoodLine(confidence, brainwash, wokeness, false);
        }

        // General fail fallback
        return
            $"Night falls on Day {finalDay}.\n\n" +
            $"Something inside you gives way.\n\n" +
            $"The weight of {regret} no longer feels like something you can examine from a distance. " +
            $"It has become part of the way you see yourself.\n\n" +
            BuildEndingMoodLine(confidence, brainwash, wokeness, false);
    }

    private string BuildPassSummary(GameRunState state)
    {
        int finalDay = state.CurrentDay;
        float confidence = state.Stats.Confidence;
        float brainwash = state.Stats.Brainwash;
        float wokeness = state.Stats.Wokeness;

        string regret = GetStrongestRegretText();
        if (string.IsNullOrWhiteSpace(regret))
            regret = "what has been haunting you";

        return
            $"Night falls on Day {finalDay}.\n\n" +
            $"You close your eyes, but something is different now.\n\n" +
            $"The weight of {regret} is still real, but it no longer controls the shape of your thoughts the way it did before. " +
            $"You can feel both the pull of what they taught you and the part of you that has begun to see through it.\n\n" +
            BuildEndingMoodLine(confidence, brainwash, wokeness, true);
    }

    private string BuildMentalStateLine(float confidence, float brainwash, float wokeness)
    {
        if (wokeness > brainwash && wokeness >= 40f)
        {
            return "Something about the day stays with you in an uncomfortable way. The words you heard no longer settle as easily as they once did.";
        }

        if (brainwash > wokeness && brainwash >= 40f)
        {
            return "Their words keep echoing in your head. Part of you wants to resist, but another part feels strangely tired of resisting at all.";
        }

        if (confidence <= 25f)
        {
            return "You feel drained, as if even your own thoughts have become heavy to carry.";
        }

        if (confidence >= 70f)
        {
            return "Even in the quiet, some part of you still holds on. Not everything inside you is ready to bend.";
        }

        return "Your thoughts drift in circles, never staying still long enough to let you rest.";
    }

    private string BuildClosingLine(float confidence, float brainwash, float wokeness)
    {
        if (wokeness > brainwash && wokeness >= 45f)
        {
            return "Sleep comes slowly, but beneath the exhaustion, a question remains: what if the voice inside you is still worth trusting?";
        }

        if (brainwash > wokeness && brainwash >= 45f)
        {
            return "Sleep comes slowly, and with it comes the dangerous comfort of believing that surrender might be easier.";
        }

        if (confidence <= 25f)
        {
            return "Tonight, rest feels less like peace and more like escape.";
        }

        return "Eventually, your eyes close, but the questions do not.";
    }

    private string BuildEndingMoodLine(float confidence, float brainwash, float wokeness, bool passed)
    {
        if (passed)
        {
            if (wokeness >= brainwash)
            {
                return "For the first time in a while, rest does not feel like surrender. It feels like the beginning of seeing clearly.";
            }

            return "Even now, the struggle inside you is unfinished. But somewhere in that struggle, you have begun to understand that fear is not the same thing as truth.";
        }

        if (brainwash > wokeness && confidence < 30f)
        {
            return "By the time sleep finds you, it no longer feels like you are choosing what to believe. It feels like that choice has already been made for you.";
        }

        if (wokeness >= brainwash)
        {
            return "You sensed that something was wrong, but sensing it was not enough. Night comes anyway.";
        }

        return "The night is quiet, but not gentle. Whatever part of you tried to hold itself together has grown very small.";
    }

    private string GetStrongestRegretText()
    {
        if (GameManager.Instance == null || GameManager.Instance.RegretSystem == null)
            return null;

        Regret strongestRegret = GameManager.Instance.RegretSystem.GetStrongestRegret();
        if (strongestRegret == null)
            return null;

        return strongestRegret.Text;
    }
}