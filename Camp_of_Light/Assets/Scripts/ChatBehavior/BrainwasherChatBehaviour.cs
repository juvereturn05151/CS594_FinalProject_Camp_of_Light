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
You are roleplaying a manipulative cultist from the Only Truth Expedition.

Make it sound a strict Christian parent who knows nothing except bible. 

Stay in character.
Use the provided doctrine and tactics as your source of truth. Make it shorter than 120 words.
Don't include bible verse unless player is resisting. Only ask 1 question. Don't ask the player the same question in a day.
Increase the brainwash delta and decrese confidence delta if they  are agreeing with the Bible.

If it is not relevant, please say it is not relevant.  

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

            if (usePrompt)
            {
                bool isTurnFinished = gameDirector.OnTurnFinished_Brainwash();

                if (next_Button != null)
                {
                    next_Button.SetActive(isTurnFinished);
                }

                if (isTurnFinished) 
                {
                    parsed.CultistComment += isTurnFinished
                        ? " You have done well today. Take some rest and prepare for tomorrow."
                        : " Let's continue our conversation.";
                }
            }

            session.LastExtractedRegret = parsed.PlayerStoryOrRegret ?? string.Empty;

            AddAndRecordCultistBubble(parsed.CultistComment);

            if (GameManager.Instance != null && GameManager.Instance.State != null)
            {
                GameManager.Instance.State.LastExtractedRegret = session.LastExtractedRegret;
                GameManager.Instance.State.LastBibleVerse = session.LastBibleVerse;
                GameManager.Instance.NotifyCultTurnCompleted();
            }
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

            if (day <= 7)
            {
                return "How do you feel about the lesson today?";
            }

            if (day <= 14)
            {
                return "Tell me this plainly. What regret are you still carrying in your heart?";
            }

            if (stats == null)
            {
                return "Your heart is unstable. Speak honestly. What is happening inside you right now?";
            }

            // Day 14+ ask based on stats
            if (stats.Wokeness >= stats.Brainwash && stats.Wokeness >= stats.Confidence)
            {
                return "You still seem resistant. Why are you still holding on to your own thoughts instead of surrendering?";
            }

            if (stats.Confidence >= stats.Brainwash)
            {
                return "You are still relying on yourself. What part of your life keeps proving that your own strength is failing you?";
            }

            if (stats.Brainwash >= 70)
            {
                return "You have already seen the truth. So tell me, what are you afraid will happen if you leave this place now?";
            }

            return "Open your heart completely. What regret or sin is still following you even now?";
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

            string lastVerse = string.IsNullOrWhiteSpace(session.LastBibleVerse)
                ? "None"
                : TrimToLength(session.LastBibleVerse, 140);

            string mainConversation = TrimToLength(manual_openningLine, 140);

            string doctrineLine = doctrine == null
                ? "None"
                : $"{doctrine.verse} - {TrimToLength(doctrine.translation, 120)}";

            string tacticLine = tactic == null
                ? "None"
                : $"{tactic.title} - {TrimToLength(tactic.description, 120)}";

            return
                $@"
                Cultist's Goal: {stageGoal}

                Player's Current State: {lastRegret}

                Cultist's Doctrine: {doctrineLine}
                Cultist's Tactic: {tacticLine}

                Player's Input: {playerText}";
        }

        private string GetStageGoal(int day)
        {
            if (day <= 7)
                return "Ask about the player's feelings and keep learning about them while reinforcing that the Bible is real.";

            if (day <= 14)
                return "Pressure the player to confess the regret they carry and admit they need Jesus.";

            if (session != null &&
                session.Stats != null &&
                session.Stats.Brainwash >= 70)
            {
                return "Make the player fear leaving and returning to sin.";
            }

            return "Force the player to go deeper into their regret and expose what they are still hiding.";
        }

        private CultistResponse ParseResponse(string raw)
        {
            return DeserializeJsonOrDefault(raw, CultistResponse.Default);
        }
    }
}