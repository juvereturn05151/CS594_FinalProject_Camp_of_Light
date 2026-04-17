using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    public string verse;
    public string explanation;
}

public class BibleHelperLocalScene : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private string modelRelativePath = "models/module6_bible_helper.gguf";
    [SerializeField] private int contextSize = 4096;
    [SerializeField] private int gpuLayerCount = 35;

    [Header("UI")]
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Scene")]
    [SerializeField] private string backSceneName = "MainMenu";

    [Header("Behavior")]
    [SerializeField] private bool submitOnEnter = true;
    [SerializeField] private bool autoFocusInput = true;
    [SerializeField] private bool showDebugLogs = true;

    [Header("Output Limits")]
    [SerializeField] private int maxTokens = 96;
    [SerializeField] private int maxVisibleCharacters = 600;

    [Header("Debug Output")]
    [SerializeField] private string verse = "";
    [SerializeField] private string explanation = "";

    private const string SYSTEM_PROMPT = @"You are a Bible assistant.

Rules:
- Return ONLY one valid JSON object.
- Return exactly these two fields:
  ""verse""
  ""explanation""
- Use double quotes only.
- Do not use single quotes.
- Do not include markdown.
- Do not include code fences.
- Do not include extra commentary.
- Do not repeat the explanation.
- Do not generate a second JSON object.
- Stop immediately after the final }.
- Keep the explanation short, clear, and practical.";

    private LLamaWeights _model;
    private LLamaContext _context;
    private InteractiveExecutor _executor;
    private ChatSession _chatSession;

    private CancellationTokenSource _lifetimeCts;
    private CancellationTokenSource _generationCts;

    private bool _isModelReady;
    private bool _isGenerating;

    private readonly StringBuilder _responseBuilder = new StringBuilder();

    private void Awake()
    {
        _lifetimeCts = new CancellationTokenSource();

        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmitPressed);

        if (inputField != null && submitOnEnter)
            inputField.onSubmit.AddListener(OnInputSubmitted);

        SetOutputText(string.Empty);
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
                Debug.LogError($"[BibleHelper] Model file not found: {fullModelPath}");
                SetStatus("Model file not found.");
                SetOutputText(fullModelPath);
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

            ResetConversationHistory();

            _isModelReady = true;
            SetInteractable(true);
            SetStatus("Bible Helper is ready.");
            SetOutputText("Please tell me your issue.");
            FocusInput();

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
            SetOutputText("Check Console for details.");
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
            StopCurrentGeneration();

        ResetConversationHistory();

        verse = string.Empty;
        explanation = string.Empty;

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

        _responseBuilder.Clear();
        SetOutputText(string.Empty);

        try
        {
            ResetConversationHistory();

            string prompt = BuildBiblePrompt(userMessage);

            var inferenceParams = new InferenceParams
            {
                MaxTokens = maxTokens,
                AntiPrompts = new List<string>
                {
                    "User:",
                    "Assistant:",
                    "Bible Helper:",
                    "System:"
                }
            };

            if (showDebugLogs)
            {
                Debug.Log("[BibleHelper] ===== SYSTEM PROMPT =====");
                Debug.Log(SYSTEM_PROMPT);
                Debug.Log("[BibleHelper] ===== USER PROMPT =====");
                Debug.Log(prompt);
            }

            bool jsonStarted = false;
            bool jsonFinished = false;
            bool inString = false;
            bool escaping = false;
            int braceDepth = 0;
            bool stopNow = false;

            await foreach (var piece in StreamChatAsync(
                _chatSession.ChatAsync(
                    new ChatHistory.Message(AuthorRole.User, prompt),
                    inferenceParams
                ),
                genToken
            ))
            {
                if (genToken.IsCancellationRequested)
                    break;

                for (int i = 0; i < piece.Length; i++)
                {
                    char c = piece[i];

                    // Wait for first {
                    if (!jsonStarted)
                    {
                        if (c == '{')
                        {
                            jsonStarted = true;
                            braceDepth = 1;
                            _responseBuilder.Append(c);
                        }

                        continue;
                    }

                    // If first JSON is already finished and another { appears,
                    // stop immediately and DO NOT append it.
                    if (jsonFinished)
                    {
                        if (c == '{')
                        {
                            if (showDebugLogs)
                                Debug.LogWarning("[BibleHelper] Second JSON object detected. Stopping before duplicate '{'.");
                            stopNow = true;
                            break;
                        }

                        // ignore trailing junk after first JSON
                        continue;
                    }

                    _responseBuilder.Append(c);

                    if (escaping)
                    {
                        escaping = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escaping = true;
                        continue;
                    }

                    if (c == '"')
                    {
                        inString = !inString;
                        continue;
                    }

                    if (inString)
                        continue;

                    if (c == '{')
                    {
                        braceDepth++;
                    }
                    else if (c == '}')
                    {
                        braceDepth--;

                        if (braceDepth == 0)
                        {
                            jsonFinished = true;
                            stopNow = true;
                            break;
                        }
                    }
                }

                SetOutputText(ClampText(_responseBuilder.ToString(), maxVisibleCharacters));

                if (stopNow)
                {
                    StopCurrentGeneration();
                    break;
                }

                await UniTask.Yield();
            }

            string rawResponse = _responseBuilder.ToString();

            if (showDebugLogs)
            {
                Debug.Log("[BibleHelper] ===== RAW/CAPTURED RESPONSE =====");
                Debug.Log(rawResponse);
            }

            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                SetStatus("No response generated.");
                SetOutputText("I couldn't generate a response. Please try again.");
                return;
            }

            BibleConversation parsed = TryParseConversation(rawResponse);

            if (parsed != null)
            {
                verse = parsed.verse?.Trim() ?? string.Empty;
                explanation = parsed.explanation?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(verse) && string.IsNullOrWhiteSpace(explanation))
                {
                    SetStatus("Parsed empty response.");
                    SetOutputText("The model returned empty fields. Please try again.");
                    return;
                }

                SetOutputText($"{verse}\n\n{explanation}");
                SetStatus("Ready.");

                if (showDebugLogs)
                {
                    Debug.Log("[BibleHelper] ===== PARSED VERSE =====");
                    Debug.Log(verse);
                    Debug.Log("[BibleHelper] ===== PARSED EXPLANATION =====");
                    Debug.Log(explanation);
                }
            }
            else
            {
                verse = string.Empty;
                explanation = string.Empty;

                SetStatus("Could not parse JSON.");
                Debug.LogError($"[BibleHelper] rawResponse: {rawResponse}");
                SetOutputText(ClampText(CleanResponse(rawResponse), maxVisibleCharacters));

                Debug.LogWarning("[BibleHelper] Failed to parse structured JSON.");
            }
        }
        catch (OperationCanceledException)
        {
            if (!string.IsNullOrWhiteSpace(_responseBuilder.ToString()))
                SetStatus("Ready.");
            else
                SetStatus("Generation stopped.");
        }
        catch (Exception e)
        {
            SetStatus("Generation failed.");
            SetOutputText("Something went wrong. Check Console.");
            Debug.LogError($"[BibleHelper] Generation failed: {e}");
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

    private void ResetConversationHistory()
    {
        if (_chatSession == null)
            return;

        _chatSession.History.Messages.Clear();
        _chatSession.History.AddMessage(AuthorRole.System, SYSTEM_PROMPT);
    }

    private async IAsyncEnumerable<string> StreamChatAsync(
        IAsyncEnumerable<string> tokenStream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
    {
        await foreach (var tokenPiece in tokenStream)
        {
            if (token.IsCancellationRequested)
                yield break;

            yield return tokenPiece;
        }
    }

    private BibleConversation TryParseConversation(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        try
        {
            string json = ExtractFirstCompleteJsonObject(raw);

            if (string.IsNullOrWhiteSpace(json))
                return null;

            if (showDebugLogs)
            {
                Debug.Log("[BibleHelper] ===== EXTRACTED JSON =====");
                Debug.Log(json);
            }

            return JsonUtility.FromJson<BibleConversation>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BibleHelper] JSON parse failed: {e}");
            return null;
        }
    }

    private string ExtractFirstCompleteJsonObject(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        bool started = false;
        bool inString = false;
        bool escaping = false;
        int braceDepth = 0;
        int startIndex = -1;

        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];

            if (!started)
            {
                if (c == '{')
                {
                    started = true;
                    startIndex = i;
                    braceDepth = 1;
                }

                continue;
            }

            if (escaping)
            {
                escaping = false;
                continue;
            }

            if (c == '\\')
            {
                escaping = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
                continue;

            if (c == '{')
            {
                braceDepth++;
            }
            else if (c == '}')
            {
                braceDepth--;

                if (braceDepth == 0)
                    return raw.Substring(startIndex, i - startIndex + 1);
            }
        }

        return null;
    }

    private string BuildBiblePrompt(string issue)
    {
        return
$@"User issue:
{issue}

Return ONLY one valid JSON object.
Use double quotes only.
Do not use single quotes.
Do not add any text before or after the JSON.
Do not generate a second JSON object.
Stop immediately after the closing }}.

Format:
{{
  ""verse"": ""Bible verse with reference"",
  ""explanation"": ""Short practical explanation""
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
        bool enabled = interactable && _isModelReady && !_isGenerating;

        if (submitButton != null)
            submitButton.interactable = enabled;

        if (inputField != null)
            inputField.interactable = enabled;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void SetOutputText(string message)
    {
        if (outputText != null)
            outputText.text = message ?? string.Empty;
    }

    private void FocusInput()
    {
        if (!autoFocusInput || inputField == null || !inputField.interactable)
            return;

        inputField.ActivateInputField();
        inputField.Select();
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
        text = text.Replace("System:", string.Empty);
        return text.Trim();
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

            _isModelReady = false;
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