using UnityEngine;

public class ButtonTrigger : MonoBehaviour
{
    public GameObject[] platformsToToggle; // Assign platforms here in inspector

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SetPlatformsVisible(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SetPlatformsVisible(false);
        }
    }

    void SetPlatformsVisible(bool visible)
    {
        foreach (GameObject platform in platformsToToggle)
        {
            SpriteRenderer sr = platform.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = visible;
            }
        }
    }
}

