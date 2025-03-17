using UnityEngine;

public class ParallaxCamera : MonoBehaviour
{
    private float _oldCameraX;

    void Start()
    {
        _oldCameraX = transform.position.x;
    }

    void Update()
    {
        float deltaX = transform.position.x - _oldCameraX;
        if (Mathf.Abs(deltaX) > 0.0001f)
        {
            // Move all parallax layers.
            ParallaxLayer.MoveAllLayers(deltaX);
        }
        _oldCameraX = transform.position.x;
    }
}
