using UnityEngine;
using System.Collections.Generic;

public class FlashlightDamager : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damagePerTick = 1;
    public float damageTickRate = 0.5f;

    private List<GhostController> ghostsInLight = new List<GhostController>();
    private Dictionary<GhostController, float> ghostDamageTimers = new Dictionary<GhostController, float>();

    public void ProcessDamageTick()
    {
        // Iterate backwards for safe removal
        for (int i = ghostsInLight.Count - 1; i >= 0; i--)
        {
            // Boundary check
            if (i >= ghostsInLight.Count) continue; // Should not happen but safety first

            GhostController ghost = ghostsInLight[i];

            // 1. Handle externally destroyed / null references
            if (ghost == null) // Use Unity null check
            {
                ghostsInLight.RemoveAt(i);
                // Key assumed removed elsewhere or will be cleaned later if dictionary becomes inconsistent
                continue;
            }

            // 2. Check if Key Exists (state sanity check)
            if (!ghostDamageTimers.ContainsKey(ghost))
            {
                Debug.LogWarning($"ProcessDamageTick: Ghost '{ghost.name}' key missing! Removing from list.", ghost);
                ghostsInLight.RemoveAt(i);
                continue;
            }

            // --- 3. Process Damage Timer ---
            ghostDamageTimers[ghost] -= Time.deltaTime;

            if (ghostDamageTimers[ghost] <= 0f)
            {
                // Store reference BEFORE potential destruction
                GhostController ghostReferenceForCleanup = ghost;

                // --- Apply Damage ---
                ghost.TakeDamage(damagePerTick); // This might set isDying = true and call Destroy()

                // --- Check if Ghost is NOW dying/destroyed ---
                // Check the flag first, then Unity null check as backup
                if (ghost.isDying || ghost == null)
                {
                    // Ghost was destroyed by THIS damage tick.
                    // Remove from list AND dictionary HERE.
                    // OnTriggerExit should NOT have removed it because isDying was true.
                    Debug.Log($"ProcessDamageTick: Ghost '{ghostReferenceForCleanup?.name ?? "Unknown"}' destroyed by this tick. Cleaning up.");
                    if (ghostDamageTimers.ContainsKey(ghostReferenceForCleanup))
                    {
                        ghostDamageTimers.Remove(ghostReferenceForCleanup);
                    }
                    else
                    {
                        Debug.LogWarning($"ProcessDamageTick: Tried to remove key for dying ghost '{ghostReferenceForCleanup?.name ?? "Unknown"}' but key was already gone?");
                    }
                    ghostsInLight.RemoveAt(i); // Remove from list
                    continue; // Move to next iteration
                }
                else
                {
                    // Ghost survived this tick, reset timer.
                    // Key should still exist because OnTriggerExit didn't run or didn't remove it.
                    if (ghostDamageTimers.ContainsKey(ghost)) // Double check key
                    {
                        ghostDamageTimers[ghost] = damageTickRate;
                    }
                    else
                    {
                        // If key is gone even though ghost isn't dying, something is wrong. Clean up.
                        Debug.LogError($"ProcessDamageTick: Ghost '{ghost.name}' survived AND not dying, but key vanished! Removing from list.", ghost);
                        ghostsInLight.RemoveAt(i);
                        continue;
                    }
                }
            } // End if timer <= 0
        } // End of for loop
    } // End of ProcessDamageTick


    void OnTriggerEnter2D(Collider2D other)
    {
        GhostController ghost = other.GetComponent<GhostController>();
        // Only add if it's a valid ghost, not already tracked, and not already dying
        if (ghost != null && !ghost.isDying && !ghostsInLight.Contains(ghost))
        {
            ghostsInLight.Add(ghost);
            if (!ghostDamageTimers.ContainsKey(ghost))
            {
                ghostDamageTimers.Add(ghost, damageTickRate);
                Debug.Log($"Ghost '{ghost.name}' entered light. Added timer.");
            }
            else { /* Handle potential inconsistency */ }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        GhostController ghost = other.GetComponent<GhostController>();
        if (ghost != null)
        {
            // --- MODIFIED: Only remove if the ghost is NOT currently dying ---
            // This prevents this from interfering with ProcessDamageTick's cleanup
            if (!ghost.isDying)
            {
                Debug.Log($"Ghost '{ghost.name}' exited light NORMALLY. Removing timer.");
                RemoveGhostFromTracking(ghost); // Use helper
            }
            else
            {
                Debug.Log($"Ghost '{ghost.name}' exited light WHILE DYING (Destroy called). Ignoring exit cleanup.");
            }
        }
    }

    // Helper function remains useful for normal exits
    void RemoveGhostFromTracking(GhostController ghost)
    {
        if (ghost == null) return;
        ghostsInLight.Remove(ghost);
        if (ghostDamageTimers.ContainsKey(ghost)) // Check key before removing
        {
            ghostDamageTimers.Remove(ghost);
        }
    }
}