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

    private int ScoreDoctrine(CultDoctrineEntry entry, string playerText, string lastRegret, string phase)
    {
        int score = 0;
        score += MatchText(entry.tags, playerText) * 2;
        score += MatchText(entry.tags, lastRegret) * 3;
        score += MatchText(entry.regret_types, lastRegret) * 4;

        if (entry.phase != null && entry.phase.Contains(phase))
            score += 2;

        score += entry.priority;
        return score;
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