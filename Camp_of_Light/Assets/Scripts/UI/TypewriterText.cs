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

    public void StartTyping(string fullText)
    {
        currentText = fullText;

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
    }

    public void SkipTyping()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            textUI.maxVisibleCharacters = currentText.Length;
            isTyping = false;
        }
    }
}