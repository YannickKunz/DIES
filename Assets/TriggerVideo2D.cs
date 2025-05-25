using UnityEngine;
using UnityEngine.Video;

public class TriggerVideo2D : MonoBehaviour
{
    public VideoPlayer player;          // your VideoPlayer
    public MonoBehaviour followScript;  // your Camera-follow script component
    public string triggerTag = "Player";
    bool hasPlayed = false;

    void Start()
    {
        // make sure the video isn’t visible at start
        player.targetCameraAlpha = 0f;
        player.loopPointReached += OnVideoFinished;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasPlayed && other.CompareTag(triggerTag))
        {
            hasPlayed = true;
            if (followScript) followScript.enabled = false;
            player.targetCameraAlpha = 1f;
            player.Play();
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        vp.Stop();
        player.targetCameraAlpha = 0f;  // clear the last frame
        if (followScript) followScript.enabled = true;
    }
}
