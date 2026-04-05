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
                session.Stats.Spirituality,
                session.Stats.Skepticism,
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

            CultDoctrineEntry selected = SelectBestDoctrine(doctrine, strongestRegret);
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
                session.Stats.Spirituality,
                session.Stats.Skepticism,
                1
            );

            if (doctrine != null && doctrine.Count > 0)
                doctrineKey = NormalizeKeyPart(doctrine[0].verse);

            return $"{day}|D:{doctrineKey}|C:{session.Stats.Confidence}|B:{session.Stats.Spirituality}|W:{session.Stats.Skepticism}|R:{strongestRegret}";
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
            int day = GetCurrentDay();

            if (day <= 2)
                return "Good morning, everyone. Today we will look at the order of the world and what it reveals.";

            if (day <= 7)
                return "Good morning. Let us open the scripture and see what it says about sin and salvation.";

            if (day <= 14)
                return "Good morning. Today we will look honestly at the weakness of human judgment.";

            return "Good morning. Today we will receive the truth and understand what must be done with it.";
        }

        private List<string> GetFallbackPreachingLines()
        {
            int day = GetCurrentDay();

            if (day <= 2)
            {
                return new List<string>
                {
                    "Scripture speaks about the world with an order people did not invent for themselves.",
                    "That should make you pause before trusting only what feels obvious to you.",
                    "Truth may exist before your own opinion does."
                };
            }

            if (day <= 7)
            {
                return new List<string>
                {
                    "Romans 3:23 says, 'All have sinned.'",
                    "This means no one can stand clean by their own strength.",
                    "And if sin leads to death, then salvation cannot come from yourself."
                };
            }

            if (day <= 14)
            {
                return new List<string>
                {
                    "Jeremiah 17:9 says the heart is deceitful above all things.",
                    "That means the problem is not only what you have done, but what you trust inside yourself.",
                    "Your own thinking keeps leading you back into the same places."
                };
            }

            return new List<string>
            {
                "Truth is not meant to stay with you alone.",
                "If people are lost, someone must speak to them.",
                "And if man cannot trust himself, then he must carry a message greater than himself."
            };
        }

        private CultDoctrineEntry SelectBestDoctrine(List<CultDoctrineEntry> doctrine, string strongestRegret)
        {
            int day = GetCurrentDay();
            string regretLower = strongestRegret?.ToLowerInvariant() ?? string.Empty;

            List<string> regretKeywords = regretLower
                .Split(new[] { ' ', ',', '.', ':', ';', '-', '_', '\n', '\r', '\t' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Where(k => k.Length >= 4)
                .Distinct()
                .ToList();

            return doctrine
                .OrderByDescending(d => ScoreEntryForPresentation(d, regretKeywords, day))
                .ThenBy(d => d.verse)
                .First();
        }

        private float ScoreEntryForPresentation(CultDoctrineEntry entry, List<string> regretKeywords, int day)
        {
            float score = 0f;

            if (entry == null)
                return score;

            if (entry.day_range == null || (day >= entry.day_range.start && day <= entry.day_range.end))
                score += 10f;

            if (entry.tags != null)
            {
                foreach (string tag in entry.tags)
                {
                    if (string.IsNullOrWhiteSpace(tag))
                        continue;

                    string lowerTag = tag.ToLowerInvariant();

                    if (day <= 2 && (lowerTag.Contains("god_is_real") || lowerTag.Contains("design") || lowerTag.Contains("truth") || lowerTag.Contains("creation")))
                        score += 4f;

                    else if (day <= 7 && (lowerTag.Contains("sin") || lowerTag.Contains("death") || lowerTag.Contains("salvation") || lowerTag.Contains("jesus_paid")))
                        score += 4f;

                    else if (day <= 14 && (lowerTag.Contains("self_distrust") || lowerTag.Contains("submission") || lowerTag.Contains("authority")))
                        score += 4f;

                    else if (day > 14 && (lowerTag.Contains("evangelism") || lowerTag.Contains("sin") || lowerTag.Contains("self_distrust")))
                        score += 4f;
                }
            }

            string translation = entry.translation?.ToLowerInvariant() ?? string.Empty;
            string useCase = entry.use_case?.ToLowerInvariant() ?? string.Empty;
            string verseText = entry.text?.ToLowerInvariant() ?? string.Empty;

            foreach (string keyword in regretKeywords)
            {
                if (translation.Contains(keyword))
                    score += 2f;
                if (useCase.Contains(keyword))
                    score += 2f;
                if (verseText.Contains(keyword))
                    score += 1f;
            }

            return score;
        }

        private List<string> BuildPreachingLines(CultDoctrineEntry entry, string strongestRegret)
        {
            int day = GetCurrentDay();

            string regretText = string.IsNullOrWhiteSpace(strongestRegret)
                ? "that weight in your heart"
                : strongestRegret.Trim();

            string verseLine = $"{entry.verse} says, \"{entry.text}\"";
            string meaningLine = $"This shows that {ToLowerFirst(entry.translation)}";
            string interpretationLine = BuildInterpretation(entry, day);
            string personalLine = BuildPersonalLine(day, regretText);
            string useCaseLine = BuildUseCaseLine(entry.use_case, regretText, day);
            string reinforcementLine = BuildReinforcement(entry, day);

            List<string> lines = new List<string>
            {
                verseLine,
                meaningLine,
                interpretationLine,
                personalLine
            };

            if (!string.IsNullOrWhiteSpace(useCaseLine))
                lines.Add(useCaseLine);

            if (!string.IsNullOrWhiteSpace(reinforcementLine))
                lines.Add(reinforcementLine);

            return lines;
        }

        private string BuildInterpretation(CultDoctrineEntry entry, int day)
        {
            if (day <= 2)
                return "This is not only about words on a page. It suggests that truth exists beyond what people casually assume.";

            if (day <= 7)
                return "This is not only a religious idea. It is explaining why guilt, death, and the need for salvation follow human life.";

            if (day <= 14)
                return "This is not only a warning. It explains why trusting your own thoughts keeps returning you to confusion.";

            return "This truth does not end with private belief. Once you see it, it changes what you owe to others and how you must live.";
        }

        private string BuildPersonalLine(int day, string regretText)
        {
            if (day <= 2)
                return $"When you think about {regretText}… doesn’t it show how limited human certainty really is?";

            if (day <= 7)
                return $"When you think about {regretText}… doesn’t it feel like something in human life is already broken?";

            if (day <= 14)
                return $"When you think about {regretText}… hasn’t your own heart already shown you that it cannot always be trusted?";

            return $"And when you think about {regretText}… can you really keep this truth to yourself and still pretend it changes nothing?";
        }

        private string BuildUseCaseLine(string useCase, string regretText, int day)
        {
            if (string.IsNullOrWhiteSpace(useCase))
                return string.Empty;

            string lower = useCase.ToLowerInvariant();

            if (lower.Contains("truth") || lower.Contains("knowledge") || lower.Contains("design"))
                return "The world does not have to ask your permission to be true before it is true.";

            if (lower.Contains("guilt"))
                return $"That feeling around {regretText} is not random. It is revealing something about your condition.";

            if (lower.Contains("death") || lower.Contains("judgment"))
                return "And once the consequence is real, delay stops being neutral.";

            if (lower.Contains("submission") || lower.Contains("control"))
                return "Sometimes what keeps hurting you is not ignorance, but your insistence on remaining in charge.";

            if (lower.Contains("evangelism") || lower.Contains("spread"))
                return "If this truth matters, then silence is no longer innocent.";

            if (lower.Contains("clean") || lower.Contains("forgive"))
                return $"You may try to bury {regretText}, but being buried is not the same as being removed.";

            return "There is a reason this keeps appearing in front of you instead of disappearing.";
        }

        private string BuildReinforcement(CultDoctrineEntry entry, int day)
        {
            if (day <= 2)
                return "Do not reject something simply because it did not begin from your own mind.";

            if (day <= 7)
                return "If that unsettles you, perhaps it is because it is touching something real.";

            if (day <= 14)
                return "You do not have to answer immediately. But you already know your own heart has failed you before.";

            return "Once truth becomes clear, responsibility follows it.";
        }

        private int GetCurrentDay()
        {
            if (GameManager.Instance != null && GameManager.Instance.State != null)
                return GameManager.Instance.State.CurrentDay;

            return 1;
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