using UnityEngine;
using OpenAI.Samples.Chat;

public class BrainwashPhaseManager : BasePhaseManager
{
    [SerializeField] private BrainwasherChatBehaviour brainwasherController;
    public BrainwasherChatBehaviour BrainwasherController => brainwasherController;
    [SerializeField] private GameObject brainwashScene;
    [SerializeField] private GameObject brainwashUI;
    [SerializeField] private GameObject cultProgressUI;

    public override GamePhase Phase => GamePhase.BrainwashingLesson;

    public override void EnterPhase(GameRunState state)
    {
        SetActive(gameObject, true);
        SetActive(brainwashScene, true);
        SetActive(brainwashUI, true);
        SetActive(cultProgressUI, true);

        if (brainwasherController != null)
        {
            brainwasherController.gameObject.SetActive(true);
            brainwasherController.UpdateGameSession(BuildSessionFromState(state));
            brainwasherController.Begin();
        }
    }

    public override void ExitPhase()
    {
        SetActive(brainwasherController, false);
        SetActive(brainwashScene, false);
        SetActive(brainwashUI, false);
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