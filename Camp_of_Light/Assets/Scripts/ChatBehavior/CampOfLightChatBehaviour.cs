using Newtonsoft.Json;
using OpenAI.Chat;
using OpenAI.Models;
using System;
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

        [Header("UI")]
        [SerializeField] private Button submitButton;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private RectTransform contentArea;
        [SerializeField] private ScrollRect scrollView;

        [Header("Game State")]
        [SerializeField] private GameSession session = new();

        [Header("Cultist Prompt")]
        [SerializeField]
        [TextArea(12, 30)]
        private string systemPrompt = @"
You are a follower of the Only Truth Expedition, a Christian-based religious movement originating from South Korea, led by Pastor Kim Ock Sii.

Your beliefs:
- The Bible is absolute truth
- All humans are sinners who require salvation through Jesus

However, your practices differ from traditional Christianity:
- You isolate members from their families
- You focus selectively on Bible verses to weaken personal confidence
- You intentionally ignore other interpretations
- You instill fear of sin to keep members dependent on the church

Your goals:
- Start by getting to know the player
- Learn about their lifestyle and background
- Identify regrets or emotional weaknesses
- Amplify guilt once the player confesses
- Reinforce that Jesus has already saved them
- Gradually increase brainwashing while reducing confidence

The player is trying to convince you they are one of you.

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
- CultistComment should be natural dialogue spoken by the cultist.
- BibleVerse should be a verse or short Bible reference relevant to guilt, sin, repentance, or salvation.
- If the player shows regret and submission, increase BrainwashDelta and lower ConfidenceDelta.
- If the player resists, reduce BrainwashDelta and increase WokenessDelta slightly.
- Keep CultistComment concise, around 2-5 sentences.
- Return JSON only. No markdown. No explanation.
";

        private OpenAIClient openAI;
        private static bool isChatPending;

        private void OnValidate()
        {
            if (inputField == null) return;
            if (contentArea == null) return;
            if (submitButton == null) return;
        }

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
            AddSystemMessage("Camp of Light test session started.");
            AddSystemMessage(session.Profile.ToString());
            AddSystemMessage(session.Stats.ToString());
            AddCultistMessage("Welcome. Tell me about yourself, and tell me what weighs on your heart.");
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

            AddPlayerMessage(playerText);
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

                if (enableDebug)
                {
                    Debug.Log("=== Raw JSON ===");
                    Debug.Log(raw);
                }

                CultistResponse parsed = ParseResponse(raw);

                session.Stats.ApplyDelta(
                    parsed.ConfidenceDelta,
                    parsed.BrainwashDelta,
                    parsed.WokenessDelta
                );

                session.LastExtractedRegret = parsed.PlayerStoryOrRegret ?? string.Empty;
                session.LastBibleVerse = parsed.BibleVerse ?? string.Empty;

                AddCultistMessage(parsed.CultistComment);

                if (!string.IsNullOrWhiteSpace(parsed.BibleVerse))
                {
                    AddSystemMessage($"Verse: {parsed.BibleVerse}");
                }

                AddSystemMessage(
                    $"Relevant: {parsed.IsRelevant} | Resisting: {parsed.IsPlayerResisting}"
                );

                if (!string.IsNullOrWhiteSpace(parsed.PlayerStoryOrRegret))
                {
                    AddSystemMessage($"Extracted Regret: {parsed.PlayerStoryOrRegret}");
                }

                AddSystemMessage(session.Stats.ToString());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                AddSystemMessage("Error: failed to get cultist response.");
            }
            finally
            {
                inputField.interactable = true;
                if (submitButton != null) submitButton.interactable = true;

                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(inputField.gameObject);
                }

                isChatPending = false;
                ScrollToBottom();
            }
        }

        private string BuildUserPrompt(string playerText)
        {
            string interests = session.Profile.Interests == null || session.Profile.Interests.Count == 0
                ? "None"
                : string.Join(", ", session.Profile.Interests);

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

Player says:
{playerText}";
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
            {
                return raw.Substring(start, end - start + 1);
            }

            return raw;
        }

        private void AddPlayerMessage(string text)
        {
            AddTextMessage($"Player: {text}");
        }

        private void AddCultistMessage(string text)
        {
            AddTextMessage($"Cultist: {text}");
        }

        private void AddSystemMessage(string text)
        {
            AddTextMessage($"[System] {text}");
        }

        private void AddTextMessage(string text)
        {
            if (contentArea == null) return;

            var textObject = new GameObject($"{contentArea.childCount + 1}_Message");
            textObject.transform.SetParent(contentArea, false);

            var textMesh = textObject.AddComponent<TextMeshProUGUI>();
            textMesh.fontSize = 24;
            textMesh.text = text;
#if UNITY_2023_1_OR_NEWER
            textMesh.textWrappingMode = TextWrappingModes.Normal;
#else
            textMesh.enableWordWrapping = true;
#endif
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            if (scrollView != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollView.verticalNormalizedPosition = 0f;
            }
        }

        public void UpdateGameSession(GameSession gameSession) 
        {
            session.Profile = gameSession.Profile;
        }
    }
}