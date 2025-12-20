using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Playback")]
    [Tooltip("Play on start (first scene where this object exists).")]
    public bool playOnStart = true;

    [Tooltip("Default clip used when no scene-specific clip is found.")]
    public AudioClip defaultClip;

    [Range(0f, 1f)]
    public float volume = 0.5f;
    [Tooltip("Seconds to fade out/in when switching music.")]
    public float fadeDuration = 1f;

    [Header("Scene -> Music mapping (assign in Inspector)")]
    [Tooltip("You may specify either a Scene Name or a Build Index. Build Index takes precedence when >= 0.")]
    public List<SceneMusicEntry> sceneMusic = new List<SceneMusicEntry>();

    [Header("Optional")]
    [Tooltip("If true, when PlayMusic is called with the same clip currently playing, it will not restart.")]
    public bool skipIfSameClip = true;

    AudioSource audioSource;
    Coroutine fadeCoroutine;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D music
        audioSource.volume = volume;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (playOnStart)
        {
            // Use currently active scene to decide music
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Try build index first (if entry exists with matching buildIndex >= 0)
        SceneMusicEntry match = null;

        foreach (var entry in sceneMusic)
        {
            if (entry.buildIndex >= 0 && entry.buildIndex == scene.buildIndex)
            {
                match = entry;
                break;
            }
        }

        // If not found by buildIndex, try by name
        if (match == null)
        {
            foreach (var entry in sceneMusic)
            {
                if (!string.IsNullOrEmpty(entry.sceneName) && entry.sceneName == scene.name)
                {
                    match = entry;
                    break;
                }
            }
        }

        if (match != null && match.clip != null)
        {
            PlayMusic(match.clip);
        }
        else
        {
            if (defaultClip != null)
                PlayMusic(defaultClip);
            else
                StopMusic();
        }
    }

    /// <summary>Play an AudioClip (with crossfade).</summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (skipIfSameClip && audioSource.clip == clip && audioSource.isPlaying) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeToNewClip(clip));
    }

    /// <summary>Stop immediately (no fade)</summary>
    public void StopMusicImmediate()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        audioSource.Stop();
        audioSource.clip = null;
    }

    /// <summary>Stop with fade out.</summary>
    public void StopMusic()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutAndStop());
    }

    IEnumerator FadeToNewClip(AudioClip newClip)
    {
        float t = 0f;
        float startVol = audioSource.isPlaying ? audioSource.volume : 0f;
        // Fade out
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();

        // Fade in
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, volume, t / fadeDuration);
            yield return null;
        }
        audioSource.volume = volume;
        fadeCoroutine = null;
    }

    IEnumerator FadeOutAndStop()
    {
        float t = 0f;
        float startVol = audioSource.volume;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
            yield return null;
        }
        audioSource.Stop();
        audioSource.clip = null;
        audioSource.volume = volume;
        fadeCoroutine = null;
    }

    // Exposed API for runtime control
    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        audioSource.volume = volume;
    }

    public void ChangeMusicImmediate(AudioClip clip)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
    }

    [System.Serializable]
    public class SceneMusicEntry
    {
        [Tooltip("Optional: the exact scene name (case-sensitive). Used if buildIndex < 0 or no buildIndex match found.")]
        public string sceneName = "";

        [Tooltip("Optional: build index of scene in Build Settings. Set to -1 to ignore. (Build index takes precedence)")]
        public int buildIndex = -1;

        [Tooltip("AudioClip to play for this scene.")]
        public AudioClip clip = null;
    }
}
