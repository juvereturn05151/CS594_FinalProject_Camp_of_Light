using System;
using UnityEngine;

[Serializable]
public class PlayerStats
{
    public int Confidence = 50;
    public int Brainwash = 0;
    public int Wokeness = 50;

    public void ApplyDelta(int confidenceDelta, int brainwashDelta, int wokenessDelta)
    {
        Confidence += confidenceDelta;
        Brainwash += brainwashDelta;
        Wokeness += wokenessDelta;
        Clamp();
    }

    public void Clamp()
    {
        Confidence = Mathf.Clamp(Confidence, 0, 100);
        Brainwash = Mathf.Clamp(Brainwash, 0, 100);
        Wokeness = Mathf.Clamp(Wokeness, 0, 100);
    }

    public override string ToString()
    {
        return $"Confidence: {Confidence}, Brainwash: {Brainwash}, Wokeness: {Wokeness}";
    }
}