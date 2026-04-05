using OpenAI.Chat;
using OpenAI.Models;
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

        [SerializeField] private GameObject next_Button;

        private GameSharedSystem sharedSystem;
        private string manual_openningLine;
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

            var request = new ChatRequest(
                messages: new[]
                {
                    new Message(Role.System, conscienceSystemPrompt),
                    new Message(Role.User, prompt)
                },
                model: Model.GPT5_Chat
            );

            var response = await openAI.ChatEndpoint.GetCompletionAsync(request);
            string raw = response.FirstChoice.Message.Content?.ToString() ?? string.Empty;

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

            string interests = state != null && state.Profile.Name != null && state.Profile.Interests.Count > 0
                ? string.Join(", ", state.Profile.Name)
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
            return DeserializeJsonOrDefault(raw, ConscienceResponse.Default);
        }
    }
}