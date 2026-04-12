using UnityEngine;

public class PlayBGM : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SoundManager.Instance.PlayMusic("bible_helper");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
