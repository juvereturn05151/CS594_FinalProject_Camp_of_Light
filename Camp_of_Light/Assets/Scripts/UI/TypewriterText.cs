using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class TypewriterText : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text textUI;

    [Header("Typing Settings")]
    [SerializeField] private float typingSpeed = 0.03f;

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private string currentText;
    private Action onTypingComplete;

    public bool IsTyping => isTyping;

    public void StartTyping(string fullText, Action onComplete = null)
    {
        currentText = fullText ?? string.Empty;
        onTypingComplete = onComplete;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (textUI == null)
        {
            Debug.LogWarning($"{nameof(TypewriterText)} on {name} has no TMP_Text assigned.");
            return;
        }

        typingCoroutine = StartCoroutine(TypeText(currentText));
    }

    private IEnumerator TypeText(string fullText)
    {
        isTyping = true;

        textUI.text = fullText;
        textUI.maxVisibleCharacters = 0;

        int totalChars = fullText.Length;

        for (int i = 0; i <= totalChars; i++)
        {
            textUI.maxVisibleCharacters = i;

            if (i < totalChars)
                yield return new WaitForSecondsRealtime(typingSpeed);
        }

        textUI.maxVisibleCharacters = fullText.Length;

        isTyping = false;
        typingCoroutine = null;
        onTypingComplete?.Invoke();
    }

    public void SkipTyping()
    {
        if (!isTyping)
            return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        textUI.text = currentText;
        textUI.maxVisibleCharacters = currentText.Length;
        isTyping = false;

        onTypingComplete?.Invoke();
    }

    private void OnDisable()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
    }
}