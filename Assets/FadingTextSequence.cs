using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // For TextMeshPro
using System.Collections;

public class FadingTextSequence : MonoBehaviour
{
    [Header("Text Setup")]
    public TextMeshProUGUI displayText; // Assign your TextMeshPro object
    public string[] textLines;          // The sequence of text to display
    public Color textColor = Color.white; // Base color for the text (alpha will be animated)

    [Header("Timing")]
    public float fadeInDuration = 1.0f;
    public float displayDuration = 3.0f; // How long text stays fully visible
    public float fadeOutDuration = 1.0f;
    public float delayBetweenTexts = 0.5f; // Delay after one text fades out before next fades in

    [Header("Scene Transition")]
    public string nextSceneName = "GameplayScene";
    public float delayBeforeNextScene = 1.0f; // Delay after all texts shown before loading next scene

    private int currentTextIndex = 0;
    private bool skipInitiated = false;

    void Start()
    {
        if (displayText == null)
        {
            Debug.LogError("DisplayText object not assigned!");
            enabled = false;
            return;
        }
        if (textLines == null || textLines.Length == 0)
        {
            Debug.LogWarning("No text lines provided. Proceeding to next scene.");
            StartCoroutine(LoadNextSceneAfterDelay(0)); // Or a small delay
            return;
        }

        // Initialize text to be fully transparent with the desired base color
        displayText.text = "";
        displayText.color = new Color(textColor.r, textColor.g, textColor.b, 0);

        StartCoroutine(AnimateTextSequence());
    }

    void Update()
    {
        // Allow skipping
        //if (Input.anyKeyDown && !skipInitiated)
        //{
        //    Debug.Log("Skipping text sequence...");
        //    skipInitiated = true;
        //    StopAllCoroutines(); // Stop any ongoing fading or waiting
        //    SceneManager.LoadScene(nextSceneName);
        //}
    }

    IEnumerator AnimateTextSequence()
    {
        foreach (string line in textLines)
        {
            if (skipInitiated) yield break;

            displayText.text = line;

            // Fade In
            yield return StartCoroutine(FadeText(displayText, fadeInDuration, textColor.a, 1f)); // Fade to full alpha of base color

            // Display
            if (skipInitiated) yield break;
            yield return new WaitForSeconds(displayDuration);

            // Fade Out
            if (skipInitiated) yield break;
            yield return StartCoroutine(FadeText(displayText, fadeOutDuration, displayText.color.a, 0f));

            // Delay before next text (if not the last one)
            if (skipInitiated) yield break;
            if (currentTextIndex < textLines.Length - 1)
            {
                yield return new WaitForSeconds(delayBetweenTexts);
            }
            currentTextIndex++;
        }

        // All texts shown, proceed to next scene
        if (!skipInitiated)
        {
            StartCoroutine(LoadNextSceneAfterDelay(delayBeforeNextScene));
        }
    }

    IEnumerator FadeText(TextMeshProUGUI textElement, float duration, float startAlpha, float targetAlpha)
    {
        if (skipInitiated) yield break;

        float timer = 0f;
        Color currentColor = textElement.color; // Get current color (to preserve RGB)

        while (timer < duration)
        {
            if (skipInitiated) yield break;

            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            textElement.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
            yield return null;
        }

        // Ensure target alpha is set
        if (!skipInitiated)
        {
            textElement.color = new Color(currentColor.r, currentColor.g, currentColor.b, targetAlpha);
        }
    }

    IEnumerator LoadNextSceneAfterDelay(float delay)
    {
        if (skipInitiated) yield break;

        yield return new WaitForSeconds(delay);

        if (!skipInitiated) // Double check skip wasn't pressed during delay
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}