using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public string firstLevel;
    public float secondsBeforeLoadingNextScene = 0.5f;
    private bool loadNextScene = false;
    private SoundManager soundManager;
    private AudioSource musicSource;
    private float nextSceneLoadingTime;

    void Start()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        AudioSource audioSource = sources[0];
        if (audioSource != null)
        {
            soundManager = new SoundManager(audioSource);
        }
        musicSource = sources[1];
        nextSceneLoadingTime = secondsBeforeLoadingNextScene;
    }

    void Update()
    {
        if (Input.GetButtonDown("Jump") || Input.GetButtonDown("Fire1"))
        {
            if (soundManager != null)
                soundManager.PlaySFX(SoundManager.BLEURGH);
            loadNextScene = true;
        }
        if (loadNextScene)
        {
            if (nextSceneLoadingTime < 0)
                SceneManager.LoadScene(firstLevel);
            nextSceneLoadingTime -= Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0, 1, nextSceneLoadingTime / secondsBeforeLoadingNextScene);
        }
    }
}
