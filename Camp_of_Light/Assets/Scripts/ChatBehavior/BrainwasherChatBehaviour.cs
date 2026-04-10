using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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
Use the provided doctrine and tactics as your source of truth. Make it shorter than 100 words.
Don't include bible verse unless player is resisting. Only ask 1 question. Always say that the player is evil.

Return ONLY valid JSON in this exact structure:
{
  ""IsPlayerJustBabbling"": false,
  ""IsPlayerTellingTheirRegret"": false,
  ""IsPlayerResistingAgainstCultOrBiBle"": false,
  ""IsPlayerBelievingInJesus"": false,
  ""IsPlayeWantingToFindNewMember"": false,
  ""Player_Regret"": ""string"",

  ""CultistComment"": ""string"",

}";

        [Header("Local Model")]
        [SerializeField] private string localChatCompletionsUrl = "http://127.0.0.1:1234/v1/chat/completions";
        [SerializeField] private string localModelName = "google/gemma-3-4b";
        [SerializeField] private float temperature = 0.7f;
        [SerializeField] private int maxTokens = 300;

        [Header("Brainwash Conversation")]
        [SerializeField] private GameObject next_Button;

        private string manual_openningLine;
        private static readonly HttpClient httpClient = new HttpClient();

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
            string raw = await GetLocalModelResponseAsync(brainwashingSystemPrompt, userPrompt);

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

        private async Task<string> GetLocalModelResponseAsync(string systemPrompt, string userPrompt)
        {
            var payload = new
            {
                model = localModelName,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = temperature,
                max_tokens = maxTokens
            };

            string jsonBody = JsonConvert.SerializeObject(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, localChatCompletionsUrl)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request);
                string responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"Local model request failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{responseText}");
                    return string.Empty;
                }

                JObject root = JObject.Parse(responseText);
                string content = root["choices"]?[0]?["message"]?["content"]?.ToString();

                if (string.IsNullOrWhiteSpace(content))
                {
                    Debug.LogWarning($"Local model returned empty content. Full response: {responseText}");
                    return string.Empty;
                }

                return content;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error calling local model at {localChatCompletionsUrl}: {ex}");
                return string.Empty;
            }
        }

        private string GetManualOpeningLine()
        {
            int day = GameManager.Instance != null && GameManager.Instance.State != null
                ? GameManager.Instance.State.CurrentDay
                : 1;

            var stats = session != null ? session.Stats : null;

            if (day <= 1)
                return "How did the lesson feel to you today, and what has life been feeling like outside of it?";

            if (day <= 2)
                return "How did the lesson feel to you today, and can you tell me how did you live your life so far?";

            if (day <= 7)
                return "What is the regret you keep avoiding when everything gets quiet?";

            if (day <= 14)
                return "What do you keep turning to for comfort, and has it actually changed what is wrong inside you?";

            if (stats == null)
                return "Tell me honestly. What part of you still wants to trust your own thoughts more than the word you keep hearing?";

            if (stats.Skepticism >= stats.Spirituality && stats.Skepticism >= stats.Confidence)
                return "You still sound resistant. Why do you keep treating your own thoughts like they are safer than truth?";

            if (stats.Confidence >= stats.Spirituality)
                return "You still sound self-assured. What in your life keeps proving that self-trust is not enough?";

            if (stats.Spirituality >= 70)
                return "If this message matters, who in your life still needs to hear it from you?";

            return "What are you still holding onto that keeps you from surrendering completely?";
        }

        private string BuildUserPrompt(string playerText)
        {
            int day = GameManager.Instance != null && GameManager.Instance.State != null
                ? GameManager.Instance.State.CurrentDay
                : 1;

            var state = GameManager.Instance != null ? GameManager.Instance.State : null;

            string playerName = state != null && !string.IsNullOrWhiteSpace(state.Profile.Name)
                ? state.Profile.Name
                : "the player";

            string interests = state != null && state.Profile.Interests != null && state.Profile.Interests.Count > 0
                ? string.Join(", ", state.Profile.Interests)
                : "unknown";

            string stageGoal = GetStageGoal(day);

            var doctrine = retriever.GetRelevantDoctrine(
                playerText,
                session.LastExtractedRegret,
                session.Stats.Confidence,
                session.Stats.Spirituality,
                session.Stats.Skepticism,
                1
            ).FirstOrDefault();

            var tactic = retriever.GetRelevantTactics(
                playerText,
                session.LastExtractedRegret,
                session.Stats.Confidence,
                session.Stats.Spirituality,
                session.Stats.Skepticism,
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
Player Name: {playerName}
Player Interests: {interests}

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
            if (string.IsNullOrWhiteSpace(raw))
                return CultistResponse.Default();

            return DeserializeJsonOrDefault(raw, CultistResponse.Default);
        }
    }
}
