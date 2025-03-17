using UnityEngine;

public class SectionEndTrigger : MonoBehaviour
{
    [Tooltip("Prefab for the next section.")]
    public GameObject nextSectionPrefab;

    [Tooltip("How wide is this section? Used to position the next one.")]
    public float sectionWidth = 86.5f;  // Actual ground length

    private bool spawned = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only spawn if the Player enters
        if (!spawned && other.CompareTag("Player"))
        {
            spawned = true; // Prevent multiple spawns

            // Use the parent's position (current section's position)
            Vector3 currentSectionPos = transform.parent.position;
            // Calculate the spawn position by moving right by sectionWidth.
            // Adjust this calculation if your prefab's pivot is not at the left edge.
            Vector3 spawnPos = new Vector3(currentSectionPos.x + sectionWidth, currentSectionPos.y, currentSectionPos.z);

            // Instantiate the new section at the calculated position
            Instantiate(nextSectionPrefab, spawnPos, Quaternion.identity);

            // Optionally, destroy this trigger so it won't fire again
            Destroy(gameObject);
        }
    }
}
