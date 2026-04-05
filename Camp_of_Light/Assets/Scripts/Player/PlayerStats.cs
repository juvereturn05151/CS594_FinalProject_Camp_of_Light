using System;
using UnityEngine;

[Serializable]
public class PlayerStats
{
    public int Confidence = 50;
    public int Spirituality = 0;
    public int Skepticism = 0;

    public void ApplyDelta(int confidenceDelta, int brainwashDelta, int wokenessDelta)
    {
        Confidence += confidenceDelta;
        Spirituality += brainwashDelta;
        Skepticism += wokenessDelta;
        Clamp();
    }

    public void Clamp()
    {
        Confidence = Mathf.Clamp(Confidence, 0, 100);
        Spirituality = Mathf.Clamp(Spirituality, 0, 100);
        Skepticism = Mathf.Clamp(Skepticism, 0, 100);
    }

    public override string ToString()
    {
        return $"Confidence: {Confidence}, Brainwash: {Spirituality}, Wokeness: {Skepticism}";
    }
}