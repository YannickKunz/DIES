using UnityEngine;

public class MusicTrigger : MonoBehaviour
{
    public AudioClip musicClip;
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
        
        if (musicClip == null)
        {
            Debug.LogError("MusicTrigger: No AudioClip assigned!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Something entered the trigger: " + other.name);

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered the trigger.");

            if ((!hasPlayed || !playOnlyOnce) && musicSource != null && musicClip != null)
            {
                Debug.Log("Trigger conditions met. Playing music...");
                musicSource.Stop();
                musicSource.clip = musicClip;
                musicSource.volume = volume;
                musicSource.Play();
                Debug.Log("Music triggered! Volume: " + musicSource.volume);

                if (playOnlyOnce)
                    hasPlayed = true;
            }
            else
            {
                Debug.Log("Trigger conditions not met.");
            }
            }
            else
            {
                Debug.Log("Non-player object entered the trigger.");
            }
    }

    private void OnTriggerExit2D(Collider2D other)
{
    if (other.CompareTag("Player"))
    {
        // Optional: stop music or revert to previous
        musicSource.Stop();
    }
}

}