using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Regret
{
    public string Id;
    public string Text;
    public int Strength;
    public int TimesMentioned;

    public DateTime LastUpdated;
}

public class RegretSystem : MonoBehaviour
{
    public List<Regret> regrets = new();

    public void AddOrUpdateRegret(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        Regret existing = FindSimilar(text);

        if (existing == null)
        {
            Regret newRegret = new Regret
            {
                Id = Guid.NewGuid().ToString(),
                Text = text,
                Strength = 10,
                TimesMentioned = 1,
                LastUpdated = DateTime.UtcNow
            };

            regrets.Add(newRegret);
        }
        else
        {
            existing.Strength = Mathf.Clamp(existing.Strength + 5, 0, 100);
            existing.TimesMentioned++;
            existing.LastUpdated = DateTime.UtcNow; 
        }
    }

    private Regret FindSimilar(string text)
    {
        foreach (var r in regrets)
        {
            if (text.ToLower().Contains(r.Text.ToLower()) ||
                r.Text.ToLower().Contains(text.ToLower()))
            {
                return r;
            }
        }
        return null;
    }

    public Regret GetStrongestRegret()
    {
        Regret best = null;
        int max = -1;

        foreach (var r in regrets)
        {
            if (r.Strength > max)
            {
                best = r;
                max = r.Strength;
            }
        }

        return best;
    }

    public Regret GetStrongestRecentRegret(float recencyWeight = 1f)
    {
        Regret best = null;
        float bestScore = float.MinValue;

        foreach (var r in regrets)
        {
            float recencyScore = (float)(DateTime.UtcNow - r.LastUpdated).TotalSeconds;
            recencyScore = 1f / (1f + recencyScore); // recent = higher

            float score = r.Strength + recencyScore * recencyWeight * 100f;

            if (score > bestScore)
            {
                bestScore = score;
                best = r;
            }
        }

        return best;
    }
}