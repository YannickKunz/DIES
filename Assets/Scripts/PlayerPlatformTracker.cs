using UnityEngine;

public class PlayerPlatformTracker : MonoBehaviour
{
    [Tooltip("Layer mask for what is considered a ground platform.")]
    [SerializeField] private LayerMask groundLayer;

    private GameObject _currentPlayerPlatform;
    private PlatformInfo _currentPlatformInfo;

    void Start()
    {
        if (groundLayer.value == 0)
        {
            Debug.LogWarning($"PlayerPlatformTracker on {gameObject.name}: Ground Layer is not set. Will attempt to use default 'Ground' layer.");
            groundLayer = LayerMask.GetMask("Ground");
            if (groundLayer.value == 0)
            {
                Debug.LogError($"PlayerPlatformTracker on {gameObject.name}: Default 'Ground' layer not found. Platform tracking will not work.");
            }
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // Check if the collision is with a ground object
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            if (collision.gameObject != _currentPlayerPlatform)
            {
                _currentPlayerPlatform = collision.gameObject;
                
                PlatformInfo newPlatformInfo = new PlatformInfo
                {
                    platform = _currentPlayerPlatform,
                    name = _currentPlayerPlatform.name,
                    tag = _currentPlayerPlatform.tag,
                    layer = _currentPlayerPlatform.layer,
                    localPlayerPos = _currentPlayerPlatform.transform.InverseTransformPoint(transform.position)
                };

                if (!ArePlatformInfosEqual(_currentPlatformInfo, newPlatformInfo))
                {
                    _currentPlatformInfo = newPlatformInfo;
                    WorldState.UpdateCurrentPlatform(_currentPlatformInfo);
                    // Debug.Log($"Player is on new platform: {newPlatformInfo.name}");
                }
            }
            // Optionally, update localPlayerPos even if it's the same platform
            else if (_currentPlayerPlatform != null)
            {
                 PlatformInfo updatedInfo = _currentPlatformInfo;
                 updatedInfo.localPlayerPos = _currentPlayerPlatform.transform.InverseTransformPoint(transform.position);
                 if (!ArePlatformInfosEqual(_currentPlatformInfo, updatedInfo))
                 {
                    _currentPlatformInfo = updatedInfo;
                    WorldState.UpdateCurrentPlatform(_currentPlatformInfo); // Update if local position changed significantly
                 }
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        // If the player leaves the platform they were on
        if (collision.gameObject == _currentPlayerPlatform)
        {
            _currentPlayerPlatform = null;
            _currentPlatformInfo = new PlatformInfo(); // Reset platform info
            WorldState.UpdateCurrentPlatform(_currentPlatformInfo); // Notify that player is not on a platform
            // Debug.Log("Player left platform: " + collision.gameObject.name);
        }
    }

    private bool ArePlatformInfosEqual(PlatformInfo a, PlatformInfo b)
    {
        return a.platform == b.platform &&
               a.name == b.name &&
               a.tag == b.tag &&
               a.layer == b.layer &&
               Vector3.SqrMagnitude(a.localPlayerPos - b.localPlayerPos) < 0.01f; // Tolerance for position change
    }
}