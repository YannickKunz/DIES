using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Make sure to include this for TextMeshPro
using System.Collections;

public class TextScroller : MonoBehaviour
{
    [Header("Text Setup")]
    public TextMeshProUGUI scrollingText; // Assign your TextMeshPro object
    public float scrollSpeed = 50f;       // Pixels per second (positive for upward scroll)

    [Header("Scene Transition")]
    public string nextSceneName = "GameplayScene"; // Name of the scene to load after scroll
    public float delayAfterScrollFinishes = 2.0f; // Delay before loading next scene

    [Header("Positioning & Control (Upward Scroll)")]
    [Tooltip("How far BELOW the bottom of the screen the text should start. " +
             "If 0, text top starts at screen bottom. " +
             "If positive, text starts further down (off-screen).")]
    public float startOffsetFromScreenBottom = 0f;

    [Tooltip("Extra space above the screen top before considering text fully scrolled.")]
    public float paddingAboveScreen = 100f;

    private RectTransform textRectTransform;
    private RectTransform canvasRectTransform;
    private float initialTextYPosition; // Y position of the text's pivot (bottom edge)
    private float targetYPosition;      // Y position of the text's pivot (bottom edge) when it's off-screen
    private bool isScrolling = true;
    private bool skipInitiated = false;

    void Start()
    {
        if (scrollingText == null)
        {
            Debug.LogError("ScrollingText object not assigned!");
            enabled = false; // Disable script if no text
            return;
        }

        textRectTransform = scrollingText.GetComponent<RectTransform>();
        if (textRectTransform.anchorMin.y != 0 || textRectTransform.anchorMax.y != 0 || textRectTransform.pivot.y != 0)
        {
            // Forcing:
            textRectTransform.anchorMin = new Vector2(textRectTransform.anchorMin.x, 0);
            textRectTransform.anchorMax = new Vector2(textRectTransform.anchorMax.x, 0);
            textRectTransform.pivot = new Vector2(textRectTransform.pivot.x, 0);
        }

        canvasRectTransform = scrollingText.canvas.GetComponent<RectTransform>();
        if (canvasRectTransform == null)
        {
            Debug.LogError("Text object is not on a Canvas or Canvas is missing RectTransform!");
            enabled = false;
            return;
        }

        // --- Initial Position Calculation ---
        // Text's pivot is at its bottom edge, anchored to the canvas bottom.
        // We want the *top* of the text to start 'startOffsetFromScreenBottom' units below the screen bottom.
        // Screen bottom is at y=0 (relative to canvas bottom anchor).
        // So, top of text should be at: 0 - startOffsetFromScreenBottom.
        // Since textRectTransform.anchoredPosition.y is the bottom edge of the text,
        // initialTextYPosition = (top_of_text_target_pos) - textRectTransform.rect.height
        // initialTextYPosition = (0 - startOffsetFromScreenBottom) - textRectTransform.rect.height;
        // This can be simplified: Place the *bottom edge* of the text so its *top edge* is off-screen.
        // If startOffsetFromScreenBottom = 0, top of text is at screen bottom.
        // Bottom edge of text = screen_bottom - text_height + start_offset (downwards).
        // Since positive Y is up:
        initialTextYPosition = -textRectTransform.rect.height - startOffsetFromScreenBottom;
        textRectTransform.anchoredPosition = new Vector2(textRectTransform.anchoredPosition.x, initialTextYPosition);

        // --- Target Position Calculation ---
        // Scroll is finished when the *bottom edge* of the text is above the *top edge* of the canvas + padding.
        // Top edge of canvas (relative to bottom anchor) is at canvasRectTransform.rect.height.
        targetYPosition = canvasRectTransform.rect.height + paddingAboveScreen;

        Debug.Log($"Canvas Height: {canvasRectTransform.rect.height}, Text Height: {textRectTransform.rect.height}");
        Debug.Log($"Initial Text Y (bottom edge): {initialTextYPosition}, Target Text Y (bottom edge): {targetYPosition}");

        // Ensure text RectTransform height is sufficient for its content.
        // Consider using a ContentSizeFitter on the TextMeshPro object (Vertical Fit: Preferred Size)
        // if the text content is dynamic or you don't want to manually set the height.
        // If using ContentSizeFitter, you might need to get textRectTransform.rect.height *after* a frame or two,
        // or force a canvas rebuild. For simplicity here, we assume height is correctly set.
    }

    void Update()
    {
        if (!isScrolling || skipInitiated) return;

        // Move text upwards
        // Note: anchoredPosition moves the pivot. Since pivot Y is 0 (bottom), this moves the bottom edge up.
        textRectTransform.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);

        // Check if scrolling is finished
        // (i.e., bottom of text has passed above the target Y position)
        if (textRectTransform.anchoredPosition.y >= targetYPosition)
        {
            isScrolling = false;
            // Snap to final position to avoid overshooting due to Time.deltaTime variance
            textRectTransform.anchoredPosition = new Vector2(textRectTransform.anchoredPosition.x, targetYPosition);
            StartCoroutine(LoadNextSceneAfterDelay());
        }

        // Allow skipping
        if (Input.anyKeyDown) // Any key, or mouse button
        {
            HandleSkip();
        }
    }

    void HandleSkip()
    {
        if (skipInitiated) return; // Already skipping

        skipInitiated = true;
        Debug.Log(isScrolling ? "Skipping scroll..." : "Skipping delay...");
        StopAllCoroutines(); // Stop any potential coroutines (like LoadNextSceneAfterDelay)
        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator LoadNextSceneAfterDelay()
    {
        if (skipInitiated) yield break; // If skip was pressed during the frame scroll finished

        yield return new WaitForSeconds(delayAfterScrollFinishes);

        if (!skipInitiated) // Double check skip wasn't pressed during delay
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}