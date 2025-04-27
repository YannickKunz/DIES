using UnityEngine;
using System.Collections.Generic; // Needed for List

public class FlashlightDamager : MonoBehaviour
{
    public int damagePerTick = 1;
    public float damageTickRate = 0.5f; // How often to apply damage (e.g., twice per second)

    // Keep track of ghosts currently in the light to manage damage ticks
    private List<GhostController> ghostsInLight = new List<GhostController>();
    private Dictionary<GhostController, float> ghostDamageTimers = new Dictionary<GhostController, float>();

    void Update()
    {
        // Create a copy of the list to iterate over, as ghosts might be destroyed
        List<GhostController> ghostsToRemove = new List<GhostController>();
        List<GhostController> currentGhosts = new List<GhostController>(ghostsInLight);

        foreach (GhostController ghost in currentGhosts)
        {
            if (ghost == null) // Check if ghost was destroyed
            {
                ghostsToRemove.Add(ghost); // Mark for removal from tracking later
                continue;
            }

            // Check if enough time has passed to damage this specific ghost again
            if (ghostDamageTimers.ContainsKey(ghost))
            {
                ghostDamageTimers[ghost] -= Time.deltaTime;
                if (ghostDamageTimers[ghost] <= 0f)
                {
                    ghost.TakeDamage(damagePerTick);
                    ghostDamageTimers[ghost] = damageTickRate; // Reset timer
                }
            }
        }

        // Clean up any ghosts that were destroyed while iterating
        foreach (var deadGhost in ghostsToRemove)
        {
           RemoveGhostFromTracking(deadGhost);
        }
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        GhostController ghost = other.GetComponent<GhostController>();
        if (ghost != null && !ghostsInLight.Contains(ghost))
        {
            // Add ghost to tracking list and initialize its damage timer
            ghostsInLight.Add(ghost);
            if (!ghostDamageTimers.ContainsKey(ghost)) // Should usually be true here
            {
                ghostDamageTimers.Add(ghost, damageTickRate); // Start timer for next damage
                 // Optional: Deal immediate damage on enter?
                 // ghost.TakeDamage(damagePerTick);
            }
             Debug.Log("Ghost entered light: " + other.name);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        GhostController ghost = other.GetComponent<GhostController>();
        if (ghost != null)
        {
            RemoveGhostFromTracking(ghost);
            Debug.Log("Ghost exited light: " + other.name);
        }
    }

    // Helper function to remove ghost from both list and dictionary
    void RemoveGhostFromTracking(GhostController ghost)
    {
         if (ghostsInLight.Contains(ghost))
         {
            ghostsInLight.Remove(ghost);
         }
         if (ghostDamageTimers.ContainsKey(ghost))
         {
            ghostDamageTimers.Remove(ghost);
         }
    }
}