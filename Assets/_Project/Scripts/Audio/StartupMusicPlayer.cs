using System.Collections;
using UnityEngine;

public sealed class StartupMusicPlayer : MonoBehaviour
{
    private const string MusicResourcePath = "Music/EpicStarWarsCompilation";
    public const string VolumePrefKey = "cosma_music_volume";

    private static StartupMusicPlayer instance;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private string clipResourcePath = MusicResourcePath;
    [SerializeField, Range(0f, 1f)] private float volume = 0.12f;
    [SerializeField] private bool loopPlayback = true;
    [SerializeField, Min(0f)] private float initialPlaybackDelaySeconds = 0.15f;

    private Coroutine loadRoutine;
    private bool playbackRequested;

    public static float GetSavedVolume(float fallback = 0.12f) =>
        PlayerPrefs.GetFloat(VolumePrefKey, fallback);

    public static void SetVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumePrefKey, value);
        PlayerPrefs.Save();
        if (instance != null)
        {
            instance.volume = value;
            if (instance.audioSource != null)
                instance.audioSource.volume = value;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            instance.BeginLoadingAndPlayback();
            return;
        }

        StartupMusicPlayer existingInstance = FindAnyObjectByType<StartupMusicPlayer>();
        if (existingInstance != null)
        {
            instance = existingInstance;
            instance.BeginLoadingAndPlayback();
            return;
        }

        GameObject musicObject = new GameObject("StartupMusicPlayer");
        instance = musicObject.AddComponent<StartupMusicPlayer>();
        instance.BeginLoadingAndPlayback();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureAudioSource();
    }

    private void OnEnable()
    {
        ExecutionPauseController.PauseStateChanged += HandlePauseStateChanged;
        BeginLoadingAndPlayback();
    }

    private void OnDisable()
    {
        ExecutionPauseController.PauseStateChanged -= HandlePauseStateChanged;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void BeginLoadingAndPlayback()
    {
        playbackRequested = true;
        EnsureAudioSource();
        if (audioSource == null)
        {
            return;
        }

        ApplyAudioSourceSettings();

        if (audioSource.clip != null)
        {
            ResumePlaybackIfPossible();
            return;
        }

        if (loadRoutine == null && isActiveAndEnabled)
        {
            loadRoutine = StartCoroutine(LoadAndPlayRoutine());
        }
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        ApplyAudioSourceSettings();
    }

    private void ApplyAudioSourceSettings()
    {
        if (audioSource == null)
        {
            return;
        }

        if (PlayerPrefs.HasKey(VolumePrefKey))
        {
            volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefKey, volume));
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.dopplerLevel = 0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.loop = loopPlayback;
        audioSource.volume = volume;
    }

    private void HandlePauseStateChanged(bool paused)
    {
        if (audioSource == null)
        {
            return;
        }

        if (paused)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
            }

            return;
        }

        ResumePlaybackIfPossible();
    }

    private IEnumerator LoadAndPlayRoutine()
    {
        if (initialPlaybackDelaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(initialPlaybackDelaySeconds);
        }
        else
        {
            yield return null;
        }

        ResourceRequest clipLoadRequest = Resources.LoadAsync<AudioClip>(clipResourcePath);
        yield return clipLoadRequest;

        loadRoutine = null;

        audioSource = audioSource != null ? audioSource : GetComponent<AudioSource>();
        if (audioSource == null)
        {
            yield break;
        }

        audioSource.clip = clipLoadRequest.asset as AudioClip;
        if (audioSource.clip == null)
        {
            Debug.LogWarning($"Startup music clip not found at Resources/{clipResourcePath}.");
            yield break;
        }

        ApplyAudioSourceSettings();
        ResumePlaybackIfPossible();
    }

    private void ResumePlaybackIfPossible()
    {
        if (!playbackRequested || audioSource == null || audioSource.clip == null)
        {
            return;
        }

        if (ExecutionPauseController.IsPaused)
        {
            audioSource.Pause();
            return;
        }

        audioSource.UnPause();
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
}
