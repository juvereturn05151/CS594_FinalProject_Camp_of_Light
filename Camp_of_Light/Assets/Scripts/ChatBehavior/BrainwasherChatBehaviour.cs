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

Stay in character.
Use the provided doctrine and tactics as your source of truth.

Return ONLY valid JSON in this exact structure:
{
  ""IsRelevant"": true,
  ""IsPlayerResisting"": false,
  ""PlayerStoryOrRegret"": ""string"",
  ""BibleVerse"": ""string"",
  ""CultistComment"": ""string"",
  ""ConfidenceDelta"": 0,
  ""BrainwashDelta"": 0,
  ""WokenessDelta"": 0
}";

        [Header("Brainwash Conversation")]
        [SerializeField] private GameObject next_Button;

        [TextArea(3, 8)]
        [SerializeField]
        private string defaultInitiationContext =
            "Start a manipulative follow-up conversation based on the player's current mental state and what the cult preached today.";

        public override void Begin()
        {
            base.Begin();

            if (next_Button != null) 
            {
                next_Button.SetActive(false);
            }

            InitiateConversation();


        }

        protected override async Task ProcessPlayerTurnAsync(string playerText)
        {
            string userPrompt = BuildUserPrompt(playerText);

            var request = new ChatRequest(
                messages: new[]
                {
                    new Message(Role.System, brainwashingSystemPrompt),
                    new Message(Role.User, userPrompt)
                },
                model: Model.GPT5_Mini
            );

            var response = await openAI.ChatEndpoint.GetCompletionAsync(request);
            string raw = response.FirstChoice.Message.Content?.ToString() ?? string.Empty;

            CultistResponse parsed = ParseResponse(raw);

            ruleEngine.ApplyCultistRules(parsed, session.Stats);

            bool isTurnFinished = gameDirector.OnTurnFinished_Brainwash();

            if (next_Button != null)
                next_Button.SetActive(isTurnFinished);

            session.LastExtractedRegret = parsed.PlayerStoryOrRegret ?? string.Empty;
            session.LastBibleVerse = parsed.BibleVerse ?? string.Empty;

            AddAndRecordCultistBubble(parsed.CultistComment);

            if (GameManager.Instance != null && GameManager.Instance.State != null)
            {
                GameManager.Instance.State.LastExtractedRegret = session.LastExtractedRegret;
                GameManager.Instance.State.LastBibleVerse = session.LastBibleVerse;
                GameManager.Instance.NotifyCultTurnCompleted();
            }
        }

        protected override string BuildInitiationConversation()
        {
            string preachedToday = GetTodayPreachingSummary();
            string lastRegret = string.IsNullOrWhiteSpace(session.LastExtractedRegret)
                ? "No clear regret has been extracted yet."
                : session.LastExtractedRegret;

            string lastVerse = string.IsNullOrWhiteSpace(session.LastBibleVerse)
                ? "No previous Bible verse has been used yet."
                : session.LastBibleVerse;

            return
$@"{defaultInitiationContext}

Current player state:
Confidence: {session.Stats.Confidence}
Brainwash: {session.Stats.Brainwash}
Wokeness: {session.Stats.Wokeness}

Last extracted regret:
{lastRegret}

Previous Bible verse:
{lastVerse}

What the cult preached today:
{preachedToday}

Start the conversation naturally as the cultist.
Push the player emotionally using their state and today's preaching.";
        }

        public void HackAutoSkip()
        {
            gameDirector.OnHack_Brainwash();

            if (next_Button != null)
                next_Button.SetActive(true);
        }

        private string BuildUserPrompt(string playerText)
        {
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

            return
        $@"Player stats:
C:{session.Stats.Confidence} B:{session.Stats.Brainwash} W:{session.Stats.Wokeness}

Last regret:
{session.LastExtractedRegret}

Doctrine:
{doctrine?.verse} - {doctrine?.translation}

Tactic:
{tactic?.title} - {tactic?.description}

Player says:
{playerText}";
        }

        private string GetTodayPreachingSummary()
        {
            if (GameManager.Instance != null &&
                GameManager.Instance.State != null &&
                !string.IsNullOrWhiteSpace(GameManager.Instance.State.LastBibleVerse))
            {
                return GameManager.Instance.State.LastBibleVerse;
            }

            return "No preaching summary is available for today.";
        }

        private CultistResponse ParseResponse(string raw)
        {
            return DeserializeJsonOrDefault(raw, CultistResponse.Default);
        }
    }
}