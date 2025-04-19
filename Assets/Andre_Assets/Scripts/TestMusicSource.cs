using UnityEngine;

public class TestMusicSource : MonoBehaviour
{
    public AudioSource musicSource; // Assign the Audio Source in the Inspector

    void Update()
    {
        // Press "P" to play the music
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
                Debug.Log("Music started playing.");
            }
        }

        // Press "S" to stop the music
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (musicSource.isPlaying)
            {
                musicSource.Stop();
                Debug.Log("Music stopped.");
            }
        }

        // Press "L" to toggle looping
        if (Input.GetKeyDown(KeyCode.L))
        {
            musicSource.loop = !musicSource.loop;
            Debug.Log("Looping toggled: " + musicSource.loop);
        }
    }
}