using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("UI Sounds")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    [Header("Settings")]
    [Range(0f, 1f)] public float hoverVolume = 0.5f;
    [Range(0f, 1f)] public float clickVolume = 0.5f;

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // persists across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayHover()
    {
        PlaySound(hoverSound);
    }

    public void PlayClick()
    {
        PlaySound(clickSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            float volume = clip == hoverSound ? hoverVolume : clickVolume;
            audioSource.PlayOneShot(clip, volume);
        }
    }
}