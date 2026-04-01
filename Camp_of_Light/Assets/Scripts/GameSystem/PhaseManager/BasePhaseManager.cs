using UnityEngine;

public abstract class BasePhaseManager : MonoBehaviour, IPhaseManager
{
    public abstract GamePhase Phase { get; }

    public abstract void EnterPhase(GameRunState state);

    public virtual void ExitPhase()
    {
        gameObject.SetActive(false);
    }

    protected static void SetActive(GameObject target, bool value)
    {
        if (target != null)
            target.SetActive(value);
    }

    protected static void SetActive(Component target, bool value)
    {
        if (target != null)
            target.gameObject.SetActive(value);
    }
}