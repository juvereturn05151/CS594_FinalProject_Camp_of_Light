using Newtonsoft.Json;
using OpenAI.Chat;
using OpenAI.Models;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenAI.Samples.Chat
{
    public class PreacherChatBehaviour : BaseCultChatBehaviour
    {
        [SerializeField]
        [TextArea(8, 20)]
        private string preachingSystemPrompt = @"
            You are roleplaying a manipulative cultist preacher from the Only Truth Expedition.

            Generate a short preaching sequence for the player to read through line by line.

            Return ONLY valid JSON in this structure:
            {
              ""Theme"": ""string"",
              ""Lines"": [""string"", ""string"", ""string""]
            }";

        private bool isGenerating;
        private bool preachingComplete;
        private List<string> preachingLines = new();
        private int preachingIndex = -1;

        private void Update()
        {
            if (isGenerating || !gameObject.activeInHierarchy)
                return;

            if (Input.GetKeyDown(KeyCode.A) ||
                Input.GetKeyDown(KeyCode.B) ||
                Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.Return))
            {
                AdvancePreachingLine();
            }
        }

        public override void Init()
        {
            base.Init();
            preachingComplete = false;
            preachingLines.Clear();
            preachingIndex = -1;
        }

        public async void BeginPreachingPhase()
        {
            if (isGenerating) return;

            isGenerating = true;
            AddCultistBubble("...The preaching begins.");

            try
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

                preachingLines = parsed?.Lines != null && parsed.Lines.Count > 0
                    ? parsed.Lines
                    : new List<string>
                    {
                        "Truth does not bend for the comfort of the heart.",
                        "Sin grows wherever the self remains unbroken.",
                        "Only surrender opens the path to cleansing."
                    };

                AdvancePreachingLine();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                preachingLines = new List<string>
                {
                    "Truth does not bend for the comfort of the heart.",
                    "Sin grows wherever the self remains unbroken.",
                    "Only surrender opens the path to cleansing."
                };

                AdvancePreachingLine();
            }
            finally
            {
                isGenerating = false;
            }
        }

        public void OnPreachingAdvancePressed()
        {
            AdvancePreachingLine();
        }

        public bool IsPreachingPhaseComplete()
        {
            return preachingComplete;
        }

        private void AdvancePreachingLine()
        {
            if (preachingLines == null || preachingLines.Count == 0)
                return;

            if (preachingIndex + 1 < preachingLines.Count)
            {
                preachingIndex++;
                AddCultistBubble(preachingLines[preachingIndex]);

                if (GameManager.Instance != null && GameManager.Instance.State != null)
                {
                    GameManager.Instance.State.AddDialogue("Cultist", preachingLines[preachingIndex]);
                }

                if (preachingIndex >= preachingLines.Count - 1)
                {
                    preachingComplete = true;

                    GameManager.Instance?.RefreshPhaseButtonState();

                    GameManager.Instance?.AdvancePhase();
                }
            }
        }

        private string BuildPreachingPrompt()
        {
            string strongestRegret = regretSystem != null && regretSystem.GetStrongestRegret() != null
                ? regretSystem.GetStrongestRegret().Text
                : session.LastExtractedRegret;

            List<CultDoctrineEntry> doctrine = retriever.GetRelevantDoctrine(
                strongestRegret,
                session.LastExtractedRegret,
                session.Stats.Confidence,
                session.Stats.Brainwash,
                session.Stats.Wokeness,
                3
            );

            return
            $@"Current day:
            {(GameManager.Instance != null && GameManager.Instance.State != null ? GameManager.Instance.State.CurrentDay : 1)}

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
            if (string.IsNullOrWhiteSpace(raw))
                return PreachingPhaseResponse.Default();

            try
            {
                return JsonConvert.DeserializeObject<PreachingPhaseResponse>(raw) ?? PreachingPhaseResponse.Default();
            }
            catch
            {
                string cleaned = ExtractJson(raw);
                return JsonConvert.DeserializeObject<PreachingPhaseResponse>(cleaned) ?? PreachingPhaseResponse.Default();
            }
        }
    }
}