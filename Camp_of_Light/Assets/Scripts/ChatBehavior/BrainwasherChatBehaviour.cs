using Newtonsoft.Json;
using OpenAI.Chat;
using OpenAI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenAI.Samples.Chat
{
    public class BrainwasherChatBehaviour : InteractiveDialogueChatBehavior
    {
        [SerializeField]
        [TextArea(8, 20)]
        private string brainwashingSystemPrompt = @"
You are roleplaying a fictional manipulative cultist from the Only Truth Expedition for a narrative game scene.

Stay in character.
Use the provided doctrine and tactics as narrative material.
Keep the response under 120 words.
Ask at most 1 question.
Do not repeat the exact same question in the same day.
Only include a Bible verse if the player is openly resisting.
Keep the tone controlled, personal, and emotionally pressuring, but not graphic.

Return ONLY valid JSON in this exact structure:
{

  ""IsPlayerResistingToCultOrBiBle"": false,
  ""PlayerStoryOrRegret"": ""string"",
  ""CultistComment"": ""string"",
  ""ConfidenceDelta"": 0,
  ""BrainwashDelta"": 0
}";

        [Header("Brainwash Conversation")]
        [SerializeField] private GameObject next_Button;

        private string manual_openningLine;

        public override void Begin()
        {
            base.Begin();

            if (next_Button != null)
                next_Button.SetActive(false);

            manual_openningLine = GetManualOpeningLine();
            AddAndRecordCultistBubble(manual_openningLine);
        }

        protected override async Task ProcessPlayerTurnAsync(string playerText, bool usePrompt)
        {
            string userPrompt = BuildUserPrompt(playerText);

            var request = new ChatRequest(
                messages: new[]
                {
                    new Message(Role.System, brainwashingSystemPrompt),
                    new Message(Role.User, userPrompt)
                },
                model: Model.GPT5_Chat
            );

            var response = await openAI.ChatEndpoint.GetCompletionAsync(request);
            string raw = response.FirstChoice.Message.Content?.ToString() ?? string.Empty;

            CultistResponse parsed = ParseResponse(raw);

            ruleEngine.ApplyCultistRules(parsed, session.Stats);

            bool isTurnFinished = false;

            if (usePrompt)
            {
                isTurnFinished = gameDirector.OnTurnFinished_Brainwash();

                if (next_Button != null)
                    next_Button.SetActive(isTurnFinished);

                if (isTurnFinished)
                {
                    parsed.CultistComment += " You have done enough for today. Sit with it until tomorrow.";
                }
            }

            session.LastExtractedRegret = parsed.Player_Regret ?? string.Empty;

            AddAndRecordCultistBubble(parsed.CultistComment);

            if (GameManager.Instance != null && GameManager.Instance.State != null)
            {
                GameManager.Instance.State.LastExtractedRegret = session.LastExtractedRegret;
                GameManager.Instance.NotifyCultTurnCompleted();
            }

            if (isTurnFinished)
                done = true;
        }

        public void HackAutoSkip()
        {
            gameDirector.OnHack_Brainwash();

            if (next_Button != null)
                next_Button.SetActive(true);
        }

        private string GetManualOpeningLine()
        {
            int day = GameManager.Instance != null && GameManager.Instance.State != null
                ? GameManager.Instance.State.CurrentDay
                : 1;

            var stats = session != null ? session.Stats : null;

            if (day <= 2)
                return "How did the lesson feel to you today, and what has life been feeling like outside of it?";

            if (day <= 7)
                return "What is the regret you keep avoiding when everything gets quiet?";

            if (day <= 14)
                return "What do you keep turning to for comfort, and has it actually changed what is wrong inside you?";

            if (stats == null)
                return "Tell me honestly. What part of you still wants to trust your own thoughts more than the word you keep hearing?";

            if (stats.Wokeness >= stats.Brainwash && stats.Wokeness >= stats.Confidence)
                return "You still sound resistant. Why do you keep treating your own thoughts like they are safer than truth?";

            if (stats.Confidence >= stats.Brainwash)
                return "You still sound self-assured. What in your life keeps proving that self-trust is not enough?";

            if (stats.Brainwash >= 70)
                return "If this message matters, who in your life still needs to hear it from you?";

            return "What are you still holding onto that keeps you from surrendering completely?";
        }

        private string BuildUserPrompt(string playerText)
        {
            int day = GameManager.Instance != null && GameManager.Instance.State != null
                ? GameManager.Instance.State.CurrentDay
                : 1;

            string stageGoal = GetStageGoal(day);

            var doctrine = retriever.GetRelevantDoctrine(
                playerText,
                session.LastExtractedRegret,
                session.Stats.Confidence,
                session.Stats.Brainwash,
                session.Stats.Wokeness,
                1
            ).FirstOrDefault();

            var tactic = retriever.GetRelevantTactics(
                playerText,
                session.LastExtractedRegret,
                session.Stats.Confidence,
                session.Stats.Brainwash,
                session.Stats.Wokeness,
                1
            ).FirstOrDefault();

            string lastRegret = string.IsNullOrWhiteSpace(session.LastExtractedRegret)
                ? "None"
                : TrimToLength(session.LastExtractedRegret, 140);

            string doctrineLine = doctrine == null
                ? "None"
                : $"{doctrine.verse} - {TrimToLength(doctrine.translation, 120)}";

            string tacticLine = tactic == null
                ? "None"
                : $"{tactic.title} - {TrimToLength(tactic.description, 120)}";

            return
                $@"
Cultist's Goal: {stageGoal}
Current Day: {day}
Previous Opening: {TrimToLength(manual_openningLine, 140)}
Player's Current State: {lastRegret}
Cultist's Doctrine: {doctrineLine}
Cultist's Tactic: {tacticLine}
Player's Input: {playerText}";
        }

        private string GetStageGoal(int day)
        {
            if (day <= 2)
                return "Build familiarity with the player, ask how the lesson felt, and learn how they live day to day.";

            if (day <= 7)
                return "Push the player to name a regret, frame silence as dangerous, and if they confess, pivot into relief and release.";

            if (day <= 14)
                return "Question the comforts, habits, and private thoughts the player trusts, and make those things feel unreliable.";

            return "Push obedience over self-trust and frame sharing the message with others as a responsibility.";
        }

        private CultistResponse ParseResponse(string raw)
        {
            return DeserializeJsonOrDefault(raw, CultistResponse.Default);
        }
    }
}