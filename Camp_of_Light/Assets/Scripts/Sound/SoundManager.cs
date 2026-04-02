using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SoundEntry
{
    public string id;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    public bool loop = false;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Sound Library")]
    [SerializeField] private List<SoundEntry> musicList = new List<SoundEntry>();
    [SerializeField] private List<SoundEntry> sfxList = new List<SoundEntry>();

    [Header("Volumes")]
    [Range(0f, 1f)][SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float musicVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;

    private Dictionary<string, SoundEntry> musicDict = new Dictionary<string, SoundEntry>();
    private Dictionary<string, SoundEntry> sfxDict = new Dictionary<string, SoundEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildDictionaries();
        ApplyVolume();
    }

    private void Reset()
    {
        SetupDefaultAudioSources();
    }

    private void OnValidate()
    {
        if (musicSource != null || sfxSource != null)
        {
            ApplyVolume();
        }
    }

    private void SetupDefaultAudioSources()
    {
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }

        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }
    }

    private void BuildDictionaries()
    {
        musicDict.Clear();
        sfxDict.Clear();

        foreach (SoundEntry sound in musicList)
        {
            if (sound == null || string.IsNullOrWhiteSpace(sound.id) || sound.clip == null)
                continue;

            if (!musicDict.ContainsKey(sound.id))
                musicDict.Add(sound.id, sound);
        }

        foreach (SoundEntry sound in sfxList)
        {
            if (sound == null || string.IsNullOrWhiteSpace(sound.id) || sound.clip == null)
                continue;

            if (!sfxDict.ContainsKey(sound.id))
                sfxDict.Add(sound.id, sound);
        }
    }

    private void ApplyVolume()
    {
        if (musicSource != null)
            musicSource.volume = masterVolume * musicVolume;

        if (sfxSource != null)
            sfxSource.volume = masterVolume * sfxVolume;
    }

    public void PlayMusic(string id)
    {
        if (!musicDict.TryGetValue(id, out SoundEntry sound))
        {
            Debug.LogWarning($"[SoundManager] Music '{id}' not found.");
            return;
        }

        if (musicSource.clip == sound.clip && musicSource.isPlaying)
            return;

        musicSource.clip = sound.clip;
        musicSource.volume = masterVolume * musicVolume * sound.volume;
        musicSource.pitch = sound.pitch;
        musicSource.loop = sound.loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null)
            return;

        musicSource.Stop();
        musicSource.clip = null;
    }

    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (musicSource != null)
            musicSource.UnPause();
    }

    public void PlaySFX(string id)
    {
        if (!sfxDict.TryGetValue(id, out SoundEntry sound))
        {
            Debug.LogWarning($"[SoundManager] SFX '{id}' not found.");
            return;
        }

        sfxSource.pitch = sound.pitch;
        sfxSource.PlayOneShot(sound.clip, masterVolume * sfxVolume * sound.volume);
    }

    public void PlaySFX(string id, float volumeMultiplier)
    {
        if (!sfxDict.TryGetValue(id, out SoundEntry sound))
        {
            Debug.LogWarning($"[SoundManager] SFX '{id}' not found.");
            return;
        }

        sfxSource.pitch = sound.pitch;
        sfxSource.PlayOneShot(sound.clip, masterVolume * sfxVolume * sound.volume * volumeMultiplier);
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyVolume();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        ApplyVolume();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyVolume();
    }

    public float GetMasterVolume() => masterVolume;
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
}