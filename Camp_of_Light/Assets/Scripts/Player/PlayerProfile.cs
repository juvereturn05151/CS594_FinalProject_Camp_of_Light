using System;
using System.Collections.Generic;

[Serializable]
public class PlayerProfile
{
    public string Name;

    public string CharacterAppearancePrompt;
    public string PlayerCharacterImagePath;

    public List<string> Interests = new();

    public string SpiritCharacterPrompt;
    public string SpiritCharacterImagePath;

    public PlayerProfile()
    {
        Name = string.Empty;

        CharacterAppearancePrompt = string.Empty;
        PlayerCharacterImagePath = string.Empty;

        Interests = new List<string>();

        SpiritCharacterPrompt = string.Empty;
        SpiritCharacterImagePath = string.Empty;
    }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name)
            && !string.IsNullOrWhiteSpace(CharacterAppearancePrompt)
            && !string.IsNullOrWhiteSpace(PlayerCharacterImagePath)
            && Interests != null
            && Interests.Count == 3
            && !string.IsNullOrWhiteSpace(SpiritCharacterImagePath);
    }

    public override string ToString()
    {
        string interestsText = Interests == null || Interests.Count == 0
            ? "None"
            : string.Join(", ", Interests);

        return
            $"Name: {Name}\n" +
            $"Appearance Prompt: {CharacterAppearancePrompt}\n" +
            $"Player Character Image Path: {PlayerCharacterImagePath}\n" +
            $"Interests: {interestsText}\n" +
            $"Spirit Prompt: {SpiritCharacterPrompt}\n" +
            $"Spirit Image Path: {SpiritCharacterImagePath}";
    }
}