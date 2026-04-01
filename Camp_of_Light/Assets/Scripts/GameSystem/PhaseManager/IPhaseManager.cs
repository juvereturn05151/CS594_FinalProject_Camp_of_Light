public interface IPhaseManager
{
    GamePhase Phase { get; }
    void EnterPhase(GameRunState state);
    void ExitPhase();
}