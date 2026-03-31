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

        string phase = ResolvePhase(brainwash, wokeness);

        return knowledgeBase.DoctrineEntries
            .Select(entry => new
            {
                Entry = entry,
                Score = ScoreDoctrine(entry, playerText, lastRegret, phase)
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Entry.priority)
            .Take(maxResults)
            .Where(x => x.Score > 0)
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

        string phase = ResolvePhase(brainwash, wokeness);

        return knowledgeBase.TacticEntries
            .Select(entry => new
            {
                Entry = entry,
                Score = ScoreTactic(entry, playerText, lastRegret, phase)
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Entry.priority)
            .Take(maxResults)
            .Where(x => x.Score > 0)
            .Select(x => x.Entry)
            .ToList();
    }

    private float ScoreDoctrine(CultDoctrineEntry entry, string playerText, string lastRegret, string phase)
    {
        float score = 0f;

        string player = playerText?.ToLowerInvariant() ?? string.Empty;
        string regret = lastRegret?.ToLowerInvariant() ?? string.Empty;
        string translation = entry.translation?.ToLowerInvariant() ?? string.Empty;
        string useCase = entry.use_case?.ToLowerInvariant() ?? string.Empty;
        string verseText = entry.text?.ToLowerInvariant() ?? string.Empty;
        string currentPhase = phase?.ToLowerInvariant() ?? string.Empty;

        // Base importance from the data itself
        score += entry.priority * 10f;

        // Match against player input
        score += CountKeywordOverlap(translation, player) * 2.5f;
        score += CountKeywordOverlap(useCase, player) * 2.0f;

        // Match against strongest regret
        score += CountKeywordOverlap(translation, regret) * 3.5f;
        score += CountKeywordOverlap(useCase, regret) * 3.0f;
        score += CountKeywordOverlap(verseText, regret) * 1.5f;

        return score;
    }

    private int CountKeywordOverlap(string sourceText, string targetText)
    {
        if (string.IsNullOrWhiteSpace(sourceText) || string.IsNullOrWhiteSpace(targetText))
            return 0;

        string[] keywords = sourceText
            .Split(new[] { ' ', ',', '.', ':', ';', '-', '_', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

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

    private int ScoreTactic(CultTacticEntry entry, string playerText, string lastRegret, string phase)
    {
        int score = 0;
        score += MatchText(entry.tags, playerText) * 2;
        score += MatchText(entry.tags, lastRegret) * 2;

        if (entry.phase != null && entry.phase.Contains(phase))
            score += 2;

        score += entry.priority;
        return score;
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

    private string ResolvePhase(int brainwash, int wokeness)
    {
        if (brainwash >= 60) return "late";
        if (brainwash >= 30 || wokeness >= 30) return "mid";
        return "early";
    }
}