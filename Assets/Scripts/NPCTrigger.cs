using UnityEngine;

[RequireComponent(typeof(Collider2D))]    // or Collider for 3D
public class NPCTrigger : MonoBehaviour
{
    [TextArea(3,10)]
    public string[] dialogueLines;       // set these in Inspector

    public float triggerRadius = 2f;
    private Transform player;
    private bool playerInRange = false;

    void Start()
    {
        // find your player by tag (make sure your player GameObject is tagged “Player”)
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        // simple distance check
        if (Vector3.Distance(transform.position, player.position) <= triggerRadius)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                // automatically start dialogue when entering range
                DialogueManager.Instance.StartDialogue(dialogueLines);
            }
        }
        else
        {
            playerInRange = false;
        }
    }

    // (Optional) to visualize radius in Editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
