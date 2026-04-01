using Newtonsoft.Json;
using OpenAI.Chat;
using OpenAI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

        [Header("Game State")]
        [SerializeField] protected GameSession session = new();
        protected CultGameDirector gameDirector;
        protected RegretSystem regretSystem;
        protected RuleEngine ruleEngine;

        [Header("Knowledge")]
        protected CultRetriever retriever;

        protected OpenAIClient openAI;
        protected static bool isChatPending;

        protected virtual void Awake()
        {
            if (session.Profile == null)
                session.Profile = new PlayerProfile();

            if (session.Stats == null)
                session.Stats = new PlayerStats();
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

            if (playerBubbleText != null)
                playerBubbleText.text = text;
        }

        protected void AddCultistBubble(string text)
        {
            if (cultistBubbleObject != null)
                cultistBubbleObject.SetActive(true);

            if (playerBubbleObject != null)
                playerBubbleObject.SetActive(false);

            if (cultistBubbleText != null)
                cultistBubbleText.text = text;
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