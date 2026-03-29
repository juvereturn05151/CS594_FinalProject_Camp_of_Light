using Newtonsoft.Json;
using OpenAI.Chat;
using OpenAI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace OpenAI.Samples.Chat
{
    public class CampOfLightChatBehaviour : MonoBehaviour
    {
        [Header("OpenAI")]
        [SerializeField] private OpenAIConfiguration configuration;
        [SerializeField] private bool enableDebug = true;
        [SerializeField] private bool writeLogToFile = false;
        [SerializeField] private string logFileName = "camp_of_light_log.txt";

        [Header("UI")]
        [SerializeField] private Button submitButton;
        [SerializeField] private TMP_InputField inputField;

        [Header("Bubble UI")]
        [SerializeField] private GameObject playerBubbleObject;
        [SerializeField] private TMP_Text playerBubbleText;
        [SerializeField] private GameObject cultistBubbleObject;
        [SerializeField] private TMP_Text cultistBubbleText;

        [Header("Game State")]
        [SerializeField] private GameSession session = new();
        [SerializeField] private CultGameDirector gameDirector;
        [SerializeField] private RegretSystem regretSystem;
        [SerializeField] private CultRuleEngine ruleEngine;

        [Header("Cultist Prompt")]
        [SerializeField] private CultKnowledgeBase knowledgeBase;
        [SerializeField] private CultRetriever retriever;
        [SerializeField]
        [TextArea(8, 20)]
        private string systemPrompt = @"
You are roleplaying a manipulative cultist from the Only Truth Expedition.

Stay in character.
Use the provided doctrine and tactics as your source of truth.
Do not mention that you are using doctrine or tactics.
Speak naturally and persuasively.

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
}

Rules:
- CultistComment should be natural dialogue.
- Keep CultistComment concise, around 2-5 sentences.
- Return JSON only. No markdown. No explanation.
";

        private OpenAIClient openAI;
        private static bool isChatPending;

        private readonly List<LLMExchangeLog> exchangeHistory = new();
        private string LogFilePath => Path.Combine(Application.persistentDataPath, logFileName);

        private void Awake()
        {
            openAI = new OpenAIClient(configuration)
            {
                EnableDebug = enableDebug
            };

            if (submitButton != null)
                submitButton.onClick.AddListener(SubmitChat);

            if (inputField != null)
                inputField.onSubmit.AddListener(SubmitChat);

            if (session.Profile == null)
                session.Profile = new PlayerProfile();

            if (session.Stats == null)
                session.Stats = new PlayerStats();
        }

        private void Start()
        {
            HideAllBubbles();
            AddCultistBubble("Welcome. Tell me about yourself, and tell me what weighs on your heart.");
            LogSystemState("Camp of Light test session started.");
            LogSystemState(session.Profile.ToString());
            LogSystemState(session.Stats.ToString());
        }

        private void SubmitChat(string _) => SubmitChat();

        public async void SubmitChat()
        {
            if (isChatPending) return;
            if (inputField == null || string.IsNullOrWhiteSpace(inputField.text)) return;

            isChatPending = true;

            string playerText = inputField.text.Trim();

            inputField.ReleaseSelection();
            inputField.interactable = false;
            if (submitButton != null) submitButton.interactable = false;

            AddPlayerBubble(playerText);
            inputField.text = string.Empty;

            try
            {
                string userPrompt = BuildUserPrompt(playerText);

                var request = new ChatRequest(
                    messages: new[]
                    {
                        new Message(Role.System, systemPrompt),
                        new Message(Role.User, userPrompt)
                    },
                    model: Model.GPT5_Mini
                );

                var response = await openAI.ChatEndpoint.GetCompletionAsync(request);
                string raw = response.FirstChoice.Message.Content?.ToString() ?? string.Empty;

                CultistResponse parsed = ParseResponse(raw);

                ruleEngine.ApplyRules(parsed, session.Stats);
                gameDirector.OnTurnFinished();
                gameDirector.CheckWinCondition(session.Stats);

                session.LastExtractedRegret = parsed.PlayerStoryOrRegret ?? string.Empty;
                session.LastBibleVerse = parsed.BibleVerse ?? string.Empty;

                AddCultistBubble(parsed.CultistComment);
                LogLLMExchange(playerText, userPrompt, raw, parsed);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                AddCultistBubble("...I need a moment.");
                LogSystemState("Error: failed to get cultist response.\n" + e);
            }
            finally
            {
                inputField.interactable = true;
                if (submitButton != null) submitButton.interactable = true;

                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(inputField.gameObject);

                isChatPending = false;
            }
        }

        private string BuildUserPrompt(string playerText)
        {
            string interests = session.Profile.Interests == null || session.Profile.Interests.Count == 0
                ? "None"
                : string.Join(", ", session.Profile.Interests);

            List<CultDoctrineEntry> doctrine = retriever.GetRelevantDoctrine(
                playerText,
                session.LastExtractedRegret,
                session.Stats.Confidence,
                session.Stats.Brainwash,
                session.Stats.Wokeness,
                3
            );

            List<CultTacticEntry> tactics = retriever.GetRelevantTactics(
                playerText,
                session.LastExtractedRegret,
                session.Stats.Confidence,
                session.Stats.Brainwash,
                session.Stats.Wokeness,
                2
            );

            string doctrineBlock = FormatDoctrineBlock(doctrine);
            string tacticBlock = FormatTacticBlock(tactics);

            return
            $@"Player profile:
            Name: {session.Profile.Name}
            Age: {session.Profile.Age}
            Profession: {session.Profile.Profession}
            Interests: {interests}

            Current player stats:
            Confidence: {session.Stats.Confidence}
            Brainwash: {session.Stats.Brainwash}
            Wokeness: {session.Stats.Wokeness}

            Previous extracted regret:
            {session.LastExtractedRegret}

            Retrieved cult doctrine:
            {doctrineBlock}

            Retrieved cult tactics:
            {tacticBlock}

            Player says:
            {playerText}";
        }

        private string FormatDoctrineBlock(List<CultDoctrineEntry> doctrine)
        {
            if (doctrine == null || doctrine.Count == 0)
                return "None";

            List<string> lines = new();

            foreach (var entry in doctrine)
            {
                lines.Add(
                    $"- [{entry.id}] Ref: {entry.reference} | Theme: {entry.theme} | Use: {entry.use_case}\n" +
                    $"  Text: {entry.text}"
                );
            }

            return string.Join("\n", lines);
        }

        private string FormatTacticBlock(List<CultTacticEntry> tactics)
        {
            if (tactics == null || tactics.Count == 0)
                return "None";

            List<string> lines = new();

            foreach (var entry in tactics)
            {
                lines.Add(
                    $"- [{entry.id}] {entry.title}\n" +
                    $"  Description: {entry.description}\n" +
                    $"  Example: {entry.example_line}"
                );
            }

            return string.Join("\n", lines);
        }

        private CultistResponse ParseResponse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return CultistResponse.Default();

            try
            {
                var parsed = JsonConvert.DeserializeObject<CultistResponse>(raw);
                return parsed ?? CultistResponse.Default();
            }
            catch
            {
                string cleaned = ExtractJson(raw);

                try
                {
                    var parsed = JsonConvert.DeserializeObject<CultistResponse>(cleaned);
                    return parsed ?? CultistResponse.Default();
                }
                catch
                {
                    Debug.LogWarning("Failed to parse CultistResponse JSON:\n" + raw);
                    return CultistResponse.Default();
                }
            }
        }

        private string ExtractJson(string raw)
        {
            int start = raw.IndexOf('{');
            int end = raw.LastIndexOf('}');

            if (start >= 0 && end > start)
                return raw.Substring(start, end - start + 1);

            return raw;
        }

        private void AddPlayerBubble(string text)
        {
            if (playerBubbleObject != null)
                playerBubbleObject.SetActive(true);

            if (cultistBubbleObject != null)
                cultistBubbleObject.SetActive(false);

            if (playerBubbleText != null)
                playerBubbleText.text = text;
        }

        private void AddCultistBubble(string text)
        {
            if (cultistBubbleObject != null)
                cultistBubbleObject.SetActive(true);

            if (playerBubbleObject != null)
                playerBubbleObject.SetActive(false);

            if (cultistBubbleText != null)
                cultistBubbleText.text = text;
        }

        private void HideAllBubbles()
        {
            if (playerBubbleObject != null)
                playerBubbleObject.SetActive(false);

            if (cultistBubbleObject != null)
                cultistBubbleObject.SetActive(false);
        }

        private void LogLLMExchange(string playerText, string userPrompt, string raw, CultistResponse parsed)
        {
            var log = new LLMExchangeLog
            {
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                PlayerText = playerText,
                UserPrompt = userPrompt,
                RawResponse = raw,
                ParsedResponse = parsed,
                UpdatedConfidence = session.Stats.Confidence,
                UpdatedBrainwash = session.Stats.Brainwash,
                UpdatedWokeness = session.Stats.Wokeness,
                StoredRegret = session.LastExtractedRegret,
                StoredBibleVerse = session.LastBibleVerse
            };

            exchangeHistory.Add(log);

            if (enableDebug)
            {
                Debug.Log("=== LLM EXCHANGE ===");
                Debug.Log(JsonConvert.SerializeObject(log, Formatting.Indented));
            }

            if (writeLogToFile)
            {
                try
                {
                    File.AppendAllText(
                        LogFilePath,
                        JsonConvert.SerializeObject(log, Formatting.Indented) + Environment.NewLine + Environment.NewLine
                    );
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Failed to write log file: " + e.Message);
                }
            }
        }

        private void LogSystemState(string text)
        {
            if (enableDebug)
                Debug.Log("[CampOfLight] " + text);

            if (writeLogToFile)
            {
                try
                {
                    File.AppendAllText(LogFilePath, "[System] " + text + Environment.NewLine);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Failed to write system log: " + e.Message);
                }
            }
        }

        public void UpdateGameSession(GameSession gameSession)
        {
            session.Profile = gameSession.Profile;
        }

        public PlayerStats GetStats()
        {
            return session != null ? session.Stats : null;
        }

        public GameSession GetSession()
        {
            return session;
        }
    }

    [Serializable]
    public class LLMExchangeLog
    {
        public string Timestamp;
        public string PlayerText;
        public string UserPrompt;
        public string RawResponse;
        public CultistResponse ParsedResponse;
        public int UpdatedConfidence;
        public int UpdatedBrainwash;
        public int UpdatedWokeness;
        public string StoredRegret;
        public string StoredBibleVerse;
    }
}