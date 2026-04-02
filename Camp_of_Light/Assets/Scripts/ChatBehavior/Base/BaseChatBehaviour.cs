using Newtonsoft.Json;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities.Async;
using Utilities.Audio;
using Debug = UnityEngine.Debug;

namespace OpenAI.Samples.Chat
{
    public abstract class BaseChatBehaviour : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] protected Button submitButton;
        [SerializeField] protected TMP_InputField inputField;

        [Header("Bubble UI")]
        [SerializeField] protected GameObject playerBubbleObject;
        [SerializeField] protected TMP_Text playerBubbleText;
        [SerializeField] protected GameObject cultistBubbleObject;
        [SerializeField] protected TMP_Text cultistBubbleText;

        [SerializeField] protected TypewriterText playerTypewriterText;
        [SerializeField] protected TypewriterText cultistTypewriterText;

        [Header("Voice")]
        [SerializeField] protected bool enableVoice = true;
        [SerializeField] protected bool speakCultistLines = true;
        [SerializeField] protected bool speakPlayerLines = false;
        [SerializeField] protected Voice voice;
        [SerializeField] protected StreamAudioSource streamAudioSource;
        [SerializeField] protected bool enableVoiceDebug = false;

        [Header("Game State")]
        [SerializeField] protected GameSession session = new();
        protected CultGameDirector gameDirector;
        protected RegretSystem regretSystem;
        protected RuleEngine ruleEngine;

        [Header("Knowledge")]
        protected CultRetriever retriever;

        protected OpenAIClient openAI;
        protected static bool isChatPending;

        private CancellationTokenSource voiceLifetimeCts;
        protected CancellationToken VoiceCancellationToken => voiceLifetimeCts?.Token ?? CancellationToken.None;

        protected virtual void Awake()
        {
            if (session.Profile == null)
                session.Profile = new PlayerProfile();

            if (session.Stats == null)
                session.Stats = new PlayerStats();

            if (streamAudioSource == null)
                streamAudioSource = GetComponent<StreamAudioSource>();
        }

        protected virtual void OnEnable()
        {
            if (voiceLifetimeCts == null || voiceLifetimeCts.IsCancellationRequested)
                voiceLifetimeCts = new CancellationTokenSource();
        }

        protected virtual void OnDestroy()
        {
            if (voiceLifetimeCts != null)
            {
                voiceLifetimeCts.Cancel();
                voiceLifetimeCts.Dispose();
                voiceLifetimeCts = null;
            }
        }


        public virtual void Init()
        {
            openAI = GameSharedSystem.Instance.OpenAI;
            gameDirector = GameSharedSystem.Instance.GameDirector;
            regretSystem = GameSharedSystem.Instance.RegretSystem;
            ruleEngine = GameSharedSystem.Instance.RuleEngine;
            retriever = GameSharedSystem.Instance.Retriever;
        }

        public virtual void Begin()
        {
            HideAllBubbles();
        }

        public virtual void UpdateGameSession(GameSession gameSession)
        {
            if (gameSession == null)
                return;

            session = gameSession;

            if (session.Profile == null)
                session.Profile = new PlayerProfile();

            if (session.Stats == null)
                session.Stats = new PlayerStats();
        }

        public GameSession GetSession()
        {
            return session;
        }

        protected void AddPlayerBubble(string text)
        {
            if (playerBubbleObject != null)
                playerBubbleObject.SetActive(true);

            if (cultistBubbleObject != null)
                cultistBubbleObject.SetActive(false);

            if (playerTypewriterText != null)
                playerTypewriterText.StartTyping(text);

        }

        protected async void AddCultistBubble(string text)
        {
            if (cultistBubbleObject != null)
                cultistBubbleObject.SetActive(true);

            if (playerBubbleObject != null)
                playerBubbleObject.SetActive(false);

            if (cultistTypewriterText != null)
                cultistTypewriterText.StartTyping(text);

            if (speakCultistLines)
                await GenerateSpeechAsync(text, VoiceCancellationToken);
        }

        protected async Task GenerateSpeechAsync(string text, CancellationToken cancellationToken = default)
        {
            if (!enableVoice || streamAudioSource == null || openAI == null)
                return;

            if (string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                var request = new SpeechRequest(
                    input: text,
                    model: Model.TTS_1,
                    voice: voice,
                    responseFormat: SpeechResponseFormat.PCM);

                var stopwatch = Stopwatch.StartNew();

                using var speechClip = await openAI.AudioEndpoint.GetSpeechAsync(
                    request,
                    async partialClip => await streamAudioSource.SampleCallbackAsync(partialClip.AudioSamples),
                    cancellationToken);

                var playbackTime = speechClip.Length - (float)stopwatch.Elapsed.TotalSeconds + 0.1f;

                if (playbackTime > 0)
                    await Awaiters.DelayAsync(playbackTime, cancellationToken);

                if (enableVoiceDebug)
                    Debug.Log($"Speech cache: {speechClip.CachePath}");
            }
            catch (TaskCanceledException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogError($"Voice generation failed: {e}");
            }
        }


        protected void HideAllBubbles()
        {
            if (playerBubbleObject != null)
                playerBubbleObject.SetActive(false);

            if (cultistBubbleObject != null)
                cultistBubbleObject.SetActive(false);
        }

        protected void SetInputInteractable(bool isInteractable)
        {
            if (inputField != null)
                inputField.interactable = isInteractable;

            if (submitButton != null)
                submitButton.interactable = isInteractable;
        }

        protected virtual void RecordDialogue(string speaker, string text)
        {
            if (GameManager.Instance != null && GameManager.Instance.State != null)
            {
                GameManager.Instance.State.AddDialogue(speaker, text);
            }
        }

        protected virtual void AddAndRecordPlayerBubble(string text)
        {
            AddPlayerBubble(text);
            RecordDialogue("Player", text);
        }

        protected virtual void AddAndRecordCultistBubble(string text)
        {
            AddCultistBubble(text);
            RecordDialogue("Cultist", text);
        }

        protected string ExtractJson(string raw)
        {
            int start = raw.IndexOf('{');
            int end = raw.LastIndexOf('}');

            if (start >= 0 && end > start)
                return raw.Substring(start, end - start + 1);

            return raw;
        }

        protected T DeserializeJsonOrDefault<T>(string raw, Func<T> fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return fallback();

            try
            {
                return JsonConvert.DeserializeObject<T>(raw) ?? fallback();
            }
            catch
            {
                try
                {
                    string cleaned = ExtractJson(raw);
                    return JsonConvert.DeserializeObject<T>(cleaned) ?? fallback();
                }
                catch
                {
                    return fallback();
                }
            }
        }

        protected string FormatDoctrineBlock(List<CultDoctrineEntry> doctrine)
        {
            if (doctrine == null || doctrine.Count == 0)
                return "None";

            List<string> lines = new();

            foreach (var entry in doctrine)
            {
                lines.Add(
                    $"- Verse: {entry.verse} | Meaning: {entry.translation} | Priority: {entry.priority:0.00}\n" +
                    $"  Text: {entry.text}"
                );
            }

            return string.Join("\n", lines);
        }

        protected string FormatTacticBlock(List<CultTacticEntry> tactics)
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

        protected string TrimToLength(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "None";

            value = value.Trim();

            if (value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength) + "...";
        }

        protected string NormalizeKeyPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "none";

            value = value.Trim().ToLowerInvariant();

            if (value.Length > 120)
                value = value.Substring(0, 120);

            return value;
        }
    }
}