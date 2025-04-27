using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathScreenManager : MonoBehaviour
{
    // Name of your main game scene file to restart
    public string gameSceneName = "Black Forest 1"; // <<< CHANGE THIS to your game scene file name!

    // Name of your start menu scene file
    public string mainMenuSceneName = "StartMenuScene"; // <<< CHANGE THIS if your start menu scene has a different name!

    // --- We need a way to know WHICH level to restart if you have multiple levels ---
    // Simple solution for now: Assume the 'gameSceneName' is the one to restart.
    // More advanced: Use a static variable or a GameManager to store the last played level name.
    // Let's stick to the simple solution first.

    public void RestartLevel()
    {
        Debug.Log("Restart button clicked. Loading scene: " + gameSceneName);
        // TODO: If you have multiple levels, load the *correct* last played level.
        SceneManager.LoadScene(gameSceneName);
    }

    public void GoToMainMenu()
    {
        Debug.Log("Main Menu button clicked. Loading scene: " + mainMenuSceneName);
        SceneManager.LoadScene(mainMenuSceneName);
    }

     // Optional: Add a quit button functionality from the death screen
    public void QuitGame()
    {
        Debug.Log("Quit Game button clicked.");
        Application.Quit();

        // If running in the Unity Editor, stop playing
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}