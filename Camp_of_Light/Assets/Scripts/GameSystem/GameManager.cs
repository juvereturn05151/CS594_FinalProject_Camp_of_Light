using OpenAI.Samples.Chat;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private CampOfLightChatBehaviour campOfLightChatBehaviour;
    [SerializeField] private GameplaySaveBridge gameplaySaveBridge;
    private void Start()
    {
        if (!gameplaySaveBridge.LoadIntoSceneSystems()) 
        {
            campOfLightChatBehaviour.Init();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
