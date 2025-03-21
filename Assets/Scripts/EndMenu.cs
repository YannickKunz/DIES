using UnityEngine;
using UnityEngine.SceneManagement;

public class EndMenu : MonoBehaviour
{
    private SoundManager soundManager;
    public float secondsBeforeLoadingNextScene = 0.5f;
    private bool loadNextScene = false;

    void Start()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            soundManager = new SoundManager(audioSource);
        }
    }

    void Update()
    {
        if (Input.GetButtonDown("Jump") || Input.GetButtonDown("Fire1"))
        {
            if (soundManager != null)
                soundManager.PlaySFX(SoundManager.GARBAGE);
            loadNextScene = true;
        }
        if (loadNextScene)
        {
            if (secondsBeforeLoadingNextScene < 0)
                SceneManager.LoadScene("start_screen");
            secondsBeforeLoadingNextScene -= Time.deltaTime;
        }
    }
}
