using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotButtonUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;

    [Header("Buttons")]
    [SerializeField] private Button selectButton;
    [SerializeField] private Button deleteButton;

    [Header("Optional Labels")]
    [SerializeField] private TMP_Text selectButtonText;

    private SaveSlotMeta slot;
    private LoadGameMenuController controller;

    public void Bind(SaveSlotMeta slotMeta, LoadGameMenuController owner)
    {
        slot = slotMeta;
        controller = owner;

        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        if (slot == null)
            return;

        if (titleText != null)
        {
            titleText.text = string.IsNullOrWhiteSpace(slot.SaveDisplayName)
                ? "Empty Slot"
                : slot.SaveDisplayName;
        }

        bool hasData = slot.HasData;

        if (subtitleText != null)
        {
            subtitleText.text = hasData
                ? $"Updated: {slot.UpdatedAtUtc}"
                : "Empty";
        }

        bool isNewGameMode = controller != null && controller.IsNewGameMode();
        bool isLoadGameMode = controller != null && controller.IsLoadGameMode();

        bool canSelect = false;

        if (isNewGameMode)
        {
            // New Game: only EMPTY slots can be selected
            canSelect = !hasData;

            if (selectButtonText != null)
                selectButtonText.text = hasData ? "Occupied" : "New Game";
        }
        else if (isLoadGameMode)
        {
            // Load Game: only EXISTING saves can be selected
            canSelect = hasData;

            if (selectButtonText != null)
                selectButtonText.text = hasData ? "Load" : "Empty";
        }
        else
        {
            if (selectButtonText != null)
                selectButtonText.text = "Select";
        }

        if (selectButton != null)
            selectButton.interactable = canSelect;

        if (deleteButton != null)
            deleteButton.interactable = hasData;
    }

    public void OnClickLoad()
    {
        if (controller == null || slot == null)
            return;

        controller.OnSelectSlot(slot.SlotId);
    }

    public void OnClickDelete()
    {
        if (controller == null || slot == null)
            return;

        controller.OnDeleteSlot(slot.SlotId);
    }
}