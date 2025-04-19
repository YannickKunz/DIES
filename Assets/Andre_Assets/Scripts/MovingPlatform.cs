using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public float speed = 2f; // Speed of the platform
    public float height = 2f; // Distance the platform moves up and down
    public float phaseOffset = 0f; // Phase offset to control movement direction

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position; // Store the initial position of the platform
    }

    void Update()
    {
        // Calculate the new position using a sine wave with a phase offset
        float newY = startPosition.y + Mathf.Sin(Time.time * speed + phaseOffset) * height;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }
}