using UnityEngine;
using UnityEngine.SceneManagement; // Essential for loading scenes!

public class LevelExit : MonoBehaviour
{
    [Tooltip("The exact name of the scene file to load when triggered.")]
    public string nextSceneName; // You will set this in the Inspector!

    private bool isLoadingNextLevel = false; // Prevents trying to load multiple times

    // This function is called by Unity WHEN another Collider2D enters this trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if we're already loading and if the object entering is the Player
        if (!isLoadingNextLevel && other.CompareTag("Player")) // Assumes your player GameObject has the "Player" tag
        {
            Debug.Log("Player entered the Level Exit trigger for scene: " + nextSceneName);

            // Check if a scene name has actually been provided
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogError("LevelExit script on " + gameObject.name + " is missing the 'Next Scene Name'!");
                return; // Stop execution if no scene name is set
            }

            // Set the flag to prevent multiple loads
            isLoadingNextLevel = true;

            // --- Load the next scene ---
            LoadNextLevel();
        }
    }

    private void LoadNextLevel()
    {
        // Optional: Add a fade-out effect here before loading

        Debug.Log("Loading scene: " + nextSceneName);
        SceneManager.LoadScene(nextSceneName);
    }

    // Optional: Draw a visual representation of the trigger in the Scene view
    void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f); // Semi-transparent green
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
    }
}