using System.Collections;
using UnityEngine;

public class PortalTeleporter : MonoBehaviour
{
    [Tooltip("Assign the destination portal (the other portal's transform).")]
    public Transform destinationPortal;

    [Tooltip("Cooldown time to prevent immediate re-teleportation (in seconds).")]
    public float teleportCooldown = 1f;

    private bool canTeleport = true;

    // This method is called when another collider enters the trigger attached to the portal.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the other object is tagged "Player" and the portal is ready to teleport.
        if (canTeleport && other.CompareTag("Player"))
        {
            // Teleport the player to the destination portal position.
            other.transform.position = destinationPortal.position;

            // Start a cooldown on this portal to prevent immediate re-activation.
            StartCoroutine(TeleportCooldown());

            // Also, disable teleporting on the destination temporarily so the player doesn't instantly teleport back.
            PortalTeleporter destTeleporter = destinationPortal.GetComponent<PortalTeleporter>();
            if (destTeleporter != null)
            {
                destTeleporter.StartCoroutine(destTeleporter.DisableTeleportTemporarily());
            }
        }
    }

    // Coroutine to disable teleport for a short duration on this portal.
    private IEnumerator TeleportCooldown()
    {
        canTeleport = false;
        yield return new WaitForSeconds(teleportCooldown);
        canTeleport = true;
    }

    // Coroutine to disable teleport on the destination portal temporarily.
    public IEnumerator DisableTeleportTemporarily()
    {
        canTeleport = false;
        yield return new WaitForSeconds(teleportCooldown);
        canTeleport = true;
    }
}
