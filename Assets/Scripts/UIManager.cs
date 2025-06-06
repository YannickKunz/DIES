using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Drag your 'ControlsHelpPanel' GameObject from the Hierarchy here in the Inspector.
    public GameObject controlsHelpPanel;

    private bool isPanelActive = false;

    void Start()
    {
        // Ensure the panel is hidden at the start of the game, just in case.
        if (controlsHelpPanel != null)
        {
            controlsHelpPanel.SetActive(false);
            isPanelActive = false;
        }
    }

    void Update()
    {
        // Listen for the 'H' key to be pressed down.
        if (Input.GetKeyDown(KeyCode.H))
        {
            ToggleHelpPanel();
        }

        // Bonus: Also allow 'Escape' to close the panel if it's open.
        if (isPanelActive && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleHelpPanel();
        }
    }

    // A public function to handle the logic.
    public void ToggleHelpPanel()
    {
        if (controlsHelpPanel == null)
        {
            Debug.LogError("Help Panel has not been assigned in the UIManager script!");
            return;
        }

        // Toggle the state.
        isPanelActive = !isPanelActive;
        controlsHelpPanel.SetActive(isPanelActive);

        // Pause or unpause the game when the panel is shown/hidden.
        if (isPanelActive)
        {
            // Pause the game.
            Time.timeScale = 0f;
        }
        else
        {
            // Resume the game.
            Time.timeScale = 1f;
        }
    }
}