using UnityEngine;

public class FanRotation : MonoBehaviour
{
    public float rotationSpeed = 200f; // Speed of rotation in degrees per second
    public Vector3 rotationAxis = Vector3.up; // Axis of rotation (default is Y-axis)

    void Update()
    {
        // Rotate the object around the specified axis
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime, Space.Self);
    }
}