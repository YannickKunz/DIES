using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Tooltip("Reference to the player's transform.")]
    public Transform player;

    [Tooltip("How fast the background moves relative to the player (1 = same speed).")]
    public float speed = 1f;

    // We'll store the player's previous position so we know how far they've moved.
    private Vector3 lastPlayerPos;

    void Start()
    {
        // Record the player's position at the start.
        lastPlayerPos = player.position;
    }

    void Update()
    {
        // Calculate how much the player moved since the last frame.
        float deltaX = player.position.x - lastPlayerPos.x;

        // Move the background horizontally by deltaX * speed.
        // (If speed = 1, it moves exactly at the player's speed.)
        transform.position += new Vector3(deltaX * speed, 0f, 0f);

        // Update the last known player position.
        lastPlayerPos = player.position;
    }
}
