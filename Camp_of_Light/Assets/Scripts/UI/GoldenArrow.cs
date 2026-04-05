using UnityEngine;

public class GoldenArrow : MonoBehaviour
{
    [SerializeField]
    private GameObject goldenArrow;

    [SerializeField]
    private GameObject generatingSpiritEffect;
    public void OnArrowReached() 
    {
        goldenArrow.SetActive(false);   
        generatingSpiritEffect.SetActive(true);
    }
}
