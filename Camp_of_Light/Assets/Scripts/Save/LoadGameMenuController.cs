using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadGameMenuController : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject saveSlotButtonPrefab;
    [SerializeField] private string gameplaySceneName = "Gameplay";

    private void Start()
    {
        Populate();
    }

    private void Populate()
    {
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        List<SaveSlotMeta> slots = SaveManager.Instance.GetAllSlots();

        foreach (SaveSlotMeta slot in slots)
        {
            GameObject go = Instantiate(saveSlotButtonPrefab, contentRoot);
            SaveSlotButtonUI ui = go.GetComponent<SaveSlotButtonUI>();
            ui.Bind(slot, this);
        }
    }

    public void OnSelectSlot(string slotId)
    {
        SaveData save = SaveManager.Instance.Load(slotId);
        if (save == null)
            return;

        GameRuntimeContext.Instance.SetCurrentSave(save);
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void OnDeleteSlot(string slotId)
    {
        SaveManager.Instance.DeleteSlot(slotId);
        Populate();
    }
}