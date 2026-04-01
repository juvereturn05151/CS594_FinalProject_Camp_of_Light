using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenAI.Samples.Chat
{
    public class PreacherChatBehaviour : MonologueSequenceChatBehavior
    {
        public override void Begin()
        {
            base.Begin();
        }

        public async Task PreloadPreachingPhaseAsync(bool forceRefresh = false)
        {
            await PreloadSequenceAsync(forceRefresh);
        }

        public void BeginPreachingPhase()
        {
            BeginSequence();
        }

        public void OnPreachingAdvancePressed()
        {
            OnAdvancePressed();
        }

        public bool IsPreachingPhaseComplete()
        {
            return IsSequenceComplete();
        }

        public bool IsPreachingReady()
        {
            return sequenceReady;
        }

        protected override string GetPreparingLine()
        {
            return "...The preacher opens the scripture.";
        }

        protected override Task<List<string>> GenerateSequenceLinesAsync()
        {
            string strongestRegret = regretSystem != null && regretSystem.GetStrongestRegret() != null
                ? regretSystem.GetStrongestRegret().Text
                : session.LastExtractedRegret;

            strongestRegret = TrimToLength(strongestRegret, 180);

            List<CultDoctrineEntry> doctrine = retriever.GetRelevantDoctrine(
                strongestRegret,
                session.LastExtractedRegret,
                session.Stats.Confidence,
                session.Stats.Brainwash,
                session.Stats.Wokeness,
                3
            );

            List<string> lines = new List<string>
            {
                GetIntroductionLine()
            };

            if (doctrine == null || doctrine.Count == 0)
            {
                lines.AddRange(GetFallbackPreachingLines());
                return Task.FromResult(lines);
            }

            CultDoctrineEntry selected = SelectBestDoctrine(doctrine);
            lines.AddRange(BuildPreachingLines(selected, strongestRegret));

            return Task.FromResult(lines);
        }

        protected override string GetSequenceCacheKey()
        {
            string strongestRegret = regretSystem != null && regretSystem.GetStrongestRegret() != null
                ? regretSystem.GetStrongestRegret().Text
                : session.LastExtractedRegret;

            strongestRegret = NormalizeKeyPart(strongestRegret);

            int day = GameManager.Instance != null && GameManager.Instance.State != null
                ? GameManager.Instance.State.CurrentDay
                : 1;

            string doctrineKey = "none";
            List<CultDoctrineEntry> doctrine = retriever.GetRelevantDoctrine(
                strongestRegret,
                session.LastExtractedRegret,
                session.Stats.Confidence,
                session.Stats.Brainwash,
                session.Stats.Wokeness,
                1
            );

            if (doctrine != null && doctrine.Count > 0)
                doctrineKey = NormalizeKeyPart(doctrine[0].verse);

            return $"{day}|D:{doctrineKey}|C:{session.Stats.Confidence}|B:{session.Stats.Brainwash}|W:{session.Stats.Wokeness}|R:{strongestRegret}";
        }

        protected override List<string> GetFallbackLines()
        {
            return new List<string>
            {
                GetIntroductionLine(),
                "Romans 3:23 says, 'All have sinned.'",
                "This means no one can stand clean by their own strength.",
                "Even when you try to move on by yourself, your regret keeps proving that truth."
            };
        }

        protected override void OnSequenceCompleted()
        {
            GameManager.Instance?.AdvancePhase();
        }

        private string GetIntroductionLine()
        {
            int day = GameManager.Instance != null && GameManager.Instance.State != null
                ? GameManager.Instance.State.CurrentDay
                : 1;

            if (day <= 10)
                return "Good morning, everyone. Let us begin our Mind Education.";

            if (day <= 30)
                return "Good morning. Let us return to the Mind Education and open our hearts to truth.";

            return "Good morning. Sit carefully and receive today's Mind Education.";
        }

        private List<string> GetFallbackPreachingLines()
        {
            return new List<string>
            {
                "Romans 3:23 says, 'All have sinned.'",
                "This means no one can stand clean by their own strength.",
                "Even when you try to move on by yourself, your regret keeps proving that truth."
            };
        }

        private CultDoctrineEntry SelectBestDoctrine(List<CultDoctrineEntry> doctrine)
        {
            int day = GameManager.Instance != null && GameManager.Instance.State != null
                ? GameManager.Instance.State.CurrentDay
                : 1;

            bool early = day <= 10;
            bool mid = day <= 30;

            IEnumerable<CultDoctrineEntry> filtered = doctrine;

            if (early)
            {
                filtered = doctrine
                    .Where(d => d.priority <= 0.9f)
                    .DefaultIfEmpty(doctrine.OrderByDescending(d => d.priority).First());
            }
            else if (!mid)
            {
                filtered = doctrine
                    .Where(d => d.priority >= 0.9f)
                    .DefaultIfEmpty(doctrine.OrderByDescending(d => d.priority).First());
            }

            return filtered
                .OrderByDescending(d => d.priority)
                .First();
        }

        private List<string> BuildPreachingLines(CultDoctrineEntry entry, string strongestRegret)
        {
            int day = GameManager.Instance != null && GameManager.Instance.State != null
                ? GameManager.Instance.State.CurrentDay
                : 1;

            string regretText = string.IsNullOrWhiteSpace(strongestRegret)
                ? "the weight in your heart"
                : strongestRegret.Trim();

            string verseLine = $"{entry.verse} says, \"{entry.text}\"";

            string meaningLine;
            string exampleLine;

            if (day <= 7)
            {
                meaningLine = $"This teaches that {ToLowerFirst(entry.translation)}";
                exampleLine = $"Think about {regretText}. When that pain returns, it shows how deeply the heart needs truth and guidance.";
            }
            else if (day <= 14)
            {
                meaningLine = $"This means {ToLowerFirst(entry.translation)} You cannot heal by leaning on yourself alone.";
                exampleLine = $"Look at {regretText}. The more you trusted your own way, the more that wound stayed with you.";
            }
            else
            {
                meaningLine = $"This means {ToLowerFirst(entry.translation)} Your own judgment has already failed you.";
                exampleLine = $"Your regret over {regretText} is not an accident. It is what happens when a person keeps believing in themselves instead of surrendering.";
            }

            return new List<string> { verseLine, meaningLine, exampleLine };
        }

        private string ToLowerFirst(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            if (text.Length == 1)
                return text.ToLower();

            return char.ToLower(text[0]) + text.Substring(1);
        }
    }
}