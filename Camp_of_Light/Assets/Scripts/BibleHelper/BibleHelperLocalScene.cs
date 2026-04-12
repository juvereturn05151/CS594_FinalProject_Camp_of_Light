using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LLama;
using LLama.Common;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class BibleConversation
{
    public string user_story;
    public string verse;
    public string explanation;
    public List<string> steps;
    public string closing;
}

public class BibleHelperLocalScene : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private string modelRelativePath = "models/module6_bible_helper.gguf";
    [SerializeField] private int contextSize = 4096;
    [SerializeField] private int gpuLayerCount = 35;

    [Header("Prompt")]
    [TextArea(8, 20)]
    [SerializeField]
    private string systemPrompt =
@"You are Bible Helper, a local AI assistant focused on the Bible.

Rules:
- Answer questions about the Bible clearly, respectfully, and faithfully.
- When possible, include Bible verse references.
- If you are unsure, say so clearly.
- Do not invent verses or pretend certainty.
- Keep answers helpful and easy to understand.
- Keep the answer short.
- Answer in 1 to 2 short sentences only.
- Keep the visible response compact for a small game UI.

Style:
- Calm
- Respectful
- Concise";

    [Header("UI")]
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TMP_Text statusText;

    [Header("Scene")]
    [SerializeField] private string backSceneName = "MainMenu";

    [Header("Behavior")]
    [SerializeField] private bool submitOnEnter = true;
    [SerializeField] private bool autoScroll = true;
    [SerializeField] private bool showDebugLogs = true;

    [Header("Output Limits")]
    [SerializeField] private int maxVisibleCharacters = 180;
    [SerializeField] private int maxTokens = 80;

    private LLamaWeights _model;
    private LLamaContext _context;
    private InteractiveExecutor _executor;
    private ChatSession _chatSession;

    private CancellationTokenSource _lifetimeCts;
    private CancellationTokenSource _generationCts;

    private bool _isModelReady;
    private bool _isGenerating;

    private string _currentResponse = string.Empty;

    private void Awake()
    {
        _lifetimeCts = new CancellationTokenSource();

        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmitPressed);

        if (clearButton != null)
            clearButton.onClick.AddListener(OnClearPressed);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackPressed);

        if (stopButton != null)
            stopButton.onClick.AddListener(OnStopPressed);

        if (inputField != null && submitOnEnter)
            inputField.onSubmit.AddListener(OnInputSubmitted);

        if (outputText != null)
            outputText.text = string.Empty;

        SetStatus("Loading local model...");
        SetInteractable(false);
    }

    private async void Start()
    {
        await InitializeAsync(_lifetimeCts.Token);
    }

    private async UniTask InitializeAsync(CancellationToken token)
    {
        try
        {
            string fullModelPath = Path.Combine(Application.streamingAssetsPath, modelRelativePath);

            if (showDebugLogs)
                Debug.Log($"[BibleHelper] Loading model from: {fullModelPath}");

            if (!File.Exists(fullModelPath))
            {
                SetStatus($"Model not found:\n{fullModelPath}");
                Debug.LogError($"[BibleHelper] Model file not found: {fullModelPath}");
                return;
            }

            var parameters = new ModelParams(fullModelPath)
            {
                ContextSize = (uint)contextSize,
                GpuLayerCount = gpuLayerCount
            };

            var loaded = await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();

                var model = LLamaWeights.LoadFromFile(parameters);
                var context = model.CreateContext(parameters);
                var executor = new InteractiveExecutor(context);
                var session = new ChatSession(executor);
                session.AddSystemMessage(systemPrompt);

                return new LoadedLlamaState
                {
                    Model = model,
                    Context = context,
                    Executor = executor,
                    Session = session
                };
            }, token);

            if (token.IsCancellationRequested)
            {
                loaded?.Dispose();
                return;
            }

            _model = loaded.Model;
            _context = loaded.Context;
            _executor = loaded.Executor;
            _chatSession = loaded.Session;

            _isModelReady = true;

            SetStatus("Bible Helper is ready.");
            SetInteractable(true);
            SetOutputText("Please tell me your issue.");

            if (showDebugLogs)
                Debug.Log("[BibleHelper] Model initialized successfully.");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Loading canceled.");
            Debug.LogWarning("[BibleHelper] Initialization canceled.");
        }
        catch (Exception e)
        {
            SetStatus("Failed to load local model.");
            Debug.LogError($"[BibleHelper] Initialization failed: {e}");
        }
    }

    private void OnInputSubmitted(string _)
    {
        if (submitOnEnter)
            OnSubmitPressed();
    }

    public async void OnSubmitPressed()
    {
        if (!_isModelReady || _isGenerating)
            return;

        if (inputField == null)
            return;

        string userMessage = inputField.text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userMessage))
            return;

        inputField.text = string.Empty;
        FocusInput();

        if (showDebugLogs)
            Debug.Log($"[BibleHelper] User Input: {userMessage}");

        await SendMessageAsync(userMessage, _lifetimeCts.Token);
    }

    public void OnClearPressed()
    {
        if (_isGenerating)
            return;

        if (_chatSession != null)
        {
            _chatSession.History.Messages.Clear();
            _chatSession.AddSystemMessage(systemPrompt);
        }

        _currentResponse = string.Empty;
        SetOutputText("Please tell me your issue.");
        SetStatus("Chat cleared.");
        FocusInput();

        if (showDebugLogs)
            Debug.Log("[BibleHelper] Chat cleared.");
    }

    public void OnBackPressed()
    {
        if (_isGenerating)
            StopCurrentGeneration();

        CleanupModel();
        SceneManager.LoadScene(backSceneName);
    }

    public void OnStopPressed()
    {
        StopCurrentGeneration();
    }

    private async UniTask SendMessageAsync(string userMessage, CancellationToken lifetimeToken)
    {
        if (_chatSession == null)
            return;

        _isGenerating = true;
        SetInteractable(false);
        SetStatus("Generating response...");

        StopCurrentGeneration();
        _generationCts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken);
        CancellationToken genToken = _generationCts.Token;

        _currentResponse = string.Empty;
        bool receivedAnyToken = false;

        string severity = "moderate"; // or dynamic later
        string prompt = BuildBiblePrompt(userMessage, severity);

        try
        {
            var inferenceParams = new InferenceParams
            {
                AntiPrompts = new List<string>
                    {
                        "User:",
                        "Bible Helper:",
                        "Assistant:",
                        "\n\n",     // stop after paragraph
                        "{"         // stop JSON restart loop
                    },
            };

            if (showDebugLogs)
                Debug.Log($"[BibleHelper] Starting generation. MaxTokens={maxTokens}, MaxVisibleChars={maxVisibleCharacters}");

            await foreach (var token in ChatConcurrent(
                _chatSession.ChatAsync(
                    new ChatHistory.Message(AuthorRole.User, prompt),
                    inferenceParams
                ),
                genToken
            ))
            {
                if (genToken.IsCancellationRequested)
                    break;

                receivedAnyToken = true;
                _currentResponse += token;

                // HARD STOP 1: UI length limit
                if (_currentResponse.Length > maxVisibleCharacters)
                {
                    Debug.LogWarning("[BibleHelper] Force stop: exceeded UI limit");

                    StopCurrentGeneration(); // cancel stream
                    break;
                }

                // HARD STOP 2: repetition detection
                if (IsRepeating(_currentResponse))
                {
                    Debug.LogWarning("[BibleHelper] Force stop: repetition detected");

                    StopCurrentGeneration(); // cancel stream
                    break;
                }

                // Update UI AFTER checks
                SetOutputText(ClampText(_currentResponse, maxVisibleCharacters));

                await UniTask.Yield();
            }

            if (!receivedAnyToken)
            {
                Debug.LogWarning("[BibleHelper] No tokens received from model.");
                SetOutputText("I couldn't generate a response. Please try again.");
                SetStatus("No response generated.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_currentResponse))
            {
                Debug.LogWarning("[BibleHelper] Response is empty after generation.");
                SetOutputText("No meaningful response generated.");
                SetStatus("Empty response.");
                return;
            }

            var parsed = TryParse(_currentResponse);

            if (parsed != null)
            {
                if (showDebugLogs)
                    Debug.Log("[BibleHelper] Parsed structured response successfully.");

                SetOutputText(FormatShort(parsed));
            }
            else
            {
                Debug.LogWarning("[BibleHelper] Failed to parse structured output. Using fallback.");

                SetOutputText(ClampText(_currentResponse, maxVisibleCharacters));
            }
            SetStatus("Ready.");
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("[BibleHelper] Generation stopped or canceled.");
            SetStatus("Generation stopped.");
        }
        catch (Exception e)
        {
            Debug.LogError("[BibleHelper] Generation exception:");
            Debug.LogError(e);

            SetStatus("Generation failed.");
            SetOutputText("Something went wrong. Try again.");
        }
        finally
        {
            _isGenerating = false;
            SetInteractable(true);
            FocusInput();

            if (_generationCts != null)
            {
                _generationCts.Dispose();
                _generationCts = null;
            }
        }
    }

    private bool IsRepeating(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        int half = text.Length / 2;

        if (half < 30)
            return false;

        string first = text.Substring(0, half);
        string second = text.Substring(half);

        return second.Contains(first.Substring(0, Mathf.Min(30, first.Length)));
    }

    private string FormatShort(BibleConversation data)
    {
        if (data == null)
            return "No response.";

        string verse = data.verse?.Trim() ?? "";
        string explanation = data.explanation?.Trim() ?? "";

        // keep explanation short
        if (explanation.Length > 120)
            explanation = explanation.Substring(0, 120).TrimEnd() + "...";

        string result = $"{verse}\n\n{explanation}";

        // add 1–2 guidance steps only
        if (data.steps != null && data.steps.Count > 0)
        {
            int count = Mathf.Min(2, data.steps.Count);

            result += "\n\n";

            for (int i = 0; i < count; i++)
            {
                string step = data.steps[i]?.Trim();

                if (!string.IsNullOrEmpty(step))
                {
                    // shorten each step
                    if (step.Length > 80)
                        step = step.Substring(0, 80).TrimEnd() + "...";

                    result += $"• {step}\n";
                }
            }
        }

        return result.Trim();
    }

    private async IAsyncEnumerable<string> ChatConcurrent(
        IAsyncEnumerable<string> tokens,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
    {
        await UniTask.SwitchToThreadPool();

        await foreach (var tokenPiece in tokens)
        {
            if (token.IsCancellationRequested)
                yield break;

            yield return tokenPiece;
        }
    }

    private BibleConversation TryParse(string raw)
    {
        try
        {
            int start = raw.IndexOf('{');
            int end = raw.LastIndexOf('}');

            if (start >= 0 && end > start)
            {
                string json = raw.Substring(start, end - start + 1);
                return JsonUtility.FromJson<BibleConversation>(json);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BibleHelper] JSON parse failed: {e}");
        }

        return null;
    }

    private string BuildBiblePrompt(string issue, string severity)
    {
        string request;

        if (issue == "question")
        {
            request = $"Provide a Bible verse about {issue}, and explain the meaning.";
        }
        else
        {
            switch (severity)
            {
                case "mild":
                    request = $"Provide a Bible verse about {issue}, with brief encouragement.";
                    break;

                case "moderate":
                    request = $"Provide a Bible verse about {issue}, with explanation and 3–4 steps.";
                    break;

                case "severe":
                    request = $"Provide a Bible verse about {issue}, with deeper explanation and 4–6 steps.";
                    break;

                case "crisis":
                    request = $"Provide a Bible verse about {issue}, include support advice and suggest seeking help.";
                    break;

                default:
                    request = $"Provide a Bible verse about {issue}, with encouragement.";
                    break;
            }
        }

        return $@"
Create a response from: {request}

Context:
- Topic: {issue}
- Severity: {severity}

IMPORTANT:
Return ONLY valid JSON.

Format:
{{
  ""user_story"": ""..."",
  ""verse"": ""..."",
  ""explanation"": ""..."",
  ""steps"": [""..."", ""...""],
  ""closing"": ""...""
}}";
    }

    private void StopCurrentGeneration()
    {
        if (_generationCts == null)
            return;

        if (!_generationCts.IsCancellationRequested)
        {
            _generationCts.Cancel();

            if (showDebugLogs)
                Debug.Log("[BibleHelper] Current generation canceled.");
        }
    }

    private void SetInteractable(bool interactable)
    {
        if (submitButton != null)
            submitButton.interactable = interactable && _isModelReady && !_isGenerating;

        if (clearButton != null)
            clearButton.interactable = interactable && _isModelReady && !_isGenerating;

        if (inputField != null)
            inputField.interactable = interactable && _isModelReady && !_isGenerating;

        if (stopButton != null)
            stopButton.interactable = _isGenerating;

        if (backButton != null)
            backButton.interactable = true;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void SetOutputText(string message)
    {
        if (outputText == null)
            return;

        outputText.text = message;
        ForceScrollToBottom();
    }

    private string ClampText(string text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.Trim();

        if (text.Length <= maxChars)
            return text;

        return text.Substring(0, maxChars).TrimEnd() + "...";
    }

    private string CleanResponse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.Trim();
        text = text.Replace("Bible Helper:", string.Empty);
        text = text.Replace("Assistant:", string.Empty);
        text = text.Replace("User:", string.Empty);
        text = text.Replace("\r", " ");
        text = text.Replace("\n", " ");

        while (text.Contains("  "))
            text = text.Replace("  ", " ");

        return text.Trim();
    }

    private void ForceScrollToBottom()
    {
        if (!autoScroll || scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }

    private void FocusInput()
    {
        if (inputField == null || !inputField.interactable)
            return;

        inputField.ActivateInputField();
        inputField.Select();
    }

    private void CleanupModel()
    {
        StopCurrentGeneration();

        try
        {
            _chatSession = null;
            _executor = null;

            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }

            if (_model != null)
            {
                _model.Dispose();
                _model = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BibleHelper] Cleanup warning: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        if (submitButton != null)
            submitButton.onClick.RemoveListener(OnSubmitPressed);

        if (clearButton != null)
            clearButton.onClick.RemoveListener(OnClearPressed);

        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackPressed);

        if (stopButton != null)
            stopButton.onClick.RemoveListener(OnStopPressed);

        if (inputField != null && submitOnEnter)
            inputField.onSubmit.RemoveListener(OnInputSubmitted);

        StopCurrentGeneration();

        if (_lifetimeCts != null)
        {
            if (!_lifetimeCts.IsCancellationRequested)
                _lifetimeCts.Cancel();

            _lifetimeCts.Dispose();
            _lifetimeCts = null;
        }

        CleanupModel();
    }

    private sealed class LoadedLlamaState : IDisposable
    {
        public LLamaWeights Model;
        public LLamaContext Context;
        public InteractiveExecutor Executor;
        public ChatSession Session;

        public void Dispose()
        {
            try
            {
                Context?.Dispose();
            }
            catch { }

            try
            {
                Model?.Dispose();
            }
            catch { }
        }
    }
}