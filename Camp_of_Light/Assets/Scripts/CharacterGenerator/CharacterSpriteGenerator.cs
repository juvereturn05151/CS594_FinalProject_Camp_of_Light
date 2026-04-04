using System;
using System.Collections;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class CharacterSpriteGenerator : MonoBehaviour
{


    [TextArea(3, 8)]
    [SerializeField]
    private string prompt =
        "Turn this character into a polished full-body 2D game sprite. " +
        "Keep the same character identity, proportions, and clothing style. " +
        "Put the character on a pure solid white background. " +
        "No checkerboard. No transparency preview. No scene. No floor. No shadow.";

    [Header("Input")]
    [SerializeField] private string resourceInputPath = "Character/CharacterTemplate";

    [Header("Output")]
    [SerializeField] private string outputFileName = "CharacterSprite.png";
    [SerializeField] private RuntimeSpriteFromTexture spriteApplier;

    [Header("Generation Control")]
    [SerializeField] private bool generateOnStart = false;

    private const string Url = "https://api.replicate.com/v1/models/google/nano-banana/predictions";
    private string apiToken;
    private bool isGenerating = false;

    private void Start()
    {
        // Load .env (StreamingAssets recommended)
        string envPath = Path.Combine(Application.streamingAssetsPath, "nano-banana.env");
        DotEnv.Load(envPath);

        apiToken = DotEnv.Get("NANO_BANANA_KEY");

        if (string.IsNullOrEmpty(apiToken))
        {
            Debug.LogError("API token not found in .env");
        }
    }

    public void GenerateCharacterSprite()
    {
        if (isGenerating)
        {
            Debug.LogWarning("Generation already in progress.");
            return;
        }

        StartCoroutine(GenerateCharacterSpriteWrapper());
    }

    private IEnumerator GenerateCharacterSpriteWrapper()
    {
        isGenerating = true;
        yield return StartCoroutine(GenerateCharacterSpriteCoroutine());
        isGenerating = false;
    }

    private IEnumerator GenerateCharacterSpriteCoroutine()
    {
        Texture2D sourceTexture = Resources.Load<Texture2D>(resourceInputPath);

        if (sourceTexture == null)
        {
            Debug.LogError("Could not load input texture from Resources: " + resourceInputPath);
            yield break;
        }

        Texture2D readableTexture = MakeTextureReadable(sourceTexture);

        if (readableTexture == null)
        {
            Debug.LogError("Failed to create readable texture.");
            yield break;
        }

        byte[] pngBytes = readableTexture.EncodeToPNG();

        if (pngBytes == null || pngBytes.Length == 0)
        {
            Debug.LogError("Failed to encode readable texture to PNG.");
            Destroy(readableTexture);
            yield break;
        }

        string dataUrl = "data:image/png;base64," + Convert.ToBase64String(pngBytes);

        JObject body = new JObject
        {
            ["input"] = new JObject
            {
                ["prompt"] = prompt,
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
                Destroy(readableTexture);

                string responseText = request.downloadHandler.text;
                Debug.Log("Replicate response: " + responseText);

                string imageUrl = ExtractFirstOutputUrl(responseText);
                if (string.IsNullOrEmpty(imageUrl))
                {
                    Debug.LogError("No image URL found in output.");
                    yield break;
                }

                yield return StartCoroutine(DownloadSaveAndApply(imageUrl));
                yield break;
            }

            string errorText = request.downloadHandler.text;
            Debug.LogError("Replicate request failed: " + request.error);
            Debug.LogError(errorText);

            if (request.responseCode == 429)
            {
                int retryAfterSeconds = GetRetryAfterSeconds(errorText);
                retryAfterSeconds = Mathf.Max(retryAfterSeconds, 10);

                Debug.LogWarning("Rate limited. Retrying in " + retryAfterSeconds + " seconds...");
                yield return new WaitForSeconds(retryAfterSeconds);

                attempt++;
                continue;
            }

            Destroy(readableTexture);
            yield break;
        }

        Destroy(readableTexture);
        Debug.LogError("Replicate request failed after max retries.");
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
        catch (Exception e)
        {
            Debug.LogWarning("Failed to parse retry_after: " + e.Message);
        }

        return 10;
    }

    private IEnumerator DownloadSaveAndApply(string imageUrl)
    {
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download output image: " + request.error);
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(request);

        if (texture == null)
        {
            Debug.LogError("Downloaded texture is null.");
            yield break;
        }

        Texture2D cleanedTexture = RemoveWhiteBackgroundSoft(texture);

        byte[] pngBytes = cleanedTexture.EncodeToPNG();

        string relativeAssetPath = "Assets/Resources/Character/Output/CharacterSprite.png";
        string outputPath = Path.Combine(
            Application.dataPath,
            "Resources/Character/Output/CharacterSprite.png"
        );

        File.WriteAllBytes(outputPath, pngBytes);

        Debug.Log("Saved to: " + outputPath);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.ImportAsset(relativeAssetPath, UnityEditor.ImportAssetOptions.ForceUpdate);

        var importer = UnityEditor.AssetImporter.GetAtPath(relativeAssetPath) as UnityEditor.TextureImporter;
        if (importer != null)
        {
            importer.textureType = UnityEditor.TextureImporterType.Sprite;
            importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
            importer.alphaSource = UnityEditor.TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
#endif

        if (spriteApplier != null)
        {
            spriteApplier.ApplyTexture(cleanedTexture);
        }
    }
}