using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class CharacterSpriteGenerator : MonoBehaviour
{
    [Header("Reference Image")]
    [SerializeField] private string referenceResourcePath = "Character/CharacterTemplate";

    [Header("Prompt Style - Player Character")]
    [TextArea(3, 10)]
    [SerializeField]
    private string playerStyleSuffix =
        "Use the provided reference image as the pose and composition reference. " +
        "The character must remain in a sitting position like the reference. " +
        "Keep a clean readable 2D game character design. " +
        "Single character only. Pure solid white background. No text. No extra characters.";

    [Header("Prompt Style - Spirit Character")]
    [TextArea(3, 10)]
    [SerializeField]
    private string spiritStyleSuffix =
        "Use the provided reference image as the pose and composition reference. " +
        "The spirit must remain in a sitting position like the reference. " +
        "Create a mystical spirit version with strong visual identity. " +
        "Single character only. Pure solid white background. No text. No extra characters.";

    [Header("Output Folders")]
    [SerializeField] private string playerOutputFolderRelative = "Assets/Resources/Character/CharacterOutput";
    [SerializeField] private string spiritOutputFolderRelative = "Assets/Resources/Character/SpiritOutput";

    private const string Url = "https://api.replicate.com/v1/models/google/nano-banana/predictions";

    private string apiToken;
    private bool isGenerating = false;

    public bool IsGenerating => isGenerating;

    private void Start()
    {
        string envPath = Path.Combine(Application.streamingAssetsPath, "nano-banana.env");
        DotEnv.Load(envPath);

        apiToken = DotEnv.Get("NANO_BANANA_KEY");

        if (string.IsNullOrEmpty(apiToken))
        {
            Debug.LogError("[CharacterSpriteGenerator] API token not found in nano-banana.env");
        }
    }

    public string BuildPlayerPrompt(string playerName, string appearancePrompt)
    {
        string safeName = string.IsNullOrWhiteSpace(playerName) ? "the player" : playerName;
        string look = string.IsNullOrWhiteSpace(appearancePrompt)
            ? "a memorable fantasy-inspired appearance"
            : appearancePrompt.Trim();

        return
            $"Generate a seated player character inspired by {safeName}. " +
            $"The character should look like: {look}. " +
            $"Use the provided template as the body pose reference and keep the sitting posture. " +
            playerStyleSuffix;
    }

    public string BuildSpiritPrompt(string playerName, string appearancePrompt, List<string> interests)
    {
        string safeName = string.IsNullOrWhiteSpace(playerName) ? "the player" : playerName;
        string look = string.IsNullOrWhiteSpace(appearancePrompt)
            ? "a mystical and memorable appearance"
            : appearancePrompt.Trim();

        string interestsText = interests == null || interests.Count == 0
            ? "unknown interests"
            : string.Join(", ", interests);

        return
            $"Generate a seated spirit character inspired by {safeName}. " +
            $"The spirit should visually relate to this appearance: {look}. " +
            $"The spirit must embody these three interests: {interestsText}. " +
            $"Use the provided template as the body pose reference and keep the sitting posture. " +
            spiritStyleSuffix;
    }

    public void GeneratePlayerCharacter(
        string slotId,
        string playerName,
        string appearancePrompt,
        Action<bool, string, Texture2D, string, string> onCompleted)
    {
        if (isGenerating)
        {
            onCompleted?.Invoke(false, null, null, null, "Generation already in progress.");
            return;
        }

        if (string.IsNullOrWhiteSpace(slotId))
        {
            onCompleted?.Invoke(false, null, null, null, "Invalid slot id.");
            return;
        }

        if (string.IsNullOrWhiteSpace(apiToken))
        {
            onCompleted?.Invoke(false, null, null, null, "Nano Banana API token is missing.");
            return;
        }

        string finalPrompt = BuildPlayerPrompt(playerName, appearancePrompt);
        string outputPath = GetPlayerOutputPath(slotId);

        StartCoroutine(GenerateCharacterRoutine(finalPrompt, outputPath, onCompleted));
    }

    public void GenerateSpiritCharacter(
        string slotId,
        string playerName,
        string appearancePrompt,
        List<string> interests,
        Action<bool, string, Texture2D, string, string> onCompleted)
    {
        if (isGenerating)
        {
            onCompleted?.Invoke(false, null, null, null, "Generation already in progress.");
            return;
        }

        if (string.IsNullOrWhiteSpace(slotId))
        {
            onCompleted?.Invoke(false, null, null, null, "Invalid slot id.");
            return;
        }

        if (string.IsNullOrWhiteSpace(apiToken))
        {
            onCompleted?.Invoke(false, null, null, null, "Nano Banana API token is missing.");
            return;
        }

        if (interests == null || interests.Count != 3)
        {
            onCompleted?.Invoke(false, null, null, null, "Exactly 3 interests are required.");
            return;
        }

        string finalPrompt = BuildSpiritPrompt(playerName, appearancePrompt, interests);
        string outputPath = GetSpiritOutputPath(slotId);

        StartCoroutine(GenerateCharacterRoutine(finalPrompt, outputPath, onCompleted));
    }

    private IEnumerator GenerateCharacterRoutine(
        string finalPrompt,
        string outputPath,
        Action<bool, string, Texture2D, string, string> onCompleted)
    {
        isGenerating = true;

        Texture2D referenceTexture = Resources.Load<Texture2D>(referenceResourcePath);
        if (referenceTexture == null)
        {
            isGenerating = false;
            onCompleted?.Invoke(false, null, null, null,
                $"Could not load reference texture from Resources/{referenceResourcePath}");
            yield break;
        }

        Texture2D readableReference = MakeTextureReadable(referenceTexture);
        byte[] pngBytes = readableReference.EncodeToPNG();

        if (pngBytes == null || pngBytes.Length == 0)
        {
            Destroy(readableReference);
            isGenerating = false;
            onCompleted?.Invoke(false, null, null, null, "Failed to encode reference image.");
            yield break;
        }

        string dataUrl = "data:image/png;base64," + Convert.ToBase64String(pngBytes);

        string outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        JObject body = new JObject
        {
            ["input"] = new JObject
            {
                ["prompt"] = finalPrompt,
                ["image_input"] = new JArray { dataUrl }
            }
        };

        byte[] bodyRaw = Encoding.UTF8.GetBytes(body.ToString());

        int maxRetries = 3;
        int attempt = 0;

        while (attempt < maxRetries)
        {
            using UnityWebRequest request = new UnityWebRequest(Url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Authorization", "Bearer " + apiToken);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Prefer", "wait");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Destroy(readableReference);

                string responseText = request.downloadHandler.text;
                string imageUrl = ExtractFirstOutputUrl(responseText);

                if (string.IsNullOrEmpty(imageUrl))
                {
                    isGenerating = false;
                    onCompleted?.Invoke(false, null, null, null, "No image URL returned.");
                    yield break;
                }

                yield return StartCoroutine(
                    DownloadAndSaveGeneratedImage(imageUrl, outputPath, finalPrompt, onCompleted));
                yield break;
            }

            string errorText = request.downloadHandler.text;

            if (request.responseCode == 429)
            {
                int retryAfterSeconds = GetRetryAfterSeconds(errorText);
                retryAfterSeconds = Mathf.Max(retryAfterSeconds, 10);
                yield return new WaitForSeconds(retryAfterSeconds);
                attempt++;
                continue;
            }

            Destroy(readableReference);
            isGenerating = false;
            onCompleted?.Invoke(false, null, null, null, request.error);
            yield break;
        }

        Destroy(readableReference);
        isGenerating = false;
        onCompleted?.Invoke(false, null, null, null, "Generation failed after maximum retries.");
    }

    private IEnumerator DownloadAndSaveGeneratedImage(
        string imageUrl,
        string outputPath,
        string finalPrompt,
        Action<bool, string, Texture2D, string, string> onCompleted)
    {
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            isGenerating = false;
            onCompleted?.Invoke(false, null, null, null, "Failed to download generated image.");
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(request);

        if (texture == null)
        {
            isGenerating = false;
            onCompleted?.Invoke(false, null, null, null, "Downloaded texture is null.");
            yield break;
        }

        Texture2D cleanedTexture = RemoveWhiteBackgroundSoft(texture);
        byte[] pngBytes = cleanedTexture.EncodeToPNG();
        File.WriteAllBytes(outputPath, pngBytes);

#if UNITY_EDITOR
        string assetPath = ToAssetPath(outputPath);
        if (!string.IsNullOrWhiteSpace(assetPath))
        {
            UnityEditor.AssetDatabase.ImportAsset(assetPath, UnityEditor.ImportAssetOptions.ForceUpdate);

            var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
            if (importer != null)
            {
                importer.textureType = UnityEditor.TextureImporterType.Sprite;
                importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
                importer.alphaSource = UnityEditor.TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }
#endif

        isGenerating = false;
        onCompleted?.Invoke(true, outputPath, cleanedTexture, finalPrompt, null);
    }

    public string GetPlayerOutputPath(string slotId)
    {
#if UNITY_EDITOR
        Directory.CreateDirectory(playerOutputFolderRelative);
        return Path.Combine(playerOutputFolderRelative, $"{slotId}_player.png");
#else
        string folder = Path.Combine(Application.persistentDataPath, "CharacterOutput");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, $"{slotId}_player.png");
#endif
    }

    public string GetSpiritOutputPath(string slotId)
    {
#if UNITY_EDITOR
        Directory.CreateDirectory(spiritOutputFolderRelative);
        return Path.Combine(spiritOutputFolderRelative, $"{slotId}_spirit.png");
#else
        string folder = Path.Combine(Application.persistentDataPath, "SpiritOutput");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, $"{slotId}_spirit.png");
#endif
    }

    private string ToAssetPath(string fullOrRelativePath)
    {
        string normalized = fullOrRelativePath.Replace("\\", "/");

        if (normalized.StartsWith("Assets/"))
            return normalized;

        string dataPath = Application.dataPath.Replace("\\", "/");
        if (normalized.StartsWith(dataPath))
        {
            return "Assets" + normalized.Substring(dataPath.Length);
        }

        return null;
    }

    private Texture2D MakeTextureReadable(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear
        );

        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readable.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return readable;
    }

    private Texture2D RemoveWhiteBackgroundSoft(Texture2D source, float startThreshold = 0.88f, float endThreshold = 0.98f)
    {
        Texture2D readable = MakeTextureReadable(source);

        int width = readable.width;
        int height = readable.height;
        Color[] pixels = readable.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            float brightness = (c.r + c.g + c.b) / 3f;

            float alpha = 1f;

            if (brightness >= endThreshold)
            {
                alpha = 0f;
            }
            else if (brightness > startThreshold)
            {
                alpha = 1f - Mathf.InverseLerp(startThreshold, endThreshold, brightness);
            }

            pixels[i] = new Color(c.r, c.g, c.b, alpha);
        }

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.SetPixels(pixels);
        result.Apply();

        Destroy(readable);
        return result;
    }

    private string ExtractFirstOutputUrl(string json)
    {
        JObject obj = JObject.Parse(json);
        JToken output = obj["output"];

        if (output == null || output.Type == JTokenType.Null)
            return null;

        if (output.Type == JTokenType.String)
            return output.ToString();

        if (output.Type == JTokenType.Array && output.HasValues)
            return output[0]?.ToString();

        if (output.Type == JTokenType.Object)
        {
            return output["url"]?.ToString()
                ?? output["image"]?.ToString()
                ?? output["src"]?.ToString();
        }

        return null;
    }

    private int GetRetryAfterSeconds(string json)
    {
        try
        {
            JObject obj = JObject.Parse(json);
            JToken retryToken = obj["retry_after"];

            if (retryToken != null && int.TryParse(retryToken.ToString(), out int seconds))
            {
                return seconds;
            }
        }
        catch (Exception)
        {
        }

        return 10;
    }
}