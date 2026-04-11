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
        currentText = fullText;
        onTypingComplete = onComplete;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(fullText));
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
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        typingCoroutine = null;
        onTypingComplete?.Invoke();
    }

    public void SkipTyping()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;

            textUI.text = currentText;
            textUI.maxVisibleCharacters = currentText.Length;
            isTyping = false;

            onTypingComplete?.Invoke();
        }
    }
}