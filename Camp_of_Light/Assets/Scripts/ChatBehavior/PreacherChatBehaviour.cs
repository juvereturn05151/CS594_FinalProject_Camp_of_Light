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
            string strongestRegret = regretSystem != null && regretSystem.GetStrongestRecentRegret() != null
                ? regretSystem.GetStrongestRecentRegret().Text
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
            string strongestRegret = regretSystem != null && regretSystem.GetStrongestRecentRegret() != null
                ? regretSystem.GetStrongestRecentRegret().Text
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
                ? "that weight in your heart"
                : strongestRegret.Trim();

            string verseLine = $"{entry.verse} says, \"{entry.text}\"";

            string meaningLine = $"This shows that {ToLowerFirst(entry.translation)}";

            string interpretationLine = BuildInterpretation(entry, day);

            string personalLine = $"When you think about {regretText}… doesn’t it feel like this has always been true for you?";

            string useCaseLine = BuildUseCaseLine(entry.use_case, regretText, day);

            string reinforcementLine = BuildReinforcement(entry, day);

            return new List<string>
            {
                verseLine,
                meaningLine,
                interpretationLine,
                personalLine,
                useCaseLine,
                reinforcementLine
            };
        }

        private string BuildInterpretation(CultDoctrineEntry entry, int day)
        {
            if (day <= 7)
                return "It’s not just a statement… it’s describing something real about how people live.";

            if (day <= 14)
                return "This isn’t just theory. It explains why things keep repeating in your life.";

            return "You’ve already seen this pattern yourself. This is not new—it’s just something you’ve been avoiding.";
        }

        private string BuildUseCaseLine(string useCase, string regretText, int day)
        {
            if (string.IsNullOrWhiteSpace(useCase))
                return "";

            string lower = useCase.ToLower();

            if (lower.Contains("wrong path"))
                return $"That’s why even now, something feels off… like you're not walking in the direction you thought you were.";

            if (lower.Contains("desperation"))
                return $"And staying where you are… does it actually change anything? Or does it just keep you stuck?";

            if (lower.Contains("clean"))
                return $"You’ve tried to move past {regretText}… but it never really disappears, does it?";

            if (lower.Contains("submission"))
                return $"Sometimes the answer isn’t understanding… it’s just letting go of control.";

            if (lower.Contains("guilt"))
                return $"That feeling you keep carrying… it’s not random. It’s pointing to something deeper.";

            if (lower.Contains("truth") || lower.Contains("knowledge"))
                return $"And if this was already known long before us… then maybe truth doesn’t come from us at all.";

            return $"There’s a reason this keeps showing up in your life… it’s not just coincidence.";
        }

        private string BuildReinforcement(CultDoctrineEntry entry, int day)
        {
            if (day <= 7)
                return "Just sit with that for a moment.";

            if (day <= 14)
                return "You don’t have to answer right away… but you can feel it, can’t you?";

            return "At some point… you have to decide whether to keep ignoring it.";
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