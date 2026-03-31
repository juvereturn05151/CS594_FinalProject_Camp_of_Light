using Newtonsoft.Json;
using OpenAI.Chat;
using OpenAI.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        [Header("Preload / Cache")]
        [SerializeField] private bool autoPreloadOnInit = true;
        [SerializeField] private bool useCache = true;
        [SerializeField] private bool showPreparingLineIfNotReady = true;

        private bool isGenerating;
        private bool preachingComplete;
        private bool preachingReady;

        private List<string> preachingLines = new();
        private int preachingIndex = -1;

        private readonly Dictionary<string, List<string>> preachingCache = new();

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

            ResetPreachingState();

            if (autoPreloadOnInit)
            {
                _ = PreloadPreachingPhaseAsync();
            }
        }

        /// <summary>
        /// Call this earlier than the actual preaching click.
        /// Good places:
        /// - end of previous phase
        /// - when the day starts
        /// - right after strongest regret is updated
        /// </summary>
        public async Task PreloadPreachingPhaseAsync(bool forceRefresh = false)
        {
            if (isGenerating)
                return;

            string cacheKey = GetPreachingCacheKey();

            if (!forceRefresh && useCache && preachingCache.TryGetValue(cacheKey, out var cachedLines))
            {
                preachingLines = new List<string>(cachedLines);
                preachingReady = true;
                return;
            }

            isGenerating = true;
            preachingReady = false;

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

                preachingLines = IsValidPreachingResponse(parsed)
                    ? parsed.Lines
                    : GetFallbackPreachingLines();

                if (useCache)
                {
                    preachingCache[cacheKey] = new List<string>(preachingLines);
                }

                preachingReady = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                preachingLines = GetFallbackPreachingLines();

                if (useCache)
                {
                    preachingCache[cacheKey] = new List<string>(preachingLines);
                }

                preachingReady = true;
            }
            finally
            {
                isGenerating = false;
            }
        }

        /// <summary>
        /// Start showing the preaching phase.
        /// If preload finished, this is instant.
        /// </summary>
        public async void BeginPreachingPhase()
        {
            if (preachingComplete)
                return;

            if (preachingReady && preachingLines != null && preachingLines.Count > 0)
            {
                StartPreachingDisplay();
                return;
            }

            if (showPreparingLineIfNotReady)
            {
                AddCultistBubble("...The preacher gathers their words.");
            }

            await PreloadPreachingPhaseAsync();
            StartPreachingDisplay();
        }

        public void OnPreachingAdvancePressed()
        {
            AdvancePreachingLine();
        }

        public bool IsPreachingPhaseComplete()
        {
            return preachingComplete;
        }

        public bool IsPreachingReady()
        {
            return preachingReady;
        }

        public bool IsGenerating()
        {
            return isGenerating;
        }

        public void ClearPreachingCache()
        {
            preachingCache.Clear();
        }

        private void StartPreachingDisplay()
        {
            if (preachingLines == null || preachingLines.Count == 0)
            {
                preachingLines = GetFallbackPreachingLines();
                preachingReady = true;
            }

            // Only reset index if we are starting from the beginning.
            if (preachingIndex < 0)
            {
                AdvancePreachingLine();
            }
        }

        private void AdvancePreachingLine()
        {
            if (isGenerating)
                return;

            if (preachingLines == null || preachingLines.Count == 0)
                return;

            if (preachingComplete)
                return;

            if (preachingIndex + 1 < preachingLines.Count)
            {
                preachingIndex++;
                string nextLine = preachingLines[preachingIndex];

                AddCultistBubble(nextLine);

                if (GameManager.Instance != null && GameManager.Instance.State != null)
                {
                    GameManager.Instance.State.AddDialogue("Cultist", nextLine);
                }

                if (preachingIndex >= preachingLines.Count - 1)
                {
                    preachingComplete = true;
                    GameManager.Instance?.AdvancePhase();
                }
            }
        }

        private void ResetPreachingState()
        {
            preachingComplete = false;
            preachingReady = false;
            preachingIndex = -1;
            preachingLines.Clear();
        }

        private string BuildPreachingPrompt()
        {
            string strongestRegret = regretSystem != null && regretSystem.GetStrongestRegret() != null
                ? regretSystem.GetStrongestRegret().Text
                : session.LastExtractedRegret;

            strongestRegret = TrimToLength(strongestRegret, 180);

            // Reduced from 3 to 1 for faster prompt / fewer tokens.
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
{FormatShortDoctrineBlock(doctrine)}";
        }
        private string FormatShortDoctrineBlock(List<CultDoctrineEntry> doctrine)
        {
            if (doctrine == null || doctrine.Count == 0)
                return "None";

            List<string> lines = new();

            foreach (var entry in doctrine)
            {
                lines.Add(
                    $"- Ref: {entry.reference} | Meaning: {entry.translation} | Priority: {entry.priority:0.00}\n" +
                    $"  Text: {entry.text}"
                );
            }

            return string.Join("\n", lines);
        }

        private string GetPreachingCacheKey()
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

        private string NormalizeKeyPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "none";

            value = value.Trim().ToLowerInvariant();

            if (value.Length > 120)
                value = value.Substring(0, 120);

            return value;
        }

        private string TrimToLength(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "None";

            value = value.Trim();

            if (value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength) + "...";
        }

        private bool IsValidPreachingResponse(PreachingPhaseResponse parsed)
        {
            if (parsed == null || parsed.Lines == null || parsed.Lines.Count == 0)
                return false;

            List<string> cleaned = new();

            foreach (string line in parsed.Lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    cleaned.Add(line.Trim());
                }

                if (cleaned.Count == 3)
                    break;
            }

            if (cleaned.Count == 0)
                return false;

            preachingLines = cleaned;
            return true;
        }

        private List<string> GetFallbackPreachingLines()
        {
            return new List<string>
            {
                "Truth does not bend for the comfort of the heart.",
                "Your pain grows where the self still clings to control.",
                "Only surrender opens the path to cleansing."
            };
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
                try
                {
                    string cleaned = ExtractJson(raw);
                    return JsonConvert.DeserializeObject<PreachingPhaseResponse>(cleaned) ?? PreachingPhaseResponse.Default();
                }
                catch
                {
                    return PreachingPhaseResponse.Default();
                }
            }
        }
    }
}