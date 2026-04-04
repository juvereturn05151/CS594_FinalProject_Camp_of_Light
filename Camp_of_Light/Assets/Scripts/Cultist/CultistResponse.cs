using Newtonsoft.Json;
using System;

[Serializable]
public class CultistResponse
{
    public bool IsPlayerJustBabbling;
    public bool IsPlayerTellingTheirRegret;
    public bool IsPlayerResistingAgainstCultOrBiBle;
    public bool IsPlayerBelievingInJesus;
    public bool IsPlayeWantingToFindNewMember;
    public string Player_Regret;
    public string CultistComment;

    public static CultistResponse Default()
    {
        return new CultistResponse
        {
            IsPlayerJustBabbling = false,
            IsPlayerTellingTheirRegret = false,
            IsPlayerResistingAgainstCultOrBiBle = false,
            IsPlayerBelievingInJesus = false,
            IsPlayeWantingToFindNewMember = false,
            Player_Regret = "",
            CultistComment = "I am listening. Tell me more about your life and what weighs on your heart."
        };
    }

    public static string GetDefaultJson()
    {
        return JsonConvert.SerializeObject(CultistResponse.Default());
    }
}