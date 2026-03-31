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
            You are the inner conscience of the player.

            Speak naturally and briefly.
            Reflect on what the player said and what the cult is doing.
            Return ONLY valid JSON in this exact structure:
            {
              ""ConscienceComment"": ""string"",
              ""ConfidenceDelta"": 0,
              ""BrainwashDelta"": 0,
              ""WokenessDelta"": 0
            }";

        [SerializeField] private GameObject nextButton;

        protected override async Task ProcessPlayerTurnAsync(string playerText)
        {
            string prompt = BuildUserPrompt(playerText);

            var request = new ChatRequest(
                messages: new[]
                {
                    new Message(Role.System, conscienceSystemPrompt),
                    new Message(Role.User, prompt)
                },
                model: Model.GPT5_Mini
            );

            var response = await openAI.ChatEndpoint.GetCompletionAsync(request);
            string raw = response.FirstChoice.Message.Content?.ToString() ?? string.Empty;

            ConscienceResponse parsed = ParseResponse(raw);

            ruleEngine.ApplyConscienceRules(parsed, session.Stats);

            AddAndRecordCultistBubble(parsed.ConscienceComment);

            if (nextButton != null)
                nextButton.SetActive(true);
        }

        private string BuildUserPrompt(string playerText)
        {
            return
                $@"Current player stats:
                Confidence: {session.Stats.Confidence}
                Brainwash: {session.Stats.Brainwash}
                Wokeness: {session.Stats.Wokeness}

                Last extracted regret:
                {session.LastExtractedRegret}

                Last bible verse:
                {session.LastBibleVerse}

                Player says:
                {playerText}";
        }

        private ConscienceResponse ParseResponse(string raw)
        {
            return DeserializeJsonOrDefault(raw, ConscienceResponse.Default);
        }
    }
}