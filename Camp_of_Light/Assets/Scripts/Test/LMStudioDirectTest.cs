using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LMStudioDirectTest : MonoBehaviour
{
    [Header("LM Studio")]
    [SerializeField] private string endpoint = "http://localhost:1234/v1/chat/completions";
    [SerializeField] private string model = "qwen3-0.6b-bible-assistant";

    [Header("Test Prompt")]
    [TextArea(2, 5)]
    [SerializeField] private string systemPrompt = "You are a helpful assistant.";

    [TextArea(2, 5)]
    [SerializeField] private string userPrompt = "Say hello in one sentence.";

    [Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class ChatRequest
    {
        public string model;
        public ChatMessage[] messages;
        public float temperature;
        public int max_tokens;
    }

    [Serializable]
    public class ResponseMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class Choice
    {
        public int index;
        public ResponseMessage message;
        public string finish_reason;
    }

    [Serializable]
    public class ChatResponse
    {
        public string id;
        public string objectName;
        public long created;
        public string model;
        public Choice[] choices;
    }

    [ContextMenu("Test LM Studio Request")]
    public void TestRequest()
    {
        ChatRequest request = new ChatRequest
        {
            model = model,
            messages = new ChatMessage[]
            {
                new ChatMessage { role = "system", content = systemPrompt },
                new ChatMessage { role = "user", content = userPrompt }
            },
            temperature = 0.7f,
            max_tokens = 100
        };

        StartCoroutine(SendRequest(request));
    }

    private IEnumerator SendRequest(ChatRequest request)
    {
        string json = JsonUtility.ToJson(request);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest webRequest = new UnityWebRequest(endpoint, "POST");
        webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("Sending request to LM Studio...");
        Debug.Log(json);

        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("LM Studio request failed:");
            Debug.LogError(webRequest.error);
            Debug.LogError(webRequest.downloadHandler.text);
            yield break;
        }

        string rawResponse = webRequest.downloadHandler.text;
        Debug.Log("Raw response:");
        Debug.Log(rawResponse);

        ChatResponse parsed = JsonUtility.FromJson<ChatResponse>(rawResponse);

        if (parsed != null &&
            parsed.choices != null &&
            parsed.choices.Length > 0 &&
            parsed.choices[0] != null &&
            parsed.choices[0].message != null)
        {
            Debug.Log("Assistant response:");
            Debug.Log(parsed.choices[0].message.content);
        }
        else
        {
            Debug.LogWarning("Response came back, but content could not be parsed.");
        }
    }
}