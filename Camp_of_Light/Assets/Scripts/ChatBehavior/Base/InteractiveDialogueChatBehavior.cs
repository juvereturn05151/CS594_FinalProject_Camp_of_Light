using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenAI.Samples.Chat
{
    public abstract class InteractiveDialogueChatBehavior : BaseChatBehaviour
    {
        [Header("Interactive Dialogue")]
        [SerializeField] protected bool clearInputAfterSubmit = true;
        [SerializeField] protected bool refocusInputAfterResponse = true;
        [SerializeField] protected bool allowEmptySubmit = false;

        protected override void Awake()
        {
            base.Awake();

            if (submitButton != null)
                submitButton.onClick.AddListener(OnSubmitButtonClicked);

            if (inputField != null)
                inputField.onSubmit.AddListener(OnInputSubmitted);
        }

        protected virtual void OnDestroy()
        {
            if (submitButton != null)
                submitButton.onClick.RemoveListener(OnSubmitButtonClicked);

            if (inputField != null)
                inputField.onSubmit.RemoveListener(OnInputSubmitted);
        }

        public override void Begin()
        {
            base.Begin();

            SetInputInteractable(true);

            if (inputField != null)
                inputField.text = string.Empty;
        }

        private void OnSubmitButtonClicked()
        {
            SubmitChat();
        }

        private void OnInputSubmitted(string _)
        {
            SubmitChat();
        }

        public async void SubmitChat()
        {
            if (isChatPending)
                return;

            if (inputField == null)
                return;

            string playerText = inputField.text?.Trim() ?? string.Empty;

            if (!allowEmptySubmit && string.IsNullOrWhiteSpace(playerText))
                return;

            await SubmitTextAsync(playerText, true);
        }

        public async void InitiateConversation()
        {
            if (isChatPending)
                return;

            string generatedText = BuildInitiationConversation()?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(generatedText))
            {
                Debug.LogWarning($"{GetType().Name}: InitiateConversation generated empty text.");
                return;
            }

            await SubmitTextAsync(generatedText, false);
        }

        private async Task SubmitTextAsync(string text, bool showAsPlayerBubble)
        {
            isChatPending = true;

            try
            {
                if (inputField != null)
                {
                    inputField.ReleaseSelection();
                    SetInputInteractable(false);
                }

                if (showAsPlayerBubble)
                {
                    AddAndRecordPlayerBubble(text);

                    if (clearInputAfterSubmit && inputField != null)
                        inputField.text = string.Empty;
                }
                else
                {
                    Debug.Log($"System-initiated conversation: {text}");
                }

                await ProcessPlayerTurnAsync(text);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                AddCultistBubble("...I need a moment.");
            }
            finally
            {
                SetInputInteractable(true);

                if (refocusInputAfterResponse &&
                    EventSystem.current != null &&
                    inputField != null)
                {
                    EventSystem.current.SetSelectedGameObject(inputField.gameObject);
                }

                isChatPending = false;
            }
        }

        protected abstract string BuildInitiationConversation();
        protected abstract Task ProcessPlayerTurnAsync(string playerText);
    }
}