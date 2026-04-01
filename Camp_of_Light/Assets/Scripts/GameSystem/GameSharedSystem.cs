using OpenAI;
using UnityEngine;

public class GameSharedSystem : MonoBehaviour
{
    public static GameSharedSystem Instance { get; private set; }

    [Header("OpenAI")]
    [SerializeField] private OpenAIConfiguration configuration;
    public OpenAIClient OpenAI { get; private set; }

    [Header("Game State")]
    [SerializeField] private GameSession session = new();
    public GameSession Session => session;

    [SerializeField] private CultGameDirector gameDirector;
    public CultGameDirector GameDirector => gameDirector;

    [SerializeField] private RegretSystem regretSystem;
    public RegretSystem RegretSystem => regretSystem;

    [SerializeField] private RuleEngine ruleEngine;
    public RuleEngine RuleEngine => ruleEngine;

    [Header("Knowledge")]
    [SerializeField] private CultRetriever retriever;
    public CultRetriever Retriever => retriever;

    [Header("UI")]
    [SerializeField] private CultProgressUI progressUI;
    public CultProgressUI ProgressUI => progressUI;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Persist across scenes
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize()
    {
        if (configuration != null)
        {
            OpenAI = new OpenAIClient(configuration);
        }
        else
        {
            Debug.LogWarning("OpenAI Configuration is missing.");
        }
    }
}