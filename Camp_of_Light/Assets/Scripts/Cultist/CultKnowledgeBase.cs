using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class CultKnowledgeBase : MonoBehaviour
{
    [Header("JSON Sources")]
    [SerializeField] private string doctrineResourcePath = "Data/cult_doctrine";
    [SerializeField] private string tacticsResourcePath = "Data/cult_tactics";

    public List<CultDoctrineEntry> DoctrineEntries { get; private set; } = new();
    public List<CultTacticEntry> TacticEntries { get; private set; } = new();

    private void Awake()
    {
        LoadAll();
    }

    public void LoadAll()
    {
        DoctrineEntries = LoadList<CultDoctrineEntry>(doctrineResourcePath);
        TacticEntries = LoadList<CultTacticEntry>(tacticsResourcePath);

        Debug.Log($"[CultKnowledgeBase] Loaded {DoctrineEntries.Count} doctrine entries and {TacticEntries.Count} tactic entries.");
    }

    private List<T> LoadList<T>(string resourcePath)
    {
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);

        if (textAsset == null)
        {
            Debug.LogError($"[CultKnowledgeBase] Failed to load JSON at Resources/{resourcePath}.json");
            return new List<T>();
        }

        try
        {
            var list = JsonConvert.DeserializeObject<List<T>>(textAsset.text);
            return list ?? new List<T>();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CultKnowledgeBase] Failed to parse JSON at {resourcePath}: {e}");
            return new List<T>();
        }
    }
}