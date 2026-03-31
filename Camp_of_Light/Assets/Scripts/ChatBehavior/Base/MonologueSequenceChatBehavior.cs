using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenAI.Samples.Chat
{
    public abstract class MonologueSequenceChatBehavior : BaseChatBehaviour
    {
        [Header("Monologue Sequence")]
        [SerializeField] protected bool autoPreloadOnInit = true;
        [SerializeField] protected bool useCache = true;
        [SerializeField] protected bool showPreparingLineIfNotReady = true;

        protected bool isGenerating;
        protected bool sequenceComplete;
        protected bool sequenceReady;

        protected List<string> sequenceLines = new();
        protected int sequenceIndex = -1;

        protected readonly Dictionary<string, List<string>> sequenceCache = new();

        protected virtual void Update()
        {
            if (isGenerating || !gameObject.activeInHierarchy)
                return;

            if (ShouldAdvanceSequenceThisFrame())
                AdvanceSequenceLine();
        }

        public override void Init()
        {
            base.Init();

            ResetSequenceState();

            if (autoPreloadOnInit)
                _ = PreloadSequenceAsync();
        }

        public async Task PreloadSequenceAsync(bool forceRefresh = false)
        {
            if (isGenerating)
                return;

            string cacheKey = GetSequenceCacheKey();

            if (!forceRefresh && useCache && sequenceCache.TryGetValue(cacheKey, out var cachedLines))
            {
                sequenceLines = new List<string>(cachedLines);
                sequenceReady = true;
                return;
            }

            isGenerating = true;
            sequenceReady = false;

            try
            {
                List<string> lines = await GenerateSequenceLinesAsync();
                sequenceLines = ValidateAndCleanLines(lines);

                if (sequenceLines.Count == 0)
                    sequenceLines = GetFallbackLines();

                if (useCache)
                    sequenceCache[cacheKey] = new List<string>(sequenceLines);

                sequenceReady = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                sequenceLines = GetFallbackLines();

                if (useCache)
                    sequenceCache[cacheKey] = new List<string>(sequenceLines);

                sequenceReady = true;
            }
            finally
            {
                isGenerating = false;
            }
        }

        public async void BeginSequence()
        {
            if (sequenceComplete)
                return;

            if (sequenceReady && sequenceLines != null && sequenceLines.Count > 0)
            {
                StartSequenceDisplay();
                return;
            }

            if (showPreparingLineIfNotReady)
                AddCultistBubble(GetPreparingLine());

            await PreloadSequenceAsync();
            StartSequenceDisplay();
        }

        public void OnAdvancePressed()
        {
            AdvanceSequenceLine();
        }

        public bool IsSequenceComplete() => sequenceComplete;
        public bool IsSequenceReady() => sequenceReady;
        public bool IsGenerating() => isGenerating;

        public void ClearSequenceCache()
        {
            sequenceCache.Clear();
        }

        protected virtual bool ShouldAdvanceSequenceThisFrame()
        {
            return Input.GetKeyDown(KeyCode.A) ||
                   Input.GetKeyDown(KeyCode.B) ||
                   Input.GetKeyDown(KeyCode.Space) ||
                   Input.GetKeyDown(KeyCode.Return);
        }

        protected virtual string GetPreparingLine()
        {
            return "...The speaker gathers their words.";
        }

        protected virtual void StartSequenceDisplay()
        {
            if (sequenceLines == null || sequenceLines.Count == 0)
            {
                sequenceLines = GetFallbackLines();
                sequenceReady = true;
            }

            if (sequenceIndex < 0)
                AdvanceSequenceLine();
        }

        protected virtual void AdvanceSequenceLine()
        {
            if (isGenerating)
                return;

            if (sequenceLines == null || sequenceLines.Count == 0)
                return;

            if (sequenceComplete)
                return;

            if (sequenceIndex + 1 < sequenceLines.Count)
            {
                sequenceIndex++;
                string nextLine = sequenceLines[sequenceIndex];

                AddAndRecordCultistBubble(nextLine);

                if (sequenceIndex >= sequenceLines.Count - 1)
                {
                    sequenceComplete = true;
                    OnSequenceCompleted();
                }
            }
        }

        protected virtual void OnSequenceCompleted()
        {
        }

        protected virtual void ResetSequenceState()
        {
            sequenceComplete = false;
            sequenceReady = false;
            sequenceIndex = -1;
            sequenceLines.Clear();
        }

        protected virtual List<string> ValidateAndCleanLines(List<string> lines)
        {
            List<string> cleaned = new();

            if (lines == null)
                return cleaned;

            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    cleaned.Add(line.Trim());
            }

            return cleaned;
        }

        protected abstract Task<List<string>> GenerateSequenceLinesAsync();
        protected abstract string GetSequenceCacheKey();
        protected abstract List<string> GetFallbackLines();
    }
}