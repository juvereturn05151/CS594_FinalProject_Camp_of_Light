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
        state.PromptsUsedToday_Brainwash =
            Mathf.Clamp(state.PromptsUsedToday_Brainwash, 0, state.MaxPromptsPerDay_Brainwash);

        SetActive(gameObject, true);
        SetActive(brainwashScene, true);
        SetActive(brainwashUI, true);
        SetActive(cultProgressUI, true);
        SoundManager.Instance.PlayMusic("Brainwash");
        if (brainwasherController != null)
        {
            brainwasherController.gameObject.SetActive(true);
            brainwasherController.UpdateGameSession(BuildSessionFromState(state));
            brainwasherController.Begin();
        }
    }

    public override void ExitPhase()
    {
        SoundManager.Instance.StopMusic();
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
        };
    }
}