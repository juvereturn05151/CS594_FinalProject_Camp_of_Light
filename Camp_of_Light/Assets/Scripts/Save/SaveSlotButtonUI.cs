using TMPro;
using UnityEngine;

public class SaveSlotButtonUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;

    private SaveSlotMeta slot;
    private LoadGameMenuController controller;

    public void Bind(SaveSlotMeta slotMeta, LoadGameMenuController owner)
    {
        slot = slotMeta;
        controller = owner;

        titleText.text = slot.SaveDisplayName;
        subtitleText.text = $"Updated: {slot.UpdatedAtUtc}";
    }

    public void OnClickLoad()
    {
        controller.OnSelectSlot(slot.SlotId);
    }

    public void OnClickDelete()
    {
        controller.OnDeleteSlot(slot.SlotId);
    }
}