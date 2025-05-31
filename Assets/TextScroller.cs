using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Make sure to include this for TextMeshPro
using System.Collections;

public class TextScroller : MonoBehaviour
{
    [Header("Text Setup")]
    public TextMeshProUGUI scrollingText; // Assign your TextMeshPro object
    public float scrollSpeed = 50f;       // Pixels per second

    [Header("Scene Transition")]
    public string nextSceneName = "GameplayScene"; // Name of the scene to load after scroll
    public float delayAfterScrollFinishes = 2.0f; // Delay before loading next scene

    [Header("Positioning & Control")]
    public float startYPositionOffset = 0f; // How far ABOVE the top of the screen the text should start.
                                            // If 0, text top starts at screen top.
                                            // If positive, text starts further up (off-screen).

    public float paddingBelowScreen = 100f; // Extra space to scroll before considering it "off-screen"

    private RectTransform textRectTransform;
    private RectTransform canvasRectTransform;
    private float initialTextYPosition;
    private float targetYPosition;
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
        if (textRectTransform.anchorMin.y != 1 || textRectTransform.anchorMax.y != 1 || textRectTransform.pivot.y != 1)
        {
            Debug.LogWarning("For best results, set Text's RectTransform Anchor Preset to Top-Stretch and Pivot Y to 1 (Top).");
        }

        canvasRectTransform = scrollingText.canvas.GetComponent<RectTransform>();

        // Set initial position: Top of text element starts 'startYPositionOffset' units ABOVE the top of the canvas.
        // Since Y is positive upwards from the anchor (which is at the top of the screen),
        // a positive startYPositionOffset pushes it further up.
        initialTextYPosition = startYPositionOffset;
        textRectTransform.anchoredPosition = new Vector2(textRectTransform.anchoredPosition.x, initialTextYPosition);

        // Calculate target Y position:
        // Text's top edge needs to be below the screen bottom by its own height + padding.
        // Screen bottom (relative to top anchor) is -canvasRectTransform.rect.height.
        // So, text top needs to reach -canvasRectTransform.rect.height - textRectTransform.rect.height - paddingBelowScreen
        // However, since we're moving it from its initial position,
        // it's simpler to think about total distance.
        // Or, where the top of the text should end up.
        targetYPosition = -(canvasRectTransform.rect.height + textRectTransform.rect.height + paddingBelowScreen);

        // More precise target Y if text starts at screen top (startYPositionOffset = 0):
        // Its top needs to move from 0 down to -canvasHeight - textHeight - padding.
        // If startYPositionOffset is positive, it starts higher, so its final target Y will be lower.
        // The current targetYPosition is okay for this "scrolls down" effect.
        // Let's adjust for starting position:
        // End condition: Top of text should be below the bottom of the screen.
        // Bottom of screen Y relative to top anchor is -canvasRectTransform.rect.height.
        // So, top of text should be < -canvasRectTransform.rect.height.
        // Add text height to ensure ALL text is off-screen.
        // Add padding for good measure.
        targetYPosition = -canvasRectTransform.rect.height - paddingBelowScreen; // Top of text passes bottom of screen
                                                                                 // This is when the text's TOP edge has gone below the screen.
                                                                                 // If you want when the BOTTOM edge of text leaves screen, it's simpler:
                                                                                 // top of text needs to reach -canvasHeight - textHeight

        // Let's use a simpler target: When the top of the text goes below the bottom of the screen + its own height
        targetYPosition = -(canvasRectTransform.rect.height + textRectTransform.rect.height + paddingBelowScreen);

        // If text starts AT THE TOP OF THE SCREEN (Pos Y = 0 in editor, startYPositionOffset=0 in script)
        // and its top anchor is at the top of the screen,
        // then targetYPosition should be when its TOP edge is at:
        // -(height of canvas) - (height of text rect) - padding
        // This ensures the entire text block scrolls off.
        float canvasHeight = canvasRectTransform.rect.height;
        float textHeight = textRectTransform.rect.height; // Make sure RectTransform height is set large enough for all text!

        // Initial position for the text's top edge (relative to top anchor of canvas)
        textRectTransform.anchoredPosition = new Vector2(textRectTransform.anchoredPosition.x, startYPositionOffset);

        // Target Y for the text's top edge to indicate it's fully off-screen at the bottom
        targetYPosition = -(canvasHeight + textHeight + paddingBelowScreen);

    }

    void Update()
    {
        if (!isScrolling || skipInitiated) return;

        // Move text downwards
        textRectTransform.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);

        // Check if scrolling is finished
        // (i.e., top of text has passed below the target Y position)
        if (textRectTransform.anchoredPosition.y <= targetYPosition)
        {
            isScrolling = false;
            // Snap to final position to avoid overshooting due to Time.deltaTime variance
            textRectTransform.anchoredPosition = new Vector2(textRectTransform.anchoredPosition.x, targetYPosition);
            StartCoroutine(LoadNextSceneAfterDelay());
        }

        // Allow skipping
        if (Input.anyKeyDown) // Any key, or mouse button
        {
            if (isScrolling) // If scrolling, skip scroll and delay
            {
                Debug.Log("Skipping scroll...");
                skipInitiated = true;
                StopAllCoroutines(); // Stop any potential coroutines
                SceneManager.LoadScene(nextSceneName);
            }
            else // If scroll finished and in delay period, skip delay
            {
                Debug.Log("Skipping delay...");
                skipInitiated = true;
                StopAllCoroutines();
                SceneManager.LoadScene(nextSceneName);
            }
        }
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