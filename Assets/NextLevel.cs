using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevel : MonoBehaviour
{
    // This function is called by Unity automatically when another
    // object with a Collider2D enters this object's trigger.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // We check if the object that entered has the "Player" tag.
        // This is important so that enemies or other objects don't trigger the level change.
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player has reached the exit. Loading next level...");

            // Get the index of the current scene in the build settings.
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

            // Calculate the index of the next scene.
            int nextSceneIndex = currentSceneIndex + 1;

            // Check if there *is* a next scene to load.
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                // Load the next scene.
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                // If there's no next scene, we're at the end.
                // You can load the main menu (scene 0) or a "You Win" screen.
                Debug.Log("You finished the last level! Returning to Main Menu.");
                SceneManager.LoadScene(0); // Loads the first scene in the build order.
            }
        }
    }
}