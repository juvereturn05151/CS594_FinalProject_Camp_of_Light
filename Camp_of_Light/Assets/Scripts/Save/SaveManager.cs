using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public string CurrentSlotId { get; private set; }

    private string SaveFolder => Path.Combine(Application.persistentDataPath, "Saves");
    private string ManifestPath => Path.Combine(SaveFolder, "manifest.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Directory.CreateDirectory(SaveFolder);
    }

    public string CreateNewSlot(string displayName)
    {
        string slotId = Guid.NewGuid().ToString("N");
        CurrentSlotId = slotId;

        SaveData data = new SaveData
        {
            SlotId = slotId,
            SaveDisplayName = string.IsNullOrWhiteSpace(displayName) ? "New Save" : displayName,
            CreatedAtUtc = DateTime.UtcNow.ToString("o"),
            UpdatedAtUtc = DateTime.UtcNow.ToString("o")
        };

        WriteSave(data);
        UpdateManifest(data);
        return slotId;
    }

    public void SetCurrentSlot(string slotId)
    {
        CurrentSlotId = slotId;
    }

    public void Save(SaveData data)
    {
        if (string.IsNullOrWhiteSpace(data.SlotId))
        {
            if (string.IsNullOrWhiteSpace(CurrentSlotId))
                throw new Exception("No current save slot selected.");

            data.SlotId = CurrentSlotId;
        }

        data.UpdatedAtUtc = DateTime.UtcNow.ToString("o");
        CurrentSlotId = data.SlotId;

        WriteSave(data);
        UpdateManifest(data);
    }

    public SaveData Load(string slotId)
    {
        string path = GetSlotPath(slotId);
        if (!File.Exists(path))
        {
            Debug.LogError($"Save file not found: {path}");
            return null;
        }

        string json = File.ReadAllText(path);
        SaveData data = JsonConvert.DeserializeObject<SaveData>(json);
        CurrentSlotId = slotId;
        return data;
    }

    public List<SaveSlotMeta> GetAllSlots()
    {
        SaveManifest manifest = LoadManifest();
        return manifest.Slots
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ToList();
    }

    public void DeleteSlot(string slotId)
    {
        string path = GetSlotPath(slotId);
        if (File.Exists(path))
            File.Delete(path);

        SaveManifest manifest = LoadManifest();
        manifest.Slots.RemoveAll(x => x.SlotId == slotId);
        File.WriteAllText(ManifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));

        if (CurrentSlotId == slotId)
            CurrentSlotId = null;
    }

    private void WriteSave(SaveData data)
    {
        string path = GetSlotPath(data.SlotId);
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(path, json);
    }

    private void UpdateManifest(SaveData data)
    {
        SaveManifest manifest = LoadManifest();
        SaveSlotMeta existing = manifest.Slots.FirstOrDefault(x => x.SlotId == data.SlotId);

        if (existing == null)
        {
            manifest.Slots.Add(new SaveSlotMeta
            {
                SlotId = data.SlotId,
                SaveDisplayName = data.SaveDisplayName,
                UpdatedAtUtc = data.UpdatedAtUtc
            });
        }
        else
        {
            existing.SaveDisplayName = data.SaveDisplayName;
            existing.UpdatedAtUtc = data.UpdatedAtUtc;
        }

        File.WriteAllText(ManifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));
    }

    private SaveManifest LoadManifest()
    {
        if (!File.Exists(ManifestPath))
            return new SaveManifest();

        string json = File.ReadAllText(ManifestPath);
        return JsonConvert.DeserializeObject<SaveManifest>(json) ?? new SaveManifest();
    }

    private string GetSlotPath(string slotId)
    {
        return Path.Combine(SaveFolder, $"{slotId}.json");
    }
}