using Newtonsoft.Json;
using OpenAI.Chat;
using OpenAI.Models;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenAI.Samples.Chat
{
    public class BrainwasherChatBehaviour : BaseChatBehaviour
    {
        [SerializeField]
        [TextArea(8, 20)]
        private string brainwashingSystemPrompt = @"
            You are roleplaying a manipulative cultist from the Only Truth Expedition.

            Stay in character.
            Use the provided doctrine and tactics as your source of truth.

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
            }";

        [SerializeField]
        private GameObject next_Button;

        protected override void Awake()
        {
            base.Awake();

            if (submitButton != null)
                submitButton.onClick.AddListener(SubmitChat);

            if (inputField != null)
                inputField.onSubmit.AddListener(SubmitChat);
        }

        public override void Init()
        {
            base.Init();

            if (inputField != null)
            {
                inputField.interactable = true;
                inputField.text = "";
            }

            if (submitButton != null)
                submitButton.interactable = true;
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
                        new Message(Role.System, brainwashingSystemPrompt),
                        new Message(Role.User, userPrompt)
                    },
                    model: Model.GPT5_Mini
                );

                var response = await openAI.ChatEndpoint.GetCompletionAsync(request);
                string raw = response.FirstChoice.Message.Content?.ToString() ?? string.Empty;

                CultistResponse parsed = ParseResponse(raw);

                ruleEngine.ApplyRules(parsed, session.Stats);
                bool isTurnFinished = gameDirector.OnTurnFinished_Brainwash();
                next_Button.SetActive(isTurnFinished);
                //gameDirector.CheckWinCondition(session.Stats);

                session.LastExtractedRegret = parsed.PlayerStoryOrRegret ?? string.Empty;
                session.LastBibleVerse = parsed.BibleVerse ?? string.Empty;

                AddCultistBubble(parsed.CultistComment);

                if (GameManager.Instance != null && GameManager.Instance.State != null)
                {
                    GameManager.Instance.State.LastExtractedRegret = session.LastExtractedRegret;
                    GameManager.Instance.State.LastBibleVerse = session.LastBibleVerse;
                    GameManager.Instance.State.AddDialogue("Player", playerText);
                    GameManager.Instance.State.AddDialogue("Cultist", parsed.CultistComment);
                    GameManager.Instance.NotifyCultTurnCompleted();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                AddCultistBubble("...I need a moment.");
            }
            finally
            {
                inputField.interactable = true;
                if (submitButton != null) submitButton.interactable = true;

                if (EventSystem.current != null && inputField != null)
                    EventSystem.current.SetSelectedGameObject(inputField.gameObject);

                isChatPending = false;
            }
        }

        public void HackAutoSkip() 
        {
            gameDirector.OnHack_Brainwash();
            next_Button.SetActive(true);
        }

        private string BuildUserPrompt(string playerText)
        {
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

            return
$@"Current player stats:
Confidence: {session.Stats.Confidence}
Brainwash: {session.Stats.Brainwash}
Wokeness: {session.Stats.Wokeness}

Previous extracted regret:
{session.LastExtractedRegret}

Retrieved doctrine:
{FormatDoctrineBlock(doctrine)}

Retrieved tactics:
{FormatTacticBlock(tactics)}

Player says:
{playerText}";
        }

        private CultistResponse ParseResponse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return CultistResponse.Default();

            try
            {
                return JsonConvert.DeserializeObject<CultistResponse>(raw) ?? CultistResponse.Default();
            }
            catch
            {
                string cleaned = ExtractJson(raw);
                return JsonConvert.DeserializeObject<CultistResponse>(cleaned) ?? CultistResponse.Default();
            }
        }
    }
}