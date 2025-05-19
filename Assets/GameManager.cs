using UnityEngine;
using UnityEngine.SceneManagement; // For loading scenes
using UnityEngine.UI; // If you want to display the count on screen

public class GameManager : MonoBehaviour
{
    // --- Singleton Pattern (Optional but common for GameManagers) ---
    public static GameManager Instance { get; private set; }

    // --- Mushroom Collection ---
    public int mushroomsToCollect = 10; // Target number of mushrooms
    private int currentMushroomsCollected = 0;

    // --- UI (Optional) ---
    [Header("UI Elements")]
    public Text mushroomCountText; // Assign a UI Text element in the Inspector

    // --- Level Transition ---
    [Header("Scene Management")]
    public string nextSceneName; // Name of the scene to load after collecting all mushrooms

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Uncomment if your GameManager needs to persist across scenes (e.g., for multi-level games where mushroom count carries over)
            // For a single level leading to a next scene, DontDestroyOnLoad might not be needed for this specific logic.
        }
        else
        {
            Destroy(gameObject); // Ensures only one instance exists
            return;
        }

        // Initialize
        currentMushroomsCollected = 0;
        UpdateMushroomUI(); // Update UI at the start
    }

    // Public method to be called by Mushroom.cs when a mushroom is collected
    public void MushroomCollected()
    {
        currentMushroomsCollected++;
        Debug.Log("Mushrooms Collected: " + currentMushroomsCollected + "/" + mushroomsToCollect);

        UpdateMushroomUI(); // Update the UI display

        // Check if all mushrooms are collected
        if (currentMushroomsCollected >= mushroomsToCollect)
        {
            Debug.Log("All mushrooms collected! Proceeding to next level.");
            LoadNextLevel();
        }
    }

    void UpdateMushroomUI()
    {
        if (mushroomCountText != null)
        {
            mushroomCountText.text = "Mushrooms: " + currentMushroomsCollected + " / " + mushroomsToCollect;
        }
    }

    void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log("Loading next scene: " + nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("Next scene name not set in GameManager!");
            // Optionally, load a "You Win!" scene or similar default.
            // SceneManager.LoadScene("WinScreen");
        }
    }

    // --- Optional: Reset for when the player dies and restarts the level ---
    // This might be called by PlayerHealth or the DeathScreenController
    public void ResetMushroomCount()
    {
        currentMushroomsCollected = 0;
        UpdateMushroomUI();
        Debug.Log("Mushroom count reset.");
    }
}