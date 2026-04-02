using System.Collections;
using TMPro;
using UnityEngine;

public class StatusChangeFeedbackUI : MonoBehaviour
{
    [Header("Feedback Texts")]
    [SerializeField] private TMP_Text confidenceFeedbackText;
    [SerializeField] private TMP_Text brainwashFeedbackText;
    [SerializeField] private TMP_Text wokenessFeedbackText;

    [Header("Animation")]
    [SerializeField] private float floatDistance = 40f;
    [SerializeField] private float duration = 1.0f;

    private Coroutine confidenceRoutine;
    private Coroutine brainwashRoutine;
    private Coroutine wokenessRoutine;

    public void ShowFeedback(int confidenceDelta, int brainwashDelta, int wokenessDelta)
    {
        if (confidenceDelta != 0)
        {
            if (confidenceRoutine != null) StopCoroutine(confidenceRoutine);
            confidenceRoutine = StartCoroutine(
                PlayFeedback(confidenceFeedbackText, "Confidence", confidenceDelta)
            );
        }

        if (brainwashDelta != 0)
        {
            if (brainwashRoutine != null) StopCoroutine(brainwashRoutine);
            brainwashRoutine = StartCoroutine(
                PlayFeedback(brainwashFeedbackText, "Brainwash", brainwashDelta)
            );
        }

        if (wokenessDelta != 0)
        {
            if (wokenessRoutine != null) StopCoroutine(wokenessRoutine);
            wokenessRoutine = StartCoroutine(
                PlayFeedback(wokenessFeedbackText, "Wokeness", wokenessDelta)
            );
        }
    }

    private IEnumerator PlayFeedback(TMP_Text targetText, string label, int delta)
    {
        if (targetText == null)
            yield break;

        RectTransform rect = targetText.rectTransform;
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = startPos + Vector2.up * floatDistance;

        targetText.gameObject.SetActive(true);

        targetText.text = $"{label}: {(delta > 0 ? "+" : "")}{delta}";

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            Color c = targetText.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            targetText.color = c;

            yield return null;
        }

        rect.anchoredPosition = startPos;
        targetText.gameObject.SetActive(false);
    }
}