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
    public abstract class BaseCultChatBehaviour : MonoBehaviour
    {
        [Header("OpenAI")]
        [SerializeField] protected OpenAIConfiguration configuration;
        [SerializeField] protected bool enableDebug = true;
        [SerializeField] protected bool writeLogToFile = false;
        [SerializeField] protected string logFileName = "camp_of_light_log.txt";

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
        [SerializeField] protected CultGameDirector gameDirector;
        [SerializeField] protected RegretSystem regretSystem;
        [SerializeField] protected CultRuleEngine ruleEngine;

        [Header("Knowledge")]
        [SerializeField] protected CultKnowledgeBase knowledgeBase;
        [SerializeField] protected CultRetriever retriever;

        protected OpenAIClient openAI;
        protected static bool isChatPending;

        protected string LogFilePath => Path.Combine(Application.persistentDataPath, logFileName);

        protected virtual void Awake()
        {
            openAI = new OpenAIClient(configuration)
            {
                EnableDebug = enableDebug
            };

            if (session.Profile == null)
                session.Profile = new PlayerProfile();

            if (session.Stats == null)
                session.Stats = new PlayerStats();
        }

        public virtual void Init()
        {
            HideAllBubbles();
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

        protected string ExtractJson(string raw)
        {
            int start = raw.IndexOf('{');
            int end = raw.LastIndexOf('}');

            if (start >= 0 && end > start)
                return raw.Substring(start, end - start + 1);

            return raw;
        }

        protected string FormatDoctrineBlock(List<CultDoctrineEntry> doctrine)
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

        protected void LogSystemState(string text)
        {
            if (enableDebug)
                Debug.Log($"[{GetType().Name}] {text}");

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
    }
}