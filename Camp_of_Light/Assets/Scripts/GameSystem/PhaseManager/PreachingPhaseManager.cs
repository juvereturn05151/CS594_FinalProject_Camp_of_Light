using UnityEngine;
using OpenAI.Samples.Chat;

public class PreachingPhaseManager : BasePhaseManager
{
    [SerializeField] private PreacherChatBehaviour preacherController;
    public PreacherChatBehaviour PreacherController => preacherController;
    [SerializeField] private GameObject preachingPanel;
    [SerializeField] private GameObject preachingScene;

    public override GamePhase Phase => GamePhase.PreachingLesson;

    public override void EnterPhase(GameRunState state)
    {
        SetActive(gameObject, true);
        SetActive(preachingPanel, true);
        SetActive(preachingScene, true);

        if (preacherController != null)
        {
            preacherController.gameObject.SetActive(true);
            preacherController.UpdateGameSession(BuildSessionFromState(state));
            preacherController.Begin();
            preacherController.BeginPreachingPhase();
        }
    }

    public override void ExitPhase()
    {
        SetActive(preacherController, false);
        SetActive(preachingPanel, false);
        SetActive(preachingScene, false);
        base.ExitPhase();
    }

    private GameSession BuildSessionFromState(GameRunState state)
    {
        return new GameSession
        {
            Profile = state.Profile,
            Stats = state.Stats,
            LastExtractedRegret = state.LastExtractedRegret,
        };
    }
}