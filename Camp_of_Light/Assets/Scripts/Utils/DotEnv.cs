using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class DotEnv
{
    private static Dictionary<string, string> env = new Dictionary<string, string>();

    public static void Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($".env file not found at {filePath}");
            return;
        }

        foreach (var line in File.ReadAllLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var parts = line.Split('=', 2);
            if (parts.Length != 2)
                continue;

            string key = parts[0].Trim();
            string value = parts[1].Trim();

            // remove quotes if present
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Substring(1, value.Length - 2);
            }

            env[key] = value;
        }
    }

    public static string Get(string key)
    {
        if (env.TryGetValue(key, out var value))
            return value;

        Debug.LogWarning($"Key {key} not found in .env");
        return null;
    }
}