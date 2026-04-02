using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIImageFadeOut : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private bool playOnStart = true;

    private Coroutine fadeCoroutine;

    private void OnEnable()
    {
        if (playOnStart)
        {
            ResetAlpha(1f);
            StartFadeOut();
        }
    }

    public void StartFadeOut()
    {
        if (targetImage == null)
        {
            Debug.LogWarning("UIImageFadeOut: Target Image is not assigned.");
            return;
        }

        targetImage.gameObject.SetActive(true);

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOutRoutine());
    }

    public void ResetAlpha(float alpha = 1f)
    {
        if (targetImage == null)
            return;

        Color color = targetImage.color;
        color.a = Mathf.Clamp01(alpha);
        targetImage.color = color;
    }

    private IEnumerator FadeOutRoutine()
    {
        Color color = targetImage.color;
        float startAlpha = color.a;

        float holdTime = fadeDuration * 0.5f;
        float fadeTime = fadeDuration * 0.5f;

        // 1. HOLD (stay fully visible)
        float elapsed = 0f;
        while (elapsed < holdTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 2. FAST FADE OUT
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;

            color.a = Mathf.Lerp(startAlpha, 0f, t);
            targetImage.color = color;

            yield return null;
        }

        // Ensure fully invisible
        color.a = 0f;
        targetImage.color = color;

        fadeCoroutine = null;
        targetImage.gameObject.SetActive(false);
    }
}