using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class ParallaxLayer : MonoBehaviour
{
    // You can adjust these defaults as needed.
    [Tooltip("Automatically set parallax factor if not manually overridden.")]
    public float parallaxFactor = -1f;  // Use -1 to indicate 'unset'

    private static List<ParallaxLayer> allLayers = new List<ParallaxLayer>();

    private void OnEnable()
    {
        // Register this layer in the static list.
        allLayers.Add(this);

        // Automatically assign the parallax factor based on tag if not set.
        if (parallaxFactor < 0)  // Not manually set.
        {
            if (CompareTag("Foreground"))
            {
                parallaxFactor = 0.2f;
            }
            else if (CompareTag("Midground"))
            {
                parallaxFactor = 0.5f;
            }
            else if (CompareTag("Background"))
            {
                parallaxFactor = 0.8f;
            }
            else
            {
                // Default fallback if no recognized tag is found.
                parallaxFactor = 0.2f;
            }
        }
    }

    private void OnDisable()
    {
        // Remove this layer when disabled or destroyed.
        allLayers.Remove(this);
    }

    // Static method to move all registered layers.
    public static void MoveAllLayers(float deltaX)
    {
        foreach (ParallaxLayer layer in allLayers)
        {
            layer.Move(deltaX);
        }
    }

    // The actual movement logic.
    private void Move(float deltaX)
    {
        Vector3 newPos = transform.localPosition;
        newPos.x -= deltaX * parallaxFactor;
        transform.localPosition = newPos;
    }
}
