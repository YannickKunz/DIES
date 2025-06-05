using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Needed for Coroutine support

public class LevelExit : MonoBehaviour
{
    [Tooltip("The exact name of the scene file to load when triggered.")]
    public string nextSceneName;

    [Tooltip("Audio source for door open sound")]
    public AudioSource doorOpenSound;

    [Tooltip("Prefab of the door draft effect (optional)")]
    public GameObject doorDraftPrefab;

    [Tooltip("Position to spawn the draft effect (optional)")]
    public Transform draftSpawnPoint;

    private bool isLoadingNextLevel = false;
    public bool canExit = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isLoadingNextLevel && other.CompareTag("Player") && canExit)
        {
            Debug.Log("Player entered the Level Exit trigger for scene: " + nextSceneName);

            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogError("LevelExit script on " + gameObject.name + " is missing the 'Next Scene Name'!");
                return;
            }

            isLoadingNextLevel = true;

            // Play sound
            if (doorOpenSound != null)
            {
                doorOpenSound.Play();
            }

            // Spawn draft visual effect
            if (doorDraftPrefab != null)
            {
                Instantiate(doorDraftPrefab, draftSpawnPoint != null ? draftSpawnPoint.position : transform.position, Quaternion.identity);
            }

            // Start delayed level loading
            StartCoroutine(LoadNextLevelWithDelay(1f)); // 1-second delay
        }
    }

    private IEnumerator LoadNextLevelWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("Loading scene: " + nextSceneName);
        SceneManager.LoadScene(nextSceneName);
    }

    void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
    }
}
