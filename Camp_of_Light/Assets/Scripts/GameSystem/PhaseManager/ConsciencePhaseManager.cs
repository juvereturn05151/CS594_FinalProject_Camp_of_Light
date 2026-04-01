using UnityEngine;
using OpenAI.Samples.Chat;

public class ConsciencePhaseManager : BasePhaseManager
{
    [SerializeField] private ConscienceChatBehaviour conscienceController;
    public ConscienceChatBehaviour ConscienceController => conscienceController;
    [SerializeField] private GameObject conscienceScene;
    [SerializeField] private GameObject conscienceUI;
    [SerializeField] private GameObject cultProgressUI;

    public override GamePhase Phase => GamePhase.ConscienceTalk;

    public override void EnterPhase(GameRunState state)
    {
        SetActive(gameObject, true);
        SetActive(conscienceScene, true);
        SetActive(conscienceUI, true);
        SetActive(cultProgressUI, true);

        if (conscienceController != null)
        {
            conscienceController.gameObject.SetActive(true);
            conscienceController.UpdateGameSession(BuildSessionFromState(state));
            conscienceController.Begin();
        }
    }

    public override void ExitPhase()
    {
        SetActive(conscienceController, false);
        SetActive(conscienceScene, false);
        SetActive(conscienceUI, false);
        SetActive(cultProgressUI, false);
        base.ExitPhase();
    }

    private GameSession BuildSessionFromState(GameRunState state)
    {
        return new GameSession
        {
            Profile = state.Profile,
            Stats = state.Stats,
            LastExtractedRegret = state.LastExtractedRegret,
            LastBibleVerse = state.LastBibleVerse
        };
    }
}