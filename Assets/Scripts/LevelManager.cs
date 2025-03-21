using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    private enum LevelState
    {
        Ready, Start, Play, End, NextScene
    }

    public int scoreToWin;
    public int levelTimeInSeconds;
    public string nextScene;
    public float secondsBeforeLoadingNextScene = 0.5f;

    private LevelState levelState = LevelState.Ready;
    private int collectedGarbage = 0;
    private Text scoreText;
    private Text timerText;
    private Text countdownText;
    private Text endText;
    private float time;
    private float countdownTime;
    private readonly float countdownStartTime = 3;
    private readonly int countdownMinSize = 1;
    private readonly int countdownMaxSize = 150;
    private GameObject[] garbage;
    private GameObject startPanel;
    private GameObject uiPanel;
    private GameObject endPanel;
    private GameObject player;
    private Vector3 playerStartPosition;
    private bool won = false;
    private SoundManager soundManager;
    private AudioSource musicSource;
    private float nextSceneLoadingTime;

    void Start()
    {
        //get required components
        AudioSource[] sources = GetComponents<AudioSource>();
        AudioSource audioSource = sources[0];
        if (audioSource != null)
        {
            soundManager = new SoundManager(audioSource);
        }
        musicSource = sources[1];
        GameObject scoreGameObject = GameObject.Find("ScoreText");
        if (scoreGameObject != null)
            scoreText = scoreGameObject.GetComponent<Text>();
        GameObject timerGameObject = GameObject.Find("TimerText");
        if (timerGameObject != null)
            timerText = timerGameObject.GetComponent<Text>();
        uiPanel = GameObject.Find("UIPanel");
        startPanel = GameObject.Find("StartPanel");
        GameObject countdownTextGameObject = GameObject.Find("CountdownText");
        if (countdownTextGameObject != null)
            countdownText = countdownTextGameObject.GetComponent<Text>();
        endPanel = GameObject.Find("EndPanel");
        if (endPanel != null)
            endText = GameObject.Find("EndText").GetComponent<Text>();
        player = GameObject.Find("Player");
        if (player != null)
            playerStartPosition = player.transform.position;
        else
            Debug.LogError("Object with 'Player' name not found");
        garbage = GameObject.FindGameObjectsWithTag("Garbage");
        //init options
        InitLevelParameters();
        nextSceneLoadingTime = secondsBeforeLoadingNextScene;
    }

    // Update is called once per frame
    void Update()
    {
        switch (levelState)
        {
            case LevelState.Ready:
                if (Input.GetButtonDown("Jump") || Input.GetButtonDown("Fire1"))
                {
                    levelState = LevelState.Start;
                    startPanel.SetActive(false);
                    countdownText.gameObject.SetActive(true);
                    countdownTime = countdownStartTime;
                }
                break;
            case LevelState.Start:
                UpdateCountDown();
                break;
            case LevelState.Play:
                time -= Time.deltaTime;
                if (time <= 0)
                {
                    Lose();
                }
                else
                {
                    int minutes = (int)(time / 60);
                    int seconds = (int)(time - minutes * 60);
                    timerText.text = minutes + ":" + (seconds < 10 ? "0" : "") + seconds;
                }
                break;
            case LevelState.End:
                if (Input.GetButtonDown("Jump") || Input.GetButtonDown("Fire1"))
                {
                    PlaySFX(SoundManager.BLEURGH);
                    if (won)
                    {
                        levelState = LevelState.NextScene;
                    }
                    else
                    {
                        InitLevelParameters();
                    }
                }
                break;
            case LevelState.NextScene:
                if (nextSceneLoadingTime < 0)
                {
                    if (nextScene == null || nextScene.Trim().Length == 0)
                        SceneManager.LoadScene("end_screen");
                    else
                        SceneManager.LoadScene(nextScene);
                }
                nextSceneLoadingTime -= Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0, 1, nextSceneLoadingTime / secondsBeforeLoadingNextScene);
                break;
        }
    }

    public bool CanPlay()
    {
        return levelState == LevelState.Play;
    }

    public void InitLevelParameters()
    {
        collectedGarbage = 0;
        if (scoreText != null)
        {
            scoreText.text = "" + collectedGarbage + "/" + scoreToWin;
        }
        time = levelTimeInSeconds;
        levelState = LevelState.Ready;
        if (uiPanel != null)
            uiPanel.SetActive(false);
        if (startPanel != null)
            startPanel.SetActive(true);
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
        if (endPanel != null)
            endPanel.SetActive(false);
        if (player != null)
        {
            player.transform.position = playerStartPosition;
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector3.zero;
        }
        for (int g = 0; g < garbage.Length; g++)
        {
            garbage[g].SetActive(true);
        }
    }

    public void IncrementAmountOfGarbage()
    {
        collectedGarbage++;
        scoreText.text = "" + collectedGarbage + "/" + scoreToWin;
        if (collectedGarbage == scoreToWin)
        {
            Win();
        }
    }

    public void Win()
    {
        endText.text = "You won!\n\n<size=40>press jump button to continue</size>";
        endPanel.SetActive(true);
        levelState = LevelState.End;
        uiPanel.SetActive(false);
        won = true;
        PlaySFX(SoundManager.WIN);
    }

    public void Lose()
    {
        endText.text = "You lose!\n\n<size=40>press jump button to restart</size>";
        endPanel.SetActive(true);
        levelState = LevelState.End;
        uiPanel.SetActive(false);
        won = false;
        PlaySFX(SoundManager.LOSE);
    }

    private void UpdateCountDown()
    {
        countdownTime -= Time.deltaTime;
        float fontSize = Mathf.Lerp(countdownMinSize, countdownMaxSize, 1 - (countdownTime - (int)countdownTime));
        countdownText.fontSize = (int)Mathf.Round(fontSize);
        string previousText = countdownText.text;
        if (countdownTime < -1)
        {
            levelState = LevelState.Play;
            countdownText.gameObject.SetActive(false);
            uiPanel.SetActive(true);
        }
        else if (countdownTime < 0)
        {
            countdownText.text = "GO!";
            countdownText.fontSize = countdownMaxSize;
        }
        else
        {
            countdownText.text = "" + (int)(countdownTime + 1);
        }
        if (!countdownText.text.Equals(previousText))
        {
            StopSFX();
            if (countdownTime < 0)
                PlaySFX(SoundManager.COUNTDOWN_GO);
            else
                PlaySFX(SoundManager.COUNTDOWN);
        }
    }

    public void PlaySFX(int soundEffect)
    {
        if (soundManager != null)
            soundManager.PlaySFX(soundEffect);
    }

    public void StopSFX()
    {
        if (soundManager != null)
            soundManager.StopSFX();
    }
}
