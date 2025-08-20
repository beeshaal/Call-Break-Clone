using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(0.1f, 3f)]
    public float pitch = 1f;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Settings")]
    [SerializeField] private Sound[] sfxSounds;
    [SerializeField] private AudioSource sfxSourcePrefab;
    [SerializeField] private float defaultGuaranteedPlayTime = 0.1f;

    private Dictionary<string, Sound> sfxSoundsDictionary;
    private Dictionary<AudioClip, float> sfxLastPlayedTime = new();

    public bool IsSoundEnabled = true;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSoundDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSoundDictionary()
    {
        sfxSoundsDictionary = new Dictionary<string, Sound>();

        foreach (Sound sound in sfxSounds)
        {
            if (!string.IsNullOrEmpty(sound.name) && sound.clip != null)
            {
                sfxSoundsDictionary[sound.name] = sound;
            }
        }
    }

    #region SFX Controls

    public void PlaySFX(string name, float guaranteedPlayTime = -1f)
    {
        if (!IsSoundEnabled) return;

        if (sfxSoundsDictionary.TryGetValue(name, out Sound sound))
        {
            if (IsGuaranteedTimeActive(sound.clip, guaranteedPlayTime)) return;

            AudioSource sfxSource = LeanPool.Spawn(sfxSourcePrefab, transform);
            sfxSource.clip = sound.clip;
            sfxSource.volume = sound.volume;
            sfxSource.pitch = sound.pitch;

            sfxSource.Play();
            StartCoroutine(ReleaseAfterPlay(sfxSource));

            sfxLastPlayedTime[sound.clip] = Time.time;
        }
        else
        {
            Debug.LogWarning($"SFX not found: {name}");
        }
    }

    public void PlaySFXWithRandomPitch(string name, float minPitch = 0.8f, float maxPitch = 1.2f, float guaranteedPlayTime = -1f)
    {
        if (!IsSoundEnabled) return;
        if (sfxSoundsDictionary.TryGetValue(name, out Sound sound))
        {
            if (IsGuaranteedTimeActive(sound.clip, guaranteedPlayTime)) return;

            AudioSource sfxSource = LeanPool.Spawn(sfxSourcePrefab, transform);
            sfxSource.clip = sound.clip;
            sfxSource.volume = sound.volume;
            sfxSource.pitch = Random.Range(minPitch, maxPitch);

            sfxSource.Play();
            StartCoroutine(ReleaseAfterPlay(sfxSource));

            sfxLastPlayedTime[sound.clip] = Time.time;
        }
        else
        {
            Debug.LogWarning($"SFX not found: {name}");
        }
    }

    public void StopSpecificSFX(string name)
    {
        if (sfxSoundsDictionary.TryGetValue(name, out Sound sound))
        {
            AudioSource[] activeSources = FindObjectsOfType<AudioSource>();

            foreach (var source in activeSources)
            {
                if (source.isPlaying && source.clip == sound.clip)
                {
                    source.Stop();
                    LeanPool.Despawn(source);
                }
            }
        }
    }

    private IEnumerator ReleaseAfterPlay(AudioSource sfx)
    {
        yield return new WaitWhile(() => sfx.isPlaying);
        LeanPool.Despawn(sfx);
    }

    private bool IsGuaranteedTimeActive(AudioClip clip, float overrideTime = -1f)
    {
        if (!sfxLastPlayedTime.TryGetValue(clip, out float lastTime)) return false;

        float guaranteedTime = overrideTime >= 0f ? overrideTime : defaultGuaranteedPlayTime;
        return Time.time - lastTime < guaranteedTime;
    }

    public int GetActiveSFXCount()
    {
        return FindObjectsOfType<AudioSource>().Length;
    }

    #endregion
}
