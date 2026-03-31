using Newtonsoft.Json;
using OpenAI.Chat;
using OpenAI.Models;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenAI.Samples.Chat
{
    public class ConscienceChatBehaviour : BaseChatBehaviour
    {
        [Header("Conscience UI")]
        [SerializeField] private GameObject consciencePanel;
        [SerializeField] private TMP_Text promptsRemainingText;

        [SerializeField]
        [TextArea(8, 20)]
        private string conscienceSystemPrompt = @"
            You are the player's conscience.

            You are calm, supportive, reflective, and human.
            You do NOT manipulate, preach doctrine, shame, threaten, or claim absolute truth.
            You help the player reflect on what they feel, especially around regret.

            Focus on:
            - the player's emotional state
            - the regret they carry
            - helping them name what they feel
            - gently encouraging self-understanding
            - helping them separate guilt, fear, and truth

            Keep the response short and natural.

            Return ONLY valid JSON in this exact structure:
            {
              ""IsRelevant"": true,
              ""ReflectedFeeling"": ""string"",
              ""RegretFocus"": ""string"",
              ""ConscienceComment"": ""string"",
              ""ConfidenceDelta"": 0,
              ""BrainwashDelta"": 0,
              ""WokenessDelta"": 0
            }";

        [SerializeField] private GameObject next_Button;

        private Regret currentRegret;

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

            RefreshPromptUI();
        }

        public void ShowReflection(GameRunState state, Regret strongestRegret)
        {
            currentRegret = strongestRegret;

            if (consciencePanel != null)
                consciencePanel.SetActive(true);

            UpdateGameSession(new GameSession
            {
                Profile = state.Profile,
                Stats = state.Stats,
                LastExtractedRegret = state.LastExtractedRegret,
                LastBibleVerse = state.LastBibleVerse
            });

            Init();

            if (state != null)
            {
                state.PromptsUsedToday_Conscience = Mathf.Clamp(
                    state.PromptsUsedToday_Conscience,
                    0,
                    state.MaxPromptsPerDay_Conscience
                );
            }

            AddCultistBubble(BuildOpeningLine());
            RefreshPromptUI();
        }

        public void Hide()
        {
            if (consciencePanel != null)
                consciencePanel.SetActive(false);
        }

        private string BuildOpeningLine()
        {
            if (currentRegret != null && !string.IsNullOrWhiteSpace(currentRegret.Text))
            {
                return $"You keep coming back to \"{currentRegret.Text}\".\n\nWhat hurts the most about it?";
            }

            if (!string.IsNullOrWhiteSpace(session?.LastExtractedRegret))
            {
                return $"You’ve been carrying this: \"{session.LastExtractedRegret}\".\n\nWhat are you feeling right now?";
            }

            return "You’re finally alone with your own thoughts.\n\nWhat are you feeling right now?";
        }

        private void SubmitChat(string _) => SubmitChat();

        public async void SubmitChat()
        {
            if (isChatPending) return;
            if (GameManager.Instance == null || GameManager.Instance.State == null) return;
            if (inputField == null || string.IsNullOrWhiteSpace(inputField.text)) return;

            var state = GameManager.Instance.State;

            if (state.PromptsUsedToday_Conscience >= state.MaxPromptsPerDay_Conscience)
                return;

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
                        new Message(Role.System, conscienceSystemPrompt),
                        new Message(Role.User, userPrompt)
                    },
                    model: Model.GPT5_Mini
                );

                var response = await openAI.ChatEndpoint.GetCompletionAsync(request);
                string raw = response.FirstChoice.Message.Content?.ToString() ?? string.Empty;

                ConscienceResponse parsed = ParseResponse(raw);

                ApplyConscienceEffects(parsed);

                bool isTurnFinished = gameDirector.OnTurnFinished_Conscience();
                next_Button.SetActive(isTurnFinished);

                if (!string.IsNullOrWhiteSpace(parsed.RegretFocus))
                    session.LastExtractedRegret = parsed.RegretFocus;

                AddCultistBubble(parsed.ConscienceComment);

                state.LastExtractedRegret = session.LastExtractedRegret;
                state.AddDialogue("Player", playerText);
                state.AddDialogue("Conscience", parsed.ConscienceComment);

                RefreshPromptUI();
                GameManager.Instance.NotifyCultTurnCompleted();

                if (state.PromptsUsedToday_Conscience >= state.MaxPromptsPerDay_Conscience)
                {
                    if (inputField != null) inputField.interactable = false;
                    if (submitButton != null) submitButton.interactable = false;

                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                AddCultistBubble("...Slow down. Try again. What are you really feeling?");
            }
            finally
            {
                bool canContinue =
                    GameManager.Instance != null &&
                    GameManager.Instance.State != null &&
                    GameManager.Instance.State.PromptsUsedToday_Conscience < GameManager.Instance.State.MaxPromptsPerDay_Conscience;

                if (inputField != null)
                    inputField.interactable = canContinue;

                if (submitButton != null)
                    submitButton.interactable = canContinue;

                if (canContinue && EventSystem.current != null && inputField != null)
                    EventSystem.current.SetSelectedGameObject(inputField.gameObject);

                isChatPending = false;
            }
        }

        public void HackAutoSkip()
        {
            gameDirector.OnHack_Conscience();
            next_Button.SetActive(true);
        }

        private string BuildUserPrompt(string playerText)
        {
            string regretText = currentRegret != null && !string.IsNullOrWhiteSpace(currentRegret.Text)
                ? currentRegret.Text
                : session.LastExtractedRegret;

            int promptsUsed = 0;
            int promptsMax = 5;

            if (GameManager.Instance != null && GameManager.Instance.State != null)
            {
                promptsUsed = GameManager.Instance.State.PromptsUsedToday_Conscience;
                promptsMax = GameManager.Instance.State.MaxPromptsPerDay_Conscience;
            }

            return
$@"Current player stats:
Confidence: {session.Stats.Confidence}
Brainwash: {session.Stats.Brainwash}
Wokeness: {session.Stats.Wokeness}

Current regret focus:
{regretText}

Conscience turn:
{promptsUsed + 1} of {promptsMax}

Player says:
{playerText}

Respond like an inner conscience helping the player reflect on their feelings about this regret.";
        }

        private ConscienceResponse ParseResponse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return ConscienceResponse.Default();

            try
            {
                return JsonConvert.DeserializeObject<ConscienceResponse>(raw) ?? ConscienceResponse.Default();
            }
            catch
            {
                string cleaned = ExtractJson(raw);
                return JsonConvert.DeserializeObject<ConscienceResponse>(cleaned) ?? ConscienceResponse.Default();
            }
        }

        private void ApplyConscienceEffects(ConscienceResponse parsed)
        {
            if (session == null || session.Stats == null || parsed == null)
                return;

            session.Stats.Confidence = Mathf.Clamp(session.Stats.Confidence + parsed.ConfidenceDelta, 0, 100);
            session.Stats.Brainwash = Mathf.Clamp(session.Stats.Brainwash + parsed.BrainwashDelta, 0, 100);
            session.Stats.Wokeness = Mathf.Clamp(session.Stats.Wokeness + parsed.WokenessDelta, 0, 100);
        }

        private void RefreshPromptUI()
        {
            if (promptsRemainingText == null || GameManager.Instance == null || GameManager.Instance.State == null)
                return;

            int remaining = Mathf.Max(
                0,
                GameManager.Instance.State.MaxPromptsPerDay_Conscience -
                GameManager.Instance.State.PromptsUsedToday_Conscience
            );

            promptsRemainingText.text = $"Reflections Remaining: {remaining}";
        }
    }
}