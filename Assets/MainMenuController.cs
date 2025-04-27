using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management

public class MainMenuController : MonoBehaviour
{
    // Name of your main game scene file
    public string gameSceneName = "YourGameSceneName"; // <<< CHANGE THIS to your actual game scene file name!

    public void StartGame()
    {
        Debug.Log("Start Game button clicked. Loading scene: " + gameSceneName);
        SceneManager.LoadScene(gameSceneName);
    }

    // Optional: Add a quit button functionality
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