using UnityEngine;

public class RotatingPlatform : MonoBehaviour
{
    public Vector3 rotationAxis = Vector3.up; // Axis of rotation (default is Y-axis)
    public float rotationSpeed = 50f; // Speed of rotation in degrees per second
    public float startRotation = 0f; // Initial rotation offset in degrees

    void Start()
    {
        // Apply the initial rotation offset
        transform.Rotate(rotationAxis * startRotation);
    }

    void Update()
    {
        // Rotate the platform around the specified axis
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}