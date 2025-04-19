using UnityEngine;

public class MusicTrigger : MonoBehaviour
{
    public AudioClip triggerMusicClip; // Music to play in the trigger zone
    public AudioClip defaultMusicClip; // Default background music
    public AudioSource musicSource;
    public bool playOnlyOnce = true;
    [Range(0f, 1f)]
    public float volume = 1f;

    private bool hasPlayed = false;

    private void Start()
    {
        // Validate components
        if (musicSource == null)
        {
            Debug.LogError("MusicTrigger: No AudioSource assigned!");
        }

        if (triggerMusicClip == null)
        {
            Debug.LogError("MusicTrigger: No trigger AudioClip assigned!");
        }

        if (defaultMusicClip == null)
        {
            Debug.LogError("MusicTrigger: No default AudioClip assigned!");
        }

        // Start playing the default music
        if (musicSource != null && defaultMusicClip != null)
        {
            musicSource.clip = defaultMusicClip;
            musicSource.volume = volume;
            musicSource.Play();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if ((!hasPlayed || !playOnlyOnce) && musicSource != null && triggerMusicClip != null)
            {
                musicSource.Stop();
                musicSource.clip = triggerMusicClip;
                musicSource.volume = volume;
                musicSource.Play();

                if (playOnlyOnce)
                    hasPlayed = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Revert to default background music
            if (musicSource != null && defaultMusicClip != null)
            {
                musicSource.Stop();
                musicSource.clip = defaultMusicClip;
                musicSource.volume = volume;
                musicSource.Play();
            }
        }
    }
}