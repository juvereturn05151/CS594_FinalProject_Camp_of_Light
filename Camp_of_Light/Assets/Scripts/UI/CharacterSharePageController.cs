using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSharePageController : MonoBehaviour
{
    [Header("Profile Display")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text interestsText;

    [SerializeField] private Image playerImage;
    [SerializeField] private Image spiritImage;

    [Header("Optional")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject rootPageObject;

    [Header("Share")]
    [SerializeField] private string screenshotFilePrefix = "OnlyTruthExpedition_Share";
    [SerializeField]
    private string shareMessageTemplate =
        "Meet my character and spirit from Only Truth Expedition. #indiegame #gamedev #unity";

    private string lastScreenshotPath;

    // Preview data injected by ProfileCreationController
    private PlayerProfile previewProfile;
    private bool hasPreviewProfile = false;

    private void OnEnable()
    {
        RefreshPage();
    }

    public void SetPreviewProfile(
        string playerName,
        List<string> interests,
        string playerCharacterImagePath,
        string spiritCharacterImagePath,
        string characterAppearancePrompt = "",
        string spiritCharacterPrompt = "")
    {
        previewProfile = new PlayerProfile
        {
            Name = playerName ?? string.Empty,
            CharacterAppearancePrompt = characterAppearancePrompt ?? string.Empty,
            PlayerCharacterImagePath = playerCharacterImagePath ?? string.Empty,
            Interests = interests != null ? new List<string>(interests) : new List<string>(),
            SpiritCharacterPrompt = spiritCharacterPrompt ?? string.Empty,
            SpiritCharacterImagePath = spiritCharacterImagePath ?? string.Empty
        };

        hasPreviewProfile = true;
        RefreshPage();
    }

    public void ClearPreviewProfile()
    {
        previewProfile = null;
        hasPreviewProfile = false;
        RefreshPage();
    }

    public void RefreshPage()
    {
        PlayerProfile profile = GetCurrentProfile();
        if (profile == null)
        {
            SetStatus("No profile data found.");
            ClearVisuals();
            return;
        }

        if (playerNameText != null)
            playerNameText.text = string.IsNullOrWhiteSpace(profile.Name) ? "Unknown Player" : profile.Name;

        if (interestsText != null)
        {
            string interests = (profile.Interests != null && profile.Interests.Count > 0)
                ? string.Join(", ", profile.Interests)
                : "No interests set";

            interestsText.text = $"Interests: {interests}";
        }

        LoadImageInto(profile.PlayerCharacterImagePath, playerImage);
        LoadImageInto(profile.SpiritCharacterImagePath, spiritImage);

        SetStatus("Ready to share.");
    }

    public void OnClickSaveScreenshot()
    {
        StartCoroutine(CaptureScreenshotRoutine(false, null));
    }

    public void OnClickShareX()
    {
        StartCoroutine(CaptureScreenshotRoutine(true, ShareToX));
    }

    public void OnClickShareFacebook()
    {
        StartCoroutine(CaptureScreenshotRoutine(true, ShareToFacebook));
    }

    public void OnClickShareLinkedIn()
    {
        StartCoroutine(CaptureScreenshotRoutine(true, ShareToLinkedIn));
    }

    public void OnClickOpenScreenshotFolder()
    {
        string folder = Path.Combine(Application.persistentDataPath, "SharedCaptures");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        Application.OpenURL("file://" + folder.Replace("\\", "/"));
    }

    private IEnumerator CaptureScreenshotRoutine(bool openShareAfter, Action<string> shareAction)
    {
        SetStatus("Capturing screenshot...");

        yield return new WaitForEndOfFrame();

        string folder = Path.Combine(Application.persistentDataPath, "SharedCaptures");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"{screenshotFilePrefix}_{timestamp}.png";
        string fullPath = Path.Combine(folder, fileName);

        ScreenCapture.CaptureScreenshot(fullPath);

        float timeout = 2f;
        float elapsed = 0f;

        while (!File.Exists(fullPath) && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        lastScreenshotPath = fullPath;

        if (File.Exists(fullPath))
        {
            SetStatus($"Saved screenshot:\n{fullPath}");

            if (openShareAfter && shareAction != null)
                shareAction(fullPath);
        }
        else
        {
            SetStatus("Failed to save screenshot.");
        }
    }

    private void ShareToX(string imagePath)
    {
        string text = BuildShareMessage();
        string url = "https://twitter.com/intent/tweet?text=" + Uri.EscapeDataString(text);
        Application.OpenURL(url);

        SetStatus(
            "Opened X share page.\n" +
            "Your screenshot was saved locally. Attach it manually to the post."
        );
    }

    private void ShareToFacebook(string imagePath)
    {
        Application.OpenURL("https://www.facebook.com/");
        SetStatus(
            "Opened Facebook.\n" +
            "Your screenshot was saved locally. Create a post and attach the image manually."
        );
    }

    private void ShareToLinkedIn(string imagePath)
    {
        string text = BuildShareMessage();
        Application.OpenURL("https://www.linkedin.com/feed/");

        SetStatus(
            "Opened LinkedIn.\n" +
            "Suggested post text copied below in the status. Attach the saved screenshot manually.\n\n" +
            text
        );
    }

    private string BuildShareMessage()
    {
        PlayerProfile profile = GetCurrentProfile();

        string playerName = profile != null && !string.IsNullOrWhiteSpace(profile.Name)
            ? profile.Name
            : "my character";

        string interests = (profile != null && profile.Interests != null && profile.Interests.Count > 0)
            ? string.Join(", ", profile.Interests)
            : "mystery, reflection, and identity";

        StringBuilder sb = new StringBuilder();
        sb.Append($"Meet {playerName} and their spirit from Only Truth Expedition. ");
        sb.Append($"Interests: {interests}. ");
        sb.Append(shareMessageTemplate);

        return sb.ToString();
    }

    private PlayerProfile GetCurrentProfile()
    {
        if (hasPreviewProfile && previewProfile != null)
            return previewProfile;

        if (GameManager.Instance != null &&
            GameManager.Instance.State != null &&
            GameManager.Instance.State.Profile != null)
        {
            return GameManager.Instance.State.Profile;
        }

        if (GameSharedSystem.Instance != null &&
            GameSharedSystem.Instance.Session != null &&
            GameSharedSystem.Instance.Session.Profile != null)
        {
            return GameSharedSystem.Instance.Session.Profile;
        }

        return null;
    }

    private void LoadImageInto(string imagePath, Image targetImage)
    {
        if (targetImage == null)
            return;

        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            targetImage.sprite = null;
            targetImage.enabled = false;
            return;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(bytes);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(1.0f, 1.0f)
            );

            targetImage.sprite = sprite;
            targetImage.enabled = true;
            targetImage.preserveAspect = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load image from path '{imagePath}': {e.Message}");
            targetImage.sprite = null;
            targetImage.enabled = false;
        }
    }

    private void ClearVisuals()
    {
        if (playerNameText != null)
            playerNameText.text = "Unknown Player";

        if (interestsText != null)
            interestsText.text = "Interests: None";

        if (playerImage != null)
        {
            playerImage.sprite = null;
            playerImage.enabled = false;
        }

        if (spiritImage != null)
        {
            spiritImage.sprite = null;
            spiritImage.enabled = false;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    public string GetLastScreenshotPath()
    {
        return lastScreenshotPath;
    }
}