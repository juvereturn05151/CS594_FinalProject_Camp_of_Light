using OpenAI.Samples.Chat;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplaySaveBridge : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private CampOfLightChatBehaviour chatBehaviour;
    [SerializeField] private CultGameDirector gameDirector;
    [SerializeField] private RegretSystem regretSystem;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        LoadIntoSceneSystems();
    }

    public void SaveGame()
    {
        if (!GameStateRuntime.Instance.HasSaveLoaded())
        {
            Debug.LogError("No loaded save in GameStateRuntime.");
            return;
        }

        SaveData save = GameStateRuntime.Instance.CurrentSave;

        save.Profile = ToProfileData(chatBehaviour.GetSession().Profile);
        save.Stats = ToStatsData(chatBehaviour.GetSession().Stats);
        save.Campaign = ToCampaignData(gameDirector);
        save.Session = ToSessionData(chatBehaviour.GetSession());
        save.Regrets = ToRegretData(regretSystem.regrets);

        SaveManager.Instance.Save(save);
        GameStateRuntime.Instance.SetCurrentSave(save);

        Debug.Log("Game saved.");
    }

    public void SaveAndReturnToMenu()
    {
        SaveGame();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void LoadIntoSceneSystems()
    {
        if (!GameStateRuntime.Instance.HasSaveLoaded())
        {
            Debug.LogWarning("No save loaded. Gameplay scene will use defaults.");
            return;
        }

        SaveData save = GameStateRuntime.Instance.CurrentSave;

        GameSession session = new GameSession
        {
            Profile = ToProfileRuntime(save.Profile),
            Stats = ToStatsRuntime(save.Stats),
            LastExtractedRegret = save.Session.LastExtractedRegret,
            LastBibleVerse = save.Session.LastBibleVerse
        };

        chatBehaviour.UpdateGameSession(session);

        gameDirector.CurrentDay = save.Campaign.CurrentDay;
        gameDirector.MaxDays = save.Campaign.MaxDays;
        gameDirector.PromptsUsed = save.Campaign.PromptsUsed;
        gameDirector.MaxPromptsPerDay = save.Campaign.MaxPromptsPerDay;
        gameDirector.IsGameOver = save.Campaign.IsGameOver;
        gameDirector.Escaped = save.Campaign.Escaped;

        regretSystem.regrets = ToRegretRuntime(save.Regrets);
    }

    private PlayerProfileData ToProfileData(PlayerProfile profile)
    {
        return new PlayerProfileData
        {
            Name = profile.Name,
            Age = profile.Age,
            Profession = profile.Profession,
            Interests = new List<string>(profile.Interests)
        };
    }

    private PlayerStatsData ToStatsData(PlayerStats stats)
    {
        return new PlayerStatsData
        {
            Confidence = stats.Confidence,
            Brainwash = stats.Brainwash,
            Wokeness = stats.Wokeness
        };
    }

    private CampaignData ToCampaignData(CultGameDirector director)
    {
        return new CampaignData
        {
            CurrentDay = director.CurrentDay,
            MaxDays = director.MaxDays,
            PromptsUsed = director.PromptsUsed,
            MaxPromptsPerDay = director.MaxPromptsPerDay,
            IsGameOver = director.IsGameOver,
            Escaped = director.Escaped
        };
    }

    private SessionData ToSessionData(GameSession session)
    {
        return new SessionData
        {
            LastExtractedRegret = session.LastExtractedRegret,
            LastBibleVerse = session.LastBibleVerse
        };
    }

    private List<RegretData> ToRegretData(List<Regret> regrets)
    {
        List<RegretData> result = new();

        foreach (Regret r in regrets)
        {
            result.Add(new RegretData
            {
                Id = r.Id,
                Text = r.Text,
                Strength = r.Strength,
                TimesMentioned = r.TimesMentioned
            });
        }

        return result;
    }

    private PlayerProfile ToProfileRuntime(PlayerProfileData data)
    {
        return new PlayerProfile
        {
            Name = data.Name,
            Age = data.Age,
            Profession = data.Profession,
            Interests = new List<string>(data.Interests)
        };
    }

    private PlayerStats ToStatsRuntime(PlayerStatsData data)
    {
        return new PlayerStats
        {
            Confidence = data.Confidence,
            Brainwash = data.Brainwash,
            Wokeness = data.Wokeness
        };
    }

    private List<Regret> ToRegretRuntime(List<RegretData> regrets)
    {
        List<Regret> result = new();

        foreach (RegretData r in regrets)
        {
            result.Add(new Regret
            {
                Id = r.Id,
                Text = r.Text,
                Strength = r.Strength,
                TimesMentioned = r.TimesMentioned
            });
        }

        return result;
    }
}