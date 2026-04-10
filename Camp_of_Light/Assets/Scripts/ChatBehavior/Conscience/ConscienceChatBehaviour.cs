using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenAI.Samples.Chat
{
    public class ConscienceChatBehaviour : InteractiveDialogueChatBehavior
    {
        [SerializeField]
        [TextArea(8, 20)]
        private string conscienceSystemPrompt = @"
            
            You are the player's conscience.

            You are calm, supportive, reflective, and human.
            You do NOT manipulate, preach doctrine, shame, threaten, or claim absolute truth.
            You help the player reflect on what they feel, especially around regret.

            Focus on:
            - the player's emotional state
            - the regret they carry
            - helping them name what they feel
            - gently encouraging self-understanding
            - helping them separate guilt, fear, and truth

            Keep the response short and natural.

            Return ONLY valid JSON in this exact structure:
            {

      	     ""IsPlayerTellingTheirRegret"": false,
	            ""IsPlayerResistingToCultOrBiBle"": false,
	            ""IsPlayerBelievingInThemselves"": false,
              ""ConscienceComment"": ""string""

            }";

        [Header("Local Model")]
        [SerializeField] private string localChatCompletionsUrl = "http://127.0.0.1:1234/v1/chat/completions";
        [SerializeField] private string localModelName = "local-model";
        [SerializeField] private float temperature = 0.7f;
        [SerializeField] private int maxTokens = 300;

        [SerializeField] private GameObject next_Button;

        private GameSharedSystem sharedSystem;
        private string manual_openningLine;

        private static readonly HttpClient httpClient = new HttpClient();

        public override void Begin()
        {
            base.Begin();

            if (next_Button != null)
                next_Button.SetActive(false);

            manual_openningLine = "Please Reflect on your conversation today";
            AddAndRecordCultistBubble(manual_openningLine);
        }

        protected override string BuildInitiationConversation()
        {
            if (sharedSystem == null)
                sharedSystem = GameSharedSystem.Instance;

            if (sharedSystem == null)
                return "The player is quiet and thinking.";

            var session = sharedSystem.Session;
            var regretSystem = sharedSystem.RegretSystem;

            string strongestRegret = regretSystem != null && regretSystem.GetStrongestRecentRegret() != null
                ? regretSystem.GetStrongestRecentRegret().Text
                : "no clear regret yet";

            int confidence = session != null ? session.Stats.Confidence : 0;
            int brainwash = session != null ? session.Stats.Spirituality : 0;
            int wokeness = session != null ? session.Stats.Skepticism : 0;

            return
                $"Start a conversation naturally. " +
                $"Their strongest regret is: {strongestRegret}. " +
                $"Their current state is Confidence={confidence}, Brainwash={brainwash}, Wokeness={wokeness}. " +
                $"Respond as a cultist beginning the conscience talk.";
        }

        protected override async Task ProcessPlayerTurnAsync(string playerText, bool usePrompt)
        {
            string prompt = BuildUserPrompt(playerText);
            string raw = await GetLocalModelResponseAsync(conscienceSystemPrompt, prompt);

            ConscienceResponse parsed = ParseResponse(raw);

            ruleEngine.ApplyConscienceRules(parsed, session.Stats);

            bool isTurnFinished = false;

            if (usePrompt)
            {
                isTurnFinished = gameDirector.OnTurnFinished_Conscience();

                if (next_Button != null)
                {
                    next_Button.SetActive(isTurnFinished);
                }

                if (isTurnFinished)
                {
                    parsed.ConscienceComment += isTurnFinished
                        ? " You have done well today. Take some rest and prepare for tomorrow."
                        : " Let's continue our conversation.";
                }
            }

            AddAndRecordCultistBubble(parsed.ConscienceComment);

            if (GameManager.Instance != null && GameManager.Instance.State != null)
            {
                GameManager.Instance.NotifyCultTurnCompleted();
            }

            if (isTurnFinished)
            {
                done = true;
            }
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

        public void HackAutoSkip()
        {
            gameDirector.OnHack_Conscience();

            if (next_Button != null)
                next_Button.SetActive(true);
        }

        private string BuildUserPrompt(string playerText)
        {
            var state = GameManager.Instance != null ? GameManager.Instance.State : null;

            string playerName = state != null && !string.IsNullOrWhiteSpace(state.Profile.Name)
                ? state.Profile.Name
                : "the player";

            string interests = state != null && state.Profile.Interests != null && state.Profile.Interests.Count > 0
                ? string.Join(", ", state.Profile.Interests)
                : "unknown";

            return
                $@"
            Player Name: {playerName}
            Player Interests: {interests}

            Current player stats:
            Confidence: {session.Stats.Confidence}
            Brainwash: {session.Stats.Spirituality}
            Wokeness: {session.Stats.Skepticism}

            Last extracted regret:
            {session.LastExtractedRegret}

            Player says:
            {playerText}";
        }

        private ConscienceResponse ParseResponse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return ConscienceResponse.Default();

            return DeserializeJsonOrDefault(raw, ConscienceResponse.Default);
        }
    }
}