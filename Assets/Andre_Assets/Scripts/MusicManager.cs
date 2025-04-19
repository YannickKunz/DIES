using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public AudioSource normalMusic; // Reference to the normal music AudioSource
    public AudioSource triggerMusic; // Reference to the trigger music AudioSource

    private void Start()
    {
        // Ensure normal music starts playing
        if (normalMusic != null)
        {
            normalMusic.Play();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the player enters the trigger zone
        if (other.CompareTag("Player"))
        {
            if (normalMusic != null) normalMusic.Pause();
            if (triggerMusic != null) triggerMusic.Play();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if the player exits the trigger zone
        if (other.CompareTag("Player"))
        {
            if (triggerMusic != null) triggerMusic.Stop();
            if (normalMusic != null) normalMusic.Play();
        }
    }
}