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

    private const int MaxSlots = 3;

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
        EnsureFixedSlotsExist();
    }

    // Keep this method name so other scripts do not break,
    // but now it only creates metadata for an EMPTY fixed slot if needed.
    public string CreateNewSlot(string displayName)
    {
        SaveSlotMeta emptySlot = GetAllSlots().FirstOrDefault(x => !x.HasData);
        if (emptySlot == null)
        {
            Debug.LogWarning("[SaveManager] No empty save slot available.");
            return null;
        }

        CurrentSlotId = emptySlot.SlotId;
        return emptySlot.SlotId;
    }

    public void SetCurrentSlot(string slotId)
    {
        if (!IsValidFixedSlot(slotId))
        {
            Debug.LogWarning($"[SaveManager] Invalid slot id: {slotId}");
            return;
        }

        CurrentSlotId = slotId;
    }

    public void Save(SaveData data)
    {
        if (data == null)
        {
            Debug.LogError("[SaveManager] Cannot save null SaveData.");
            return;
        }

        if (string.IsNullOrWhiteSpace(data.SlotId))
        {
            if (string.IsNullOrWhiteSpace(CurrentSlotId))
                throw new Exception("No current save slot selected.");

            data.SlotId = CurrentSlotId;
        }

        if (!IsValidFixedSlot(data.SlotId))
        {
            Debug.LogError($"[SaveManager] Invalid fixed slot id: {data.SlotId}");
            return;
        }

        if (string.IsNullOrWhiteSpace(data.CreatedAtUtc))
            data.CreatedAtUtc = DateTime.UtcNow.ToString("o");

        data.UpdatedAtUtc = DateTime.UtcNow.ToString("o");
        CurrentSlotId = data.SlotId;

        WriteSave(data);
        UpdateManifest(data);
    }

    public SaveData Load(string slotId)
    {
        if (!IsValidFixedSlot(slotId))
        {
            Debug.LogWarning($"[SaveManager] Invalid slot id: {slotId}");
            return null;
        }

        string path = GetSlotPath(slotId);
        if (!File.Exists(path))
        {
            return null;
        }

        string json = File.ReadAllText(path);
        SaveData data = JsonConvert.DeserializeObject<SaveData>(json);

        if (data == null)
        {
            Debug.LogWarning($"[SaveManager] Failed to deserialize save in slot: {slotId}");
            return null;
        }

        CurrentSlotId = slotId;
        return data;
    }

    public List<SaveSlotMeta> GetAllSlots()
    {
        EnsureFixedSlotsExist();

        SaveManifest manifest = LoadManifest();

        // Always return all 3 slots in fixed order
        List<SaveSlotMeta> result = new();

        for (int i = 1; i <= MaxSlots; i++)
        {
            string slotId = GetFixedSlotId(i);
            SaveSlotMeta meta = manifest.Slots.FirstOrDefault(x => x.SlotId == slotId);

            if (meta == null)
            {
                meta = CreateEmptyMeta(slotId, i);
            }
            else
            {
                string path = GetSlotPath(slotId);
                meta.HasData = File.Exists(path);

                if (!meta.HasData)
                {
                    meta.SaveDisplayName = $"Slot {i}";
                    meta.UpdatedAtUtc = "";
                }
            }

            result.Add(meta);
        }

        return result;
    }

    public bool SlotHasData(string slotId)
    {
        if (!IsValidFixedSlot(slotId))
            return false;

        return File.Exists(GetSlotPath(slotId));
    }

    public void DeleteSlot(string slotId)
    {
        if (!IsValidFixedSlot(slotId))
        {
            Debug.LogWarning($"[SaveManager] Invalid slot id: {slotId}");
            return;
        }

        string path = GetSlotPath(slotId);
        if (File.Exists(path))
            File.Delete(path);

        SaveManifest manifest = LoadManifest();
        SaveSlotMeta existing = manifest.Slots.FirstOrDefault(x => x.SlotId == slotId);

        int slotNumber = GetSlotNumber(slotId);

        if (existing == null)
        {
            manifest.Slots.Add(CreateEmptyMeta(slotId, slotNumber));
        }
        else
        {
            existing.SaveDisplayName = $"Slot {slotNumber}";
            existing.UpdatedAtUtc = "";
            existing.HasData = false;
        }

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
                SaveDisplayName = string.IsNullOrWhiteSpace(data.SaveDisplayName)
                    ? $"Slot {GetSlotNumber(data.SlotId)}"
                    : data.SaveDisplayName,
                UpdatedAtUtc = data.UpdatedAtUtc,
                HasData = true
            });
        }
        else
        {
            existing.SaveDisplayName = string.IsNullOrWhiteSpace(data.SaveDisplayName)
                ? $"Slot {GetSlotNumber(data.SlotId)}"
                : data.SaveDisplayName;
            existing.UpdatedAtUtc = data.UpdatedAtUtc;
            existing.HasData = true;
        }

        File.WriteAllText(ManifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));
    }

    private SaveManifest LoadManifest()
    {
        if (!File.Exists(ManifestPath))
            return CreateDefaultManifest();

        string json = File.ReadAllText(ManifestPath);
        SaveManifest manifest = JsonConvert.DeserializeObject<SaveManifest>(json) ?? new SaveManifest();

        if (manifest.Slots == null)
            manifest.Slots = new List<SaveSlotMeta>();

        return manifest;
    }

    private void EnsureFixedSlotsExist()
    {
        SaveManifest manifest = LoadManifest();
        bool changed = false;

        for (int i = 1; i <= MaxSlots; i++)
        {
            string slotId = GetFixedSlotId(i);
            SaveSlotMeta existing = manifest.Slots.FirstOrDefault(x => x.SlotId == slotId);

            if (existing == null)
            {
                manifest.Slots.Add(CreateEmptyMeta(slotId, i));
                changed = true;
            }
            else
            {
                string path = GetSlotPath(slotId);
                bool hasData = File.Exists(path);

                if (existing.HasData != hasData)
                {
                    existing.HasData = hasData;
                    changed = true;
                }

                if (!hasData)
                {
                    string expectedName = $"Slot {i}";
                    if (existing.SaveDisplayName != expectedName || !string.IsNullOrEmpty(existing.UpdatedAtUtc))
                    {
                        existing.SaveDisplayName = expectedName;
                        existing.UpdatedAtUtc = "";
                        changed = true;
                    }
                }
            }
        }

        // Remove old random GUID-based slots from manifest
        int removed = manifest.Slots.RemoveAll(x => !IsValidFixedSlot(x.SlotId));
        if (removed > 0)
            changed = true;

        manifest.Slots = manifest.Slots
            .OrderBy(x => GetSlotNumber(x.SlotId))
            .ToList();

        if (changed)
        {
            File.WriteAllText(ManifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));
        }
    }

    private SaveManifest CreateDefaultManifest()
    {
        SaveManifest manifest = new SaveManifest();

        for (int i = 1; i <= MaxSlots; i++)
        {
            manifest.Slots.Add(CreateEmptyMeta(GetFixedSlotId(i), i));
        }

        return manifest;
    }

    private SaveSlotMeta CreateEmptyMeta(string slotId, int slotNumber)
    {
        return new SaveSlotMeta
        {
            SlotId = slotId,
            SaveDisplayName = $"Slot {slotNumber}",
            UpdatedAtUtc = "",
            HasData = false
        };
    }

    private string GetFixedSlotId(int slotNumber)
    {
        return $"slot_{slotNumber}";
    }

    private bool IsValidFixedSlot(string slotId)
    {
        return slotId == "slot_1" || slotId == "slot_2" || slotId == "slot_3";
    }

    private int GetSlotNumber(string slotId)
    {
        return slotId switch
        {
            "slot_1" => 1,
            "slot_2" => 2,
            "slot_3" => 3,
            _ => -1
        };
    }

    private string GetSlotPath(string slotId)
    {
        return Path.Combine(SaveFolder, $"{slotId}.json");
    }
}