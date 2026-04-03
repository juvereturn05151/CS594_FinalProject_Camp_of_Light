using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CultRetriever : MonoBehaviour
{
    [SerializeField] private CultKnowledgeBase knowledgeBase;

    public List<CultDoctrineEntry> GetRelevantDoctrine(
        string playerText,
        string lastRegret,
        int confidence,
        int brainwash,
        int wokeness,
        int maxResults = 3)
    {
        if (knowledgeBase == null || knowledgeBase.DoctrineEntries == null)
            return new List<CultDoctrineEntry>();

        int currentDay = GetCurrentDay();
        List<string> playerTags = ExtractPlayerTags(playerText);
        List<string> regretTags = ExtractPlayerTags(lastRegret);
        List<string> dayThemeTags = GetDoctrineDayThemeTags(currentDay);

        return knowledgeBase.DoctrineEntries
            .Where(entry => IsDoctrineDayMatch(entry, currentDay))
            .Select(entry => new
            {
                Entry = entry,
                Score = ScoreDoctrine(entry, playerText, lastRegret, playerTags, regretTags, dayThemeTags, currentDay)
            })
            .Where(x => x.Score > 0f)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Entry.verse)
            .Take(maxResults)
            .Select(x => x.Entry)
            .ToList();
    }

    public List<CultTacticEntry> GetRelevantTactics(
        string playerText,
        string lastRegret,
        int confidence,
        int brainwash,
        int wokeness,
        int maxResults = 2)
    {
        if (knowledgeBase == null || knowledgeBase.TacticEntries == null)
            return new List<CultTacticEntry>();

        int currentDay = GetCurrentDay();
        List<string> playerTags = ExtractPlayerTags(playerText);
        List<string> regretTags = ExtractPlayerTags(lastRegret);
        List<string> dayThemeTags = GetTacticDayThemeTags(currentDay);

        return knowledgeBase.TacticEntries
            .Where(entry => IsTacticDayMatch(entry, currentDay))
            .Select(entry => new
            {
                Entry = entry,
                Score = ScoreTactic(entry, playerTags, regretTags, dayThemeTags, playerText, lastRegret)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Entry.title)
            .Take(maxResults)
            .Select(x => x.Entry)
            .ToList();
    }

    private float ScoreDoctrine(
        CultDoctrineEntry entry,
        string playerText,
        string lastRegret,
        List<string> playerTags,
        List<string> regretTags,
        List<string> dayThemeTags,
        int currentDay)
    {
        float score = 0f;

        string player = playerText?.ToLowerInvariant() ?? string.Empty;
        string regret = lastRegret?.ToLowerInvariant() ?? string.Empty;
        string translation = entry.translation?.ToLowerInvariant() ?? string.Empty;
        string useCase = entry.use_case?.ToLowerInvariant() ?? string.Empty;
        string verseText = entry.text?.ToLowerInvariant() ?? string.Empty;
        List<string> entryTags = NormalizeTags(entry.tags);

        if (IsDoctrineDayMatch(entry, currentDay))
            score += 10f;

        score += CountTagOverlap(entryTags, dayThemeTags) * 4.0f;
        score += CountTagOverlap(entryTags, playerTags) * 5.0f;
        score += CountTagOverlap(entryTags, regretTags) * 4.5f;

        score += CountKeywordOverlap(translation, player) * 1.5f;
        score += CountKeywordOverlap(useCase, player) * 1.5f;
        score += CountKeywordOverlap(translation, regret) * 2.0f;
        score += CountKeywordOverlap(useCase, regret) * 2.0f;
        score += CountKeywordOverlap(verseText, regret) * 1.0f;

        return score;
    }

    private int ScoreTactic(
        CultTacticEntry entry,
        List<string> playerTags,
        List<string> regretTags,
        List<string> dayThemeTags,
        string playerText,
        string lastRegret)
    {
        int score = 0;
        List<string> entryTags = NormalizeTags(entry.tags);

        score += CountTagOverlap(entryTags, dayThemeTags) * 4;
        score += CountTagOverlap(entryTags, playerTags) * 5;
        score += CountTagOverlap(entryTags, regretTags) * 4;

        score += MatchText(entry.tags, playerText) * 2;
        score += MatchText(entry.tags, lastRegret) * 2;

        return score;
    }

    private int GetCurrentDay()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != null)
            return Mathf.Max(1, GameManager.Instance.State.CurrentDay);

        return 1;
    }

    private bool IsDoctrineDayMatch(CultDoctrineEntry entry, int currentDay)
    {
        if (entry == null || entry.day_range == null)
            return true;

        return currentDay >= entry.day_range.start && currentDay <= entry.day_range.end;
    }

    private bool IsTacticDayMatch(CultTacticEntry entry, int currentDay)
    {
        if (entry == null || entry.day_range == null)
            return true;

        return currentDay >= entry.day_range.start && currentDay <= entry.day_range.end;
    }

    private int CountKeywordOverlap(string sourceText, string targetText)
    {
        if (string.IsNullOrWhiteSpace(sourceText) || string.IsNullOrWhiteSpace(targetText))
            return 0;

        string[] keywords = sourceText
            .Split(new[] { ' ', ',', '.', ':', ';', '-', '_', '\n', '\r', '\t', '"', '\'', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);

        int matches = 0;

        foreach (string keyword in keywords)
        {
            if (keyword.Length < 4)
                continue;

            if (targetText.Contains(keyword))
                matches++;
        }

        return matches;
    }

    private int MatchText(List<string> keywords, string text)
    {
        if (keywords == null || string.IsNullOrWhiteSpace(text))
            return 0;

        int matches = 0;
        string lower = text.ToLowerInvariant();

        foreach (var keyword in keywords)
        {
            if (!string.IsNullOrWhiteSpace(keyword) &&
                lower.Contains(keyword.ToLowerInvariant()))
            {
                matches++;
            }
        }

        return matches;
    }

    private int CountTagOverlap(List<string> sourceTags, List<string> targetTags)
    {
        if (sourceTags == null || targetTags == null || sourceTags.Count == 0 || targetTags.Count == 0)
            return 0;

        HashSet<string> source = new HashSet<string>(sourceTags, StringComparer.OrdinalIgnoreCase);
        int matches = 0;

        foreach (string tag in targetTags)
        {
            if (!string.IsNullOrWhiteSpace(tag) && source.Contains(tag))
                matches++;
        }

        return matches;
    }

    private List<string> NormalizeTags(List<string> tags)
    {
        if (tags == null)
            return new List<string>();

        return tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    private List<string> ExtractPlayerTags(string text)
    {
        List<string> tags = new List<string>();

        if (string.IsNullOrWhiteSpace(text))
            return tags;

        string lower = text.ToLowerInvariant();

        AddIfContainsAny(tags, lower, new[] { "game", "games", "gaming", "video game", "play games" }, "games");
        AddIfContainsAny(tags, lower, new[] { "fun", "pleasure", "enjoy", "entertainment" }, "pleasure");
        AddIfContainsAny(tags, lower, new[] { "distract", "escape", "waste time", "addicted", "obsessed" }, "distraction");
        AddIfContainsAny(tags, lower, new[] { "affair", "cheat on", "cheated on", "adultery", "mistress", "slept with" }, "affair");
        AddIfContainsAny(tags, lower, new[] { "lust", "porn", "sexual", "hookup" }, "lust");
        AddIfContainsAny(tags, lower, new[] { "lie", "lied", "lying", "dishonest" }, "lying");
        AddIfContainsAny(tags, lower, new[] { "steal", "stole", "theft" }, "stealing");
        AddIfContainsAny(tags, lower, new[] { "angry", "anger", "rage", "hate" }, "anger");
        AddIfContainsAny(tags, lower, new[] { "pride", "ego", "arrogant", "self-made", "my way" }, "pride");
        AddIfContainsAny(tags, lower, new[] { "guilt", "ashamed", "shame", "regret", "sorry" }, "guilt");
        AddIfContainsAny(tags, lower, new[] { "fear", "afraid", "scared", "anxious", "anxiety" }, "fear");
        AddIfContainsAny(tags, lower, new[] { "lonely", "loneliness", "alone", "isolated" }, "loneliness");
        AddIfContainsAny(tags, lower, new[] { "family", "mother", "father", "parents", "wife", "husband", "relationship" }, "family");
        AddIfContainsAny(tags, lower, new[] { "money", "rich", "career", "success", "work", "job" }, "career");
        AddIfContainsAny(tags, lower, new[] { "judge", "judging", "judged" }, "judgment");
        AddIfContainsAny(tags, lower, new[] { "trust myself", "my own understanding", "my own thinking", "my judgment" }, "self_trust");
        AddIfContainsAny(tags, lower, new[] { "spread", "preach", "evangelize", "bring others", "save others" }, "evangelism");
        AddIfContainsAny(tags, lower, new[] { "habit", "hobby", "comfort", "coping" }, "habits");

        return tags.Distinct().ToList();
    }

    private void AddIfContainsAny(List<string> tags, string source, string[] terms, string tag)
    {
        foreach (string term in terms)
        {
            if (source.Contains(term))
            {
                tags.Add(tag);
                return;
            }
        }
    }

    private List<string> GetDoctrineDayThemeTags(int currentDay)
    {
        if (currentDay <= 2)
        {
            return new List<string>
            {
                "god_is_real", "design", "truth", "creation", "knowledge"
            };
        }

        if (currentDay <= 7)
        {
            return new List<string>
            {
                "sin", "death", "salvation", "jesus_paid", "guilt", "judgment"
            };
        }

        if (currentDay <= 14)
        {
            return new List<string>
            {
                "self_distrust", "submission", "authority", "deception", "break_confidence"
            };
        }

        return new List<string>
        {
            "evangelism", "self_distrust", "sin", "submission", "authority", "guilt"
        };
    }

    private List<string> GetTacticDayThemeTags(int currentDay)
    {
        if (currentDay <= 2)
        {
            return new List<string>
            {
                "rapport", "familiarity", "belonging", "feelings", "daily_life"
            };
        }

        if (currentDay <= 7)
        {
            return new List<string>
            {
                "confession", "guilt", "fear", "relief", "regret"
            };
        }

        if (currentDay <= 14)
        {
            return new List<string>
            {
                "identity_erosion", "self_doubt", "dependency", "thoughts", "habits"
            };
        }

        return new List<string>
        {
            "obedience", "authority", "mission", "recruitment", "evangelism", "self_doubt"
        };
    }
}
