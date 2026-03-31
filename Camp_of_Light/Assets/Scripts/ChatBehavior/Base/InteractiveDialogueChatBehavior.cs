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

        public override void Init()
        {
            base.Init();

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

            isChatPending = true;

            try
            {
                inputField.ReleaseSelection();
                SetInputInteractable(false);

                AddAndRecordPlayerBubble(playerText);

                if (clearInputAfterSubmit && inputField != null)
                    inputField.text = string.Empty;

                await ProcessPlayerTurnAsync(playerText);
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

        protected abstract Task ProcessPlayerTurnAsync(string playerText);
    }
}