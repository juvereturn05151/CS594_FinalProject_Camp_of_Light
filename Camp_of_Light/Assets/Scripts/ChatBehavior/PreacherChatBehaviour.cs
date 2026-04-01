using OpenAI.Chat;
using OpenAI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenAI.Samples.Chat
{
    public class PreacherChatBehaviour : MonologueSequenceChatBehavior
    {
        [SerializeField]
        [TextArea(8, 20)]
        private string preachingSystemPrompt = @"
You are roleplaying a manipulative cultist preacher from the Only Truth Expedition.

Generate a short preaching sequence for the player to read through line by line.

Rules:
- Return ONLY valid JSON.
- Keep it short.
- Write exactly 3 lines.
- Each line should be concise and natural for dialogue.
- Do not add markdown or explanation.

JSON format:
{
  ""Theme"": ""string"",
  ""Lines"": [""string"", ""string"", ""string""]
}";

        public override void Begin()
        {
            base.Begin();
        }

        public async Task PreloadPreachingPhaseAsync(bool forceRefresh = false)
        {
            await PreloadSequenceAsync(forceRefresh);
        }

        public void BeginPreachingPhase()
        {
            BeginSequence();
        }

        public void OnPreachingAdvancePressed()
        {
            OnAdvancePressed();
        }

        public bool IsPreachingPhaseComplete()
        {
            return IsSequenceComplete();
        }

        public bool IsPreachingReady()
        {
            return sequenceReady;
        }

        protected override string GetPreparingLine()
        {
            return "...The preacher gathers their words.";
        }

        protected override async Task<List<string>> GenerateSequenceLinesAsync()
        {
            string prompt = BuildPreachingPrompt();

            var request = new ChatRequest(
                messages: new[]
                {
                    new Message(Role.System, preachingSystemPrompt),
                    new Message(Role.User, prompt)
                },
                model: Model.GPT5_Mini
            );

            var response = await openAI.ChatEndpoint.GetCompletionAsync(request);
            string raw = response.FirstChoice.Message.Content?.ToString() ?? string.Empty;

            var parsed = ParsePreachingResponse(raw);

            if (parsed == null || parsed.Lines == null || parsed.Lines.Count == 0)
                return GetFallbackLines();

            return parsed.Lines;
        }

        protected override string GetSequenceCacheKey()
        {
            string strongestRegret = regretSystem != null && regretSystem.GetStrongestRegret() != null
                ? regretSystem.GetStrongestRegret().Text
                : session.LastExtractedRegret;

            strongestRegret = NormalizeKeyPart(strongestRegret);

            int day = GameManager.Instance != null && GameManager.Instance.State != null
                ? GameManager.Instance.State.CurrentDay
                : 1;

            return $"{day}|C:{session.Stats.Confidence}|B:{session.Stats.Brainwash}|W:{session.Stats.Wokeness}|R:{strongestRegret}";
        }

        protected override List<string> GetFallbackLines()
        {
            return new List<string>
            {
                "Truth does not bend for the comfort of the heart.",
                "Your pain grows where the self still clings to control.",
                "Only surrender opens the path to cleansing."
            };
        }

        protected override void OnSequenceCompleted()
        {
            GameManager.Instance?.AdvancePhase();
        }

        private string BuildPreachingPrompt()
        {
            string strongestRegret = regretSystem != null && regretSystem.GetStrongestRegret() != null
                ? regretSystem.GetStrongestRegret().Text
                : session.LastExtractedRegret;

            strongestRegret = TrimToLength(strongestRegret, 180);

            List<CultDoctrineEntry> doctrine = retriever.GetRelevantDoctrine(
                strongestRegret,
                session.LastExtractedRegret,
                session.Stats.Confidence,
                session.Stats.Brainwash,
                session.Stats.Wokeness,
                1
            );

            return
                $@"Current day: {(GameManager.Instance != null && GameManager.Instance.State != null ? GameManager.Instance.State.CurrentDay : 1)}

                Player stats:
                Confidence: {session.Stats.Confidence}
                Brainwash: {session.Stats.Brainwash}
                Wokeness: {session.Stats.Wokeness}

                Strongest regret:
                {strongestRegret}

                Relevant doctrine:
                {FormatDoctrineBlock(doctrine)}";
        }

        private PreachingPhaseResponse ParsePreachingResponse(string raw)
        {
            return DeserializeJsonOrDefault(raw, PreachingPhaseResponse.Default);
        }
    }
}